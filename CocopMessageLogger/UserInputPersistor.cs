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
using XmlSer = System.Xml.Serialization;

namespace CocopMessageLogger
{
    /// <summary>
    /// This class persists user input between application runs.
    /// </summary>
    public class UserInputPersistor
    {
        /// <summary>
        /// Loads saved user input from disk. If no data has been saved, default values are returned.
        /// </summary>
        /// <returns>Persisted user input or default values.</returns>
        public static UserInputPersistor Load()
        {
            try
            {
                var serializer = new XmlSer.XmlSerializer(typeof(UserInputPersistor));

                using (var reader = new System.IO.StreamReader(GetFilepath()))
                {
                    return (UserInputPersistor)serializer.Deserialize(reader);
                }
            }
            catch
            {
                // Silent failure, because persisting is not very important
                return new UserInputPersistor();
            }
        }

        /// <summary>
        /// Saves user input to disk.
        /// </summary>
        /// <param name="persistor">Persistor object.</param>
        public static void Save(UserInputPersistor persistor)
        {
            try
            {
                var serializer = new XmlSer.XmlSerializer(typeof(UserInputPersistor));

                using (var writer = new System.IO.StreamWriter(GetFilepath()))
                {
                    serializer.Serialize(writer, persistor);
                }
            }
            catch { } // Silent failure, because persisting is not very important
        }

        private static string GetFilepath()
        {
            return AppDomain.CurrentDomain.BaseDirectory + "userinput.xml";
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserInputPersistor()
        {
            // Setting defaults
            Host = "localhost";
            Exchange = "my-exchange";
            TopicPattern = "#";
            Username = "";
            IsSecureConnection = false;
        }

        [XmlSer.XmlElement()]
        public string Host
        {
            get; set;
        }

        [XmlSer.XmlElement()]
        public string Exchange
        {
            get; set;
        }

        [XmlSer.XmlElement()]
        public string TopicPattern
        {
            get; set;
        }

        [XmlSer.XmlElement()]
        public string Username
        {
            get; set;
        }

        [XmlSer.XmlElement()]
        public bool IsSecureConnection
        {
            get; set;
        }
    }
}
