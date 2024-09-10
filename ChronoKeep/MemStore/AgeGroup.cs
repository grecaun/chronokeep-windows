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
            group.GroupId = database.AddAgeGroup(group);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    if (!ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> value))
                    {
                        value = new List<AgeGroup>();
                        ageGroups[group.DistanceId] = value;
                    }
                    value.Add(group);
                    SetAgeGroups();
                    memStoreLock.ExitWriteLock();
                }
                return group.GroupId;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLocks");
            }
        }

        public List<AgeGroup> AddAgeGroups(List<AgeGroup> groups)
        {
            Log.D("MemStore", "AddAgeGroups");
            List<AgeGroup> output = database.AddAgeGroups(groups);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    foreach (AgeGroup group in output)
                    {
                        if (!ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> value))
                        {
                            value = new List<AgeGroup>();
                            ageGroups[group.DistanceId] = value;
                        }
                        value.Add(group);
                    }
                    SetAgeGroups();
                    memStoreLock.ExitWriteLock();
                }
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public List<AgeGroup> GetAgeGroups(int eventId)
        {
            Log.D("MemStore", "GetAgeGroups");
            List<AgeGroup> output = new();
            try
            {
                if (memStoreLock.TryEnterReadLock(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        foreach (List<AgeGroup> groups in ageGroups.Values)
                        {
                            output.AddRange(groups);
                        }
                    }
                    else
                    {
                        output.AddRange(database.GetAgeGroups(eventId));
                    }
                    memStoreLock.ExitReadLock();
                }
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public List<AgeGroup> GetAgeGroups(int eventId, int distanceId)
        {
            Log.D("MemStore", "GetAgeGroups");
            List<AgeGroup> output = new();
            try
            {
                if (memStoreLock.TryEnterReadLock(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        if (ageGroups.TryGetValue(distanceId, out List<AgeGroup> groups))
                        {
                            output.AddRange(groups);
                        }
                    }
                    else
                    {
                        output.AddRange(database.GetAgeGroups(eventId, distanceId));
                    }
                    memStoreLock.ExitReadLock();
                }
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void RemoveAgeGroup(AgeGroup group)
        {
            Log.D("MemStore", "RemoveAgeGroup");
            database.RemoveAgeGroup(group);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    if (ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> list))
                    {
                        list.Remove(group);
                    }
                    SetAgeGroups();
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void RemoveAgeGroups(int eventId, int distanceId)
        {
            Log.D("MemStore", "RemoveAgeGroup");
            database.RemoveAgeGroups(eventId, distanceId);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        if (ageGroups.TryGetValue(distanceId, out List<AgeGroup> list))
                        {
                            list.Clear();
                        }
                        SetAgeGroups();
                    }
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void RemoveAgeGroups(List<AgeGroup> groups)
        {
            Log.D("MemStore", "RemoveAgeGroups");
            database.RemoveAgeGroups(groups);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    foreach (AgeGroup group in groups)
                    {
                        if (ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> list))
                        {
                            list.Remove(group);
                        }
                    }
                    SetAgeGroups();
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void ResetAgeGroups(int eventId)
        {
            Log.D("MemStore", "ResetAgeGroups");
            database.ResetAgeGroups(eventId);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        ageGroups.Clear();
                        SetAgeGroups();
                    }
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void UpdateAgeGroup(AgeGroup group)
        {
            Log.D("MemStore", "UpdateAgeGroup");
            database.UpdateAgeGroup(group);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
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
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }
    }
}
