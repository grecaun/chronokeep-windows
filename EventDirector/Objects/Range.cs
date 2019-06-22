using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Objects
{
    class Range : IComparable<Range>, IEquatable<Range>
    {
        public int StartBib { get; set; }
        public int EndBib { get; set; }
        public int StartChip { get; set; }
        public int EndChip { get; set; }

        public int CompareTo(object obj)
        {
            if (Object.ReferenceEquals(obj.GetType(), this.GetType()))
            {
                return StartBib.CompareTo(((Range)obj).StartBib);
            }
            return -1;
        }

        public int CompareTo(Range other)
        {
            return StartBib.CompareTo(other.StartBib);
        }

        public bool Equals(Range other)
        {
            return StartBib.Equals(other.StartBib) && EndBib.Equals(other.EndBib) && StartChip.Equals(other.StartChip) && EndChip.Equals(other.EndChip);
        }

        public bool IsValid()
        {
            return StartBib <= EndBib;
        }

        public bool Violates(Range other)
        {
            return (other.StartBib >= this.StartBib && other.StartBib <= this.EndBib) || (other.EndBib >= this.StartBib && other.EndBib <= this.EndBib)
                || (this.StartBib >= other.StartBib && this.StartBib <= other.EndBib) || (this.EndBib >= other.StartBib && this.EndBib <= other.EndBib)
                || (other.StartChip >= this.StartChip && other.StartChip <= this.EndChip) || (other.EndChip >= this.StartChip && other.EndChip <= this.EndChip)
                || (this.StartChip >= other.StartChip && this.StartChip <= other.EndChip) || (this.EndChip >= other.StartChip && this.EndChip <= other.EndChip);
        }
    }
}
