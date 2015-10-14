using HibernatingRhinos.Profiler.Appender.NHibernate;
using NHibernate;
using NHibernate.Exceptions;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using uShip.Infrastructure.Adapters;

namespace UOW
{
    [TestFixture]
    public class UnitOfWorkTests
    {
        private ISessionFactory _sessionFactory;

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            _sessionFactory = new DatabaseSessionFactory();
            NHibernateProfiler.Initialize();
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
        }

        private ISession NewSession()
        {
            return _sessionFactory.OpenSession();
        }

        private Auction LoadAuctionByTitle(string expectedTitle)
        {
            return NewSession()
                .QueryOver<Auction>()
                .Where(x => x.Title == expectedTitle)
                .SingleOrDefault();
        }

        private readonly Random _prng = new Random();
        private Auction NewAuctionWithRandomTitle()
        {
            return new Auction
            {
                Title = string.Format(
                    "One pallet of {0} awesome things",
                    _prng.Next().ToString(CultureInfo.InvariantCulture)),
                CreatedUTC = DateTime.UtcNow,
                SellerName = "Bob Loblaw",
            };
        }

        [Test]
        public void Can_start_UnitOfWork()
        {
            // ReSharper disable once ObjectCreationAsStatement
            _sessionFactory.UnitOfWork(session => { });
        }

        [Test]
        public void Should_dispose_session_and_transaction()
        {
            // Arrange
            var trans = Substitute.For<ITransaction>();
            trans.IsActive.Returns(true);

            var session = Substitute.For<ISession>();
            session.Transaction.Returns(trans);
            session
                .BeginTransaction(Arg.Any<IsolationLevel>())
                .Returns(trans);

            var sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.OpenSession().Returns(session);

            // Act
            sessionFactory.UnitOfWork(s =>
            {
                Assert.AreSame(trans, s.Transaction);
            });

            // Assert
            trans.Received(1).Dispose();
            session.Received(1).Dispose();
        }

        [Test]
        public void UsingDispose()
        {
            var session = Substitute.For<ISession>();
            using (session)
            {
            }
            session.Received(1).Dispose();
        }

        [Test]
        public void Automatic_commit()
        {
            // Arrange
            var expected = NewAuctionWithRandomTitle();

            // Act
            _sessionFactory.UnitOfWork(session =>
            {
                session.Save(expected);
                // NOTICE: no call to uow.Commit()
            });

            // Assert
            var actual = LoadAuctionByTitle(expected.Title);
            Assert.AreEqual(expected, actual);
        }

        public class BusinessException : Exception { }

        [Test]
        public void Automatic_rollback()
        {
            // Arrange
            var auction = NewAuctionWithRandomTitle();
            ITransaction trans = null;

            try
            {
                _sessionFactory.UnitOfWork(session =>
                {
                    trans = session.Transaction;
                    session.Save(auction);
                    // Act
                    throw new BusinessException();
                });

                Assert.Fail("No BusinessException was thrown.");
            }
            catch (BusinessException)
            {
                // This is the expected path.
            }

            // Assert
            //Assert.IsFalse(session.IsOpen);           // Why doesn't this work?
            Assert.IsNotNull(trans);
            Assert.IsFalse(trans.IsActive);
            Assert.IsFalse(trans.WasCommitted);
            Assert.IsNull(LoadAuctionByTitle(auction.Title));
        }

        [Test]
        public void Manual_commit()
        {
            // Arrange
            var commitCount = 0;

            var trans = Substitute.For<ITransaction>();
            trans.When(x => x.Commit()).Do(callInfo => { commitCount++; });
            trans.IsActive.Returns(callInfo => (commitCount == 0));

            var session = Substitute.For<ISession>();
            session.Transaction.Returns(trans);
            session.BeginTransaction().Returns(trans);

            var sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.OpenSession().Returns(session);

            sessionFactory.UnitOfWork(s =>
            {
                // Act
                s.Transaction.Commit();
            });

            // Assert
            trans.DidNotReceive().Rollback();
            trans.Received(1).Commit();
        }

        [Test]
        public void Manual_rollback()
        {
            // Arrange
            var rollbackCount = 0;

            var trans = Substitute.For<ITransaction>();
            trans.When(x => x.Rollback()).Do(callInfo => { rollbackCount++; });
            trans.IsActive.Returns(callInfo => (rollbackCount == 0));

            var session = Substitute.For<ISession>();
            session.Transaction.Returns(trans);
            session.BeginTransaction().Returns(trans);

            var sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.OpenSession().Returns(session);

            sessionFactory.UnitOfWork(s =>
            {
                // Act
                s.Transaction.Rollback();

                // The session is in precarious state here.  NHibernate docs
                // stipulate that, after a Rollback(), we must no longer rely
                // on the session.  That is, if you need to do more DB work
                // after a rollback, start a new session -- which means this
                // Unit Of Work is completely borked.

                // Assert
                trans.Received(1).Rollback();
            });

            trans.DidNotReceive().Commit();
            trans.Received(1).Rollback();
        }

