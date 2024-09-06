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
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                locationsLock.AcquireWriterLock(lockTimeout);
                int output = -1;
                output = database.AddTimingLocation(tp);
                tp.Identifier = output;
                if (tp.EventIdentifier == theEvent.Identifier && tp.Identifier > 0)
                {
                    locations[tp.Identifier] = tp;
                }
                eventLock.ReleaseReaderLock();
                locationsLock.ReleaseWriterLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring locationsLock. " + e.Message);
                throw new MutexLockException("locationsLock");
            }
        }

        public List<TimingLocation> AddTimingLocations(List<TimingLocation> locs)
        {
            Log.D("MemStore", "AddTimingLocations");
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                locationsLock.AcquireWriterLock(lockTimeout);
                List<TimingLocation> output = new();
                output = database.AddTimingLocations(locs);
                foreach (TimingLocation tp in locs)
                {
                    if (tp.EventIdentifier == theEvent.Identifier && tp.Identifier > 0)
                    {
                        locations[tp.Identifier] = tp;
                    }
                }
                eventLock.ReleaseReaderLock();
                locationsLock.ReleaseWriterLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring locationsLock. " + e.Message);
                throw new MutexLockException("locationsLock");
            }
        }

        public int GetTimingLocationID(TimingLocation tp)
        {
            Log.D("MemStore", "GetTimingLocationID");
            try
            {
                locationsLock.AcquireReaderLock(lockTimeout);
                int output = -1;
                foreach (TimingLocation loc in locations.Values)
                {
                    if (loc.Name.Equals(tp.Name, StringComparison.OrdinalIgnoreCase)
                        && loc.EventIdentifier == tp.EventIdentifier)
                    {
                        output = loc.Identifier;
                        break;
                    }
                }
                locationsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring locationsLock. " + e.Message);
                throw new MutexLockException("locationsLock");
            }
        }

        public List<TimingLocation> GetTimingLocations(int eventId)
        {
            Log.D("MemStore", "GetTimingLocations");
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
                return database.GetTimingLocations(eventId);
            }
            try
            {
                locationsLock.AcquireReaderLock(lockTimeout);
                List<TimingLocation> output = new();
                output.AddRange(locations.Values);
                locationsLock.ReleaseReaderLock();
                output.RemoveAll(x => x.Identifier == Constants.Timing.LOCATION_FINISH || x.Identifier == Constants.Timing.LOCATION_START);
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring locationsLock. " + e.Message);
                throw new MutexLockException("locationsLock");
            }
        }

        public void RemoveTimingLocation(TimingLocation tp)
        {
            Log.D("MemStore", "RemoveTimingLocation");
            try
            {
                locationsLock.AcquireWriterLock(lockTimeout);
                database.RemoveTimingLocation(tp);
                locations.Remove(tp.Identifier);
                locationsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring locationsLock. " + e.Message);
                throw new MutexLockException("locationsLock");
            }
        }

        public void RemoveTimingLocation(int identifier)
        {
            Log.D("MemStore", "RemoveTimingLocation");
            try
            {
                locationsLock.AcquireWriterLock(lockTimeout);
                database.RemoveTimingLocation(identifier);
                locations.Remove(identifier);
                locationsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring locationsLock. " + e.Message);
                throw new MutexLockException("locationsLock");
            }
        }

        public void UpdateTimingLocation(TimingLocation tp)
        {
            Log.D("MemStore", "RemoveTimingLocation");
            try
            {
                locationsLock.AcquireWriterLock(lockTimeout);
                database.UpdateTimingLocation(tp);
                if (locations.TryGetValue(tp.Identifier, out TimingLocation loc))
                {
                    loc.CopyFrom(tp);
                }
                locationsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring locationsLock. " + e.Message);
                throw new MutexLockException("locationsLock");
            }
        }
    }
}
