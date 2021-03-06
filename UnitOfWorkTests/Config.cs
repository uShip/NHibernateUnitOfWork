﻿using System.Configuration;
using System.IO;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;

namespace UnitOfWorkTests
{
    class Config
    {
        public static FluentConfiguration Database { get { return MsSqlDatabase; } }

        public static bool ShowSql
        {
            get
            {
                bool showSql;
                var appSetting = ConfigurationManager.AppSettings["NHibernate.ShowSql"];
                return (null != appSetting)
                    && bool.TryParse(appSetting, out showSql)
                    && showSql;
            }
        }

        #region MS SQL Server
        private static readonly ConnectionStringSettings MsSqlDbConnectionString =
            ConfigurationManager.ConnectionStrings["UnitOfWorkTest"];

        protected static FluentConfiguration MsSqlDatabase
        {
            get
            {
                var msSqlConfiguration = MsSqlConfiguration
                    .MsSql2012
                    .ConnectionString(MsSqlDbConnectionString.ConnectionString);

                if (ShowSql)
                {
                    msSqlConfiguration
                        .ShowSql()
                        .FormatSql();
                }

                return Fluently.Configure()
                    .Database(msSqlConfiguration)
                    .Mappings(m => m.FluentMappings
                        .AddFromAssemblyOf<AuctionMap>());
            }
        }
        #endregion MS SQL Server

        #region SQLite
        private static readonly FileInfo DbDataFile = new FileInfo(Path.GetTempFileName());
        private static readonly string SqliteFileDbConnectionString = string.Format(
            @"Data Source={0}; Version=3;",
            DbDataFile.FullName);

        private const string SqliteMemoryDbConnectionString = 
            @"Data Source=:memory:; Version=3; New=True;";

        protected static FluentConfiguration SqliteDatabase
        {
            get
            {
                return Fluently.Configure()
                    .Database(SQLiteConfiguration
                        .Standard
                        .ConnectionString(SqliteMemoryDbConnectionString)
                        .ShowSql()
                        .FormatSql())
                    .Mappings(m => m.FluentMappings
                        .AddFromAssemblyOf<AuctionMap>());
            }
        }
        #endregion SQLite
    }
}
