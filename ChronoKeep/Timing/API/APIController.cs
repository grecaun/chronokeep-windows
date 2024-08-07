﻿using Chronokeep.Helpers;
using Chronokeep.Interfaces;
using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.API;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronokeep.Timing.API
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

        public static async Task<AddResultsResponse> DeleteResults(APIObject api, string slug, string year)
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
            if (mut.WaitOne(6000))
            {
                Log.D("API.APIController", "Shutting down API Auto Upload.");
                KeepAlive = false;
                waiter.Release();
                mut.ReleaseMutex();
            }
        }

        public async void Run()
        {
            Log.D("API.APIController", "API Controller is now running.");
            if (mut.WaitOne(6000))
            {
                if (Running)
                {
                    Log.D("API.APIController", "API Controller thread is already running.");
                    mut.ReleaseMutex();
                    return;
                }
                Running = true;
                KeepAlive = true;
                mut.ReleaseMutex();
            }
            else
            {
                Log.D("API.APIController", "Unable to acquire mutex.");
                return;
            }
            mainWindow.UpdateTimingFromController();
            // keep looping until told to stop
            while (true)
            {
                // Boolean for tracking errors to abort current loot.
                bool loop_error = false;
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
                APIObject api;
                try
                {
                    api = database.GetAPI(theEvent.API_ID);
                }
                catch
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
                // Remove all results to upload that don't have a place set, are not DNF/DNS results, and are also not start times.
                results.RemoveAll(x => x.Place < 1
                    && x.Status != Constants.Timing.TIMERESULT_STATUS_DNF
                    && x.Status != Constants.Timing.TIMERESULT_STATUS_DNS
                    && x.SegmentId != Constants.Timing.SEGMENT_START);
                Log.D("API.APIController", "Results count: " + results.Count.ToString());
                if (results.Count > 0)
                {
                    // Change TimeResults to APIResults
                    List<APIResult> upRes = new List<APIResult>();
                    DateTime start = DateTime.SpecifyKind(DateTime.Parse(theEvent.Date), DateTimeKind.Local).AddSeconds(theEvent.StartSeconds).AddMilliseconds(theEvent.StartMilliseconds);
                    Dictionary<string, DateTime> waveStartTimes = new();
                    foreach (Distance d in database.GetDistances(theEvent.Identifier))
                    {
                        waveStartTimes[d.Name] = start.AddSeconds(d.StartOffsetSeconds).AddMilliseconds(d.StartOffsetMilliseconds);
                    }
                    foreach (TimeResult tr in results)
                    {
                        tr.Uploaded = Constants.Timing.TIMERESULT_UPLOADED_TRUE;
                        DateTime trStart = waveStartTimes.ContainsKey(tr.RealDistanceName) ? waveStartTimes[tr.RealDistanceName] : start;
                        upRes.Add(new APIResult(theEvent, tr, trStart));
                    }
                    Log.D("API.APIController", "Attempting to upload " + upRes.Count.ToString() + " results.");
                    int total = 0;
                    int loops = upRes.Count / Constants.Timing.API_LOOP_COUNT;
                    AddResultsResponse response = null;
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
                            loop_error = true;
                            break;
                        }
                        if (response != null)
                        {
                            total += response.Count;
                            Log.D("API.APIController", "Total: " + total + " Count: " + response.Count);
                        }
                    }
                    int leftovers = upRes.Count - (loops * Constants.Timing.API_LOOP_COUNT);
                    if (leftovers > 0 && !loop_error)
                    {
                        response = null;
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
                            loop_error = true;
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
                    if (!loop_error)
                    {
                        this.Errors = 0;
                    }
                    mainWindow.UpdateTimingFromController();
                }
                else // KeepAlive check
                {
                    try
                    {
                        bool healthy = await APIHandlers.IsHealthy(api);
                        // clear errors if we don't get an exception
                        if (healthy)
                        {
                            this.Errors = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.D("API.APIController", ex.Message);
                        this.Errors += 1;
                        mainWindow.UpdateTimingFromController();
                    }
                }
                // Block with timeout on a semaphore
                // Use this to allow us to only send information every so often based upon a global
                // interval set, or the SleepSeconds value if the global value isn't in the correct range.
                // We could check for if we've been signaled, but we're only signaled if we're
                // told to exit, so we can just check KeepAlive after.
                Log.D("API.APIController", "Waiting to upload more results.");
                int sleepFor = Globals.UploadInterval;
                if (sleepFor < 1 || sleepFor > 60)
                {
                    sleepFor = SleepSeconds;
                }
                waiter.WaitOne(sleepFor * 1000);
                // Check if we're supposed to exit the loop
                if (mut.WaitOne(6000))
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
                    mainWindow.UpdateTimingFromController();
                    return;
                }
            }
        }
    }
}
