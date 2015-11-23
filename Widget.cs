using FluentNHibernate.Data;
using FluentNHibernate.Mapping;

namespace UOW
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
