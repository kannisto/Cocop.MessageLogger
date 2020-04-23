//
// Please make sure to read and understand README.md and LICENSE.txt.
// 
// This file was prepared in the research project COCOP (Coordinating
// Optimisation of Complex Industrial Processes).
// https://cocop-spire.eu/
//
// Authors: Tapio Vaaranmaa and Petri Kannisto, Tampere University, Finland
// File created: 11/2019
// Last modified: 3/2020

using System;
using System.Collections.Generic;
using System.Threading;
using RmqCl = RabbitMQ.Client;

namespace CocopMessageLogger
{
    /// <summary>
    /// AMQP client.
    /// </summary>
    class AmqpClient : AmqpClientBase
    {
        private RmqCl.Events.EventingBasicConsumer m_messageConsumer = null;

        private readonly object m_amqpClientLock = new object();
        private readonly AutoResetEvent m_connectionWait = new AutoResetEvent(false);
        private Thread m_connectionHandler = null;

        // Using the SyncBoolean class for flags, because there is no need to manage
        // thread synchronisation together for the flags.
        // Where these flags are applied, it is acknowledged that some code can be
        // executed when it no longer should, such as soon after the dispose method
        // was already called. However, the design aims to be such that it does not matter.
        // The price of these "events that occur too late" is considered lower
        // than the price of causing delays due to long code segments
        // inside lock blocks. In particular, it is considered expensive
        // to block a thread calling dispose.

        // Whether the client should quit completely
        private readonly SyncBoolean m_disposed = new SyncBoolean(false);
        
        // This indicates that the connection restore function must be started.
        // Once started, this flag must be set to false to enable detection
        // if the connection has been lost again right after connecting.
        private readonly SyncBoolean m_triggerConnectionRestore = new SyncBoolean(false);

        // Whether the user has explicitly asked for termination. This affects if
        // notifications are sent using the callbacks.
        private readonly SyncBoolean m_userHasRequestedTerminate = new SyncBoolean(false);


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connCb">Callback for connection-related events excluding connection failure.</param>
        /// <param name="msgCb">Callback for the reception of messages.</param> 
        public AmqpClient(ConnectionEventCallback connCb, MessageReceivedCallback msgCb)
            : base(connCb, msgCb)
        {
            m_connectionHandler = new Thread(ConnectionHandlerProgram);
            m_connectionHandler.Start();

        } // AmqpClient


        #region ConnectionHandlerProgram

        private Exception MakeNewConnection(ref RmqCl.IConnection connection, ref RmqCl.IModel channel, bool reconnecting)
        {
            // This method attempts to make a new connection. Returns true if success, otherwise false.

            var connRequest = ConnectionRequestObj;

            var factory = new RmqCl.ConnectionFactory()
            {
                HostName = connRequest.Host,
                UserName = connRequest.Username,
                Password = connRequest.Password
            };

            // Secure connection?
            if (connRequest.Secure)
            {
                factory.Ssl.Enabled = true;
                factory.Ssl.ServerName = connRequest.Host;
                factory.Ssl.Version = System.Security.Authentication.SslProtocols.Tls12;
            }

            try
            {
                // Create connection and channel
                connection = factory.CreateConnection();
                channel = connection.CreateModel();
                
                // Declare the exchange
                channel.ExchangeDeclare(exchange: connRequest.Exchange, type: "topic", autoDelete: false, durable: true, arguments: null);

                // Create a queue to receive messages
                var queueName = channel.QueueDeclare(queue: "", // Use a generated queue name
                                                     durable: false, // The queue does not survive a broker restart
                                                     exclusive: true, // The queue is only for this application, and it will be deleted on app exit
                                                     autoDelete: true, // The queue is deleted when no one is bound to it
                                                     arguments: null
                                                    ).QueueName;

                // Bind the queue to the topic pattern
                channel.QueueBind(queue: queueName, exchange: connRequest.Exchange, routingKey: connRequest.TopicPattern, arguments: null);

                // Set up a consumer for messages
                m_messageConsumer = new RmqCl.Events.EventingBasicConsumer(channel);
                m_messageConsumer.Received += MessageReceived;
                channel.BasicConsume(queue: queueName, noAck: true, consumerTag: "", noLocal: false, exclusive: false, arguments: new Dictionary<string, object> { }, consumer: m_messageConsumer);

                // Sign up for the shutdown event
                channel.ModelShutdown += ModelShutdown; // This event will fire if the connection is lost

                return null;
            }
            catch (Exception e)
            {
                // Clean the connection
                DestroyConnection(ref connection, ref channel);

                return e;
            }

        } // MakeNewConnection
        
        private void DestroyConnection(ref RmqCl.IConnection connection, ref RmqCl.IModel channel)
        {
            // This method disposes the AMQP-connection-related object

            try
            {
                if (channel != null)
                {
                    if (m_messageConsumer != null)
                    {
                        m_messageConsumer.Received -= MessageReceived;
                    }
                    
                    try
                    {
                        channel.ModelShutdown -= ModelShutdown;
                        channel.Close();
                    }
                    catch { }
                    channel.Dispose();
                    channel = null;
                }

                if (connection != null)
                {
                    try
                    {
                        connection.Close();
                    }
                    catch { }
                    connection.Dispose();
                    connection = null;
                }
            }
            catch {}

        } // DestroyConnection
        
