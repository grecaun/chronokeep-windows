using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * TimingLocation Functions
         */

        public int AddTimingLocation(TimingLocation tp)
        {
            Log.D("MemStore", "AddTimingLocation");
            int output = database.AddTimingLocation(tp);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    tp.Identifier = output;
                    if (theEvent != null && tp.EventIdentifier == theEvent.Identifier && tp.Identifier > 0)
                    {
                        locations[tp.Identifier] = tp;
                    }
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
            return output;
        }

        public List<TimingLocation> AddTimingLocations(List<TimingLocation> locs)
        {
            Log.D("MemStore", "AddTimingLocations");
            List<TimingLocation> output = database.AddTimingLocations(locs);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    foreach (TimingLocation tp in locs)
                    {
                        if (theEvent != null && tp.EventIdentifier == theEvent.Identifier && tp.Identifier > 0)
                        {
                            locations[tp.Identifier] = tp;
                        }
                    }
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
            return output;
        }

        public int GetTimingLocationID(TimingLocation tp)
        {
            Log.D("MemStore", "GetTimingLocationID");
            int output = -1;
            try
            {
                if (memStoreLock.TryEnterReadLock(lockTimeout))
                {
                    foreach (TimingLocation loc in locations.Values)
                    {
                        if (loc.Name.Equals(tp.Name, StringComparison.OrdinalIgnoreCase)
                            && loc.EventIdentifier == tp.EventIdentifier)
                        {
                            output = loc.Identifier;
                            break;
                        }
                    }
                    memStoreLock.ExitReadLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
            return output;
        }

        public List<TimingLocation> GetTimingLocations(int eventId)
        {
            Log.D("MemStore", "GetTimingLocations");
            List<TimingLocation> output = new();
            try
            {
                if (memStoreLock.TryEnterReadLock(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        foreach (TimingLocation loc in locations.Values)
                        {
                            if (loc.Identifier != Constants.Timing.LOCATION_FINISH
                                && loc.Identifier == Constants.Timing.LOCATION_START
                                && loc.Identifier == Constants.Timing.LOCATION_ANNOUNCER)
                            {
                                output.Add(loc);
                            }
                        }
                    }
                    else
                    {
                        output.AddRange(database.GetTimingLocations(eventId));
                    }
                    memStoreLock.ExitReadLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
            return output;
        }

        public void RemoveTimingLocation(TimingLocation tp)
        {
            Log.D("MemStore", "RemoveTimingLocation");
            database.RemoveTimingLocation(tp);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    locations.Remove(tp.Identifier);
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void RemoveTimingLocation(int identifier)
        {
            Log.D("MemStore", "RemoveTimingLocation");
            database.RemoveTimingLocation(identifier);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    locations.Remove(identifier);
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void UpdateTimingLocation(TimingLocation tp)
        {
            Log.D("MemStore", "RemoveTimingLocation");
            database.UpdateTimingLocation(tp);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    if (locations.TryGetValue(tp.Identifier, out TimingLocation loc))
                    {
                        loc.CopyFrom(tp);
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
    }
}
