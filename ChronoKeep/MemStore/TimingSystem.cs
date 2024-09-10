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
            int output = database.AddTimingSystem(system);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    system.SystemIdentifier = output;
                    timingSystems[system.IPAddress.Trim()] = system;
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

        public List<TimingSystem> GetTimingSystems()
        {
            Log.D("MemStore", "GetTimingSystems");
            List<TimingSystem> output = new();
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    output.AddRange(timingSystems.Values);
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

        public void RemoveTimingSystem(TimingSystem system)
        {
            Log.D("MemStore", "RemoveTimingSystem");
            database.RemoveTimingSystem(system);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    timingSystems.Remove(system.IPAddress.Trim());
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void RemoveTimingSystem(int systemId)
        {
            Log.D("MemStore", "RemoveTimingSystem");
            database.RemoveTimingSystem(systemId);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    string ip = "";
                    foreach (TimingSystem system in timingSystems.Values)
                    {
                        if (system.SystemIdentifier == systemId)
                        {
                            ip = system.IPAddress.Trim();
                            break;
                        }
                    }
                    if (ip.Length > 0)
                    {
                        timingSystems.Remove(ip);
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

        public void SetTimingSystems(List<TimingSystem> systems)
        {
            Log.D("MemStore", "SetTimingSystems");
            database.SetTimingSystems(systems);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    timingSystems.Clear();
                    foreach (TimingSystem system in systems)
                    {
                        timingSystems[system.IPAddress.Trim()] = system;
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

        public void UpdateTimingSystem(TimingSystem system)
        {
            Log.D("MemStore", "UpdateTimingSystem");
            database.UpdateTimingSystem(system);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    if (timingSystems.TryGetValue(system.IPAddress.Trim(), out TimingSystem oldSystem))
                    {
                        oldSystem.CopyFrom(system);
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
