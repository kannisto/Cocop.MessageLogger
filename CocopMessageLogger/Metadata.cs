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
    /// Holds message metadata. This class has redundancy with Entity Framework (EF) classes,
    /// but the intention is to encapsulate EF as much as possible to eliminate any
    /// unnecessary dependencies.
    /// </summary>
    class Metadata
    {
        /// <summary>
        /// Constructor. Use this when writing to database.
        /// </summary>
        /// <param name="host">Host.</param>
        /// <param name="exc">Exchange.</param>
        /// <param name="topic">Topic.</param>
        /// <param name="recvAt">When the message was received. This must be in UTC.</param>
        /// <param name="name">Name.</param>
        /// <param name="payload">Payload summary.</param>
        /// <param name="paylType">Payload type.</param>
        /// <param name="path">Filepath.</param>
        /// <exception cref="ArgumentException">Thrown if a DateTime without the UTC kind is provided.</exception>
        public Metadata(string host, string exc, string topic, DateTime recvAt, string name, string payload, PayloadTypeType paylType, string path)
            : this(id: -1, host: host, exc: exc, topic: topic, recvAt: recvAt, name: name, payload: payload, paylType: paylType, path: path)
        {
            // Empty ctor body
        }

        /// <summary>
        /// Constructor. Use this when reading from database.
        /// </summary>
        /// <param name="id">Running identifier.</param>
        /// <param name="host">Host.</param>
        /// <param name="exc">Exchange.</param>
        /// <param name="topic">Topic.</param>
        /// <param name="recvAt">When the message was received. This must be in UTC.</param>
        /// <param name="name">Name.</param>
        /// <param name="payload">Payload summary.</param>
        /// <param name="paylType">Payload type.</param>
        /// <param name="path">Filepath.</param>
        /// <exception cref="ArgumentException">Thrown if a DateTime without the UTC kind is provided.</exception>
        public Metadata(int id, string host, string exc, string topic, DateTime recvAt, string name, string payload, PayloadTypeType paylType, string path)
        {
            // Require UTC time
            ExpectUtc(recvAt);

            RunningId = id;
            Host = host;
            Exchange = exc;
            Topic = topic;
            ReceivedAt = recvAt;
            Name = name;
            PayloadSummary = payload;
            PayloadType = paylType;
            Filepath = path;
        }
        
        /// <summary>
        /// Running identifier.
        /// </summary>
        public int RunningId
        { get; }

        /// <summary>
        /// Host.
        /// </summary>
        public string Host
        { get; }

        /// <summary>
        /// Exchange.
        /// </summary>
        public string Exchange
        { get; }

        /// <summary>
        /// Topic.
        /// </summary>
        public string Topic
        { get; }

        /// <summary>
        /// When the message was received (in UTC time).
        /// </summary>
        public DateTime ReceivedAt
        { get; }

        /// <summary>
        /// Name.
        /// </summary>
        public string Name
        { get; }

        /// <summary>
        /// Summary of the payload.
        /// </summary>
        public string PayloadSummary
        { get; }
        
        /// <summary>
        /// The type of the payload.
        /// </summary>
        public PayloadTypeType PayloadType
        { get;  }

        /// <summary>
        /// Filepath.
        /// </summary>
        public string Filepath
        { get; }

        private void ExpectUtc(DateTime dt)
        {
            if (dt.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("DateTime kind must be UTC");
            }
        }
    }
}
