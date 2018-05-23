using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class BibChipAssociation : IEquatable<BibChipAssociation>, IComparable<BibChipAssociation>
    {
        public BibChipAssociation()
        {
            this.EventId = -1;
            this.Bib = -1;
            this.Chip = -1;
        }

        public int EventId { get; set; }
        public int Bib { get; set; }
        public int Chip { get; set; }

        public int CompareTo(BibChipAssociation other)
        {
            if (other == null) return 1;
            else if (this.EventId == other.EventId)
            {
                return this.Bib.CompareTo(other.Bib);
            }
            return this.EventId.CompareTo(other.EventId);
        }

        public bool Equals(BibChipAssociation other)
        {
            if (other == null) return false;
            return this.EventId == other.EventId && this.Bib == other.Bib;
        }
    }
}