        [Test]
        public void Cannot_open_a_concurrent_transaction()
        {
            // Arrange
            _sessionFactory.UnitOfWork(session =>
            {
                // Act and Assert
                var t1 = session.Transaction;
                var t2 = session.BeginTransaction();
                Assert.AreSame(t1, t2);
            });
        }

        private enum MsSqlServerErrorCode
        {
            UniqueConstraintViolation = 2627,
        }

        private static void AssertAreEqual(
            MsSqlServerErrorCode expected,
            GenericADOException exc)
        {
            Assert.IsNotNull(exc);

            var sqlExc = exc.InnerException as SqlException;
            Assert.IsNotNull(sqlExc);

            for (int i = 0; i < sqlExc.Errors.Count; i++)
            {
                if (sqlExc.Errors[i].Number == (int)expected)
                {
                    return;
                }
            }

            Assert.Fail("SQL Error Number {0} was not among the errors", expected);
        }

        [Test]
        public void SQL_error_in_transaction()
        {
            // Arrange
            var auction1 = NewAuctionWithRandomTitle();
            var auction2 = NewAuctionWithRandomTitle();
            auction2.Title = auction1.Title; // unique constraint violation ahead!

            ITransaction trans = null;
            try
            {
                _sessionFactory.UnitOfWork(session =>
                {
                    trans = session.Transaction;
                    session.Save(auction1);

                    // Act
                    session.Save(auction2); // SqlException is thrown here
                });

                // Assert
                Assert.Fail("GenericADOException was not thrown.");
            }
            catch (GenericADOException exc)
            {
                Assert.IsTrue(exc.InnerException.Message.Contains("UNIQUE constraint failed: Auction.Title"));
            }

            //Assert.IsFalse(session.IsOpen);           // Why doesn't this work?
            Assert.IsNotNull(trans);
            Assert.IsFalse(trans.IsActive);
            Assert.IsFalse(trans.WasCommitted);
            Assert.IsTrue(trans.WasRolledBack);
            Assert.IsNull(LoadAuctionByTitle(auction1.Title));
            Assert.IsNull(LoadAuctionByTitle(auction2.Title));
        }

        internal class MidCommitException : Exception { }

        [Test]
        public void Exception_thrown_during_commit()
        {
            // Arrange
            var sessionFactory = Substitute.For<ISessionFactory>();
            var session = Substitute.For<ISession>();
            var trans = Substitute.For<ITransaction>();

            sessionFactory.OpenSession().Returns(session);

            session.BeginTransaction(default(IsolationLevel))
                .ReturnsForAnyArgs(trans);
            session.Transaction.Returns(trans);

            trans.IsActive.Returns(true);
            trans.WasRolledBack.Returns(false);
            trans.When(x => x.Commit())
                .Do(x => { throw new MidCommitException(); });

            // Act
            try
            {
                sessionFactory.UnitOfWork(s => { });
                Assert.Fail(
                    "MidCommitException was not thrown, but should have been");
            }
            catch (MidCommitException)
            {
                // expected exception
            }

            // Assert
            trans.Received(1).Commit();
            trans.Received(1).Rollback();
        }

        [Test]
        public void Exception_thrown_during_rollback()
        {
            // Arrange
            var sessionFactory = Substitute.For<ISessionFactory>();
            var session = Substitute.For<ISession>();
            var trans = Substitute.For<ITransaction>();

            sessionFactory.OpenSession().Returns(session);

            session.BeginTransaction(default(IsolationLevel))
                .ReturnsForAnyArgs(trans);
            session.Transaction.Returns(trans);

            trans.IsActive.Returns(true);
            trans.WasRolledBack.Returns(false);
            var exc2 = new MidCommitException();
            trans.When(x => x.Rollback()).Do(x => { throw exc2; });

            var exc1 = new BusinessException();

            // Act
            try
            {
                sessionFactory.UnitOfWork(s =>
                {
                    throw exc1;
                });

                Assert.Fail(
                    "CompoundException was not thrown, but should have been");
            }
            catch (CompoundException exc)
            {
                // Assert
                Assert.AreSame(exc1, exc.Exceptions[0]);
                Assert.AreSame(exc2, exc.Exceptions[1]);
            }
        }

