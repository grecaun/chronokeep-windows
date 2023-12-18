using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chronokeep.Objects
{
    internal class Alarm : IEquatable<Alarm>, IComparable<Alarm>
    {
        private static Mutex listMtx = new Mutex();
        private static List<Alarm> alarms = new List<Alarm>();

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

        public static List<Alarm> GetAlarms()
        {
            List<Alarm> output = new List<Alarm>();
            if (listMtx.WaitOne(3000))
            {
                output.AddRange(alarms);
            }
            return output;
        }

        public static bool RemoveAlarm(Alarm alarm)
        {
            bool output = false;
            if (listMtx.WaitOne(3000))
            {
                alarms.Remove(alarm);
                output = true;
            }
            return output;
        }

        public static bool ClearAlarms()
        {
            bool output = false;
            if (listMtx.WaitOne(3000))
            {
                alarms.Clear();
                output = true;
            }
            return output;
        }

        public static bool AddAlarm(Alarm alarm)
        {
            bool output = false;
            if (listMtx.WaitOne(3000))
            {
                alarms.Add(alarm);
                output = true;
            }
            return output;
        }

        public static bool AddAlarms(List<Alarm> newAlarms)
        {
            bool output = false;
            if (listMtx.WaitOne(3000))
            {
                alarms.AddRange(newAlarms);
                output = true;
            }
            return output;
        }

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
