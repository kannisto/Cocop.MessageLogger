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

namespace CocopMessageLogger
{
    /// <summary>
    /// The abstract base class of an Amqp client to force implement a constructor that set the callback functions declared below.
    /// </summary>
    abstract class AmqpClientBase : IDisposable
    {
        // In protected variables, using "b_" in the name to indicate that these members
        // are a part of the base class.

        // The retry interval between reconnect attempts if connecting has failed
        protected const int RetryIntervalSeconds = 15;

        // This contains the most recent connection parameters.
        // Never cleared in case the request is still needed if a message occurs just after
        // a termination request.
        private ConnectionRequest m_connectRequest = null;

        // Using "b_" in the name to indicate that these members are a part of
        // the base class.
        private readonly ConnectionEventCallback b_connectionEventCallback;
        protected readonly MessageReceivedCallback b_messageReceivedCallback;

        // Use for thread sync
        private readonly object m_lockObject = new object();

        private bool m_disposed = false;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connCb">Callback for connection-related events excluding errors.</param>
        /// <param name="msgCb">Callback for the reception of messages.</param>    
        protected AmqpClientBase(ConnectionEventCallback connCb, MessageReceivedCallback msgCb)
        {
            b_connectionEventCallback = connCb;
            b_messageReceivedCallback = msgCb;

        } // AmqpClientBase

        /// <summary>
        /// Connection request object.
        /// </summary>
        protected ConnectionRequest ConnectionRequestObj
        {
            get
            {
                lock (m_lockObject) // Thread sync just in case. The request itself cannot be modified.
                {
                    return m_connectRequest;
                }
            }
        }

        /// <summary>
        /// Opens a connection.
        /// </summary>
        /// <param name="request">Connection details.</param>
        /// <returns>True if success, otherwise false.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if already disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if already connected.</exception>
        public void Connect(ConnectionRequest request)
        {
            lock (m_lockObject)
            {
                if (m_connectRequest != null)
                {
                    // Already set to the connect state
                    throw new InvalidOperationException("Already connected");
                }

                m_connectRequest = request;
            }

            ConnectImpl();
        }

        /// <summary>
        /// Opens a connection.
        /// </summary>
        /// <returns>True if success, otherwise false.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if already disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if already connected.</exception>
        protected abstract void ConnectImpl();

        /// <summary>
        /// Terminates the current connection if a connection is active or being opened.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if already disposed.</exception>
        public abstract void Terminate();

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            lock (m_lockObject)
            {
                m_disposed = true;
            }

            DisposeImpl();
        }

        /// <summary>
        /// The actual subclass-specific dispose implementation.
        /// </summary>
        protected abstract void DisposeImpl();

        /// <summary>
        /// Sends a connection event. Use this when there is no error.
        /// </summary>
        /// <param name="connMaint">Whether connection is still being maintained or at least tried to reconnect.</param>
        /// <param name="evType">Event type.</param>
        protected void SendConnectionEvent(bool connMaint, ConnectionEventType evType)
        {
            SendConnectionEvent(connMaint, evType, null); // No relevant exception in this overload
        }

        /// <summary>
        /// Sends a connection event. Use this for errors.
        /// </summary>
        /// <param name="connMaint">Whether connection is still being maintained or at least tried to reconnect.</param>
        /// <param name="evType">Event type.</param>
        /// <param name="exc">Related exception.</param>
        protected void SendConnectionEvent(bool connMaint, ConnectionEventType evType, Exception exc)
        {
            bool disposedTemp;
            lock (m_lockObject)
            {
                disposedTemp = m_disposed;
            }
            
            // No callbacks after dispose except termination.
            // If dispose is called right after the condition, the callback can
            // occur after it, but this is unlikely and unimportant.
            if (disposedTemp &&
                evType != ConnectionEventType.TerminatingConnectionByUser &&
                evType != ConnectionEventType.ConnectionTerminatedByUser)
            {
                return;
            }

            // Event related to an error?
            if (exc != null)
            {
                var errorReason = AmqpErrorHandler.GetErrorReason(exc);
                var description = GenerateDescription(evType, errorReason.ToString());

                var connEvent = new ConnectionEvent(connMaint: connMaint, evType: evType,
                    desc: description, errReason: errorReason, excep: exc);
                b_connectionEventCallback(connEvent);
            }
            else
            {
                var description = GenerateDescription(evType, "");

                var connEvent = new ConnectionEvent(connMaint: connMaint, evType: evType, desc: description);
                b_connectionEventCallback(connEvent);
            }
        }

        private string GenerateDescription(ConnectionEventType eventType, string errorReason)
        {
            string description = ConnectionEvent.EventTypeToString(eventType);

            // Add more information to the description?
            switch (eventType)
            {
                case ConnectionEventType.Connecting:
                    description += " to " + FormatConnectionInfo();
                    break;

                case ConnectionEventType.ConnectingFailed:
                    description += string.Format(". Suspected reason: {0}. Check parameters and retry.", errorReason);
                    break;
                    
                case ConnectionEventType.ConnectionRestoreFailed:
                    description += string.Format(". Suspected reason: {0}. Retrying in {1} s.",
                        errorReason, RetryIntervalSeconds);
                    break;
                    
                default:
                    break;
            }

            return description;
        }

        private string FormatConnectionInfo()
        {
            return string.Format("{0}; exchange \"{1}\", topic pattern \"{2}\"",
                ConnectionRequestObj.Host, ConnectionRequestObj.Exchange, ConnectionRequestObj.TopicPattern);
        }
    }
}
