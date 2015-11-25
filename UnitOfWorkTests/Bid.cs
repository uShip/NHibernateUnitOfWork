using System;
using FluentNHibernate.Data;
using FluentNHibernate.Mapping;

namespace UnitOfWorkTests
{
    /// <summary>
    /// An offer to purchase a good/service for a specific price.
    /// </summary>
    public class Bid : Entity
    {
        /// <summary>
        /// The good/service auction on which this bid is placed.
        /// </summary>
        public virtual Auction Auction { get; set; }

        /// <summary>
        /// The amount of the bid, in US Dollars.
        /// </summary>
        public virtual decimal AmountUSD { get; set; }

        /// <summary>
        /// The name of the bidder.
        /// </summary>
        public virtual String BidderName { get; set; }
    }

    public class BidMap : ClassMap<Bid>
    {
        public BidMap()
        {
            Id(x => x.Id);
            References(x => x.Auction);
            Map(x => x.AmountUSD);
            Map(x => x.BidderName);
        }
    }
}
