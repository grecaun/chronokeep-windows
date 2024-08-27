using Chronokeep.Objects;
using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * TimingSystem Functions
         */

        public int AddTimingSystem(TimingSystem system)
        {
            Log.D("MemStore", "AddTimingSystem");
            try
            {
                timingSystemsLock.AcquireWriterLock(lockTimeout);
                int output = -1;
                output = database.AddTimingSystem(system);
                system.SystemIdentifier = output;
                timingSystems[system.SystemIdentifier] = system;
                timingSystemsLock.ReleaseWriterLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring timingSystemsLock. " + e.Message);
                throw new MutexLockException("timingSystemsLock");
            }
        }

        public List<TimingSystem> GetTimingSystems()
        {
            Log.D("MemStore", "GetTimingSystems");
            try
            {
                timingSystemsLock.AcquireReaderLock(lockTimeout);
                List<TimingSystem> output = new();
                output.AddRange(timingSystems.Values);
                timingSystemsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring timingSystemsLock. " + e.Message);
                throw new MutexLockException("timingSystemsLock");
            }
        }

        public void RemoveTimingSystem(TimingSystem system)
        {
            Log.D("MemStore", "RemoveTimingSystem");
            try
            {
                timingSystemsLock.AcquireWriterLock(lockTimeout);
                database.RemoveTimingSystem(system);
                timingSystems.Remove(system.SystemIdentifier);
                timingSystemsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring timingSystemsLock. " + e.Message);
                throw new MutexLockException("timingSystemsLock");
            }
        }

        public void RemoveTimingSystem(int systemId)
        {
            Log.D("MemStore", "RemoveTimingSystem");
            try
            {
                timingSystemsLock.AcquireWriterLock(lockTimeout);
                database.RemoveTimingSystem(systemId);
                timingSystems.Remove(systemId);
                timingSystemsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring timingSystemsLock. " + e.Message);
                throw new MutexLockException("timingSystemsLock");
            }
        }

        public void SetTimingSystems(List<TimingSystem> systems)
        {
            Log.D("MemStore", "SetTimingSystems");
            try
            {
                timingSystemsLock.AcquireWriterLock(lockTimeout);
                database.SetTimingSystems(systems);
                timingSystems.Clear();
                foreach (TimingSystem system in systems)
                {
                    timingSystems[system.SystemIdentifier] = system;
                }
                timingSystemsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring timingSystemsLock. " + e.Message);
                throw new MutexLockException("timingSystemsLock");
            }
        }

        public void UpdateTimingSystem(TimingSystem system)
        {
            Log.D("MemStore", "UpdateTimingSystem");
            try
            {
                timingSystemsLock.AcquireWriterLock(lockTimeout);
                database.UpdateTimingSystem(system);
                if (timingSystems.TryGetValue(system.SystemIdentifier, out TimingSystem oldSystem))
                {
                    oldSystem.CopyFrom(system);
                }
                timingSystemsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring timingSystemsLock. " + e.Message);
                throw new MutexLockException("timingSystemsLock");
            }
        }
    }
}
