using Chronokeep.Objects;
using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * Age Group Functions
         */

        private static void SetAgeGroups()
        {
            currentAgeGroups.Clear();
            lastAgeGroup.Clear();
            foreach (List<AgeGroup> groups in ageGroups.Values)
            {
                foreach (AgeGroup g in groups)
                {
                    for (int i = g.StartAge; i<= g.EndAge; i++)
                    {
                        currentAgeGroups[(g.DistanceId, i)] = g;
                    }
                    if (!lastAgeGroup.ContainsKey(g.DistanceId) || lastAgeGroup[g.DistanceId].StartAge < g.StartAge)
                    {
                        lastAgeGroup[g.DistanceId] = g;
                    }
                }
            }
        }

        public int AddAgeGroup(AgeGroup group)
        {
            Log.D("MemStore", "AddAgeGroup");
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                participantsLock.AcquireWriterLock(lockTimeout);
                group.GroupId = database.AddAgeGroup(group);
                if (theEvent != null && group.EventId == theEvent.Identifier)
                {
                    if (!ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> value))
                    {
                        value = new List<AgeGroup>();
                        ageGroups[group.DistanceId] = value;
                    }
                    value.Add(group);
                }
                SetAgeGroups();
                eventLock.ReleaseReaderLock();
                participantsLock.ReleaseWriterLock();
                return group.GroupId;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public List<AgeGroup> AddAgeGroups(List<AgeGroup> groups)
        {
            Log.D("MemStore", "AddAgeGroups");
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                participantsLock.AcquireWriterLock(lockTimeout);
                List<AgeGroup> output = database.AddAgeGroups(groups);
                foreach (AgeGroup group in output)
                {
                    if (theEvent != null && group.EventId == theEvent.Identifier)
                    {
                        if (!ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> value))
                        {
                            value = new List<AgeGroup>();
                            ageGroups[group.DistanceId] = value;
                        }
                        value.Add(group);
                    }
                }
                SetAgeGroups();
                eventLock.ReleaseReaderLock();
                participantsLock.ReleaseWriterLock();
                return output;

            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public List<AgeGroup> GetAgeGroups(int eventId)
        {
            Log.D("MemStore", "GetAgeGroups");
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
                return database.GetAgeGroups(eventId);
            }
            List<AgeGroup> output = new();
            try
            {
                participantsLock.AcquireReaderLock(lockTimeout);
                foreach (List<AgeGroup> groups in ageGroups.Values)
                {
                    output.AddRange(groups);
                }
                participantsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public List<AgeGroup> GetAgeGroups(int eventId, int distanceId)
        {
            Log.D("MemStore", "GetAgeGroups");
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
            List<AgeGroup> output = new();
            try
            {
                participantsLock.AcquireReaderLock(lockTimeout);
                if (ageGroups.TryGetValue(distanceId, out List<AgeGroup> groups))
                {
                    output.AddRange(groups);
                }
                participantsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public void RemoveAgeGroup(AgeGroup group)
        {
            Log.D("MemStore", "RemoveAgeGroup");
            try
            {
                participantsLock.AcquireWriterLock(lockTimeout);
                database.RemoveAgeGroup(group);
                if (ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> list))
                {
                    list.Remove(group);
                }
                SetAgeGroups();
                participantsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public void RemoveAgeGroups(int eventId, int distanceId)
        {
            Log.D("MemStore", "RemoveAgeGroup");
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
                participantsLock.AcquireWriterLock(lockTimeout);
                database.RemoveAgeGroups(eventId, distanceId);
                if (ageGroups.TryGetValue(distanceId, out List<AgeGroup> list))
                {
                    list.Clear();
                }
                SetAgeGroups();
                participantsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public void RemoveAgeGroups(List<AgeGroup> groups)
        {
            Log.D("MemStore", "RemoveAgeGroups");
            try
            {
                participantsLock.AcquireWriterLock(lockTimeout);
                database.RemoveAgeGroups(groups);
                foreach (AgeGroup group in groups)
                {
                    if (ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> list))
                    {
                        list.Remove(group);
                    }
                }
                SetAgeGroups();
                participantsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public void ResetAgeGroups(int eventId)
        {
            Log.D("MemStore", "ResetAgeGroups");
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
                participantsLock.AcquireWriterLock(lockTimeout);
                database.ResetAgeGroups(eventId);
                ageGroups.Clear();
                SetAgeGroups();
                participantsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }

        public void UpdateAgeGroup(AgeGroup group)
        {
            Log.D("MemStore", "UpdateAgeGroup");
            try
            {
                participantsLock.AcquireWriterLock(lockTimeout);
                database.UpdateAgeGroup(group);
                if (ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> list))
                {
                    foreach (AgeGroup ageGroup in list)
                    {
                        if (ageGroup.GroupId == group.GroupId)
                        {
                            ageGroup.StartAge = group.StartAge;
                            ageGroup.EndAge = group.EndAge;
                            ageGroup.LastGroup = group.LastGroup;
                            ageGroup.CustomName = group.CustomName;
                        }
                    }
                }
                SetAgeGroups();
                participantsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring participantsLock. " + e.Message);
                throw new MutexLockException("participantsLock");
            }
        }
    }
}
