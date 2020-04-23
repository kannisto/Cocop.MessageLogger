//
// Please make sure to read and understand README.md and LICENSE.txt.
// 
// This file was prepared in the research project COCOP (Coordinating
// Optimisation of Complex Industrial Processes).
// https://cocop-spire.eu/
//
// Author: Petri Kannisto, Tampere University, Finland
// File created: 10/2019
// Last modified: 3/2020

using System;
using SysIo = System.IO;

namespace CocopMessageLogger
{
    /// <summary>
    /// Logs information to the file system.
    /// </summary>
    class Logger
    {
        // TODO-later: implement cleanup functionality not to fill the hard disk
        
        // Folder structure:
        //-Log
        //  -(a folder for each host)
        //    -(a folder for each exchange name)
        //      -Connection
        //        -(a file for each day)
        //      -Exceptions
        //        -(a file for each day)
        //      -Messages
        //        -(a folder for each topic)
        //          -(a folder for each day)
        //            -(a file for each message)

        private const string MessagesSubfolder = "Messages";

        private readonly System.Text.Encoding m_encoding = System.Text.Encoding.UTF8;
        
        // This lock is for the event log
        private readonly object m_eventLogLock = new object();

        // This lock is for the exception log
        private readonly object m_exceptionLogLock = new object();

        private readonly string m_exeFolder;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="exeFolder">The folder of the executable. The log folder will be located under it.</param>
        public Logger(string exeFolder)
        {
            m_exeFolder = exeFolder;
            
            // Resolving folder path
            LogsRootFolder = string.Format("{0}\\Log", exeFolder);

            // Creating the folder recursively if it does not yet exist
            SysIo.Directory.CreateDirectory(LogsRootFolder);
        }


        #region Public members

        /// <summary>
        /// Returns the path of the folder where messages are stored.
        /// </summary>
        public string LogsRootFolder
        { get; } // Read only -> no thread sync required

        /// <summary>
        /// Adds an unexpected error to the log.
        /// </summary>
        /// <param name="ex">Exception.</param>
        public void AddUnexpectedError(Exception ex)
        {
            AddExceptionToLog(ex); // No exceptions are leaked from logger
        }

        /// <summary>
        /// Adds a connection event to the log.
        /// </summary>
        /// <param name="ev">Event.</param>
        public void AddConnectionEvent(ConnectionEvent ev)
        {
            try
            {
                AddConnectionEvent(ev.Description); // throws InvalidOperationException
            }
            catch { } // No can do
        }
        
        /// <summary>
        /// Adds a connection-related error to the log.
        /// </summary>
        /// <param name="reason">Suspected reason.</param>
        /// <param name="ex">Exception.</param>
        public void AddConnectionError(ErrorReasonType reason, Exception ex)
        {
            // 1) Add to event log
            try
            {
                var msg = string.Format("Failed to connect, suspected reason: {0}; {1}",
                    reason.ToString(),
                    ex.Message
                    );
                AddConnectionEvent(msg); // throws InvalidOperationException
            }
            catch (Exception e)
            {
                AddExceptionToLog(e); // No exceptions are leaked from logger
            }

            // 2) Add to exception log
            AddExceptionToLog(ex); // No exceptions are leaked from logger
        }

