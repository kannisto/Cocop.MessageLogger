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
using MsgMeas = Cocop.MessageSerialiser.Meas;

namespace CocopMessageLogger
{
    /// <summary>
    /// Extracts metadata from messages.
    /// </summary>
    class MetadataExtractor
    {
        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="msg">Message data.</param>
        /// <returns>Instance.</returns>
        public static MetadataExtractor Build(byte[] msg)
        {
            const string UnknownValue = "Unknown";

            // These are the defaults
            var contentType = ContentTypeType.Other;
            var name = UnknownValue;
            var payloadSummary = UnknownValue;
            var payloadType = PayloadTypeType.Other;

            try
            {
                var messageString = System.Text.Encoding.UTF8.GetString(msg);

                // Using a dirty method to recognise the content type
                if (messageString.Contains("<?xml"))
                {
                    contentType = ContentTypeType.Xml;

                    // Using a dirty method to search a certain string in XML
                    if (messageString.Contains("OM_Observation>")) // Matches the end tag of the XML doc
                    {
                        // Observation
                        var observation = new MsgMeas.Observation(msg);

                        name = observation.Name ?? "";
                        payloadSummary = observation.Result.ToDisplayString();
                        payloadType = PayloadTypeType.ObservationXml;
                    }
                    else if (messageString.Contains("ProcessProductionSchedule>")) // Matches the end tag of the XML doc
                    {
                        // Production schedule
                        name = "Production schedule";
                        payloadSummary = "Production schedule";
                        payloadType = PayloadTypeType.ProcessProductionScheduleXml;
                    }
                }
            }
            catch { } // Use defaults

            return new MetadataExtractor(contentType, name, payloadSummary, payloadType);
        }
        
        private MetadataExtractor(ContentTypeType contentType, string name, string paylSummary, PayloadTypeType paylType)
        {
            ContentType = contentType;
            Name = name;
            PayloadSummary = paylSummary;
            PayloadType = paylType;
        }
        
        /// <summary>
        /// Content type.
        /// </summary>
        public ContentTypeType ContentType
        { get; }

        /// <summary>
        /// Payload type.
        /// </summary>
        public PayloadTypeType PayloadType
        { get; }

        /// <summary>
        /// Message name.
        /// </summary>
        public string Name
        { get; }

        /// <summary>
        /// Summary of the message payload.
        /// </summary>
        public string PayloadSummary
        { get; }
    }
}
