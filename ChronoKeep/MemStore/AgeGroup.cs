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

        public int AddAgeGroup(AgeGroup group)
        {
            Log.D("MemStore", "AddAgeGroup");
            try
            {
                ageGroupLock.AcquireWriterLock(lockTimeout);
                group.GroupId = database.AddAgeGroup(group);
                if (!ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> value))
                {
                    value = new List<AgeGroup>();
                    ageGroups[group.DistanceId] = value;
                }
                value.Add(group);
                ageGroupLock.ReleaseWriterLock();
                return group.GroupId;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public List<AgeGroup> AddAgeGroups(List<AgeGroup> groups)
        {
            Log.D("MemStore", "AddAgeGroups");
            try
            {
                ageGroupLock.AcquireWriterLock(lockTimeout);
                List<AgeGroup> output = database.AddAgeGroups(groups);
                foreach (AgeGroup group in output)
                {
                    if (!ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> value))
                    {
                        value = new List<AgeGroup>();
                        ageGroups[group.DistanceId] = value;
                    }
                    value.Add(group);
                }
                ageGroupLock.ReleaseWriterLock();
                return output;

            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public List<AgeGroup> GetAgeGroups(int eventId)
        {
            Log.D("MemStore", "GetAgeGroups");
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
            List<AgeGroup> output = new();
            try
            {
                ageGroupLock.AcquireReaderLock(lockTimeout);
                foreach (List<AgeGroup> groups in ageGroups.Values)
                {
                    output.AddRange(groups);
                }
                ageGroupLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public List<AgeGroup> GetAgeGroups(int eventId, int distanceId)
        {
            Log.D("MemStore", "GetAgeGroups");
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
            List<AgeGroup> output = new();
            try
            {
                ageGroupLock.AcquireReaderLock(lockTimeout);
                if (ageGroups.TryGetValue(distanceId, out List<AgeGroup> groups))
                {
                    output.AddRange(groups);
                }
                ageGroupLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public void RemoveAgeGroup(AgeGroup group)
        {
            Log.D("MemStore", "RemoveAgeGroup");
            try
            {
                ageGroupLock.AcquireWriterLock(lockTimeout);
                database.RemoveAgeGroup(group);
                if (ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> list))
                {
                    list.Remove(group);
                }
                ageGroupLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public void RemoveAgeGroups(int eventId, int distanceId)
        {
            Log.D("MemStore", "RemoveAgeGroup");
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
                ageGroupLock.AcquireWriterLock(lockTimeout);
                database.RemoveAgeGroups(eventId, distanceId);
                if (ageGroups.TryGetValue(distanceId, out List<AgeGroup> list))
                {
                    list.Clear();
                }
                ageGroupLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public void RemoveAgeGroups(List<AgeGroup> groups)
        {
            Log.D("MemStore", "RemoveAgeGroups");
            try
            {
                ageGroupLock.AcquireWriterLock(lockTimeout);
                database.RemoveAgeGroups(groups);
                foreach (AgeGroup group in groups)
                {
                    if (ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> list))
                    {
                        list.Remove(group);
                    }
                }
                ageGroupLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public void ResetAgeGroups(int eventId)
        {
            Log.D("MemStore", "ResetAgeGroups");
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
                ageGroupLock.AcquireWriterLock(lockTimeout);
                database.ResetAgeGroups(eventId);
                ageGroups.Clear();
                ageGroupLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }

        public void UpdateAgeGroup(AgeGroup group)
        {
            Log.D("MemStore", "UpdateAgeGroup");
            try
            {
                ageGroupLock.AcquireWriterLock(lockTimeout);
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
                ageGroupLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring ageGroupLock. " + e.Message);
                throw new MutexLockException("ageGroupLock");
            }
        }
    }
}
