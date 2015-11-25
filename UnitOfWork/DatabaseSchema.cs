using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

namespace UOW
{
    public static class DatabaseSchema
    {
        public static void Create()
        {
            Config.Database.ExposeConfiguration(CreateSchema)
                .BuildConfiguration();
        }

        private static void CreateSchema(Configuration config)
        {
            var schemaExport = new SchemaExport(config);
            schemaExport.Drop(false, true);
            schemaExport.Create(false, true);
        }
    }
}
