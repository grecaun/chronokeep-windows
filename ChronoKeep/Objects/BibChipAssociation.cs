using System;

namespace Chronokeep.Objects
{
    public class BibChipAssociation : IEquatable<BibChipAssociation>, IComparable<BibChipAssociation>
    {
        public BibChipAssociation()
        {
            EventId = -1;
            Bib = Constants.Timing.CHIPREAD_DUMMYBIB;
            Chip = Constants.Timing.CHIPREAD_DUMMYCHIP;
        }

        public int EventId { get; set; }
        public string Bib { get; set; }
        public string Chip { get; set; }

        public int CompareTo(BibChipAssociation other)
        {
            if (other == null) return 1;
            else if (EventId == other.EventId)
            {
                int bibOne, bibTwo;
                if (int.TryParse(Bib, out bibOne) && int.TryParse(other.Bib, out bibTwo))
                {
                    return bibOne.CompareTo(bibTwo);
                }
                return Bib.CompareTo(other.Bib);
            }
            return EventId.CompareTo(other.EventId);
        }

        public bool Equals(BibChipAssociation other)
        {
            if (other == null) return false;
            return EventId == other.EventId && Bib.Equals(other.Bib, StringComparison.OrdinalIgnoreCase);
        }

        public void TrimFields()
        {
            Bib = Bib.Trim();
            Chip = Chip.Trim();
        }
    }
}
