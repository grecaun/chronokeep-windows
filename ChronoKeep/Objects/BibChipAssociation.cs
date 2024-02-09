using System;

namespace Chronokeep
{
    public class BibChipAssociation : IEquatable<BibChipAssociation>, IComparable<BibChipAssociation>
    {
        public BibChipAssociation()
        {
            this.EventId = -1;
            this.Bib = Constants.Timing.CHIPREAD_DUMMYBIB;
            this.Chip = Constants.Timing.CHIPREAD_DUMMYCHIP;
        }

        public int EventId { get; set; }
        public string Bib { get; set; }
        public string Chip { get; set; }

        public int CompareTo(BibChipAssociation other)
        {
            if (other == null) return 1;
            else if (this.EventId == other.EventId)
            {
                int bibOne, bibTwo;
                if (int.TryParse(this.Bib, out bibOne) && int.TryParse(other.Bib, out bibTwo))
                {
                    return bibOne.CompareTo(bibTwo);
                }
                return this.Bib.CompareTo(other.Bib);
            }
            return this.EventId.CompareTo(other.EventId);
        }

        public bool Equals(BibChipAssociation other)
        {
            if (other == null) return false;
            return this.EventId == other.EventId && this.Bib.Equals(other.Bib, StringComparison.OrdinalIgnoreCase);
        }
    }
}
