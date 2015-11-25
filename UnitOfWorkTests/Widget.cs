using FluentNHibernate.Data;
using FluentNHibernate.Mapping;

namespace UnitOfWorkTests
{
    public class Widget : Entity
    {
        /// <summary>
        /// A brief description of the good/service offered for sale.
        /// </summary>
        public virtual string Title { get; set; }
    }

    public class WidgetMap : ClassMap<Widget>
    {
        public WidgetMap()
        {
            Id(x => x.Id);
            Map(x => x.Title).CustomType("AnsiString");
        }
    }
}
