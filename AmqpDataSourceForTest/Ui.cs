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
    /// User interface class.
    /// </summary>
    class Ui
    {
        // This class was implemented to reduce the nr of direct depedencies to the Console class

        private const string PromptEnd = " > ";


        /// <summary>
        /// Constructor.
        /// </summary>
        public Ui()
        {
            // Empty ctor body
        }

        /// <summary>
        /// Reads a line.
        /// </summary>
        /// <returns>Line.</returns>
        public string ReadLine()
        {
            Console.Write(PromptEnd);
            return Console.ReadLine();
        }
        
        /// <summary>
        /// Prints a line.
        /// </summary>
        public void PrintLine()
        {
            PrintLine("");
        }

        /// <summary>
        /// Prints a line.
        /// </summary>
        /// <param name="msg">Message.</param>
        public void PrintLine(string msg)
        {
            // This function was implemented to reduce the nr of direct depedencies to Console.WriteLine
            Console.WriteLine(msg);
        }

        /// <summary>
        /// Reads password input.
        /// </summary>
        /// <param name="prompt">Prompt to be shown.</param>
        /// <returns>The value read.</returns>
        public string ReadPassword(string prompt)
        {
            Console.Write(" " + prompt + PromptEnd);
            
            string input = "";
            bool done = false;

            // Reading input character by character and displaying '*' for each char.
            while (!done)
            {
                // Getting the next key from the console.
                // intercept = true -> do not display the character.
                var key = Console.ReadKey(intercept: true);

                switch (key.Key)
                {
                    case ConsoleKey.Enter:

                        // Enter pressed -> done
                        done = true;
                        break;

                    case ConsoleKey.Backspace:

                        if (input.Length > 0)
                        {
                            // Backspace -> remove last character from input and the console
                            input = input.Substring(0, input.Length - 1);
                            Console.Write("\b \b");
                        }

                        break;

                    default:

                        // Store the character, display the mask character
                        input += key.KeyChar;
                        Console.Write("*");
                        break;
                }
            }

            return input;
        }
    }
}
