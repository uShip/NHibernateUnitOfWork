using NHibernate;

namespace UOW
{
    public interface ISessionFactory
    {
        ISession OpenSession();
    }

    public class DatabaseSessionFactory : ISessionFactory
    {
        private readonly NHibernate.ISessionFactory _sessionFactory;

        public DatabaseSessionFactory()
        {
            _sessionFactory = Config.Database.BuildSessionFactory();
        }

        public ISession OpenSession()
        {
            return _sessionFactory.OpenSession();
        }
    }
}
