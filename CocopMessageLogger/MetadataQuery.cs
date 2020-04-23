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
    /// Represents a query for metadata.
    /// </summary>
    class MetadataQuery
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="host">Host.</param>
        /// <param name="exc">Exchange.</param>
        public MetadataQuery(string host, string exc)
        {
            Host = host;
            Exchange = exc;
            TimeWindowStart = null;
            TimeWindowEnd = null;
        }

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
        { get; set; }

        /// <summary>
        /// Start of time period.
        /// </summary>
        public DateTime? TimeWindowStart
        { get; set; }

        /// <summary>
        /// End of time period.
        /// </summary>
        public DateTime? TimeWindowEnd
        { get; set; }
    }
}
