using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Objects
{
    internal class Alarm : IEquatable<Alarm>, IComparable<Alarm>
    {
        public int Identifier { get; set; }
        public int Bib { get; set; } = -1;
        public string Chip { get; set; } = "";
        // This is the number of times to alert the user.
        public int AlertCount { get; set; } = 1;
        // This is the number of times the user has already been alerted.
        public int AlertedCount { get; set; } = 0;
        public bool Enabled { get; set; } = false;
        // Any number not assigned to a sound is assumed to be the default.
        public int AlarmSound { get; set; } = 0;

        public int CompareTo(Alarm other)
        {
            if (other == null) return 1;
            if (this.Bib == other.Bib)
            {
                return this.Chip.CompareTo(other.Chip);
            }
            return this.Bib.CompareTo(other.Bib);
        }

        public bool Equals(Alarm other)
        {
            if (other == null) return false;
            return this.Identifier == other.Identifier;
        }
    }
}
