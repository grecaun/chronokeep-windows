using Chronokeep.Database;
using Chronokeep.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Chronokeep.Objects
{
    public class Alarm(int identifier, string bib, string chip, bool enabled, int sound) : IEquatable<Alarm>, IComparable<Alarm>
    {
        private static Lock listMtx = new();
        private static readonly List<Alarm> alarms = [];
        private static readonly Dictionary<string, Alarm> bibAlarms = [];
        private static readonly Dictionary<string, Alarm> chipAlarms = [];

        public int Identifier { get; set; } = identifier;
        public string Bib { get; set; } = bib;
        public string Chip { get; set; } = chip;
        public bool Enabled { get; set; } = enabled;
        // Any number not assigned to a sound (1-5 currently) is assumed to be the default.
        public int AlarmSound { get; set; } = sound;
        public static Lock ListMtx { get => listMtx; set => listMtx = value; }

        public static void SaveAlarms(int eventId, IDBInterface database)
        {
            Log.D("Objects.Alarm", "Saving multiple alarms.");
            if (ListMtx.TryEnter(3000))
            {
                try
                {
                    database.SaveAlarms(eventId, alarms);
                }
                finally
                {
                    ListMtx.Exit();
                }
            }
            ClearAlarms();
            AddAlarms(database.GetAlarms(eventId));
        }

        public static void SaveAlarm(int eventId, IDBInterface database, Alarm alarm)
        {
            Log.D("Objects.Alarm", "Saving single alarm.");
            if (ListMtx.TryEnter(3000))
            {
                try
                {
                    database.SaveAlarm(eventId, alarm);
                }
                finally
                {
                    ListMtx.Exit();
                }
            }
            ClearAlarms();
            AddAlarms(database.GetAlarms(eventId));
        }

        public static List<Alarm> GetAlarms()
        {
            Log.D("Objects.Alarm", "Getting alarms.");
            List<Alarm> output = [];
            if (ListMtx.TryEnter(3000))
            {
                try
                {
                    output.AddRange(alarms);
                }
                finally
                {
                    ListMtx.Exit();
                }
            }
            return output;
        }

        public static (Dictionary<string, Alarm>, Dictionary<string, Alarm>) GetAlarmDictionaries()
        {
            Dictionary<string, Alarm> outBib = [];
            Dictionary<string, Alarm> outChip = [];
            if (ListMtx.TryEnter(3000))
            {
                try
                {
                    outBib = new Dictionary<string, Alarm>(bibAlarms);
                    outChip = new Dictionary<string, Alarm>(chipAlarms);
                }
                finally
                {
                    ListMtx.Exit();
                }
            }
            return (outBib, outChip);
        }

        public static bool RemoveAlarm(Alarm alarm)
        {
            bool output = false;
            if (ListMtx.TryEnter(3000))
            {
                try
                {
                    output = output && bibAlarms.Remove(alarm.Bib);
                    output = output && chipAlarms.Remove(alarm.Chip);
                    output = output && alarms.Remove(alarm);
                }
                finally
                {
                    ListMtx.Exit();
                }
            }
            return output;
        }

        public static bool ClearAlarms()
        {
            bool output = false;
            if (ListMtx.TryEnter(3000))
            {
                try
                {
                    alarms.Clear();
                    bibAlarms.Clear();
                    chipAlarms.Clear();
                    output = true;
                }
                finally
                {
                    ListMtx.Exit();
                }
            }
            return output;
        }

        public static bool AddAlarm(Alarm alarm)
        {
            Log.D("Objects.Alarm", "Adding alarm.");
            bool output = false;
            if (ListMtx.TryEnter(3000))
            {
                try
                {
                    alarms.Add(alarm);
                    if (alarm.Bib.Length > 0)
                    {
                        bibAlarms[alarm.Bib] = alarm;
                    }
                    if (alarm.Chip.Length > 0)
                    {
                        chipAlarms[alarm.Chip] = alarm;
                    }
                    output = true;
                }
                finally
                {
                    ListMtx.Exit();
                }
            }
            return output;
        }

        public static bool AddAlarms(List<Alarm> newAlarms)
        {
            Log.D("Objects.Alarm", "Adding alarms.");
            bool output = false;
            if (ListMtx.TryEnter(3000))
            {
                try
                {
                    Log.D("Objects.Alarm", "Number of alarms: " + newAlarms.Count);
                    foreach (Alarm alarm in newAlarms)
                    {
                        if (alarm.Bib.Length > 0)
                        {
                            bibAlarms[alarm.Bib] = alarm;
                        }
                        if (alarm.Chip.Length > 0)
                        {
                            chipAlarms[alarm.Chip] = alarm;
                        }
                        output = true;
                    }
                    alarms.Clear();
                    alarms.AddRange(bibAlarms.Values);
                    alarms.AddRange(chipAlarms.Values);
                }
                finally
                {
                    ListMtx.Exit();
                }
            }
            return output;
        }

        public static Alarm GetAlarmByBib(string bib)
        {
            Alarm output = null;
            if (ListMtx.TryEnter(3000))
            {
                try
                {
                    if (bibAlarms.TryGetValue(bib, out Alarm alarm))
                    {
                        output = alarm;
                    }
                }
                finally
                {
                    ListMtx.Exit();
                }
            }
            return output;
        }

        public static Alarm GetAlarmByChip(string chip)
        {
            Alarm output = null;
            if (ListMtx.TryEnter(3000))
            {
                try
                {
                    if (chipAlarms.TryGetValue(chip, out Alarm alarm))
                    {
                        output = alarm;
                    }
                }
                finally
                {
                    ListMtx.Exit();
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
