using NHibernate.Linq;
using NUnit.Framework;
using System;
using System.Linq;

namespace UOW
{
    [TestFixture]
    public class SelectiveQueryTest
    {
        private DatabaseSessionFactory _sessionFactory;

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            _sessionFactory = new DatabaseSessionFactory();
        }

        private const string SellerName = "Ann Smith";

        /// <summary>
        /// Returns the created auction's title;
        /// The SellerName property of the created auction is always
        /// the SellerName constant.
        /// </summary>
        private string CreateAndSaveAuctionWithRandomTitle()
        {
            var auctionTitle = string.Format(
                "{0} widgets",
                new Random().Next());

            using (var session = _sessionFactory.OpenSession())
            {
                session.Save(new Auction
                {
                    Title = auctionTitle,
                    CreatedUTC = DateTime.UtcNow,
                    SellerName = SellerName,
                });
            }

            return auctionTitle;
        }

        [Test]
        public void WhatDoesThisDo()
        {
            var auctionTitle = CreateAndSaveAuctionWithRandomTitle();

            string sellerName;
            using (var session = _sessionFactory.OpenSession())
            {
                var user = session
                    .Query<Auction>()
                    .FirstOrDefault(a => a.Title == auctionTitle);

                //select
                //    auction0_.Id as Id2_,
                //    auction0_.Title as Title2_,
                //    auction0_.CreatedUTC as CreatedUTC2_,
                //    auction0_.SellerName as SellerName2_ 
                //from
                //    [Auction] auction0_ 
                //where
                //    auction0_.Title=@p0 
                //ORDER BY
                //    CURRENT_TIMESTAMP OFFSET 0 ROWS FETCH FIRST 1 ROWS ONLY;
                //@p0 = '1435913167 widgets' [Type: String (4000)]

                sellerName = (null == user) ? null : user.SellerName;
            }

            Assert.AreEqual(SellerName, sellerName);
        }

        [Test]
        public void DoNotSelectWholeRow()
        {
            var auctionTitle = CreateAndSaveAuctionWithRandomTitle();

            string sellerName;
            using (var session = _sessionFactory.OpenSession())
            {
                sellerName = session
                    .Query<Auction>()
                    .Where(a => a.Title == auctionTitle)
                    .Select(a => a.SellerName)
                    .FirstOrDefault();

                //select
                //    auction0_.SellerName as col_0_0_ 
                //from
                //    [Auction] auction0_ 
                //where
                //    auction0_.Title=@p0 
                //ORDER BY
                //    CURRENT_TIMESTAMP OFFSET 0 ROWS FETCH FIRST 1 ROWS ONLY;
                //@p0 = '1763945328 widgets' [Type: String (4000)]
            }

            Assert.AreEqual(SellerName, sellerName);
        }
    }
}