        private bool AttemptConnectionRestore(ref RmqCl.IConnection connection, ref RmqCl.IModel channel)
        {
            // Clean up previous connection
            DestroyConnection(ref connection, ref channel);

            SendConnectionEvent(true, ConnectionEventType.RestoringConnection);

            // Attempting to connect
            Exception exception = MakeNewConnection(ref connection, ref channel, true);
            
            if (exception == null)
            {
                SendConnectionEvent(true, ConnectionEventType.ConnectionRestored);
                return true;
            }
            else
            {
                SendConnectionEvent(true, ConnectionEventType.ConnectionRestoreFailed, exception);
                return false;
            }

        } // RunConnectionRestoreSequence

        private void TerminateImpl(ref RmqCl.IConnection connection, ref RmqCl.IModel channel)
        {
            // This method performs the termination sequence. Using callbacks
            // to notify about events only if termination has been explicitly asked by the user.

            if (m_userHasRequestedTerminate.Value)
            {
                // Because termination has been explicitly requested, report this regardless if already disposed
                SendConnectionEvent(true, ConnectionEventType.TerminatingConnectionByUser);
            }

            DestroyConnection(ref connection, ref channel);

            if (m_userHasRequestedTerminate.Value)
            {
                // Because termination has been explicitly requested, report this regardless if already disposed
                SendConnectionEvent(false, ConnectionEventType.ConnectionTerminatedByUser);
            }
        }

        private void ConnectionHandlerProgram()
        {
            // This method is run in the connection handler thread.
            // Multiple variables are declared here, because this limits their access for this thread only.
            
            RmqCl.IConnection connection = null;
            RmqCl.IModel channel = null;

            // This indicates if the connection is being restored
            bool connectionRestoreSequenceOn = false;

            // Run the thread until "break"
            while (true)
            {
                // Wait for something to happen.
                // This wait has a maximum length, as this same wait call occurs between reconnect attempts.
                m_connectionWait.WaitOne(TimeSpan.FromSeconds(RetryIntervalSeconds));
                
                // Choosing what to do based on the flags
                if (m_disposed.Value || m_userHasRequestedTerminate.Value)
                {
                    // Quitting
                    break;
                }
                else if (m_triggerConnectionRestore.Value || connectionRestoreSequenceOn)
                {
                    // Connection lost! Attempting to restore.

                    // Reset the triggering flag
                    m_triggerConnectionRestore.Value = false;

                    // Keep on attempting restore until successful
                    connectionRestoreSequenceOn = !AttemptConnectionRestore(ref connection, ref channel);
                }
                else if (ConnectionRequestObj != null && connection == null)
                {
                    // Attempting to connect
                    
                    SendConnectionEvent(true, ConnectionEventType.Connecting);
                    var exception = MakeNewConnection(ref connection, ref channel, false);

                    if (exception == null)
                    {
                        SendConnectionEvent(true, ConnectionEventType.Connected);
                    }
                    else
                    {
                        SendConnectionEvent(false, ConnectionEventType.ConnectingFailed, exception);
                    }
                }
                // Otherwise, nothing to do
            }

            // Quitting the thread
            TerminateImpl(ref connection, ref channel);
            m_connectionWait.Dispose();

        } // ConnectionHandlerProgram
        
        #endregion ConnectionHandlerProgram


        #region Event handlers

        private void ModelShutdown(object sender, RmqCl.ShutdownEventArgs args)
        {
            if (m_disposed.Value)
            {
                // No need to react because dispose has already been called
                return;
            }
            
            SendConnectionEvent(true, ConnectionEventType.ConnectionLost);

            // These will trigger connection restore
            m_triggerConnectionRestore.Value = true;
            TriggerWorkerThread();

        } // ModelShutdown
        
        private void MessageReceived(object sender, RmqCl.Events.BasicDeliverEventArgs eventArgs)
        {
            // This event handler is run when a message arrives
            
            try
            {
                // No callbacks when already disposed.
                // If the dispose call occurs right after checking, the callback can come
                // after dispose, but this is considered unlikely and unimportant.
                if (!m_disposed.Value)
                {
                    b_messageReceivedCallback(ConnectionRequestObj.Host, ConnectionRequestObj.Exchange,
                        eventArgs.RoutingKey, eventArgs.Body);
                }
            }
            catch {}

        } // MessageReceived

        #endregion Event handlers


        #region Other members
        
        protected override void ConnectImpl()
        {
            if (m_disposed.Value) throw new ObjectDisposedException("AmqpClient");
            
            // Triggering the thread to connect
            TriggerWorkerThread();

        } // Connect

        public override void Terminate()
        {
            if (m_disposed.Value) throw new ObjectDisposedException("AmqpClient");
            
            // This indicates that the user has explicitly asked for termination
            m_userHasRequestedTerminate.Value = true;

            // Triggering the thread to terminate
            TriggerWorkerThread();
        } // Terminate

        protected override void DisposeImpl()
        {
            try
            {
                // Triggering the worker thread to quit
                m_disposed.Value = true;
                TriggerWorkerThread();
            }
            catch { }
        } // DisposeImpl
        
        private void TriggerWorkerThread()
        {
            // Implemented this function to enable code reuse
            try
            {
                m_connectionWait.Set();
            }
            catch (ObjectDisposedException) // In case already disposed
            { }
        }
        
        #endregion Other members


        #region Nested types

        /// <summary>
        /// A thread-safe boolean wrapper.
        /// </summary>
        private class SyncBoolean
        {
            private readonly object m_lockObject = new object();
            private bool m_value;

            public SyncBoolean(bool initialValue)
            {
                m_value = initialValue;
            }

            public bool Value
            {
                // Use locks to flush any caches
                get
                {
                    lock (m_lockObject)
                    { return m_value; }
                }
                set
                {
                    lock (m_lockObject)
                    { m_value = value; }
                }
            }
        }

        #endregion Nested types

    } // AmqpClient
} // CocopMessageMonitorLite
