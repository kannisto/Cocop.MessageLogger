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

namespace TimeWindowBuilderTest
{
    // Testing the TimeWindowBuilder class. Testing reliably with the UI would be difficult, so this
    // test project was created.

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void DefaultInput()
        {
            // The default input of the UI

            var testObject = new TimeWindowBuilder(null, "0:00", "");

            Assert.IsNull(testObject.TimeWindowStart);
            Assert.IsNull(testObject.TimeWindowEnd);
        }

        [TestMethod]
        public void NoParameters()
        {
            // No parameters specified at all

            var testObject = new TimeWindowBuilder(null, "", "");

            Assert.IsNull(testObject.TimeWindowStart);
            Assert.IsNull(testObject.TimeWindowEnd);
        }

        [TestMethod]
        public void StartTime_WithLeadingZero()
        {
            // Testing a time value with leading zeros

            DateTime? startDate = DateTime.Parse("2019-12-16T00:00:00+02:00");

            var testObject = new TimeWindowBuilder(startDate, "02:20", "30"); // Leading zero in the hours input

            AssertDateTime("2019-12-16T00:20:00Z", testObject.TimeWindowStart.Value); // Expecting a conversion local to UTC here
            AssertDateTime("2019-12-16T00:50:00Z", testObject.TimeWindowEnd.Value); // Expecting a conversion local to UTC here
        }

        [TestMethod]
        public void AllParams()
        {
            // All parameters are specified and correct

            DateTime? startDate = DateTime.Parse("2019-12-16T00:00:00+02:00");

            var testObject = new TimeWindowBuilder(startDate, "12:20", "30");

            AssertDateTime("2019-12-16T10:20:00Z", testObject.TimeWindowStart.Value); // Expecting a conversion local to UTC here
            AssertDateTime("2019-12-16T10:50:00Z", testObject.TimeWindowEnd.Value); // Expecting a conversion local to UTC here
        }

        [TestMethod]
        public void NoStartDate_OthersCorrect()
        {
            // No start date specified

            var testObject = new TimeWindowBuilder(null, "1:25", "60");

            // Expecting default, as the start date is mandatory for filtering
            Assert.IsNull(testObject.TimeWindowStart);
            Assert.IsNull(testObject.TimeWindowEnd);
        }

        [TestMethod]
        public void NoStartTime_OthersCorrect()
        {
            // No start time specified

            DateTime? startDate = DateTime.Parse("2019-12-16T00:00:00+02:00");

            var testObject = new TimeWindowBuilder(startDate, "", "60");

            // Expecting midnight (local time) to be the start
            AssertDateTime("2019-12-15T22:00:00Z", testObject.TimeWindowStart.Value); // Expecting a conversion local to UTC here
            AssertDateTime("2019-12-15T23:00:00Z", testObject.TimeWindowEnd.Value); // Expecting a conversion local to UTC here
        }

        [TestMethod]
        public void NoDuration_OthersCorrect()
        {
            // No duration specified

            DateTime? startDate = DateTime.Parse("2019-12-16T00:00:00+02:00");

            var testObject = new TimeWindowBuilder(startDate, "12:20", ""); // No duration specified

            AssertDateTime("2019-12-16T10:20:00Z", testObject.TimeWindowStart.Value); // Expecting a conversion local to UTC here
            Assert.IsNull(testObject.TimeWindowEnd);
        }

        [TestMethod]
        public void Err_StartTime_UnexpectedKind()
        {
            // DateTime kind not supported

            var startDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

            AssertArgumentException(
                () =>
                {
                    new TimeWindowBuilder(startDate, "", "");
                }, "Unexpected DateTime kind");
        }

        [TestMethod]
        public void Err_StartTime_CannotParse()
        {
            // Fail to parse start time

            var startDate = DateTime.Now;

            AssertArgumentException(
                () =>
                {
                    new TimeWindowBuilder(startDate, "sdfd", "");
                }, "Cannot parse time");
        }

        [TestMethod]
        public void Err_StartTime_WrongSeparator()
        {
            // Using an invalid separator between hours and minutes.
            // Expecting ':', testing with '.'.

            var startDate = DateTime.Now;

            AssertArgumentException(
                () =>
                {
                    new TimeWindowBuilder(startDate, "14.20", "");
                }, "Cannot parse time");
        }

        [TestMethod]
        public void Err_Duration_CannotParse()
        {
            // Fail to parse duration

            AssertArgumentException(
                () =>
                {
                    new TimeWindowBuilder(DateTime.Now, "14:20", "dgdg");
                }, "Cannot parse window length");
        }

        private void AssertDateTime(string expected, DateTime actual)
        {
            // Expecting UTC as the kind
            Assert.AreEqual(DateTimeKind.Utc, actual.Kind, "Expected UTC as the kind");

            // Asserting difference
            var parsedExpected = DateTime.Parse(expected).ToUniversalTime();
            var assertMsg = string.Format("Expected datetime {0}, got {1}",
                DateTimeToString(parsedExpected), DateTimeToString(actual));
            Assert.AreEqual(0, (parsedExpected - actual).TotalMilliseconds, 0.1, assertMsg);
        }

        private string DateTimeToString(DateTime dt)
        {
            return string.Format("{0} ({1})", dt.ToString("yyyy-MM-dd'T'HH:mm:ss"), dt.Kind.ToString());
        }

        private void AssertArgumentException(Action action, string expectedMsgStart)
        {
            try
            {
                action.Invoke();
                Assert.Fail("Expected an exception");
            }
            catch (ArgumentException e)
            {
                Assert.IsTrue(e.Message.StartsWith(expectedMsgStart), "Unexpected exception message " + e.Message);
            }
        }
    }
}
