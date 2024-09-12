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
            anAPI.Identifier = database.AddAPI(anAPI);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    apis[anAPI.Identifier] = anAPI;
                    memStoreLock.ReleaseMutex();
                }
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
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    output.AddRange(apis.Values);
                    memStoreLock.ReleaseMutex();
                }
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
            APIObject output = null;
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (!apis.TryGetValue(identifier, out output))
                    {
                        output = null;
                    }
                    memStoreLock.ReleaseMutex();
                }
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
            database.RemoveAPI(identifier);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    apis.Remove(identifier);
                    memStoreLock.ReleaseMutex();
                }
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
            database.UpdateAPI(anAPI);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
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
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new MutexLockException("apiLock");
            }
        }
    }
}
