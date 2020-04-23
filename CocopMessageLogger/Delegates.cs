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
    // This file contains common delegates.
    
    /// <summary>
    /// Reports a connection-related event.
    /// </summary>
    /// <param name="ev">The event being reported.</param>
    delegate void ConnectionEventCallback(ConnectionEvent ev);
    
    /// <summary>
    /// Reports the reception of a message.
    /// </summary>
    /// <param name="host">Host.</param>
    /// <param name="exc">Exchange.</param>
    /// <param name="topic">Topic that delivered the message.</param>
    /// <param name="msg">Message body.</param>
    delegate void MessageReceivedCallback(string host, string exc, string topic, byte[] msg);
}
