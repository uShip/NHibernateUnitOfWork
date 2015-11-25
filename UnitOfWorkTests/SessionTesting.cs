using System;
using System.Data;
using NHibernate;
using NSubstitute;
using NUnit.Framework;
using uShip.NHibnernate.UnitOfWork;

namespace UnitOfWorkTests
{
    [TestFixture]
    class SessionTesting
    {
        [Test]
        [Ignore("Bad Session testing story example.")]
        public void SessionTestDouble()
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
            int auctionsCreatedInPastWeekCount = int.MinValue;;
            sessionFactory.UnitOfWork(s =>
            {
                auctionsCreatedInPastWeekCount = s.QueryOver<Auction>()
                    .Where(auction =>
                        auction.SellerName == "Bob Smith"
                        && auction.CreatedUTC > DateTime.Now.AddDays(-7))
                    .ToRowCountQuery()
                    .RowCount();
            });

            // Assert
            Assert.Greater(auctionsCreatedInPastWeekCount, 0);
        }
    }
}
