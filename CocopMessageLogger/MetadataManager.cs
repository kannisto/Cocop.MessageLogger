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
using System.Linq;
using SysColl = System.Collections.Generic;

namespace CocopMessageLogger
{
    /// <summary>
    /// Manages message metadata. Encapsulates Entity Framework, that is, a local database.
    /// This class is not thread-safe!
    /// </summary>
    class MetadataManager : IDisposable
    {
        // TODO-later: Create indices for columns in the metadata DB
        // TODO-later: Implement automatic cleanup of metadata not to grow it too large

        private DbMetadataContext m_messageContext = null;
        private bool m_disposed = false;

        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="exeDir">The directory of the executable. The metadata
        /// database will be located within.</param>
        public MetadataManager(string exeDir)
        {
            m_messageContext = new DbMetadataContext(exeDir);
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            m_disposed = true;

            if (m_messageContext != null)
            {
                try
                {
                    m_messageContext.Dispose();
                    m_messageContext = null;
                }
                catch { }
            }
        }
        
        /// <summary>
        /// Adds a piece of metadata.
        /// </summary>
        /// <param name="host">Host.</param>
        /// <param name="exc">Exchange.</param>
        /// <param name="topic">Topic.</param>
        /// <param name="recvAt">When the message was received.</param>
        /// <param name="path">Filepath of the message.</param>
        /// <param name="name">Message name.</param>
        /// <param name="payload">Message payload summarised.</param>
        /// <exception cref="ArgumentException">Thrown if DateTime kind is not UTC.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if disposed.</exception>
        public void Add(Metadata metadata)
        {
            ExpectNotDisposed(); // throws ObjectDisposedException

            // Require UTC time
            ExpectUtc(metadata.ReceivedAt);
            
            // Convert to the Entity Framework class
            var efMetadata = new DbMetadataItem()
            {
                Host = metadata.Host,
                Exchange = metadata.Exchange,
                Topic = metadata.Topic,
                ReceivedAt = metadata.ReceivedAt.Ticks,
                Name = metadata.Name,
                PayloadSummary = metadata.PayloadSummary,
                PayloadType = metadata.PayloadType.ToString(),
                Filepath = metadata.Filepath
            };

            // Add and save
            m_messageContext.DbMetadataItem.Add(efMetadata);
            m_messageContext.SaveChanges();
        }

        /// <summary>
        /// Gets message metadata.
        /// </summary>
        /// <param name="query">Query.</param>
        /// <param name="maxItems">The maximum number of items to return.</param>
        /// <returns>Metadata.</returns>
        /// <exception cref="ArgumentException">Thrown if DateTime kind is not UTC.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if disposed.</exception>
        public SysColl.List<Metadata> Get(MetadataQuery query, int maxItems)
        {
            ExpectNotDisposed(); // throws ObjectDisposedException

            var retval = new SysColl.List<Metadata>();

            // Setting query defaults for the timestamps
            var startForQuery = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            var endForQuery = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);

            // Getting the actual datetime values in the query
            if (query.TimeWindowStart.HasValue)
            {
                startForQuery = query.TimeWindowStart.Value;
            }
            if (query.TimeWindowEnd.HasValue)
            {
                endForQuery = query.TimeWindowEnd.Value;
            }

            // Requiring UTC as the DateTime kind
            ExpectUtc(startForQuery);
            ExpectUtc(endForQuery);

            var discoveredItems = from md in m_messageContext.DbMetadataItem
                                  select md;
            
            // Filtering by host and exchange;
            // filtering by start and end time
            discoveredItems = discoveredItems.Where(md =>
                md.Host == query.Host &&
                md.Exchange == query.Exchange &&
                md.ReceivedAt >= startForQuery.Ticks &&
                md.ReceivedAt <= endForQuery.Ticks
                );

            // Filtering by topic if specified
            if (!string.IsNullOrEmpty(query.Topic))
            {
                discoveredItems = discoveredItems.Where(md => md.Topic == query.Topic);
            }

            // Limiting the number of items
            discoveredItems = discoveredItems.Take(maxItems);
            
            // Converting to the Entify-Framework-independent format
            foreach (var efMeta in discoveredItems.ToList()) // ToList() requires System.Linq
            {
                var metadata = ConvertForOutput(efMeta);
                retval.Add(metadata);
            }

            return retval;
        }

        /// <summary>
        /// Gets the total count of items in the host and exchange.
        /// </summary>
        /// <param name="host">Host.</param>
        /// <param name="exc">Exchange.</param>
        /// <returns>Item count.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if disposed.</exception>
        public int GetTotalCount(string host, string exc)
        {
            ExpectNotDisposed(); // throws ObjectDisposedException

            var discoveredItems = from md in m_messageContext.DbMetadataItem
                                  select md;

            // Filtering by host and exchange
            discoveredItems = discoveredItems.Where(md => md.Host == host && md.Exchange == exc);

            return discoveredItems.Count();
        }

        /// <summary>
        /// Returns all topics in given host and exchange.
        /// </summary>
        /// <param name="host">Host.</param>
        /// <param name="exc">Exchange.</param>
        /// <returns>Topics.</returns>
        public SysColl.List<string> GetTopics(string host, string exc)
        {
            ExpectNotDisposed(); // throws ObjectDisposedException

            var items = m_messageContext.DbMetadataItem
                .Where(m => m.Host == host && m.Exchange == exc)
                .Select(m => new { m.Topic })
                .Distinct()
                .OrderBy(m1 => m1.Topic);

            // Convert to strings
            var retval = new SysColl.List<string>();

            foreach (var i in items)
            {
                retval.Add(i.Topic);
            }

            return retval;
        }
        
        private Metadata ConvertForOutput(DbMetadataItem efMeta)
        {
            // Recognising payload type
            var payloadType = PayloadTypeType.Other;
            try
            {
                payloadType = (PayloadTypeType)Enum.Parse(typeof(PayloadTypeType), efMeta.PayloadType);
            }
            catch { } // Failed to parse - preserve default value
            
            // Converting Entity Framework class to the neutral metadata class
            return new Metadata(
                    id: efMeta.DbMetadataItemId,
                    host: efMeta.Host,
                    exc: efMeta.Exchange,
                    topic: efMeta.Topic,
                    recvAt: DateTime.SpecifyKind(new DateTime(efMeta.ReceivedAt), DateTimeKind.Utc),
                    name: efMeta.Name,
                    payload: efMeta.PayloadSummary,
                    paylType: payloadType,
                    path: efMeta.Filepath
                    );
        }
        
        private void ExpectUtc(DateTime dt)
        {
            if (dt.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("DateTime kind must be UTC");
            }
        }

        private void ExpectNotDisposed()
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException("MetadataManager");
            }
        }
    }
}
