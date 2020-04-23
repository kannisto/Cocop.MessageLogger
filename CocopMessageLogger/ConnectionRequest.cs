//
// Please make sure to read and understand README.md and LICENSE.txt.
// 
// This file was prepared in the research project COCOP (Coordinating
// Optimisation of Complex Industrial Processes).
// https://cocop-spire.eu/
//
// Author: Petri Kannisto, Tampere University, Finland
// File created: 10/2019
// Last modified: 3/2020

using System;

namespace CocopMessageLogger
{
    /// <summary>
    /// Represents a connection request.
    /// </summary>
    class ConnectionRequest
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="host">Host.</param>
        /// <param name="secure">Whether secure connection is used.</param>
        /// <param name="user">Username.</param>
        /// <param name="pwd">Password.</param>
        /// <param name="exc">Exchange.</param>
        /// <param name="topic">Topic pattern.</param>
        public ConnectionRequest(string host, string exc, bool secure, string user, string pwd, string topic)
        {
            Host = host;
            Secure = secure;
            Username = user;
            Password = pwd;
            Exchange = exc;
            TopicPattern = topic;
        }
        
        /// <summary>
        /// Host.
        /// </summary>
        public string Host
        {
            get;
            private set;
        }

        /// <summary>
        /// Exchange.
        /// </summary>
        public string Exchange
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether secure connection is used.
        /// </summary>
        public bool Secure
        {
            get;
            private set;
        }

        /// <summary>
        /// Username.
        /// </summary>
        public string Username
        {
            get;
            private set;
        }

        /// <summary>
        /// Password.
        /// </summary>
        public string Password
        {
            get;
            private set;
        }
        
        /// <summary>
        /// Topic pattern to listen to.
        /// </summary>
        public string TopicPattern
        {
            get;
            private set;
        }
    }
}