        [Test]
        public void Automatic_begin_transaction()
        {
            _sessionFactory.UnitOfWork(session =>
            {
                Assert.IsTrue(session.IsOpen);
                Assert.IsTrue(session.Transaction.IsActive);
            });
        }

        [Test]
        public void Specify_transaction_isolation_level()
        {
            // Arrange
            var trans = Substitute.For<ITransaction>();
            trans.IsActive.Returns(callInfo => true);

            var session = Substitute.For<ISession>();
            session.Transaction.Returns(trans);
            session.BeginTransaction().Returns(trans);

            var sessionFactory = Substitute.For<ISessionFactory>();
            sessionFactory.OpenSession().Returns(session);

            // Act
            sessionFactory.UnitOfWork(IsolationLevel.ReadUncommitted, s => { });

            // Assert
            session.Received(1).BeginTransaction(IsolationLevel.ReadUncommitted);
        }

        [Test]
        public void Can_open_serial_transactions()
        {
            // Arrange
            var auction1 = NewAuctionWithRandomTitle();
            var auction2 = NewAuctionWithRandomTitle();

            _sessionFactory.UnitOfWork(s =>
            {
                var t1 = s.Transaction;

                // Act
                s.Save(auction1);
                s.Transaction.Commit();
                var t2 = s.BeginTransaction();
                s.Save(auction2);

                // Assert
                Assert.AreSame(t2, s.Transaction);
                Assert.AreNotSame(t1, t2);
            });

            Assert.AreEqual(auction1, LoadAuctionByTitle(auction1.Title));
            Assert.AreEqual(auction2, LoadAuctionByTitle(auction2.Title));
        }

        [Test]
        public void Manual_commit_integration_test()
        {
            // Arrange
            var auction = NewAuctionWithRandomTitle();

            try
            {
                _sessionFactory.UnitOfWork(s =>
                {
                    // Act
                    s.Save(auction);
                    s.Transaction.Commit();
                    throw new BusinessException();
                });

                Assert.Fail("BusinessException was not thrown.");
            }
            catch (BusinessException)
            {
                // expected exception
            }

            // Assert
            Assert.AreEqual(auction, LoadAuctionByTitle(auction.Title));
        }

        [Test]
        public void Manual_rollback_integration_test()
        {
            // Arrange
            var auction = NewAuctionWithRandomTitle();

            _sessionFactory.UnitOfWork(s =>
            {
                // Act
                s.Save(auction);
                s.Transaction.Rollback();
            });

            // Assert
            Assert.IsNull(LoadAuctionByTitle(auction.Title));
        }

        [Test]
        public void Crossing_session_outside_to_inside()
        {
            // Arrange
            var auctionInMemory = NewAuctionWithRandomTitle();
            var modifiedTitle = auctionInMemory.Title + " MODIFIED";
            object auctionId;

            Auction auctionFromSession1;
            using (var session1 = _sessionFactory.OpenSession())
            using (var trans1 = session1.BeginTransaction())
            {
                auctionId = session1.Save(auctionInMemory);
                auctionFromSession1 = session1.Get<Auction>(auctionId);

                _sessionFactory.UnitOfWork(session2 =>
                {
                    // Act
                    auctionFromSession1.Title = modifiedTitle;
                    session2.Update(auctionFromSession1);
                });
            }

            // Assert
            Assert.AreEqual(1, NewSession().QueryOver<Auction>().RowCount()); // wrong!
            Assert.AreEqual(auctionInMemory, LoadAuctionByTitle(modifiedTitle));
            Assert.Fail("Finish this test.  It should throw in the UoW block.");
        }

        [Test]
        public void Crossing_sessions_inside_to_outside()
        {
            // Arrange
            var auctionInMemory = NewAuctionWithRandomTitle();
            var modifiedTitle = auctionInMemory.Title + " MODIFIED";
            object auctionId;
            Auction auctionFromSession2 = null;

            using (var session1 = _sessionFactory.OpenSession())
            using (var trans1 = session1.BeginTransaction())
            {
                _sessionFactory.UnitOfWork(session2 =>
                {
                    // Act
                    auctionInMemory.Title = modifiedTitle;
                    auctionId = session2.Save(auctionInMemory);
                    auctionFromSession2 = session2.Get<Auction>(auctionId);

                });

                auctionFromSession2.Title = modifiedTitle;
                session1.Update(auctionFromSession2);
            }

            // Assert
            Assert.AreEqual(1, NewSession().QueryOver<Auction>().RowCount()); // wrong!
            Assert.AreEqual(auctionInMemory, LoadAuctionByTitle(modifiedTitle));
            Assert.Fail("Finish this test.  It should throw in the inner using block.");
        }
    }
}