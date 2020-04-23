//
// Please make sure to read and understand README.md and LICENSE.txt.
// 
// This file was prepared in the research project COCOP (Coordinating
// Optimisation of Complex Industrial Processes).
// https://cocop-spire.eu/
//
// Authors: Petri Kannisto and Tapio Vaaranmaa, Tampere University, Finland
// File created: 10/2019
// Last modified: 3/2020

using System;
using SysColl = System.Collections.Generic;

namespace CocopMessageLogger
{
    /// <summary>
    /// Manages the high-level application logic.
    /// </summary>
    class AppLogic : IDisposable
    {
        private readonly Logger m_logger;
        private readonly ConnectionEventCallback m_connectionUiCallback;
        private readonly Action m_messageReceivedUiCallback;

        // The folder where application executable is located
        private readonly string m_exeFolder;

        // Use this for the thread sync of the AMQP client
        private readonly object m_amqpClientLock = new object();

        // Use this for thread sync (except for AMQP client)
        private readonly object m_generalLock = new object();
        
        private AmqpClientBase m_amqpClient = null;
        private bool m_disposed = false;


        #region Constructors and disposal

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connCb">Callback for connection-related events. When called, this callback must not leak any exceptions.</param>
        /// <param name="msgCb">Callback for the reception of a message. When called, this callback must not leak any exceptions.</param>
        public AppLogic(ConnectionEventCallback connCb, Action msgCb)
        {
            m_exeFolder = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            
            m_logger = new Logger(m_exeFolder);

            m_connectionUiCallback = connCb;
            m_messageReceivedUiCallback = msgCb;

        }
        
        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            // Disposing objects

            lock (m_generalLock)
            {
                m_disposed = true;
            }

            ClearAmqpClient();
        }

        #endregion Constructors and disposal


        #region Other public members

        /// <summary>
        /// The path of the logs folder.
        /// </summary>
        public string LogsFolder
        {
            get
            {
                return m_logger.LogsRootFolder;
            }
        }

        /// <summary>
        /// Adds an unexpected error to the log.
        /// </summary>
        /// <param name="e">Related exception.</param>
        public void AddUnexpectedErrorToLog(Exception e)
        {
            m_logger.AddUnexpectedError(e);
        }
        
        /// <summary>
        /// Opens a connection.
        /// </summary>
        /// <param name="connRequest">Connection request.</param>
        /// <exception cref="InvalidOperationException">Thrown if object state is invalid.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed.</exception>
        public void Connect(ConnectionRequest connRequest)
        {
            ExpectNotDisposed(); // throws ObjectDisposedException

            // Locking a relatively large amount of code. However, none of the operations
            // is expected to last long, and a race condition must not occur.
            lock (m_amqpClientLock)
            {
                if (m_amqpClient != null)
                {
                    throw new InvalidOperationException("Connection already open");
                }
                
                // Creating the client object. Using a lock to synchronise the variable between threads.
                m_amqpClient = CreateAmqpClient(connRequest);

                // Connecting. This is not within a lock, as the connect operation
                // supposedly can take relatively long or at least cause a deadlock in this class.
                m_amqpClient.Connect(connRequest);
            }
        }

        private AmqpClientBase CreateAmqpClient(ConnectionRequest request)
        {
        #if (FAKEAMQP)
            // Using the fake AMQP client to enable UI testing
            return new FakeAmqpClient(
              MyConnectionEventCallback,
              MyMessageReceivedCallback
            );
        #else
            return new AmqpClient(
              MyConnectionEventCallback,
              MyMessageReceivedCallback
              );
        #endif
        }

        /// <summary>
        /// Closes the active connection if any.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the object has been disposed.</exception>
        public void TerminateConnection()
        {
            ExpectNotDisposed(); // throws ObjectDisposedException

            // Locking a relatively large amount of code (the Terminate method). However, none of the operations
            // is expected to last long, and a race condition must not occur.
            lock (m_amqpClientLock)
            {
                if (m_amqpClient != null)
                {
                    // Terminate the client and clear it
                    m_amqpClient.Terminate();
                }
            }

            ClearAmqpClient();
        }
        
        /// <summary>
        /// Returns information about the currently stored metadata.
        /// </summary>
        /// <param name="query">Query for the metadata items to return.</param>
        /// <returns>Information about metadata.</returns>
        public MetadataState GetMetadataState(MetadataQuery query)
        {
            const int MaxItemsToReturn = 200;

            using (var metadataMgr = new MetadataManager(m_exeFolder))
            {
                var items = metadataMgr.Get(query, MaxItemsToReturn);

                return new MetadataState()
                {
                    Items = items,
                    Topics = metadataMgr.GetTopics(host: query.Host, exc: query.Exchange),
                    TotalItemCount = metadataMgr.GetTotalCount(host: query.Host, exc: query.Exchange),
                    IsLimitApplied = items.Count >= MaxItemsToReturn // Max item count reached?
                };
            }
        }

        #endregion Other public members


        #region Callback methods

        private void MyConnectionEventCallback(ConnectionEvent ev)
        {
            try
            {
                if (!ev.IsConnectionMaintained)
                {
                    // Connection no longer maintained -> clearing the client
                    ClearAmqpClient();
                }

                // Notifying the UI
                m_connectionUiCallback(ev); // Not supposed to leak any exceptions
                
                // If error, adding it to the log
                if (ev.IsError)
                {
                    m_logger.AddConnectionError(ev.ErrorReason, ev.RelatedException);
                }

                // Adding the connection event to log
                m_logger.AddConnectionEvent(ev);
            }
            catch (Exception e) // Using catch, although no exceptions are expected
            {
                m_logger.AddUnexpectedError(e);
            }
        }
        
        private void MyMessageReceivedCallback(string host, string exc, string topic, byte[] msg)
        {
            try
            {
                // Adding the message to log. This will also store message metadata.
                m_logger.AddReceivedMessage(host: host, exc: exc, topic: topic, msg: msg);

                // Notifying the UI
                m_messageReceivedUiCallback();
            }
            catch (Exception e) // Using catch, although no exceptions are expected
            {
                m_logger.AddUnexpectedError(e);
            }
        }

        #endregion Callback methods


        #region Private methods
        
        private void ClearAmqpClient()
        {
            // Have a lock to prevent a race condition
            lock (m_amqpClientLock)
            {
                if (m_amqpClient != null)
                {
                    try
                    {
                        m_amqpClient.Dispose();
                        m_amqpClient = null;
                    }
                    catch { }
                }
            }
        }

        private void ExpectNotDisposed()
        {
            lock (m_generalLock)
            {
                if (m_disposed)
                {
                    throw new ObjectDisposedException("AppLogic");
                }
            }
        }

        #endregion Private methods


        #region Nested types

        /// <summary>
        /// Stores metadata-related information in one object.
        /// </summary>
        public class MetadataState
        {
            public MetadataState()
            {
                // Empty ctor body
            }

            /// <summary>
            /// Items that match given query.
            /// </summary>
            public SysColl.List<Metadata> Items
            {
                get; set;
            }

            /// <summary>
            /// All currently known topics.
            /// </summary>
            public SysColl.List<string> Topics
            {
                get; set;
            }

            /// <summary>
            /// The total count of all stored metadata items.
            /// </summary>
            public int TotalItemCount
            {
                get; set;
            }

            /// <summary>
            /// Whether a limit is being applied to messages, as they would otherwise be too many.
            /// </summary>
            public bool IsLimitApplied
            {
                get; set;
            }
        }

        #endregion Nested types
    }
}
