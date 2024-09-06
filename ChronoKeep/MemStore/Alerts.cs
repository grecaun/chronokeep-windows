using Chronokeep.Objects.ChronoKeepAPI;
using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * EmailAlert Functions
         */

        public void AddEmailAlert(int eventId, int eventspecific_id)
        {
            Log.D("MemStore", "AddEmailAlert");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
                alertsLock.AcquireWriterLock(lockTimeout);
                database.AddEmailAlert(eventId, eventspecific_id);
                emailAlerts.Add(eventspecific_id);
                alertsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring alertsLock. " + e.Message);
                throw new MutexLockException("alertsLock");
            }
        }

        public List<int> GetEmailAlerts(int eventId)
        {
            Log.D("MemStore", "GetEmailAlerts");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
                alertsLock.AcquireReaderLock(lockTimeout);
                List<int> output = new();
                output.AddRange(emailAlerts);
                alertsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring alertsLock. " + e.Message);
                throw new MutexLockException("alertsLock");
            }
        }

        /**
         * SMS Functions
         */

        public void AddSMSAlert(int eventId, int eventspecific_id, int segment_id)
        {
            Log.D("MemStore", "AddSMSAlert");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
                alertsLock.AcquireWriterLock(lockTimeout);
                database.AddSMSAlert(eventId, eventspecific_id, segment_id);
                smsAlerts.Add((eventspecific_id, segment_id));
                alertsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring alertsLock. " + e.Message);
                throw new MutexLockException("alertsLock");
            }
        }

        public void AddSmsSubscriptions(int eventId, List<APISmsSubscription> subscriptions)
        {
            Log.D("MemStore", "AddSmsSubscriptions");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
                alertsLock.AcquireWriterLock(lockTimeout);
                database.AddSmsSubscriptions(eventId, subscriptions);
                smsSubscriptions.AddRange(subscriptions);
                alertsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring alertsLock. " + e.Message);
                throw new MutexLockException("alertsLock");
            }
        }

        public void DeleteSmsSubscriptions(int eventId)
        {
            Log.D("MemStore", "DeleteSmsSubscriptions");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
                alertsLock.AcquireWriterLock(lockTimeout);
                database.DeleteSmsSubscriptions(eventId);
                smsSubscriptions.Clear();
                alertsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring alertsLock. " + e.Message);
                throw new MutexLockException("alertsLock");
            }
        }

        public List<(int, int)> GetSMSAlerts(int eventId)
        {
            Log.D("MemStore", "GetSMSAlerts");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
                alertsLock.AcquireReaderLock(lockTimeout);
                List<(int, int)> output = new();
                output.AddRange(smsAlerts);
                alertsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring alertsLock. " + e.Message);
                throw new MutexLockException("alertsLock");
            }
        }

        public List<APISmsSubscription> GetSmsSubscriptions(int eventId)
        {
            Log.D("MemStore", "GetSmsSubscriptions");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
                alertsLock.AcquireReaderLock(lockTimeout);
                List<APISmsSubscription> output = new();
                output.AddRange(smsSubscriptions);
                alertsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring alertsLock. " + e.Message);
                throw new MutexLockException("alertsLock");
            }
        }
    }
}
