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
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        settings.TryGetValue(name, out output);
                    }
                    finally
                    {
                        memStoreLock.Exit();
                    }
                }
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring memStoreLock. " + e.Message);
                throw new ChronoLockException("memStoreLock");
            }
        }

        public void SetAppSetting(string name, string value)
        {
            Log.D("MemStore", "SetAppSetting");
            database.SetAppSetting(name, value);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        settings[name] = new() { Name = name, Value = value };
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

        public void SetAppSetting(AppSetting setting)
        {
            Log.D("MemStore", "SetAppSetting");
            database.SetAppSetting(setting);
            try
            {
                if (memStoreLock.TryEnter(lockTimeout))
                {
                    try
                    {
                        settings[setting.Name] = setting;
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
