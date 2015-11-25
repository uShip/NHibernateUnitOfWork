using System;
using FluentNHibernate.Data;
using FluentNHibernate.Mapping;

namespace UnitOfWorkTests
{
    /// <summary>
    /// An invitation to bid on a good or service.
    /// </summary>
    public class Auction : Entity
    {
        /// <summary>
        /// A brief description of the good/service offered for sale.
        /// </summary>
        public virtual String Title { get; set; }

        /// <summary>
        /// The date and time at which the auction was created, in UTC.
        /// </summary>
        public virtual DateTime CreatedUTC { get; set; }

        /// <summary>
        /// The name of the seller, the person who initiated this auction.
        /// </summary>
        public virtual String SellerName { get; set; }
    }

    public class AuctionMap : ClassMap<Auction>
    {
        public AuctionMap()
        {
            Id(x => x.Id);
            Map(x => x.Title).Unique();
            Map(x => x.CreatedUTC);
            Map(x => x.SellerName);
        }
    }
}
