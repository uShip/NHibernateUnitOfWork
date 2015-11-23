using System.IO;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;

namespace UOW
{
    class Config
    {
        public static FluentConfiguration Database { get { return SqliteDatabase; } }

        #region MS SQL Server
        private const string MsSqlDbConnectionString = 
            @"Server=localhost\SQLEXPRESS2;Database=UnitOfWorkTest;Trusted_Connection=True;";

        protected static FluentConfiguration MsSqlDatabase
        {
            get
            {
                return Fluently.Configure()
                    .Database(MsSqlConfiguration
                        .MsSql2012
                        .ConnectionString(MsSqlDbConnectionString)
                        .ShowSql()
                        .FormatSql())
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
