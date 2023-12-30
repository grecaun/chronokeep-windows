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
        private static Dictionary<int, Alarm> bibAlarms = new Dictionary<int, Alarm>();
        private static Dictionary<string, Alarm> chipAlarms = new Dictionary<string, Alarm>();

        public int Identifier { get; set; }
        public int Bib { get; set; } = -1;
        public string Chip { get; set; } = "";
        public bool Enabled { get; set; } = true;
        // Any number not assigned to a sound (1-5 currently) is assumed to be the default.
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

        public static (Dictionary<int, Alarm>, Dictionary<string, Alarm>) GetAlarmDictionarys()
        {
            Dictionary<int, Alarm> outBib = new Dictionary<int, Alarm>();
            Dictionary<string, Alarm> outChip = new Dictionary<string, Alarm>();
            if (listMtx.WaitOne(3000))
            {
                outBib = new Dictionary<int, Alarm>(bibAlarms);
                outChip = new Dictionary<string, Alarm>(chipAlarms);
            }
            return (outBib, outChip);
        }

        public static bool RemoveAlarm(Alarm alarm)
        {
            bool output = false;
            if (listMtx.WaitOne(3000))
            {
                alarms.Remove(alarm);
                if (bibAlarms.ContainsKey(alarm.Bib))
                {
                    bibAlarms.Remove(alarm.Bib);
                }
                if (chipAlarms.ContainsKey(alarm.Chip))
                {
                    chipAlarms.Remove(alarm.Chip);
                }
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
                bibAlarms.Clear();
                chipAlarms.Clear();
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
                if (alarm.Bib >= 0)
                {
                    bibAlarms[alarm.Bib] = alarm;
                    output = true;
                }
                if (alarm.Chip.Length > 0)
                {
                    chipAlarms[alarm.Chip] = alarm;
                    output = true;
                }
            }
            return output;
        }

        public static bool AddAlarms(List<Alarm> newAlarms)
        {
            bool output = false;
            if (listMtx.WaitOne(3000))
            {
                foreach (Alarm alarm in newAlarms)
                {
                    alarms.Add(alarm);
                    if (alarm.Bib >= 0)
                    {
                        bibAlarms.Add(alarm.Bib, alarm);
                        output = true;
                    }
                    if (alarm.Chip.Length > 0)
                    {
                        chipAlarms.Add(alarm.Chip, alarm);
                        output = true;
                    }
                }
            }
            return output;
        }

        public static Alarm GetAlarmByBib(int bib)
        {
            Alarm output = null;
            if (listMtx.WaitOne(3000))
            {
                if (bibAlarms.ContainsKey(bib))
                {
                    output = bibAlarms[bib];
                }
            }
            return output;
        }

        public static Alarm GetAlarmByChip(string chip)
        {
            Alarm output = null;
            if (listMtx.WaitOne(3000))
            {
                if (chipAlarms.ContainsKey(chip))
                {
                    output = chipAlarms[chip];
                }
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
