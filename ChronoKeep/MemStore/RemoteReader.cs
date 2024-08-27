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
                remoteReadersLock.AcquireWriterLock(lockTimeout);
                database.AddRemoteReaders(eventId, readers);
                foreach (RemoteReader reader in readers)
                {
                    remoteReaders.RemoveAll(x => reader.APIIDentifier == x.APIIDentifier && reader.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
                    remoteReaders.Add(reader);
                }
                remoteReadersLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring remoteReadersLock. " + e.Message);
                throw new MutexLockException("remoteReadersLock");
            }
        }

        public void DeleteRemoteReader(int eventId, RemoteReader reader)
        {
            Log.D("MemStore", "DeleteRemoteReader");
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
                remoteReadersLock.AcquireWriterLock(lockTimeout);
                database.DeleteRemoteReader(eventId, reader);
                remoteReaders.RemoveAll(x => reader.APIIDentifier == x.APIIDentifier && reader.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
                remoteReadersLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring remoteReadersLock. " + e.Message);
                throw new MutexLockException("remoteReadersLock");
            }
        }

        public void DeleteRemoteReaders(int eventId, List<RemoteReader> readers)
        {
            Log.D("MemStore", "DeleteRemoteReaders");
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
                remoteReadersLock.AcquireWriterLock(lockTimeout);
                database.DeleteRemoteReaders(eventId, readers);
                foreach (RemoteReader reader in readers)
                {
                    remoteReaders.RemoveAll(x => reader.APIIDentifier == x.APIIDentifier && reader.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase));
                }
                remoteReadersLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring remoteReadersLock. " + e.Message);
                throw new MutexLockException("remoteReadersLock");
            }
        }

        public List<RemoteReader> GetRemoteReaders(int eventId)
        {
            Log.D("MemStore", "GetRemoteReaders");
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
                remoteReadersLock.AcquireReaderLock(lockTimeout);
                List<RemoteReader> output = new();
                output.AddRange(remoteReaders);
                remoteReadersLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring remoteReadersLock. " + e.Message);
                throw new MutexLockException("remoteReadersLock");
            }
        }
    }
}
