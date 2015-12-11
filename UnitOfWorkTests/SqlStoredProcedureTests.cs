using System;
using NHibernate;
using NUnit.Framework;
using uShip.NHibnernate.UnitOfWork;

namespace UnitOfWorkTests
{
    [TestFixture]
    public class SqlStoredProcedureTests
    {
        private ISessionFactory _sessionFactory;

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            _sessionFactory = SessionFactory.Instance;
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
        }

        [TestCase("a")]
        [TestCase("ab")]
        [TestCase("#a")]
        [TestCase("_a")]
        [TestCase("@a")]
        [TestCase("a@")]
        [TestCase("a$")]
        [TestCase("a#")]
        [TestCase("a4")]
        [TestCase(@"DOMAIN\username.stored_proc_name")]
        public void EnsureSafeSqlIdentifier_Safe(string identifier)
        {
            Assert.DoesNotThrow(() =>
                SqlStoredProcedure.EnsureSafeSqlIdentifier(identifier));
        }

        [TestCase("*")]
        [TestCase("a*")]
        [TestCase("4a")]
        [TestCase("a b")]
        [TestCase("a\tb")]
        [TestCase("a\nb")]
        public void EnsureSafeSqlIdentifier_Unsafe(string identifier)
        {
            Assert.Throws<ArgumentException>(() =>
                SqlStoredProcedure.EnsureSafeSqlIdentifier(identifier));
        }

        [Test]
        public void StoredProcCommandText_multiple_parameters()
        {
            var actual = SqlStoredProcedure.StoredProcCommandText(
                "StoredProc", 
                new[] { "a", "b", "c" });

            Assert.AreEqual("EXEC StoredProc :a, :b, :c", actual);
        }

        [Test]
        public void StoredProcCommandText_zero_parameters()
        {
            var actual = SqlStoredProcedure.StoredProcCommandText(
                "StoredProc",
                new string[0]);

            Assert.AreEqual("EXEC StoredProc", actual);
        }

        [Test]
        public void StoredProcCommandText_one_parameter()
        {
            var actual = SqlStoredProcedure.StoredProcCommandText(
                "StoredProc",
                new[] { "a"});

            Assert.AreEqual("EXEC StoredProc :a", actual);
        }

        [Test]
        public void Constructor_throws_when_session_is_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new SqlStoredProcedure(null, "storedProcName"));
        }

        [TestCase((string)null)]
        [TestCase("")]
        [TestCase(" \t\r\n")]
        [TestCase("*a")]
        public void Constructor_throws_when_stored_proc_name_is_invalid(string storedProcName)
        {
            using (var session = _sessionFactory.OpenSession())
            {
                Assert.Throws<ArgumentException>(() =>
                    new SqlStoredProcedure(session, storedProcName));
            }
        }

        public class Table
        {
            public string Name { get; set; }
        }

        [Test]
        public void SqlStoredProcedure_ListResult()
        {
            // Arrange
            using (var session = _sessionFactory.OpenSession())
            {
                session.CreateSQLQuery(@"
                    if object_id('SqlStoredProcedureTests', 'P') is not null begin
	                    drop procedure SqlStoredProcedureTests
	                    end
                    ")
                     .ExecuteUpdate();
                session.CreateSQLQuery(@"
                    create procedure SqlStoredProcedureTests as
	                    select Name from master.sys.tables
                    ")
                     .ExecuteUpdate();

                // Act
                var results = session
                    .SqlStoredProcedure("SqlStoredProcedureTests")
                    .ListResult<Table>();

                // Assert
                Assert.Greater(results.Count, 0);
            }
        }

        [Test]
        public void SqlStoredProcedure_UniqueResult()
        {
            // Arrange
            using (var session = _sessionFactory.OpenSession())
            {
                session.CreateSQLQuery(@"
                    if object_id('SqlStoredProcedureTests', 'P') is not null begin
	                    drop procedure SqlStoredProcedureTests
	                    end
                    ")
                    .ExecuteUpdate();
                session.CreateSQLQuery(@"
                    create procedure SqlStoredProcedureTests 
                        @Name nvarchar(max)
                    as
	                    select @Name as [Name]
                    ")
                    .ExecuteUpdate();

                // Act
                var result = session
                    .SqlStoredProcedure("SqlStoredProcedureTests")
                    .SetParameter("Name", "Bob")
                    .UniqueResult<Table>();

                // Assert
                Assert.NotNull(result);
                Assert.AreEqual("Bob", result.Name);
            }
        }
    }
}
