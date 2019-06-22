using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Objects
{
    public class AvailableBib : IEquatable<AvailableBib>, IComparable<AvailableBib>
    {
        public int EventId { get; set; }
        public int GroupNumber { get; set; }
        public string GroupName { get; set; }
        public int Bib { get; set; }

        public AvailableBib(int eventId, int groupNumber, string groupName, int bib)
        {
            EventId = eventId;
            GroupNumber = groupNumber;
            GroupName = groupName;
            Bib = bib;
            if (GroupNumber == Constants.Timing.DEFAULT_BIB_GROUP)
            {
                GroupName = "Default";
            }
        }

        public int CompareTo(AvailableBib other)
        {
            if (other == null) return 1;
            else if (this.EventId == other.EventId)
            {
                if (this.GroupNumber == other.GroupNumber)
                {
                    return this.Bib.CompareTo(other.Bib);
                }
                return this.GroupNumber.CompareTo(other.GroupNumber);
            }
            return this.EventId.CompareTo(other.EventId);
        }

        public bool Equals(AvailableBib other)
        {
            if (other == null) return false;
            return this.EventId == other.EventId && this.Bib == other.Bib && this.GroupNumber == other.GroupNumber;
        }
    }
}
