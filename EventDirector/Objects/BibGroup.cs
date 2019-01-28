using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector.Objects
{
    public class BibGroup : IEquatable<BibGroup>, IComparable<BibGroup>
    {
        public BibGroup(int eventId)
        {
            this.EventId = eventId;
            this.Name = "Default";
            this.Number = Constants.Timing.DEFAULT_BIB_GROUP;
        }

        public BibGroup() { }

        public int EventId { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }

        public int CompareTo(BibGroup other)
        {
            if (other == null) return 1;
            return this.Number.CompareTo(other.Number);
        }

        public bool Equals(BibGroup other)
        {
            if (other == null) return false;
            return this.Number == other.Number && this.EventId == other.EventId;
        }
    }
}
