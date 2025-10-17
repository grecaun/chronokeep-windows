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
         * Banned Email/Phone Functions
         */

        public List<Chronoclock> GetClocks()
        {
            Log.D("MemStore", "GetClocks");
            List<Chronoclock> output = [];
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        output.AddRange(clocks.Values);
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
            return output;
        }

        public int AddClock(Chronoclock clock)
        {
            Log.D("MemStore", "AddClock");
            clock.Identifier = database.AddClock(clock);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        clocks[clock.Identifier] = clock;
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
            return clock.Identifier;
        }

        public void UpdateClock(Chronoclock clock)
        {
            Log.D("MemStore", "ClearBannedEmails");
            database.UpdateClock(clock);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        clocks[clock.Identifier] = clock;
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

        public void RemoveClocks(List<Chronoclock> iClocks)
        {
            Log.D("MemStore", "RemoveBannedPhones");
            database.RemoveClocks(iClocks);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        foreach (Chronoclock clock in iClocks)
                        {
                            clocks.Remove(clock.Identifier);
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
    }
}
