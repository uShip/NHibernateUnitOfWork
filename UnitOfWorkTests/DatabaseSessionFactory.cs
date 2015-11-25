using NHibernate;

namespace UnitOfWorkTests
{
    public static class SessionFactory
    {
        private static ISessionFactory _sessionFactory;
        public static ISessionFactory Instance
        {
            get
            {
                if (null == _sessionFactory)
                {
                    _sessionFactory = Config.Database.BuildSessionFactory();
                }

                return _sessionFactory;
            }
        }
    }
}
