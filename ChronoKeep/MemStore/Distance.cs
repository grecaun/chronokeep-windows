using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Objects;
using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * Distance Functions
         */

        public int AddDistance(Distance dist)
        {
            Log.D("MemStore", "AddDistance");
            dist.Identifier = database.AddDistance(dist);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        if (theEvent != null && dist.EventIdentifier == theEvent.Identifier && dist.Identifier > 0)
                        {
                            distances[dist.Identifier] = dist;
                            distanceNameDict[dist.Name] = dist;
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
                throw new ChronoLockException($"memStoreLock {e.Message}");
            }
            return dist.Identifier;
        }

        public List<Distance> AddDistances(List<Distance> distances)
        {
            Log.D("MemStore", "AddDistances");
            List<Distance> output = database.AddDistances(distances);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        foreach (Distance dist in output)
                        {
                            if (theEvent != null && dist.EventIdentifier == theEvent.Identifier && dist.Identifier > 0)
                            {
                                distances[dist.Identifier] = dist;
                                distanceNameDict[dist.Name] = dist;
                            }
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
                throw new ChronoLockException($"memStoreLock {e.Message}");
            }
            return output;
        }

        public Distance GetDistance(int divId)
        {
            Log.D("MemStore", "GetDistance");
            Distance output = null;
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        distances.TryGetValue(divId, out output);
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
                throw new ChronoLockException($"memStoreLock {e.Message}");
            }
            return output;
        }

        public int GetDistanceID(Distance dist)
        {
            Log.D("MemStore", "GetDistanceID");
            int output = -1;
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        foreach (Distance known in distances.Values)
                        {
                            if (known.Name.Equals(dist.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                output = known.Identifier;
                                break;
                            }
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
                throw new ChronoLockException($"memStoreLock {e.Message}");
            }
            return output;
        }

        public List<Distance> GetDistances(int eventId)
        {
            Log.D("MemStore", "GetDistances");
            List<Distance> output = [];
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        if (theEvent != null && theEvent.Identifier == eventId)
                        {
                            output.AddRange(distances.Values);
                        }
                        else
                        {
                            output.AddRange(database.GetDistances(eventId));
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
                throw new ChronoLockException($"memStoreLock {e.Message}");
            }
            return output;
        }

        public void RemoveDistance(int identifier)
        {
            Log.D("MemStore", "RemoveDistance");
            database.RemoveDistance(identifier);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        distances.Remove(identifier);
                        string distName = "";
                        foreach (Distance dist in distances.Values)
                        {
                            if (dist.Identifier == identifier)
                            {
                                distName = dist.Name;
                                break;
                            }
                        }
                        if (distName.Length > 0)
                        {
                            distanceNameDict.Remove(distName);
                        }
                        List<int> participantsToRemove = new();
                        foreach (Participant p in participants.Values)
                        {
                            if (p.EventSpecific.DistanceIdentifier == identifier)
                            {
                                participantsToRemove.Add(p.EventSpecific.Identifier);
                            }
                        }
                        foreach (int i in participantsToRemove)
                        {
                            participants.Remove(i);
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
                throw new ChronoLockException($"memStoreLock {e.Message}");
            }
        }

        public void RemoveDistance(Distance dist)
        {
            Log.D("MemStore", "RemoveDistance");
            database.RemoveDistance(dist);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        distances.Remove(dist.Identifier);
                        distanceNameDict.Remove(dist.Name);
                        List<int> participantsToRemove = new();
                        foreach (Participant p in participants.Values)
                        {
                            if (p.EventSpecific.DistanceIdentifier == dist.Identifier)
                            {
                                participantsToRemove.Add(p.EventSpecific.Identifier);
                            }
                        }
                        foreach (int i in participantsToRemove)
                        {
                            participants.Remove(i);
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
                throw new ChronoLockException($"memStoreLock {e.Message}");
            }
        }

        public void UpdateDistance(Distance dist)
        {
            Log.D("MemStore", "UpdateDistance");
            database.UpdateDistance(dist);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        Dictionary<string, string> oldDistanceNameDict = new();
                        foreach (Distance old in distances.Values)
                        {
                            if (dist.Equals(old))
                            {
                                if (!dist.Name.Equals(old.Name))
                                {
                                    oldDistanceNameDict[old.Name] = dist.Name;
                                }
                                old.Update(dist);
                            }
                        }
                        foreach (Distance old in distanceNameDict.Values)
                        {
                            if (dist.Equals(old))
                            {
                                old.Update(dist);
                            }
                        }
                        foreach (Participant p in participants.Values)
                        {
                            if (p.EventSpecific.DistanceIdentifier == dist.Identifier)
                            {
                                p.EventSpecific.DistanceName = dist.Name;
                            }
                        }
                        foreach (TimeResult res in timingResults.Values)
                        {
                            if (oldDistanceNameDict.TryGetValue(res.RealDistanceName, out string newDistName))
                            {
                                res.RealDistanceName = newDistName;
                            }
                            if (res.LinkedDistanceName.Length > 0 && oldDistanceNameDict.TryGetValue(res.LinkedDistanceName, out string newDistanceName))
                            {
                                res.LinkedDistanceName = newDistanceName;
                            }
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
                throw new ChronoLockException($"memStoreLock {e.Message}");
            }
        }

        public void SetWaveTimes(int eventId, int wave, long seconds, int milliseconds)
        {
            Log.D("MemStore", "SetWaveTimes");
            database.SetWaveTimes(eventId, wave, seconds, milliseconds);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        if (theEvent != null && theEvent.Identifier == eventId)
                        {
                            foreach (Distance old in distances.Values)
                            {
                                if (old.Wave == wave)
                                {
                                    old.SetWaveTime(wave, seconds, milliseconds);
                                }
                            }
                            foreach (Distance old in distanceNameDict.Values)
                            {
                                if (old.Wave == wave)
                                {
                                    old.SetWaveTime(wave, seconds, milliseconds);
                                }
                            }
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
                throw new ChronoLockException($"memStoreLock {e.Message}");
            }
        }
    }
}
