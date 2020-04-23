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
    // Stub class to enable testing without the actual classes integrated


    public class MetadataManager : IDisposable
    {
        public MetadataManager(object a)
        { }

        public void Dispose()
        {
            // Nothing to dispose in test
        }

        public Metadata GetById(object a)
        {
            return GetDefaultMetadataForTest();
        }

        public Metadata Add(Metadata metadata)
        {
            return GetDefaultMetadataForTest();
        }

        private Metadata GetDefaultMetadataForTest()
        {
            return new Metadata("host", "exc", "topic", DateTime.Now.ToUniversalTime(), "name", "payload", PayloadTypeType.Other, "path");
        }
    }

    public class Metadata
    {
        public Metadata(string host, string exc, string topic, DateTime recvAt, string name, string payload, PayloadTypeType paylType, string path)
        { }

        public string Filepath
        {
            get; set;
        }
    }

    public class MetadataExtractor
    {
        public static MetadataExtractor Build(object a)
        {
            return new MetadataExtractor();
        }

        public string Name
        {
            get { return "Name for test"; }
        }

        public string PayloadSummary
        {
            get { return "Payload for test"; }
        }

        public ContentTypeType ContentType
        {
            get { return ContentTypeType.Other; }
        }

        public PayloadTypeType PayloadType
        {
            get { return PayloadTypeType.Other; }
        }
    }
}
