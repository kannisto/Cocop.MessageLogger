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
using EntFw = System.Data.Entity;
using Lite = System.Data.SQLite;

namespace CocopMessageLogger
{
    /// <summary>
    /// The Entity Framework context class.
    /// </summary>
    class DbMetadataContext : EntFw.DbContext
    {
        // This application was designed for the following SQLite libs:
        // System.Data.SQLite 1.0.112.0
        // System.Data.SQLite.EF6 1.0.112.0

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="exeFolder">The folder of the current executable.</param>
        public DbMetadataContext(string exeFolder)
            : base(
                  // The connection object is disposable. Because contextOwnsConnection is true,
                  // the context object will take care of the required disposal.
                  existingConnection: GetSqLiteConnection(exeFolder),
                  contextOwnsConnection: true
                  )
        {
            // Empty ctor body
        }

        private static Lite.SQLiteConnection GetSqLiteConnection(string exeFolder)
        {
            var connStringBuilder = new Lite.SQLiteConnectionStringBuilder()
            {
                DataSource = exeFolder + "\\Metadata.db",
                ForeignKeys = true
            };
            
            return new Lite.SQLiteConnection()
            {
                ConnectionString = connStringBuilder.ConnectionString
            };
        }
        
        protected override void OnModelCreating(EntFw.DbModelBuilder modelBuilder)
        {
            // Use singular table names
            modelBuilder.Conventions.Remove<EntFw.ModelConfiguration.Conventions.PluralizingTableNameConvention>();
        }

        /// <summary>
        /// This maps to the metadata items in the database.
        /// </summary>
        public EntFw.DbSet<DbMetadataItem> DbMetadataItem
        {
            // The corresponding table name will be "DbMetadataItem" after the name of
            // the class, as PluralizingTableNameConvention has been removed.
            get; set;
        }
    }

    /// <summary>
    /// Message metadata for Entity Framework.
    /// </summary>
    class DbMetadataItem
    {
        /*
            The database schema is:

            CREATE TABLE DbMetadataItem (
            DbMetadataItemId integer primary key autoincrement unique,
            Host varchar not null,
            Exchange varchar not null,
            Topic varchar not null,
            Name varchar,
            PayloadSummary varchar,
            ReceivedAt integer not null,
            PayloadType varchar not null,
            Filepath varchar not null
            );
        */
        // The utilised version of SQLite tools: sqlite-tools-win32-x86-3300100

        public int DbMetadataItemId
        { get; set; }

        public string Host
        { get; set; }

        public string Exchange
        { get; set; }

        public string Topic
        { get; set; }

        public string Name
        { get; set; }

        public string PayloadSummary
        { get; set; }

        public long ReceivedAt
        { get; set; }

        public string PayloadType
        { get; set; }

        public string Filepath
        { get; set; }
    }
}
