﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Chronokeep.Objects
{
    public class Alarm : IEquatable<Alarm>, IComparable<Alarm>
    {
        private static Mutex listMtx = new Mutex();
        private static List<Alarm> alarms = new List<Alarm>();
        private static Dictionary<string, Alarm> bibAlarms = new Dictionary<string, Alarm>();
        private static Dictionary<string, Alarm> chipAlarms = new Dictionary<string, Alarm>();

        public int Identifier { get; set; }
        public string Bib { get; set; } = "";
        public string Chip { get; set; } = "";
        public bool Enabled { get; set; } = true;
        // Any number not assigned to a sound (1-5 currently) is assumed to be the default.
        public int AlarmSound { get; set; } = 0;

        public Alarm(int identifier, string bib, string chip, bool enabled, int sound)
        {
            this.Identifier = identifier;
            this.Bib = bib;
            this.Chip = chip;
            this.Enabled = enabled;
            this.AlarmSound = sound;
        }

        public static void SaveAlarms(int eventId, IDBInterface database)
        {
            Log.D("Objects.Alarm", "Saving multiple alarms.");
            if (listMtx.WaitOne(3000))
            {
                database.SaveAlarms(eventId, alarms);
                listMtx.ReleaseMutex();
            }
            ClearAlarms();
            AddAlarms(database.GetAlarms(eventId));
        }

        public static void SaveAlarm(int eventId, IDBInterface database, Alarm alarm)
        {
            Log.D("Objects.Alarm", "Saving single alarm.");
            if (listMtx.WaitOne(3000))
            {
                database.SaveAlarm(eventId, alarm);
                listMtx.ReleaseMutex();
            }
            ClearAlarms();
            AddAlarms(database.GetAlarms(eventId));
        }

        public static List<Alarm> GetAlarms()
        {
            Log.D("Objects.Alarm", "Getting alarms.");
            List<Alarm> output = new List<Alarm>();
            if (listMtx.WaitOne(3000))
            {
                output.AddRange(alarms);
                listMtx.ReleaseMutex();
            }
            return output;
        }

        public static (Dictionary<string, Alarm>, Dictionary<string, Alarm>) GetAlarmDictionaries()
        {
            Dictionary<string, Alarm> outBib = new Dictionary<string, Alarm>();
            Dictionary<string, Alarm> outChip = new Dictionary<string, Alarm>();
            if (listMtx.WaitOne(3000))
            {
                outBib = new Dictionary<string, Alarm>(bibAlarms);
                outChip = new Dictionary<string, Alarm>(chipAlarms);
                listMtx.ReleaseMutex();
            }
            return (outBib, outChip);
        }

        public static bool RemoveAlarm(Alarm alarm)
        {
            bool output = false;
            if (listMtx.WaitOne(3000))
            {
                if (bibAlarms.ContainsKey(alarm.Bib))
                {
                    bibAlarms.Remove(alarm.Bib);
                    output = true;
                }
                if (chipAlarms.ContainsKey(alarm.Chip))
                {
                    chipAlarms.Remove(alarm.Chip);
                    output = true;
                }
                if (alarms.Contains(alarm))
                {
                    alarms.Remove(alarm);
                    output = true;
                }
                listMtx.ReleaseMutex();
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
                listMtx.ReleaseMutex();
            }
            return output;
        }

        public static bool AddAlarm(Alarm alarm)
        {
            Log.D("Objects.Alarm", "Adding alarm.");
            bool output = false;
            if (listMtx.WaitOne(3000))
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
                listMtx.ReleaseMutex();
            }
            return output;
        }

        public static bool AddAlarms(List<Alarm> newAlarms)
        {
            Log.D("Objects.Alarm", "Adding alarms.");
            bool output = false;
            if (listMtx.WaitOne(3000))
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
                listMtx.ReleaseMutex();
            }
            return output;
        }

        public static Alarm GetAlarmByBib(string bib)
        {
            Alarm output = null;
            if (listMtx.WaitOne(3000))
            {
                if (bibAlarms.ContainsKey(bib))
                {
                    output = bibAlarms[bib];
                }
                listMtx.ReleaseMutex();
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
                listMtx.ReleaseMutex();
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
