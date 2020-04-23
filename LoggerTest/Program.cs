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
using System.Collections.Generic;
using CocopMessageLogger;

namespace LoggerTest
{
    // Testing if the logger can add message as expected.
    // Add more test cases if needed.

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var logger = new Logger(AppBaseFolder);
                    
                var descriptions = new List<string>()
                {
                    "Adding a message. Does it appear where expected?",
                    "Adding a connection event. Does it appear?",
                    "Adding an UI exception. Does it appear?",
                    "Adding a connection exception. Does it appear?"
                };

                var testCases = new List<Action>()
                {
                    () =>
                    {
                        var encoding = System.Text.Encoding.UTF8;
                        logger.AddReceivedMessage("some-host", "some-exchange", "some-topic", encoding.GetBytes("Message for test"));
                    },
                    () =>
                    {
                        var connectionEvent = new ConnectionEvent(true, ConnectionEventType.Connecting, "Just testing connection event logging");
                        logger.AddConnectionEvent(connectionEvent);
                    },
                    () =>
                    {
                        var uiException = new Exception("Just testing UI exception");
                        logger.AddUnexpectedError(uiException);
                    },
                    () =>
                    {
                        var connectionException = new Exception("Just testing connection exception");
                        logger.AddConnectionError(ErrorReasonType.Network_NotReachable, connectionException);
                    }
                };
                    
                // Run test cases
                for (int a = 0; a < testCases.Count; ++a)
                {
                    RunTestCase(a + 1, descriptions[a], testCases[a]);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine();
            Console.WriteLine("No more test cases. Press ENTER to exit.");
            Console.ReadLine();
        }
        
        // Runs a "test case", first printing a message
        private static void RunTestCase(int nr, string msg, Action testLogic)
        {
            Console.WriteLine();
            Console.WriteLine(nr + " " + msg);
            Console.Write("Press ENTER to run the test case");
            Console.ReadLine();

            // Run the test
            testLogic.Invoke();
            
            // Increment the nr
            ++nr;
        }

        private static string AppBaseFolder
        {
            get
            {
                return AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            }
        }
    }
}
