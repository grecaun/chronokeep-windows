using Chronokeep.Objects;
using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * Alarm Functions
         */

        public void DeleteAlarm(Alarm alarm)
        {
            Log.D("MemStore", "DeleteAlarms");
            database.DeleteAlarm(alarm);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    alarms.RemoveAll(x => alarm.Bib.Equals(x.Bib, StringComparison.OrdinalIgnoreCase) && alarm.Chip.Equals(x.Chip, StringComparison.OrdinalIgnoreCase));
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void DeleteAlarms(int eventId)
        {
            Log.D("MemStore", "DeleteAlarms");
            database.DeleteAlarms(eventId);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        alarms.Clear();
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public List<Alarm> GetAlarms(int eventId)
        {
            Log.D("MemStore", "GetAlarms");
            List<Alarm> output = new();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        output.AddRange(alarms);
                    }
                    memStoreLock.ReleaseMutex();
                }
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public int SaveAlarm(int eventId, Alarm alarm)
        {
            Log.D("MemStore", "SaveAlarm");
            alarm.Identifier = database.SaveAlarm(eventId, alarm);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        alarms.Add(alarm);
                    }
                    memStoreLock.ReleaseMutex();
                }
                return alarm.Identifier;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public List<Alarm> SaveAlarms(int eventId, List<Alarm> iAlarms)
        {
            Log.D("MemStore", "SaveAlarms");
            List<Alarm> output = new();
            output.AddRange(database.SaveAlarms(eventId, iAlarms));
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        alarms.AddRange(output);
                    }
                    memStoreLock.ReleaseMutex();
                }
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }
    }
}
