using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.Timing;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronokeepRemote;
using Chronokeep.Timing.Announcer;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Chronokeep.Timing.Remote
{
    class RemoteReadsController(IMainWindow mainWindow, IDBInterface database) : IRemoteReadersChangeSubscriber
    {
        private static readonly Lock remRLock = new();
        private static readonly Semaphore waiter = new(0, 1);

        private static bool Running = false;
        private static bool KeepAlive = true;
        private static bool UpdateReaders = true;

        private static readonly int SleepSeconds = 30;

        public int Errors { get; private set; } = 0;
        private readonly Dictionary<RemoteReader, DateTime> lastReaderTime = [];
        private readonly Dictionary<RemoteReader, long> RemoteNotificationDictionary = [];

        public static bool IsRunning()
        {
            bool output = false;
            if (remRLock.TryEnter(6000))
            {
                try
                {
                    output = Running;
                }
                finally
                {
                    remRLock.Exit();
                }
            }
            return output;
        }

        public void Shutdown()
        {
            if (remRLock.TryEnter(6000))
            {
                try
                {
                    Log.D("API.RemoteReadsController", "Shutting down API Auto Upload.");
                    KeepAlive = false;
                    waiter.Release();
                }
                finally
                {
                    remRLock.Exit();
                }
            }
        }

        public async void Run()
        {
            Log.D("API.RemoteReadsController", "RemoteReadsController is now running.");
            if (remRLock.TryEnter(6000))
            {
                try
                {
                    if (Running)
                    {
                        Log.D("API.RemoteReadsController", "RemoteReadsController is already running.");
                        return;
                    }
                    Running = true;
                    KeepAlive = true;
                }
                finally
                {
                    remRLock.Exit();
                }
            }
            else
            {
                Log.D("API.RemoteReadsController", "Unable to acquire lock.");
                return;
            }
            mainWindow.UpdateTimingFromController();
            // keep looping until told to stop
            Dictionary<int, APIObject> apiDictionary = [];
            List<RemoteReader> readers = [];
            // Subscribe to reader changes.
            RemoteReadersNotifier.GetRemoteReadersNotifier().Subscribe(this);
            while (true)
            {
                // check if we need to update our list of readers
                if (remRLock.TryEnter(3000))
                {
                    try
                    {
                        if (UpdateReaders)
                        {
                            Log.D("API.RemoteReadsController", "Updating readers.");
                            var theEvent = database.GetCurrentEvent();
                            readers = database.GetRemoteReaders(theEvent.Identifier);
                            apiDictionary.Clear();
                            foreach (APIObject api in database.GetAllAPI())
                            {
                                if (api.Type == Constants.APIConstants.CHRONOKEEP_REMOTE || api.Type == Constants.APIConstants.CHRONOKEEP_REMOTE_SELF)
                                {
                                    apiDictionary[api.Identifier] = api;
                                }
                            }
                            UpdateReaders = false;
                        }
                    }
                    finally
                    {
                        remRLock.Exit();
                    }
                }
                // don't query if we just started
                DateTime now = DateTime.Now;
                // Start will start out at the start of the current day for each reader
                // It will be changed based upon the last time value a reader sent us
                DateTime start, end = new(now.Year, now.Month, now.Day, 23, 59, 59);
                bool api_error = false;
                bool announcer_notify = false;
                foreach (RemoteReader reader in readers)
                {
                    if (reader.LocationID == Constants.Timing.LOCATION_ANNOUNCER)
                    {
                        announcer_notify = true;
                    }
                    // make sure we know how to check the api
                    if (apiDictionary.TryGetValue(reader.APIIDentifier, out APIObject api))
                    {
                        // reset start to the start of the day each loop
                        DateTime dateTime = new(now.Year, now.Month, now.Day, 0, 0, 0);
                        start = dateTime;
                        if (lastReaderTime.TryGetValue(reader, out DateTime lastTime))
                        {
                            // query 1 second before just in case the reader didn't send us everything they had
                            // due to really good timing on our part
                            start = lastTime.AddSeconds(-1);
                        }
                        List<ChipRead> reads = [];
                        try
                        {
                            RemoteNotification note;
                            (reads, note) = await api.GetReads(reader, start, end);
                            Log.D("API.RemoteReadsController", note == null ? "null" : note.Type);
                            if (note != null
                                && !(RemoteNotificationDictionary.TryGetValue(reader, out long noteId)
                                    && noteId == note.Id))
                            {
                                mainWindow.ShowNotificationDialog(reader.Name, "Remote", note);
                                RemoteNotificationDictionary[reader] = note.Id;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.E("API.RemoteReadsController", "Unable to handle API response - " + ex.Message);
                            api_error = true;
                        }
                        foreach (ChipRead read in reads)
                        {
                            // we want to keep track of the last reader the reader recorded so we don't request
                            // a time period we've already requested.
                            if (!lastReaderTime.TryGetValue(reader, out DateTime lTime) || lTime < read.Time)
                            {
                                lTime = read.Time;
                                lastReaderTime[reader] = lTime;
                            }
                        }
                        database.AddChipReads(reads);
                    }
                }
                if (announcer_notify)
                {
                    AnnouncerWorker.Notify();
                }
                if (api_error)
                {
                    Errors += 1;
                }
                else if (Errors > 0)
                {
                    Errors = 0;
                }
                mainWindow.UpdateTimingFromController();
                // wait for our sleep period
                Log.D("API.RemoteReadsController", "Waiting to download more reads.");
                int sleepFor = Globals.DownloadInterval;
                if (sleepFor < 1 || sleepFor > 60)
                {
                    sleepFor = SleepSeconds;
                }
                // Block with timeout on a semaphore
                // Use this to allow us to only send information every so often based upon a global
                // interval set, or the SleepSeconds value if the global value isn't in the correct range.
                // We could check for if we've been signaled, but we're only signaled if we're
                // told to exit, so we can just check KeepAlive after.
                waiter.WaitOne(sleepFor * 1000);
                // check if we should exit the loop
                if (remRLock.TryEnter(6000))
                {
                    try
                    {
                        Log.D("API.RemoteReadsController", "Checking keep alive status.");
                        if (!KeepAlive)
                        {
                            Log.D("API.RemoteReadsController", "Exiting RemoteReads thread.");
                            Running = false;
                            mainWindow.UpdateTimingFromController();
                            RemoteReadersNotifier.GetRemoteReadersNotifier().Unsubscribe(this);
                            return;
                        }
                    }
                    finally
                    {
                        remRLock.Exit();
                    }
                }
                else
                {
                    Log.D("API.RemoteReadsController", "Error with RemoteReads lock.");
                    KeepAlive = false;
                    Running = false;
                    mainWindow.UpdateTimingFromController();
                    RemoteReadersNotifier.GetRemoteReadersNotifier().Unsubscribe(this);
                    return;
                }
            }
        }

        public void NotifyRemoteReadersChange()
        {
            if (remRLock.TryEnter(3000))
            {
                try
                {
                    UpdateReaders = true;
                }
                finally
                {
                    remRLock.Exit();
                }
            }
        }
    }
}
