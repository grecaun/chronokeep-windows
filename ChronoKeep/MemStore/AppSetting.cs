using Chronokeep.Objects;
using System.Collections.Generic;
using System;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * AppSetting Functions
         */

        public AppSetting GetAppSetting(string name)
        {
            Log.D("MemStore", "GetAppSetting");
            AppSetting output = null;
            try
            {
                if (memStoreLock.TryEnterReadLock(lockTimeout))
                {
                    settings.TryGetValue(name, out output);
                    memStoreLock.ExitReadLock();
                }
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void SetAppSetting(string name, string value)
        {
            Log.D("MemStore", "SetAppSetting");
            database.SetAppSetting(name, value);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    settings[name] = new() { Name = name, Value = value };
                    memStoreLock.ExitWriteLock();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new MutexLockException("memStoreLock");
            }
        }

        public void SetAppSetting(AppSetting setting)
        {
            Log.D("MemStore", "SetAppSetting");
            database.SetAppSetting(setting);
            try
            {
                if (memStoreLock.TryEnterWriteLock(lockTimeout))
                {
                    settings[setting.Name] = setting;
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
