using Chronokeep.Objects;
using Microsoft.Extensions.Logging;
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
            try
            {
                alarmLock.AcquireWriterLock(lockTimeout);
                database.DeleteAlarm(alarm);
                alarms.Remove((alarm.Bib, alarm.Chip));
                alarmLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring alarmLock. " + e.Message);
                throw new MutexLockException("alarmLock");
            }
        }

        public void DeleteAlarms(int eventId)
        {
            Log.D("MemStore", "DeleteAlarms");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            try
            {
                alarmLock.AcquireWriterLock(lockTimeout);
                database.DeleteAlarms(eventId);
                alarms.Clear();
                alarmLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring alarmLock. " + e.Message);
                throw new MutexLockException("alarmLock");
            }
        }

        public List<Alarm> GetAlarms(int eventId)
        {
            Log.D("MemStore", "GetAlarms");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            try
            {
                alarmLock.AcquireReaderLock(lockTimeout);
                List<Alarm> output = new();
                output.AddRange(alarms.Values);
                alarmLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring alarmLock. " + e.Message);
                throw new MutexLockException("alarmLock");
            }
        }

        public int SaveAlarm(int eventId, Alarm alarm)
        {
            Log.D("MemStore", "SaveAlarm");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            try
            {
                alarmLock.AcquireWriterLock(lockTimeout);
                alarm.Identifier = database.SaveAlarm(eventId, alarm);
                alarmLock.ReleaseWriterLock();
                return alarm.Identifier;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring alarmLock. " + e.Message);
                throw new MutexLockException("alarmLock");
            }
        }

        public List<Alarm> SaveAlarms(int eventId, List<Alarm> alarms)
        {
            Log.D("MemStore", "SaveAlarms");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            try
            {
                ageGroupLock.AcquireWriterLock(lockTimeout);
                List<Alarm> output = new();
                output.AddRange(database.SaveAlarms(eventId, alarms));
                ageGroupLock.ReleaseWriterLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }
    }
}
