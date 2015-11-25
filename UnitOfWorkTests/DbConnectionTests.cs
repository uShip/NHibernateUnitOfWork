using NHibernate.Util;
using NUnit.Framework;

namespace UnitOfWorkTests
{
    [TestFixture]
    public class DbConnectionTests
    {
        [Test]
        public void Can_connect_to_database()
        {
            using (var session = SessionFactory.Instance.OpenSession())
            {
                var actual = session
                    .CreateSQLQuery("select 1")
                    .List()
                    .First();
                Assert.AreEqual(1, actual);
            }
        }
    }
}
