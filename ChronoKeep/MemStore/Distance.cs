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
            try
            {
                distanceLock.AcquireWriterLock(lockTimeout);
                dist.Identifier = database.AddDistance(dist);
                distances[dist.Identifier] = dist;
                distanceNameDict[dist.Name] = dist;
                distanceLock.ReleaseWriterLock();
                return dist.Identifier;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring distanceLock. " + e.Message);
                throw new MutexLockException("distanceLock");
            }
        }

        public List<Distance> AddDistances(List<Distance> distances)
        {
            Log.D("MemStore", "AddDistances");
            try
            {
                distanceLock.AcquireWriterLock(lockTimeout);
                List<Distance> output = database.AddDistances(distances);
                foreach (Distance dist in output)
                {
                    distances[dist.Identifier] = dist;
                    distanceNameDict[dist.Name] = dist;
                }
                distanceLock.ReleaseWriterLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring distanceLock. " + e.Message);
                throw new MutexLockException("distanceLock");
            }
        }

        public Distance GetDistance(int divId)
        {
            Log.D("MemStore", "GetDistance");
            try
            {
                distanceLock.AcquireReaderLock(lockTimeout);
                distances.TryGetValue(divId, out Distance output);
                distanceLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring distanceLock. " + e.Message);
                throw new MutexLockException("distanceLock");
            }
        }

        public int GetDistanceID(Distance dist)
        {
            Log.D("MemStore", "GetDistanceID");
            try
            {
                distanceLock.AcquireReaderLock(lockTimeout);
                int output = -1;
                foreach (Distance known in distances.Values)
                {
                    if (known.Name.Equals(dist.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        output = known.Identifier;
                        break;
                    }
                }
                distanceLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring distanceLock. " + e.Message);
                throw new MutexLockException("distanceLock");
            }
        }

        public List<Distance> GetDistances(int eventId)
        {
            Log.D("MemStore", "GetDistances");
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
                distanceLock.AcquireReaderLock(lockTimeout);
                List<Distance> output = new();
                output.AddRange(distances.Values);
                distanceLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring distanceLock. " + e.Message);
                throw new MutexLockException("distanceLock");
            }
        }

        public void RemoveDistance(int identifier)
        {
            Log.D("MemStore", "RemoveDistance");
            try
            {
                distanceLock.AcquireWriterLock(lockTimeout);
                database.RemoveDistance(identifier);
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
                distanceLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring distanceLock. " + e.Message);
                throw new MutexLockException("distanceLock");
            }
        }

        public void RemoveDistance(Distance dist)
        {
            Log.D("MemStore", "RemoveDistance");
            try
            {
                distanceLock.AcquireWriterLock(lockTimeout);
                database.RemoveDistance(dist);
                distances.Remove(dist.Identifier);
                distanceNameDict.Remove(dist.Name);
                distanceLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring distanceLock. " + e.Message);
                throw new MutexLockException("distanceLock");
            }
        }

        public void UpdateDistance(Distance dist)
        {
            Log.D("MemStore", "UpdateDistance");
            try
            {
                distanceLock.AcquireWriterLock(lockTimeout);
                database.UpdateDistance(dist);
                foreach (Distance old in distances.Values)
                {
                    if (dist.Equals(old))
                    {
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
                distanceLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring distanceLock. " + e.Message);
                throw new MutexLockException("distanceLock");
            }
        }

        public void SetWaveTimes(int eventId, int wave, long seconds, int milliseconds)
        {
            Log.D("MemStore", "SetWaveTimes");
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
                distanceLock.AcquireWriterLock(lockTimeout);
                database.SetWaveTimes(eventId, wave, seconds, milliseconds);
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
                distanceLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring distanceLock. " + e.Message);
                throw new MutexLockException("distanceLock");
            }
        }
    }
}
