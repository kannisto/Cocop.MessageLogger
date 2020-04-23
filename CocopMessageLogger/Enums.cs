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

namespace CocopMessageLogger
{
    /// <summary>
    /// Specifies a connection-related event.
    /// </summary>
    enum ConnectionEventType
    {
        /// <summary>
        /// Connecting by user request.
        /// </summary>
        Connecting,
        /// <summary>
        /// Connecting has failed.
        /// </summary>
        ConnectingFailed,
        /// <summary>
        /// Connecting has succeeded.
        /// </summary>
        Connected,
        /// <summary>
        /// Connection has been lost by accident.
        /// </summary>
        ConnectionLost,
        /// <summary>
        /// Restoring an accidentally lost connection.
        /// </summary>
        RestoringConnection,
        /// <summary>
        /// Failed to restore an accidentally lost connection.
        /// </summary>
        ConnectionRestoreFailed,
        /// <summary>
        /// An accidentally lost connection has been restored successfully.
        /// </summary>
        ConnectionRestored,
        /// <summary>
        /// The user has asked to terminate the connection or connecting attempt.
        /// </summary>
        TerminatingConnectionByUser,
        /// <summary>
        /// The user has terminated the connection or connecting attempt.
        /// </summary>
        ConnectionTerminatedByUser,
    }

    /// <summary>
    /// Specifies the reason for a connection failure.
    /// </summary>
    public enum ErrorReasonType
    {
        /// <summary>
        /// All other errors.
        /// </summary>
        Other = 0,
        /// <summary>
        /// All other broker-related errors.
        /// </summary>
        Broker_Other = 1,
        /// <summary>
        /// Authentication has failed at broker.
        /// </summary>
        Broker_Credentials = 2,
        /// <summary>
        /// The operation is not permitted at the broker.
        /// </summary>
        Broker_Permissions = 3,
        /// <summary>
        /// All other network-related errors.
        /// </summary>
        Network_Other = 11,
        /// <summary>
        /// The broker cannot be connected to.
        /// </summary>
        Network_NotReachable = 12,
        /// <summary>
        /// The broker refused the connection.
        /// </summary>
        Network_ConnectionRefused = 13,
        /// <summary>
        /// There is a certificate-related issue.
        /// </summary>
        Certificate = 21
    }

    /// <summary>
    /// Specifies the type of message payload.
    /// </summary>
    public enum PayloadTypeType
    {
        /// <summary>
        /// All other payload types.
        /// </summary>
        Other = 0,
        /// <summary>
        /// Observation.
        /// </summary>
        ObservationXml = 1,
        /// <summary>
        /// ProcessProductionSchedule request.
        /// </summary>
        ProcessProductionScheduleXml = 2
    }

    /// <summary>
    /// Specifies content type.
    /// </summary>
    public enum ContentTypeType
    {
        /// <summary>
        /// All other content types.
        /// </summary>
        Other = 0,
        /// <summary>
        /// XML.
        /// </summary>
        Xml = 1
    }
}
