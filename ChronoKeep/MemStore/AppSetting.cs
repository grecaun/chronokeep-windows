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
            try
            {
                settingsLock.AcquireReaderLock(lockTimeout);
                settings.TryGetValue(name, out AppSetting output);
                settingsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring settingsLock. " + e.Message);
                throw new MutexLockException("settingsLock");
            }
        }

        public void SetAppSetting(string name, string value)
        {
            Log.D("MemStore", "GetAppSetting");
            try
            {
                settingsLock.AcquireWriterLock(lockTimeout);
                database.SetAppSetting(name, value);
                settings[name] = new() { Name = name, Value = value };
                settingsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring settingsLock. " + e.Message);
                throw new MutexLockException("settingsLock");
            }
        }

        public void SetAppSetting(AppSetting setting)
        {
            Log.D("MemStore", "GetAppSetting");
            try
            {
                settingsLock.AcquireWriterLock(lockTimeout);
                database.SetAppSetting(setting);
                settings[setting.Name] = setting;
                settingsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring settingsLock. " + e.Message);
                throw new MutexLockException("settingsLock");
            }
        }
    }
}
