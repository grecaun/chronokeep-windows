using Chronokeep.Objects;
using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * API functions
         */

        public int AddAPI(APIObject anAPI)
        {
            Log.D("MemStore", "UpdateAgeGroup");
            try
            {
                apiLock.AcquireWriterLock(lockTimeout);
                anAPI.Identifier = database.AddAPI(anAPI);
                apis[anAPI.Identifier] = anAPI;
                apiLock.ReleaseWriterLock();
                return anAPI.Identifier;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new MutexLockException("apiLock");
            }
        }

        public List<APIObject> GetAllAPI()
        {
            Log.D("MemStore", "GetAllAPI");
            List<APIObject> output = new();
            try
            {
                apiLock.AcquireReaderLock(lockTimeout);
                output.AddRange(apis.Values);
                apiLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new MutexLockException("apiLock");
            }
            return output;
        }

        public APIObject GetAPI(int identifier)
        {
            Log.D("MemStore", "GetAPI");
            APIObject output;
            try
            {
                apiLock.AcquireReaderLock(lockTimeout);
                if (!apis.TryGetValue(identifier, out output))
                {
                    output = null;
                }
                apiLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new MutexLockException("apiLock");
            }
            return output;
        }

        public void RemoveAPI(int identifier)
        {
            Log.D("MemStore", "RemoveAPI");
            try
            {
                apiLock.AcquireWriterLock(lockTimeout);
                database.RemoveAPI(identifier);
                apis.Remove(identifier);
                apiLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new MutexLockException("apiLock");
            }
        }

        public void UpdateAPI(APIObject anAPI)
        {
            Log.D("MemStore", "UpdateAPI");
            try
            {
                apiLock.AcquireWriterLock(lockTimeout);
                database.UpdateAPI(anAPI);
                if (apis.TryGetValue(anAPI.Identifier, out APIObject api))
                {
                    api.Type = anAPI.Type;
                    api.URL = anAPI.URL;
                    api.AuthToken = anAPI.AuthToken;
                    api.Nickname = anAPI.Nickname;
                    api.WebURL = anAPI.WebURL;
                }
                else
                {
                    apis[anAPI.Identifier] = anAPI;
                }
                apiLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new MutexLockException("apiLock");
            }
        }
    }
}
