using System;

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
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    settings.TryGetValue(name, out output);
                    memStoreLock.ReleaseMutex();
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
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    settings[name] = new() { Name = name, Value = value };
                    memStoreLock.ReleaseMutex();
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
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    settings[setting.Name] = setting;
                    memStoreLock.ReleaseMutex();
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
