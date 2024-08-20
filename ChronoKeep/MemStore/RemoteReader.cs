using Chronokeep.Objects.ChronokeepRemote;
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
            throw new System.NotImplementedException();
        }

        public void DeleteRemoteReader(int eventId, RemoteReader reader)
        {
            throw new System.NotImplementedException();
        }

        public void DeleteRemoteReaders(int eventId, List<RemoteReader> readers)
        {
            throw new System.NotImplementedException();
        }

        public List<RemoteReader> GetRemoteReaders(int eventId)
        {
            throw new System.NotImplementedException();
        }
    }
}
