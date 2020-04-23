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
using CocopMessageLogger;

namespace MessageMetadataTest
{
    [TestClass]
    public class UnitTest1
    {
        // Testing the MetadataManager class. The most focus is on filtering instead of
        // the storage of non-filter-related metadata fields.

        private static MetadataManager s_metadataManager = null;

        private static DateTime s_baseTime = DateTime.Parse("2019-12-09T00:00:00Z").ToUniversalTime();


        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            s_metadataManager = new MetadataManager(AppBaseFolder);

            // Adding data, 5 items in total
            for (int a = 1; a <= 5; ++a)
            {
                var metadataOut = GetMetadataForTest(a);
                s_metadataManager.Add(metadataOut);
            }
        }

        // Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            // There is no need to clear the database, because a fresh instance
            // overwrites any existing one for every run

            if (s_metadataManager != null)
            {
                s_metadataManager.Dispose();
                s_metadataManager = null;
            }
        }

        [TestMethod]
        public void Err_Add_TimeNotUtc()
        {
            // Testing the detection of non-UTC timestamps

            AssertArgumentException("DateTime kind must be UTC",
                () =>
                {
                    var metadata = new Metadata("host", "exc", "topic",
                        DateTime.Now.ToLocalTime(), "name", "payload", PayloadTypeType.Other, "path");
                    s_metadataManager.Add(metadata);
                });
        }

        [TestMethod]
        public void Err_Get_TimeNotUtc()
        {
            // Testing the detection of non-UTC timestamps

            var queryBadStart = new MetadataQuery("host", "exc")
            {
                TimeWindowStart = DateTime.Now.ToLocalTime() // Local is bad
            };
            var queryBadEnd = new MetadataQuery("host", "exc")
            {
                TimeWindowStart = DateTime.Now.ToLocalTime() // Local is bad
            };

            AssertArgumentException("DateTime kind must be UTC",
                () =>
                {
                    s_metadataManager.Get(queryBadStart, 100);
                });
            AssertArgumentException("DateTime kind must be UTC",
                () =>
                {
                    s_metadataManager.Get(queryBadEnd, 100);
                });
        }
        
        [TestMethod]
        public void Get_AllInHostAndExchange()
        {
            // Testing the retrieval of metadata without any optional filters

            var metadata1 = s_metadataManager.Get(new MetadataQuery(
                host: "1.0.0.1", exc: "exc.1.1"), 100);
            var metadata2 = s_metadataManager.Get(new MetadataQuery(
                host: "1.0.0.2", exc: "exc.2.1"), 100);

            // Expect all in topic and exchange
            Assert.AreEqual(3, metadata1.Count);
            AssertMetadata(1, metadata1[0]);
            AssertMetadata(2, metadata1[1]);
            AssertMetadata(3, metadata1[2]);

            // Expect all in topic and exchange
            Assert.AreEqual(1, metadata2.Count);
            AssertMetadata(5, metadata2[0]);
        }

        [TestMethod]
        public void Get_AllInHostAndExchange_WithLimit()
        {
            // Testing the retrieval of metadata without any optional filters.
            // Limiting the number of returned items.

            var metadata1 = s_metadataManager.Get(new MetadataQuery(
                host: "1.0.0.1", exc: "exc.1.1"), 2); // The limit is 2
            
            // Expect all in topic and exchange except those that do not fit in the limit
            Assert.AreEqual(2, metadata1.Count);
            AssertMetadata(1, metadata1[0]);
            AssertMetadata(2, metadata1[1]);
            // Item nr 3 does not fit in
        }

        [TestMethod]
        public void Get_Topic()
        {
            // Testing the retrieval of message list from a topic

            var metadata1 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    Topic = "topic.a"
                }, 100);
            var metadata2 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    Topic = "topic.b"
                }, 100);

            // Expect all in topic.a
            Assert.AreEqual(2, metadata1.Count);
            AssertMetadata(1, metadata1[0]);
            AssertMetadata(2, metadata1[1]);

            // Expect all in topic.b
            Assert.AreEqual(1, metadata2.Count);
            AssertMetadata(3, metadata2[0]);
        }

        [TestMethod]
        public void Get_After()
        {
            // Testing filtering after given time

            var metadata1 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    TimeWindowStart = s_baseTime.AddMinutes(59)
                }, 100);
            var metadata2 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    TimeWindowStart = s_baseTime.AddMinutes(2 * 60 + 59)
                }, 100);
            var metadata3 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    TimeWindowStart = s_baseTime.AddMinutes(3 * 60 + 1)
                }, 100);

            // Expect all items in exc.1.1
            Assert.AreEqual(3, metadata1.Count);
            AssertMetadata(1, metadata1[0]);
            AssertMetadata(2, metadata1[1]);
            AssertMetadata(3, metadata1[2]);

            // Expect last item in exc.1.1
            Assert.AreEqual(1, metadata2.Count);
            AssertMetadata(3, metadata2[0]);

            // Expect nothing
            Assert.AreEqual(0, metadata3.Count);
        }

        [TestMethod]
        public void Get_Before()
        {
            // Testing filtering before given time

            var metadata1 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    TimeWindowEnd = s_baseTime.AddMinutes(3 * 60 + 1)
                }, 100);
            var metadata2 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    TimeWindowEnd = s_baseTime.AddMinutes(61)
                }, 100);
            var metadata3 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    TimeWindowEnd = s_baseTime.AddMinutes(59)
                }, 100);

            // Expect all items in exc.1.1
            Assert.AreEqual(3, metadata1.Count);
            AssertMetadata(1, metadata1[0]);
            AssertMetadata(2, metadata1[1]);
            AssertMetadata(3, metadata1[2]);

            // Expect first item in exc.1.1
            Assert.AreEqual(1, metadata2.Count);
            AssertMetadata(1, metadata2[0]);

            // Expect nothing
            Assert.AreEqual(0, metadata3.Count);
        }

        [TestMethod]
        public void Get_TimePeriod()
        {
            // Testing filtering by time with no other filters

            var metadata1 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    TimeWindowStart = s_baseTime.AddMinutes(59),
                    TimeWindowEnd = s_baseTime.AddMinutes(3 * 60 + 1)
                }, 100);
            var metadata2 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    TimeWindowStart = s_baseTime.AddMinutes(59),
                    TimeWindowEnd = s_baseTime.AddMinutes(3 * 60 - 1)
                }, 100);
            var metadata3 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    TimeWindowStart = s_baseTime.AddMinutes(58),
                    TimeWindowEnd = s_baseTime.AddMinutes(59)
                }, 100);

            // Expect all items in exc.1.1
            Assert.AreEqual(3, metadata1.Count);
            AssertMetadata(1, metadata1[0]);
            AssertMetadata(2, metadata1[1]);
            AssertMetadata(3, metadata1[2]);

            // Expect first two item in exc.1.1
            Assert.AreEqual(2, metadata2.Count);
            AssertMetadata(1, metadata2[0]);
            AssertMetadata(2, metadata2[1]);

            // Expect nothing
            Assert.AreEqual(0, metadata3.Count);
        }

        [TestMethod]
        public void Get_TimePeriod_StartAfterEnd()
        {
            // Testing filtering by time when start is after end

            var metadata1 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    TimeWindowStart = s_baseTime.AddMinutes(90),
                    TimeWindowEnd = s_baseTime.AddMinutes(80)
                }, 100);

            Assert.AreEqual(0, metadata1.Count);
        }
        
        [TestMethod]
        public void Get_Topic_TimePeriod()
        {
            // Testing the retrieval of message list from a topic; filtering by time

            var metadata1 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    TimeWindowStart = s_baseTime.AddMinutes(61),
                    TimeWindowEnd = s_baseTime.AddMinutes(2 * 60 + 1),
                    Topic = "topic.a"
                }, 100);
            var metadata2 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    TimeWindowStart = s_baseTime.AddMinutes(3 * 60),
                    TimeWindowEnd = s_baseTime.AddMinutes(4 * 60),
                    Topic = "topic.a"
                }, 100);

            // Expect second item in exc.1.1
            Assert.AreEqual(1, metadata1.Count);
            AssertMetadata(2, metadata1[0]);
            
            // Expect nothing
            Assert.AreEqual(0, metadata2.Count);
        }
        
        [TestMethod]
        public void Get_NoMatch_Topic()
        {
            // Testing filtering by time when the topic does not match

            var metadata1 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    Topic = "topic.x"
                }, 100);

            Assert.AreEqual(0, metadata1.Count);
        }

        [TestMethod]
        public void Get_NoMatch_TimePeriod()
        {
            // Testing filtering by time when the time period does not match

            var metadata1 = s_metadataManager.Get(
                new MetadataQuery(host: "1.0.0.1", exc: "exc.1.1")
                {
                    // The time period is before any message
                    TimeWindowStart = s_baseTime.AddMinutes(30),
                    TimeWindowEnd = s_baseTime.AddMinutes(40)
                }, 100);

            Assert.AreEqual(0, metadata1.Count);
        }

        [TestMethod]
        public void Get_NoMatch_Host()
        {
            // Testing filtering by time when the host name does not match

            var metadata1 = s_metadataManager.Get(new MetadataQuery(host: "1.0.0.3", exc: "exc.1.1"), 100);

            Assert.AreEqual(0, metadata1.Count);
        }

        [TestMethod]
        public void Get_NoMatch_Exchange()
        {
            // Testing filtering by time when the exchange does not match

            var metadata1 = s_metadataManager.Get(new MetadataQuery(host: "1.0.0.1", exc: "exc.1.9"), 100);

            Assert.AreEqual(0, metadata1.Count);
        }

        [TestMethod]
        public void GetTotalCount()
        {
            // Testing the retrieval of the total count of items

            var count1 = s_metadataManager.GetTotalCount(host: "1.0.0.1", exc: "exc.1.1");
            var count2 = s_metadataManager.GetTotalCount(host: "1.0.0.1", exc: "exc.1.2");
            var count3 = s_metadataManager.GetTotalCount(host: "sdfdsf", exc: "sdfsfdf");

            Assert.AreEqual(3, count1);
            Assert.AreEqual(1, count2);
            Assert.AreEqual(0, count3);
        }

        [TestMethod]
        public void GetTopics()
        {
            // Testing the retrieval of topics
            
            var topics1 = s_metadataManager.GetTopics(host: "1.0.0.1", exc: "exc.1.1");
            var topics2 = s_metadataManager.GetTopics(host: "1.0.0.2", exc: "exc.2.1");
            var topics3 = s_metadataManager.GetTopics(host: "1.0.0.9", exc: "exc.1.1");
            var topics4 = s_metadataManager.GetTopics(host: "1.0.0.1", exc: "exc.1.9");

            // Assert topics 1
            Assert.AreEqual(2, topics1.Count);
            Assert.AreEqual("topic.a", topics1[0]);
            Assert.AreEqual("topic.b", topics1[1]);

            // Assert topics 2
            Assert.AreEqual(1, topics2.Count);
            Assert.AreEqual("topic.d", topics2[0]);

            // Expect nothing in these
            Assert.AreEqual(0, topics3.Count);
            Assert.AreEqual(0, topics4.Count);
        }

        private void AssertMetadata(int idOfExpected, Metadata actual)
        {
            // Asserting each field in the metadata object

            var expected = GetMetadataForTest(idOfExpected);

            Assert.AreEqual(expected.RunningId, actual.RunningId);
            Assert.AreEqual(expected.Topic, actual.Topic);
            Assert.AreEqual(expected.ReceivedAt, actual.ReceivedAt);
            Assert.AreEqual(expected.Name, actual.Name);
            // "Payload summary" not included in the test
            Assert.AreEqual(expected.Filepath, actual.Filepath);

            // Asserting timestamp
            Assert.AreEqual(DateTimeKind.Utc, actual.ReceivedAt.Kind);
            var timeDiff = Math.Abs((expected.ReceivedAt - actual.ReceivedAt).TotalMilliseconds);
            Assert.AreEqual(0, timeDiff);
        }

        private static Metadata GetMetadataForTest(int id)
        {
            // Using the following metadata in this test.
            //
            // ID  Host    Exchange Topic   Name ReceivedAt  Filepath
            //  1  1.0.0.1 exc.1.1  topic.a msg1       1:00  path1
            //  2  1.0.0.1 exc.1.1  topic.a msg2       2:00  path2
            //  3  1.0.0.1 exc.1.1  topic.b msg3       3:00  path3
            //  4  1.0.0.1 exc.1.2  topic.c msg4       4:00  path4
            //  5  1.0.0.2 exc.2.1  topic.d msg5       5:00  path5
            
            const string exc_1_1 = "exc.1.1";
            const string exc_1_2 = "exc.1.2";
            const string exc_2_1 = "exc.2.1";
            const string topicA = "topic.a";
            const string topicB = "topic.b";
            const string topicC = "topic.c";
            const string topicD = "topic.d";
            
            // These will be assigned to the constructor
            var pHost = "1.0.0.1";
            var pExchange = exc_1_1;
            var pTopic = "";
            
            switch (id)
            {
                case 1:
                case 2:
                    pTopic = topicA;
                    break;

                case 3:
                    pTopic = topicB;
                    break;

                case 4:
                    pExchange = exc_1_2;
                    pTopic = topicC;
                    break;

                case 5:
                    pHost = "1.0.0.2";
                    pExchange = exc_2_1;
                    pTopic = topicD;
                    break;

                default:
                    throw new ArgumentException("Invalid ID " + id);
            }

            return new Metadata(id, pHost, pExchange, pTopic,
                    s_baseTime.AddHours(id), "Name for test", "Payload for test", PayloadTypeType.Other, "path" + id);
        }

        private static string s_appBaseFolder = null;
        private static string AppBaseFolder
        {
            get
            {
                if (s_appBaseFolder == null)
                {
                    s_appBaseFolder = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                }

                return s_appBaseFolder;
            }
        }

        private void AssertArgumentException(string errMsg, Action act)
        {
            try
            {
                act.Invoke();
            }
            catch (ArgumentException e)
            {
                Assert.IsTrue(e.Message.StartsWith(errMsg), "Unexpected exception message " + e.Message);
            }
        }
    }
}
