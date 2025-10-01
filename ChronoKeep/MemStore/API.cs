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
         * API functions
         */

        public int AddAPI(APIObject anAPI)
        {
            Log.D("MemStore", "UpdateAgeGroup");
            anAPI.Identifier = database.AddAPI(anAPI);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        apis[anAPI.Identifier] = anAPI;
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
                return anAPI.Identifier;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new ChronoLockException("apiLock");
            }
        }

        public List<APIObject> GetAllAPI()
        {
            Log.D("MemStore", "GetAllAPI");
            List<APIObject> output = [];
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        output.AddRange(apis.Values);
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new ChronoLockException("apiLock");
            }
            return output;
        }

        public APIObject GetAPI(int identifier)
        {
            Log.D("MemStore", "GetAPI");
            APIObject output = null;
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        if (!apis.TryGetValue(identifier, out output))
                        {
                            output = null;
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
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new ChronoLockException("apiLock");
            }
            return output;
        }

        public void RemoveAPI(int identifier)
        {
            Log.D("MemStore", "RemoveAPI");
            database.RemoveAPI(identifier);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        apis.Remove(identifier);
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new ChronoLockException("apiLock");
            }
        }

        public void UpdateAPI(APIObject anAPI)
        {
            Log.D("MemStore", "UpdateAPI");
            database.UpdateAPI(anAPI);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
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
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring apiLock. " + e.Message);
                throw new ChronoLockException("apiLock");
            }
        }
    }
}
