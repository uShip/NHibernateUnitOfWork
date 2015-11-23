using HibernatingRhinos.Profiler.Appender.NHibernate;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace UOW
{
    public static class QueryableExtensions
    {
        public static bool Exists<TSource>(
            this IQueryable<TSource> source, 
            Expression<Func<TSource, bool>> predicate)
            where TSource : FluentNHibernate.Data.Entity
        {
            return source
                .Where(predicate)
                .Select(a => a.Id)
                .Any();
        }

        public static bool Exists<TSource>(
            this IQueryOver<TSource,TSource> source,
            Expression<Func<TSource, bool>> predicate)
            where TSource : FluentNHibernate.Data.Entity
        {
            return source
                .Where(predicate)
                .Select(a => a.Id)
                .Take(1)
                .List()
                .Any();
        }
    }

    [TestFixture]
    public class ExistenceQueryAlternatives
    {
        private ISessionFactory _sessionFactory;
        private ISession _session;

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

        [SetUp]
        public void SetUp()
        {
            _session = _sessionFactory.OpenSession();
        }

        [Test]
        public void Queryable_Any()
        {
            _session
                .Query<Auction>()
                .Any(a => a.SellerName == "Bob");

            //select
            //    auction0_.Id as Id3_,
            //    auction0_.Title as Title3_,
            //    auction0_.CreatedUTC as CreatedUTC3_,
            //    auction0_.SellerName as SellerName3_ 
            //from
            //    [Auction] auction0_ 
            //where
            //    auction0_.SellerName=@p0 
            //ORDER BY
            //    CURRENT_TIMESTAMP OFFSET 0 ROWS FETCH FIRST 1 ROWS ONLY;
            //@p0 = 'Bob' [Type: String (4000)]
        }

        [Test]
        public void QueryOver_Where_RowCount()
        {
            _session
                .QueryOver<Auction>()
                .Where(a => a.SellerName == "Bob")
                .RowCount();

            //SELECT
            //    count(*) as y0_ 
            //FROM
            //    [Auction] this_ 
            //WHERE
            //    this_.SellerName = @p0;
            //@p0 = 'Bob' [Type: String (4000)]
        }

        [Test]
        public void Queryable_Count()
        {
            _session
                .Query<Auction>()
                .Count(a => a.SellerName == "Bob");     
       
            //select
            //    cast(count(*) as INT) as col_0_0_ 
            //from
            //    [Auction] auction0_ 
            //where
            //    auction0_.SellerName=@p0;
            //@p0 = 'Bob' [Type: String (4000)]
        }

        [Test]
        public void Queryable_Any_with_trivial_projection()
        {
            _session
                .Query<Auction>()
                .Where(a => a.SellerName == "Bob")
                .Select(a => a.Id)
                .Any();

            //select
            //    auction0_.Id as col_0_0_ 
            //from
            //    [Auction] auction0_ 
            //where
            //    auction0_.SellerName=@p0 
            //ORDER BY
            //    CURRENT_TIMESTAMP OFFSET 0 ROWS FETCH FIRST 1 ROWS ONLY;
            //@p0 = 'Bob' [Type: String (4000)]
        }

        [Test]
        public void Queryable_FirstOrDefault()
        {
            _session
                .Query<Auction>()
                .FirstOrDefault(a => a.SellerName == "Bob");

            //select
            //    auction0_.Id as Id3_,
            //    auction0_.Title as Title3_,
            //    auction0_.CreatedUTC as CreatedUTC3_,
            //    auction0_.SellerName as SellerName3_ 
            //from
            //    [Auction] auction0_ 
            //where
            //    auction0_.SellerName=@p0 
            //ORDER BY
            //    CURRENT_TIMESTAMP OFFSET 0 ROWS FETCH FIRST 1 ROWS ONLY;
            //@p0 = 'Bob' [Type: String (4000)]        
        }

        [Test]
        public void QueryOver_Where_Take_Count()
        {
            _session
                .QueryOver<Auction>()
                .Where(a => a.SellerName == "Bob")
                .Take(1)
                .RowCount();  
          
            //SELECT
            //    count(*) as y0_ 
            //FROM
            //    [Auction] this_ 
            //WHERE
            //    this_.SellerName = @p0;
            //@p0 = 'Bob' [Type: String (4000)]
        }

        [Test]
        public void QueryOver_FirstOrDefault()
        {
            _session
                .QueryOver<Auction>()
                .Where(a => a.SellerName == "Bob")
                .SingleOrDefault();

            //SELECT
            //    this_.Id as Id3_0_,
            //    this_.Title as Title3_0_,
            //    this_.CreatedUTC as CreatedUTC3_0_,
            //    this_.SellerName as SellerName3_0_ 
            //FROM
            //    [Auction] this_ 
            //WHERE
            //    this_.SellerName = @p0;
            //@p0 = 'Bob' [Type: String (4000)]
        }

        [Test]
        public void QueryOver_SingleOrDefault_with_trivial_projection()
        {
            _session
                .QueryOver<Auction>()
                .Where(a => a.SellerName == "Bob")
                .Select(a => a.Id)
                .SingleOrDefault();

            //SELECT
            //    this_.Id as y0_ 
            //FROM
            //    [Auction] this_ 
            //WHERE
            //    this_.SellerName = @p0;
            //@p0 = 'Bob' [Type: String (4000)]
        }

        [Test]
        public void QueryOver_Take_1_with_trivial_projection()
        {
            _session
                .QueryOver<Auction>()
                .Where(a => a.SellerName == "Bob")
                .Select(a => a.Id)
                .Take(1)
                .List()
                .Any();

            //NHibernate: 
            //    SELECT
            //        this_.Id as y0_ 
            //    FROM
            //        [Auction] this_ 
            //    WHERE
            //        this_.SellerName = @p0 
            //    ORDER BY
            //        CURRENT_TIMESTAMP OFFSET 0 ROWS FETCH FIRST @p1 ROWS ONLY;
            //    @p0 = 'Bob' [Type: String (4000)],
            //    @p1 = 1 [Type: Int32 (0)]
        }

        [Test]
        public void Queryable_Exists()
        {
            _session
                .Query<Auction>()
                .Exists(a => a.SellerName == "Bob");

            //select
            //    auction0_.Id as col_0_0_ 
            //from
            //    [Auction] auction0_ 
            //where
            //    auction0_.SellerName=@p0 
            //ORDER BY
            //    CURRENT_TIMESTAMP OFFSET 0 ROWS FETCH FIRST 1 ROWS ONLY;
            //@p0 = 'Bob' [Type: String (4000)]
        }

        [Test]
        public void QueryOver_Exists()
        {
            _session
                .QueryOver<Auction>()
                .Exists(a => a.SellerName == "Bob");
        }
    }
}