        /// <summary>
        /// Adds a received message to the log.
        /// </summary>
        /// <param name="host">Host.</param>
        /// <param name="exc">Exchange.</param>
        /// <param name="topic">Topic that delivered the message.</param>
        /// <param name="msg">Message body.</param>
        /// <returns>The identifier of the message.</returns>
        public void AddReceivedMessage(string host, string exc, string topic, byte[] msg)
        {
            var messageTime = DateTime.Now;
            var messageTimeUtc = messageTime.ToUniversalTime();

            try
            {
                // Building folder path
                var folderpath = string.Format("{0}\\{1}\\{2}",
                    GetFolderForCurrentExchange(host, exc, "Messages"), // throws InvalidOperationException
                    TransformStringForFilename(topic),
                    GetTodayForFilename()
                    );

                // Creating the folder recursively if it does not yet exist
                SysIo.Directory.CreateDirectory(folderpath);

                // Extracting additional metadata from the message body
                var metadataExtractor = MetadataExtractor.Build(msg);

                // Specifying file extension
                var fileExtension = metadataExtractor.ContentType == ContentTypeType.Xml ? "xml" : "txt";

                var filepath = string.Format("{0}\\{1}_{2}.{3}",
                    folderpath,
                    messageTime.ToString("HHmmss_fff"),
                    TransformStringForFilename(metadataExtractor.Name),
                    fileExtension
                    );
                
                // No thread sync here, because this file receives only this single write
                SysIo.File.WriteAllBytes(filepath, msg);

                // Saving metadata
                var metadata = new Metadata(host: host, exc: exc, topic: topic, recvAt: messageTimeUtc,
                    name: metadataExtractor.Name, payload: metadataExtractor.PayloadSummary,
                    paylType: metadataExtractor.PayloadType, path: filepath);

                using (var metadataMgr = new MetadataManager(m_exeFolder))
                {
                    metadataMgr.Add(metadata);
                }
            }
            catch (Exception e)
            {
                AddExceptionToLog(e); // No exceptions are leaked from logger
            }
        }

        #endregion Public members


        #region Private members

        private void AddExceptionToLog(Exception ex)
        {
            var dir = GetSubfolderOfLog("Exceptions");
            var filepath = string.Format("{0}\\{1}.txt", dir, GetTodayForFilename());

            var msg = string.Format("At {0}:{1}{2}{1}{1}",
                    DateTime.Now.ToString(),
                    Environment.NewLine,
                    ex.ToString());

            try
            {
                lock (m_exceptionLogLock)
                {
                    SysIo.File.AppendAllText(filepath, msg, m_encoding);
                }
            }
            catch { }
        }

        private void AddConnectionEvent(string eventString)
        {
            var dir = GetSubfolderOfLog("Connection");
            var filepath = string.Format("{0}\\{1}.txt", dir, GetTodayForFilename());

            var msg = string.Format("{0} {1}{2}",
                DateTime.Now.ToString("HH':'mm':'ss'.'f", System.Globalization.CultureInfo.InvariantCulture),
                eventString,
                Environment.NewLine
                );

            lock (m_eventLogLock)
            {
                SysIo.File.AppendAllText(filepath, msg, m_encoding);
            }
        }

        private string GetFolderForCurrentExchange(string host, string exc, string subFolder)
        {
            // Adding host, exchange and the subfolder after the logs root
            var subDir = string.Format("{0}\\{1}\\{2}",
                TransformStringForFilename(host),
                TransformStringForFilename(exc),
                subFolder);
            var dir = GetSubfolderOfLog(subDir);
                
            // Creating the folder recursively if it does not yet exist
            SysIo.Directory.CreateDirectory(dir);

            return dir;
        }

        private string GetSubfolderOfLog(string subPath)
        {
            var dir = LogsRootFolder + "\\" + subPath;

            // Creating the folder recursively if it does not yet exist
            SysIo.Directory.CreateDirectory(dir);

            return dir;
        }

        private string TransformStringForFilename(string strIn)
        {
            // Limit the length of "short info" not to make the filename too long
            const int MaxLen = 30;
            var shortInfo = strIn.Length > MaxLen ? strIn.Substring(0, MaxLen) : strIn;

            var stringBuilder = new System.Text.StringBuilder();

            foreach (var c in strIn)
            {
                // Replace illegal chars with '_'
                if (!char.IsDigit(c) && !char.IsLetter(c) && c != '-')
                {
                    stringBuilder.Append('_');
                }
                else
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString();
        }

        private string GetTodayForFilename()
        {
            return DateTime.Now.ToString("yyyyMMdd");
        }

        #endregion Private members
    }
}
