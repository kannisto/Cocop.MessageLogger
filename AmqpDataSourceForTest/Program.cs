//
// Please make sure to read and understand README.md and LICENSE.txt.
// 
// This file was prepared in the research project COCOP (Coordinating
// Optimisation of Complex Industrial Processes).
// https://cocop-spire.eu/
//
// Author: Petri Kannisto, Tampere University, Finland
// File created: 3/2020
// Last modified: 3/2020

using System;

namespace AmqpDataSourceForTest
{
    /// <summary>
    /// This program has been implemented to enable testing with data from AMQP.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var ui = new Ui();

            ui.PrintLine();
            ui.PrintLine("#######################");
            ui.PrintLine(" AmqpDataSourceForTest");
            ui.PrintLine("#######################");
            ui.PrintLine("Sends data to a message bus to enable the testing of message reception");
            ui.PrintLine();
            
            Run(ui, args);

            ui.PrintLine();
            ui.PrintLine("Press ENTER to exit...");
            ui.ReadLine();
        }

        private static void Run(Ui ui, string[] args)
        {
            try
            {
                // Read input arguments
                if (!HandleArgs(ui, args, out string host, out bool secure, out string user))
                {
                    // One or more arguments were invalid
                    return;
                }

                while (true)
                {
                    // Read password
                    var password = ui.ReadPassword("AMQP server password");
                    ui.PrintLine();

                    // Run the workflow
                    var workflow = new Workflow(ui: ui, host: host, sec: secure, user: user, pwd: password);

                    if (workflow.Run())
                    {
                        // User asked for exit
                        break;
                    }
                    else
                    {
                        ui.PrintLine("Failed to run. Try with another password? (y/n)");
                        var retryInput = ui.ReadLine();

                        if (retryInput.Trim().ToLower() == "n")
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Print error information
                ui.PrintLine();
                ui.PrintLine(e.ToString());
            }
        }

        private static bool HandleArgs(Ui ui, string[] args, out string host, out bool secure, out string user)
        {
            const int MaxLen = 100;
            host = "";
            secure = false;
            user = "";

            if (args.Length != 3)
            {
                ui.PrintLine("Expected arguments:");
                ui.PrintLine("<1> <2> <3>");
                ui.PrintLine(" 1 - host name");
                ui.PrintLine(" 2 - secure connection (true/false)");
                ui.PrintLine(" 3 - username");
                return false;
            }

            host = args[0];
            user = args[2];

            try
            {
                secure = bool.Parse(args[1]);
            }
            catch (FormatException)
            {
                ui.PrintLine("Arg 2: Failed to parse boolean");
                return false;
            }
            
            // Added this restriction to prevent arbitrarily long host names
            if (host.Length > MaxLen || user.Length > MaxLen)
            {
                ui.PrintLine("Argument length must not exceed " + MaxLen);
                return false;
            }
            
            return true;
        }
    }
}
