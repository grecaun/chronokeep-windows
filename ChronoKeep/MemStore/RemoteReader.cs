using Chronokeep.Objects.ChronokeepRemote;
using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * RemoteReader Functions
         */

        public void AddRemoteReaders(int eventId, List<RemoteReader> readers)
        {
            Log.D("MemStore", "AddRemoteReaders");
            database.AddRemoteReaders(eventId, readers);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        foreach (RemoteReader reader in readers)
                        {
                            remoteReaders.RemoveAll(x => reader.APIIDentifier == x.APIIDentifier && reader.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
                            remoteReaders.Add(reader);
                        }
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void DeleteRemoteReader(int eventId, RemoteReader reader)
        {
            Log.D("MemStore", "DeleteRemoteReader");
            database.DeleteRemoteReader(eventId, reader);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        remoteReaders.RemoveAll(x => reader.APIIDentifier == x.APIIDentifier && reader.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void DeleteRemoteReaders(int eventId, List<RemoteReader> readers)
        {
            Log.D("MemStore", "DeleteRemoteReaders");
            database.DeleteRemoteReaders(eventId, readers);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        foreach (RemoteReader reader in readers)
                        {
                            remoteReaders.RemoveAll(x => reader.APIIDentifier == x.APIIDentifier && reader.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
                        }
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public List<RemoteReader> GetRemoteReaders(int eventId)
        {
            Log.D("MemStore", "GetRemoteReaders");
            List<RemoteReader> output = new();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        output.AddRange(remoteReaders);
                    }
                    else
                    {
                        output.AddRange(database.GetRemoteReaders(eventId));
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
            return output;
        }
    }
}
