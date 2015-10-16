using NUnit.Framework;
using System;
using NHibernate.Util;

namespace UOW
{
    [TestFixture]
    public class DbConnectionTests
    {
        [Test]
        public void Can_connect_to_database()
        {
            var sessionFactory = new DatabaseSessionFactory();
            using (var session = sessionFactory.OpenSession())
            {
                var actual = (int) session
                    .CreateSQLQuery("select 1")
                    .List()
                    .First();
                Assert.AreEqual(1, actual);
            }
        }
    }
}
