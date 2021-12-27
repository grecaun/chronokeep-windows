using ChronoKeep.Interfaces;
using ChronoKeep.Network.API;
using ChronoKeep.Objects;
using ChronoKeep.Objects.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoKeep.API
{
    class APIController
    {
        readonly IMainWindow mainWindow;
        readonly IDBInterface database;

        private static readonly Mutex mut = new Mutex();
        private static readonly Semaphore waiter = new Semaphore(0, 1);
        private static bool Running = false;
        private static bool KeepAlive = true;

        public int Errors { get; private set; }

        private static readonly int SleepSeconds = 30;

        public APIController(IMainWindow mainWindow, IDBInterface database)
        {
            this.database = database;
            this.mainWindow = mainWindow;
            this.Errors = 0;
        }

        public static async Task<AddResultsResponse> DeleteResults(ResultsAPI api, string slug, string year)
        {
            AddResultsResponse response = null;
            try
            {
                response = await APIHandlers.DeleteResults(api, slug, year);
                Log.D("API.APIController", "API Controller response: " + response.Count);
            }
            catch { }
            return response;
        }

        public static bool GrabMutex(int millisecondsTimeout)
        {
            if (mut.WaitOne(millisecondsTimeout))
            {
                return true;
            }
            return false;
        }

        public static void ReleaseMutex()
        {
            mut.ReleaseMutex();
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
            if (mut.WaitOne(3000))
            {
                Log.D("API.APIController", "Shutting down API Auto Upload.");
                KeepAlive = false;
                waiter.Release();
                mut.ReleaseMutex();
            }
            else
            {
                // Unable to get mutex.
                return;
            }
        }

        public async void Run()
        {
            Log.D("API.APIController", "API Controller is now running.");
            if (mut.WaitOne(3000))
            {
                if (Running)
                {
                    Log.D("API.APIController", "API Controller thread is already running.");
                    mut.ReleaseMutex();
                    return;
                }
                Running = true;
                mut.ReleaseMutex();
            }
            else
            {
                Log.D("API.APIController", "Unable to aquire mutex.");
                return;
            }
            mainWindow.UpdateTimingFromController();
            // keep looping until told to stop
            while (true)
            {
                // Start upload of data to API.
                Event theEvent = database.GetCurrentEvent();
                // Get API to upload. Exit if not found
                if (theEvent.API_ID < 0 && theEvent.API_Event_ID.Length > 1)
                {
                    Log.D("API.APIController", "Unable to find API information.");
                    KeepAlive = false;
                    Running = false;
                    mainWindow.UpdateTimingFromController();
                    return;
                }
                ResultsAPI api;
                try
                {
                    api = database.GetResultsAPI(theEvent.API_ID);
                } catch
                {
                    Log.D("API.APIController", "Database doesn't contain information about the specified API.");
                    KeepAlive = false;
                    Running = false;
                    mainWindow.UpdateTimingFromController();
                    return;
                }
                // Get the event id values. Exit if not valid.
                string[] event_ids = theEvent.API_Event_ID.Split(',');
                if (event_ids.Length != 2)
                {
                    Log.D("API.APIController", "Event ID values for API upload not valid.");
                    KeepAlive = false;
                    Running = false;
                    mainWindow.UpdateTimingFromController();
                    return;
                }
                // Get results to upload.
                List<TimeResult> results = database.GetNonUploadedResults(theEvent.Identifier);
                Log.D("API.APIController", "Results count: " + results.Count.ToString());
                if (results.Count > 0)
                {
                    // Change TimeResults to APIResults
                    List<APIResult> upRes = new List<APIResult>();
                    foreach (TimeResult tr in results)
                    {
                        tr.Uploaded = Constants.Timing.TIMERESULT_UPLOADED_TRUE;
                        upRes.Add(new APIResult(theEvent, tr));
                    }
                    Log.D("API.APIController", "Attempting to upload " + upRes.Count.ToString() + " results.");
                    int total = 0;
                    int loops = upRes.Count / Constants.Timing.API_LOOP_COUNT;
                    AddResultsResponse response;
                    for (int i = 0; i < loops; i += 1)
                    {
                        Log.D("API.APIController", string.Format("Loop {0}", i));
                        try
                        {
                            response = await APIHandlers.UploadResults(api, event_ids[0], event_ids[1], upRes.GetRange(i * Constants.Timing.API_LOOP_COUNT, Constants.Timing.API_LOOP_COUNT));
                        }
                        catch
                        {
                            // Error uploading due to network issues most likely. Keep tally of these errors but continue running.
                            Log.D("API.APIController", "Unable to handle API response. Loop " + i);
                            this.Errors += 1;
                            mainWindow.UpdateTimingFromController();
                            return;
                        }
                        if (response != null)
                        {
                            total += response.Count;
                            Log.D("API.APIController", "Total: " + total + " Count: " + response.Count);
                        }
                    }
                    int leftovers = upRes.Count - (loops * Constants.Timing.API_LOOP_COUNT);
                    if (leftovers > 0)
                    {
                        try
                        {
                            response = await APIHandlers.UploadResults(api, event_ids[0], event_ids[1], upRes.GetRange(loops * Constants.Timing.API_LOOP_COUNT, leftovers));
                        }
                        catch
                        {
                            // Error uploading due to network issues most likely. Keep tally of these errors but continue running.
                            Log.D("API.APIController", "Unable to handle API response. Leftovers");
                            this.Errors += 1;
                            mainWindow.UpdateTimingFromController();
                            return;
                        }
                        if (response != null)
                        {
                            total += response.Count;
                            Log.D("API.APIController", "Total: " + total + " Count: " + response.Count);
                        }
                        Log.D("API.APIController", "Upload finished. Count total: " + total);
                    }
                    if (results.Count == total)
                    {
                        Log.D("API.APIController", "Count matches, updating records.");
                        database.AddTimingResults(results);
                    }
                }
                // Block with timeout on a semaphore
                // Use this to allow us to only send information every SleepSeconds seconds.
                // We could check for if we've been signaled, but we're only signaled if we're
                // told to exit, so we can just check KeepAlive after.
                Log.D("API.APIController", "Waiting to upload more results.");
                waiter.WaitOne(SleepSeconds * 1000);
                // Check if we're supposed to exit the loop
                if (mut.WaitOne(3000))
                {
                    Log.D("API.APIController", "Checking keep alive status.");
                    if (!KeepAlive)
                    {
                        Log.D("API.APIController", "Exiting API thread.");
                        Running = false;
                        mut.ReleaseMutex();
                        mainWindow.UpdateTimingFromController();
                        return;
                    }
                    mut.ReleaseMutex();
                }
                else
                {
                    Log.D("API.APIController", "Error with API mutex.");
                    KeepAlive = false;
                    Running = false;
                    // Unable to get mutex.
                    return;
                }
            }
        }
    }
}
