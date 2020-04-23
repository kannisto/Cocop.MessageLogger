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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SysEnc = System.Text.Encoding;
using MsgMeas = Cocop.MessageSerialiser.Meas;
using MsgBiz = Cocop.MessageSerialiser.Biz;
using CocopMessageLogger;

namespace MetadataExtractorTest
{
    [TestClass]
    public class UnitTest1
    {
        // Testing the MetadataExtractor class

        private const string Unknown = "Unknown";


        [TestMethod]
        public void EmptyContent()
        {
            // Empty message

            var msg = new byte[0];
            var testObject = MetadataExtractor.Build(msg);

            AssertDefault(testObject);
        }

        [TestMethod]
        public void UnknownContent()
        {
            // Unknown message

            var msg = SysEnc.UTF8.GetBytes("abc");
            var testObject = MetadataExtractor.Build(msg);

            AssertDefault(testObject);
        }

        private void AssertDefault(MetadataExtractor testObject)
        {
            // These are the defaults
            Assert.AreEqual(ContentTypeType.Other, testObject.ContentType);
            Assert.AreEqual(Unknown, testObject.Name);
            Assert.AreEqual(Unknown, testObject.PayloadSummary);
            Assert.AreEqual(PayloadTypeType.Other, testObject.PayloadType);
        }

        [TestMethod]
        public void UnknownXmlContent()
        {
            // Unknown XML message

            var msgString = "<?xml version=\"1.0\" encoding=\"utf-8\"?><Foo></Foo>";
            var msg = SysEnc.UTF8.GetBytes(msgString);
            var testObject = MetadataExtractor.Build(msg);

            Assert.AreEqual(ContentTypeType.Xml, testObject.ContentType);
            Assert.AreEqual(Unknown, testObject.Name);
            Assert.AreEqual(Unknown, testObject.PayloadSummary);
            Assert.AreEqual(PayloadTypeType.Other, testObject.PayloadType);
        }

        [TestMethod]
        public void Observation()
        {
            // Observation test

            // Creating an observation for testing
            var dataRecord = new MsgMeas.Item_DataRecord()
            {
                { "mass", new MsgMeas.Item_Measurement("t", 1.2) },
                { "thickness", new MsgMeas.Item_Measurement("cm", 3.5) }
            };
            var observation = new MsgMeas.Observation(dataRecord)
            {
                Name = "Some name"
            };

            var msg = observation.ToXmlBytes();

            // Extracting metadata
            var testObject = MetadataExtractor.Build(msg);

            Assert.AreEqual(ContentTypeType.Xml, testObject.ContentType);
            Assert.AreEqual("Some name", testObject.Name);
            Assert.AreEqual("Data record (2 fields)", testObject.PayloadSummary);
            Assert.AreEqual(PayloadTypeType.ObservationXml, testObject.PayloadType);
        }

        [TestMethod]
        public void ProcessProductionSchedule()
        {
            // ProcessProductionSchedule request test

            // Creating a message object for testing
            var request = new MsgBiz.ProcessProductionSchedule()
            {
                ProductionSchedules = new System.Collections.Generic.List<MsgBiz.ProductionSchedule>
                {
                    new MsgBiz.ProductionSchedule()
                    {
                        ProductionRequests = new System.Collections.Generic.List<MsgBiz.ProductionRequest>
                        {
                            new MsgBiz.ProductionRequest()
                            {
                                HierarchyScopeObj = new MsgBiz.HierarchyScope(
                                    new MsgBiz.IdentifierType("my-equipment"),
                                    MsgBiz.EquipmentElementLevelType.ProcessCell
                                    )
                            }
                        }
                    }
                }
            };

            var msg = request.ToXmlBytes();

            // Extracting metadata
            var testObject = MetadataExtractor.Build(msg);

            Assert.AreEqual(ContentTypeType.Xml, testObject.ContentType);
            Assert.AreEqual("Production schedule", testObject.Name);
            Assert.AreEqual("Production schedule", testObject.PayloadSummary);
            Assert.AreEqual(PayloadTypeType.ProcessProductionScheduleXml, testObject.PayloadType);
        }
    }
}
