//
// Please make sure to read and understand README.md and LICENSE.txt.
// 
// This file was prepared in the research project COCOP (Coordinating
// Optimisation of Complex Industrial Processes).
// https://cocop-spire.eu/
//
// Author: Petri Kannisto, Tampere University, Finland
// File created: 12/2019
// Last modified: 3/2020

using System;

namespace CocopMessageLogger
{
    /// <summary>
    /// Holds information about an event related to the network connection. This class does not infer
    /// anything client-related, because such functionality belongs to the client.
    /// </summary>
    class ConnectionEvent
    {
        /// <summary>
        /// Constructor. Use this when the event is *not* related to an error.
        /// </summary>
        /// <param name="connMaint">Whether the connection is being maintained or at least attempted.</param>
        /// <param name="evType">Current state of connection.</param>
        /// <param name="desc">Event description.</param>
        public ConnectionEvent(bool connMaint, ConnectionEventType evType, string desc)
            : this(connMaint: connMaint, evType: evType, desc: desc, errReason: ErrorReasonType.Other, excep: null)
        {
            // Empty ctor body
        }

        /// <summary>
        /// Constructor. Use this when the state is related to an error.
        /// </summary>
        /// <param name="connMaint">Whether the connection is being maintained or at least attempted.</param>
        /// <param name="evType">Current state of connection.</param>
        /// <param name="desc">Event description.</param>
        /// <param name="errReason">Error reason.</param>
        /// <param name="excep">Exception related to the error.</param>
        public ConnectionEvent(bool connMaint, ConnectionEventType evType, string desc, ErrorReasonType errReason, Exception excep)
        {
            IsConnectionMaintained = connMaint;
            EventType = evType;
            Description = desc;
            ErrorReason = errReason;
            RelatedException = excep;
        }
        
        /// <summary>
        /// True if there are at least attempts to maintain a connection.
        /// When true, there is an existing connection or connecting attempt 
        /// that must be terminated to create another connection.
        /// </summary>
        public bool IsConnectionMaintained
        {
            get;
        }
        
        /// <summary>
        /// The type of the event.
        /// </summary>
        public ConnectionEventType EventType
        {
            get;
        }

        /// <summary>
        /// The type of the event as string.
        /// </summary>
        public string EventTypeString
        {
            get { return EventTypeToString(EventType); }
        }

        /// <summary>
        /// Event description.
        /// </summary>
        public string Description
        {
            get;
        }

        /// <summary>
        /// Whether the event reports an error.
        /// </summary>
        public bool IsError
        {
            get { return RelatedException != null; }
        }

        /// <summary>
        /// Error reason. Only relevant if the event reports an error.
        /// </summary>
        public ErrorReasonType ErrorReason
        {
            get;
        }

        /// <summary>
        /// Related exception. Only relevant if the event reports an error.
        /// </summary>
        public Exception RelatedException
        {
            get;
        }

        /// <summary>
        /// Converts event type to string.
        /// </summary>
        /// <param name="evType">Event type.</param>
        /// <returns>String.</returns>
        public static string EventTypeToString(ConnectionEventType evType)
        {
            switch (evType)
            {
                case ConnectionEventType.ConnectingFailed:
                    return "Connecting failed";
                case ConnectionEventType.ConnectionLost:
                    return "Connection lost";
                case ConnectionEventType.ConnectionRestored:
                    return "Connection restored";
                case ConnectionEventType.ConnectionRestoreFailed:
                    return "Connection restore failed";
                case ConnectionEventType.ConnectionTerminatedByUser:
                    return "Connection terminated by user";
                case ConnectionEventType.RestoringConnection:
                    return "Restoring connection";
                case ConnectionEventType.TerminatingConnectionByUser:
                    return "User asked to terminate connection";
                default:
                    // Connecting
                    // Connected
                    return evType.ToString();
            }
        }
    }
}
