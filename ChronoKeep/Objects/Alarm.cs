using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Objects
{
    internal class Alarm
    {
        public int Bib { get; set; } = -1;
        public string Chip { get; set; } = "";
        // This is the number of times to alert the user.
        public int AlertCount { get; set; } = 1;
        // This is the number of times the user has already been alerted.
        public int AlertedCount { get; set; } = 0;
        public bool Enabled { get; set; } = false;
        // Any number not assigned to a sound is assumed to be the default.
        public int AlarmSound { get; set; } = 0;
    }
}
