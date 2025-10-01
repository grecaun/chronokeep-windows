﻿using Chronokeep.Database;
using Chronokeep.Helpers;
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
                    if (!lastAgeGroup.TryGetValue(g.DistanceId, out AgeGroup group) || group.StartAge < g.StartAge)
                    {
                        group = g;
                        lastAgeGroup[g.DistanceId] = group;
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
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        if (!ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> value))
                        {
                            value = [];
                            ageGroups[group.DistanceId] = value;
                        }
                        value.Add(group);
                        SetAgeGroups();
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
                return group.GroupId;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLocks");
            }
        }

        public List<AgeGroup> AddAgeGroups(List<AgeGroup> groups)
        {
            Log.D("MemStore", "AddAgeGroups");
            List<AgeGroup> output = database.AddAgeGroups(groups);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        foreach (AgeGroup group in output)
                        {
                            if (!ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> value))
                            {
                                value = [];
                                ageGroups[group.DistanceId] = value;
                            }
                            value.Add(group);
                        }
                        SetAgeGroups();
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public List<AgeGroup> GetAgeGroups(int eventId)
        {
            Log.D("MemStore", "GetAgeGroups");
            List<AgeGroup> output = [];
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
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
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public List<AgeGroup> GetAgeGroups(int eventId, int distanceId)
        {
            Log.D("MemStore", "GetAgeGroups");
            List<AgeGroup> output = [];
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
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
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public void RemoveAgeGroup(AgeGroup group)
        {
            Log.D("MemStore", "RemoveAgeGroup");
            database.RemoveAgeGroup(group);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        if (ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> list))
                        {
                            list.Remove(group);
                        }
                        SetAgeGroups();
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public void RemoveAgeGroups(int eventId, int distanceId)
        {
            Log.D("MemStore", "RemoveAgeGroup");
            database.RemoveAgeGroups(eventId, distanceId);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        if (theEvent != null && theEvent.Identifier == eventId)
                        {
                            if (ageGroups.TryGetValue(distanceId, out List<AgeGroup> list))
                            {
                                list.Clear();
                            }
                            SetAgeGroups();
                        }
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public void RemoveAgeGroups(List<AgeGroup> groups)
        {
            Log.D("MemStore", "RemoveAgeGroups");
            database.RemoveAgeGroups(groups);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        foreach (AgeGroup group in groups)
                        {
                            if (ageGroups.TryGetValue(group.DistanceId, out List<AgeGroup> list))
                            {
                                list.Remove(group);
                            }
                        }
                        SetAgeGroups();
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public void ResetAgeGroups(int eventId)
        {
            Log.D("MemStore", "ResetAgeGroups");
            database.ResetAgeGroups(eventId);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        if (theEvent != null && theEvent.Identifier == eventId)
                        {
                            ageGroups.Clear();
                            SetAgeGroups();
                        }
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public void UpdateAgeGroup(AgeGroup group)
        {
            Log.D("MemStore", "UpdateAgeGroup");
            database.UpdateAgeGroup(group);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
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
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }
    }
}
