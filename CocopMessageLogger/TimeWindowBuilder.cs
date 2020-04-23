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
    /// A class to help building metadata queries from the user interface. This
    /// functionality was implemented in a separate class to facilitate testing,
    /// as testing with the UI would be laborious.
    /// </summary>
    class TimeWindowBuilder
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="startDate">Start date of the time window.</param>
        /// <param name="startTime">Start time of the time window.</param>
        /// <param name="duration">Duration of the time window in minutes.</param>
        /// <exception cref="ArgumentException">Thrown if the processing of input fails.</exception>
        public TimeWindowBuilder(DateTime? startDate, string startTime, string duration)
        {
            var startProcessed = GetStartTimeOfTimeWindow(startDate, startTime);
            var endProcessed = GetEndTimeOfTimeWindow(startProcessed, duration);

            TimeWindowStart = startProcessed;
            TimeWindowEnd = endProcessed;
        }

        /// <summary>
        /// Start of the time window.
        /// </summary>
        public DateTime? TimeWindowStart
        {
            get;
            private set;
        }

        /// <summary>
        /// End of the time window.
        /// </summary>
        public DateTime? TimeWindowEnd
        {
            get;
            private set;
        }
        
        private DateTime? GetStartTimeOfTimeWindow(DateTime? startDate, string startTime)
        {
            if (!startDate.HasValue)
            {
                return null; // Not specified
            }

            var actualDate = startDate.Value;

            // Check datetime kind
            if (actualDate.Kind != DateTimeKind.Local &&
                actualDate.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Unexpected DateTime kind " + actualDate.Kind.ToString());
            }

            var retval = actualDate.ToUniversalTime();
            
            int hours = 0;
            int minutes = 0;

            if (!string.IsNullOrEmpty(startTime))
            {
                // Parsing the time value
                try
                {
                    var parts = startTime.Split(':');
                    hours = int.Parse(parts[0]);
                    minutes = int.Parse(parts[1]);

                    if (hours < 0 || minutes < 0) throw new ArgumentException("Must be positive");
                }
                catch (Exception e)
                {
                    throw new ArgumentException("Cannot parse time. Use format \"hours:minutes\", such as \"7:30\".", e);
                }
            }

            // Adding hours and minutes
            return retval.AddHours(hours).AddMinutes(minutes);
        }

        private DateTime? GetEndTimeOfTimeWindow(DateTime? start, string duration)
        {
            // There must be a start to calculate end
            if (!start.HasValue)
            {
                return null;
            }
            
            if (string.IsNullOrEmpty(duration))
            {
                // No end specified
                return null;
            }

            int parsed = 0;

            // Attempting to parse the duration
            try
            {
                parsed = int.Parse(duration);

                if (parsed < 1) throw new ArgumentException("Must be positive");
            }
            catch (Exception e)
            {
                throw new ArgumentException("Cannot parse window length. Expected a positive integer.", e);
            }

            // Adding to start time
            return start.Value.AddMinutes(parsed);
        }
    }
}
