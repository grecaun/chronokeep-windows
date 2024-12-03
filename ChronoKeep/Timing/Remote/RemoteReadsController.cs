using Chronokeep.Helpers;
using Chronokeep.Interfaces;
using Chronokeep.Interfaces.Timing;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronokeepRemote;
using Chronokeep.Timing.Announcer;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Chronokeep.Timing.Remote
{
    class RemoteReadsController : IRemoteReadersChangeSubscriber
    {
        readonly IMainWindow mainWindow;
        readonly IDBInterface database;

        private static readonly Mutex mut = new();
        private static readonly Semaphore waiter = new Semaphore(0, 1);

        private static bool Running = false;
        private static bool KeepAlive = true;
        private static bool UpdateReaders = true;

        private static readonly int SleepSeconds = 30;

        public int Errors { get; private set; }
        private Dictionary<RemoteReader, DateTime> lastReaderTime = new Dictionary<RemoteReader, DateTime>();
        private Dictionary<RemoteReader, long> RemoteNotificationDictionary = new Dictionary<RemoteReader, long>();

        public RemoteReadsController(IMainWindow mainWindow, IDBInterface database)
        {
            this.mainWindow = mainWindow;
            this.database = database;
            this.Errors = 0;
        }

        public static bool IsRunning()
        {
            bool output = false;
            if (mut.WaitOne(6000))
            {
                output = Running;
                mut.ReleaseMutex();
            }
            return output;
        }

        public void Shutdown()
        {
            if (mut.WaitOne(6000))
            {
                Log.D("API.RemoteReadsController", "Shutting down API Auto Upload.");
                KeepAlive = false;
                waiter.Release();
                mut.ReleaseMutex();
            }
        }

        public async void Run()
        {
            Log.D("API.RemoteReadsController", "RemoteReadsController is now running.");
            if (mut.WaitOne(6000))
            {
                if (Running)
                {
                    Log.D("API.RemoteReadsController", "RemoteReadsController is already running.");
                    mut.ReleaseMutex();
                    return;
                }
                Running = true;
                KeepAlive = true;
                mut.ReleaseMutex();
            }
            else
            {
                Log.D("API.RemoteReadsController", "Unable to acquire mutex.");
                return;
            }
            mainWindow.UpdateTimingFromController();
            // keep looping until told to stop
            Dictionary<int, APIObject> apiDictionary = new();
            List<RemoteReader> readers = new();
            // Subscribe to reader changes.
            RemoteReadersNotifier.GetRemoteReadersNotifier().Subscribe(this);
            while (true)
            {
                // check if we need to update our list of readers
                if (mut.WaitOne(3000))
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
                    mut.ReleaseMutex();
                }
                // don't query if we just started
                DateTime now = DateTime.Now;
                // Start will start out at the start of the current day for each reader
                // It will be changed based upon the last time value a reader sent us
                DateTime start, end = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59);
                bool api_error = false;
                bool announcer_notify = false;
                foreach (RemoteReader reader in readers)
                {
                    if (reader.LocationID == Constants.Timing.LOCATION_ANNOUNCER)
                    {
                        announcer_notify = true;
                    }
                    // make sure we know how to check the api
                    if (apiDictionary.ContainsKey(reader.APIIDentifier))
                    {
                        // reset start to the start of the day each loop
                        start = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                        if (lastReaderTime.ContainsKey(reader))
                        {
                            // query 1 second before just in case the reader didn't send us everything they had
                            // due to really good timing on our part
                            start = lastReaderTime[reader].AddSeconds(-1);
                        }
                        List<ChipRead> reads = new();
                        try
                        {
                            RemoteNotification note;
                            (reads, note) = await apiDictionary[reader.APIIDentifier].GetReads(reader, start, end);
                            Log.D("API.RemoteReadsController", note == null ? "null" : note.Type);
                            if (note != null
                                && (!RemoteNotificationDictionary.ContainsKey(reader)
                                    || RemoteNotificationDictionary[reader] != note.Id))
                            {
                                mainWindow.ShowNotificationDialog(reader.Name, note);
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
                            if (!lastReaderTime.ContainsKey(reader) || lastReaderTime[reader] < read.Time)
                            {
                                lastReaderTime[reader] = read.Time;
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
                if (mut.WaitOne(6000))
                {
                    Log.D("API.RemoteReadsController", "Checking keep alive status.");
                    if (!KeepAlive)
                    {
                        Log.D("API.RemoteReadsController", "Exiting RemoteReads thread.");
                        Running = false;
                        mut.ReleaseMutex();
                        mainWindow.UpdateTimingFromController();
                        RemoteReadersNotifier.GetRemoteReadersNotifier().Unsubscribe(this);
                        return;
                    }
                    mut.ReleaseMutex();
                }
                else
                {
                    Log.D("API.RemoteReadsController", "Error with RemoteReads mutex.");
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
            if (mut.WaitOne(3000))
            {
                UpdateReaders = true;
                mut.ReleaseMutex();
            }
        }
    }
}
