using System.IO;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;

namespace UOW
{
    class Config
    {
        public static FluentConfiguration Database { get { return SqliteDatabase; } }

        #region MS SQL Server
        private static readonly string MsSqlDbConnectionString =
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
        private static readonly DirectoryInfo DbDataFileDir = 
            new DirectoryInfo(@"C:\Temp");
        private const string DbDataFileName = 
            @"UnitOfWork.sqlite";
        private static readonly FileInfo DbDataFile = new FileInfo(Path.Combine(
            DbDataFileDir.FullName,
            DbDataFileName));
        public static readonly string SqliteDbConnectionString = string.Format(
            @"Data Source={0}; Version=3;",
            DbDataFile.FullName);

        protected static FluentConfiguration SqliteDatabase
        {
            get
            {
                if (!DbDataFileDir.Exists)
                {
                    DbDataFileDir.Create();
                }

                return Fluently.Configure()
                    .Database(SQLiteConfiguration
                        .Standard
                        .ConnectionString(SqliteDbConnectionString)
                        .ShowSql()
                        .FormatSql())
                    .Mappings(m => m.FluentMappings
                        .AddFromAssemblyOf<AuctionMap>());
            }
        }
        #endregion SQLite
    }
}
