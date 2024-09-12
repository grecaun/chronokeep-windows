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
            database.AddEmailAlert(eventId, eventspecific_id);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        emailAlerts.Add(eventspecific_id);
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

        public List<int> GetEmailAlerts(int eventId)
        {
            Log.D("MemStore", "GetEmailAlerts");
            List<int> output = new();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        output.AddRange(emailAlerts);
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

        /**
         * SMS Functions
         */

        public void AddSMSAlert(int eventId, int eventspecific_id, int segment_id)
        {
            Log.D("MemStore", "AddSMSAlert");
            database.AddSMSAlert(eventId, eventspecific_id, segment_id);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        smsAlerts.Add((eventspecific_id, segment_id));
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

        public void AddSmsSubscriptions(int eventId, List<APISmsSubscription> subscriptions)
        {
            Log.D("MemStore", "AddSmsSubscriptions");
            database.AddSmsSubscriptions(eventId, subscriptions);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        smsSubscriptions.AddRange(subscriptions);
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

        public void DeleteSmsSubscriptions(int eventId)
        {
            Log.D("MemStore", "DeleteSmsSubscriptions");
            database.DeleteSmsSubscriptions(eventId);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        smsSubscriptions.Clear();
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

        public List<(int, int)> GetSMSAlerts(int eventId)
        {
            Log.D("MemStore", "GetSMSAlerts");
            List<(int, int)> output = new();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        output.AddRange(smsAlerts);
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

        public List<APISmsSubscription> GetSmsSubscriptions(int eventId)
        {
            Log.D("MemStore", "GetSmsSubscriptions");
            List<APISmsSubscription> output = new();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        output.AddRange(smsSubscriptions);
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
