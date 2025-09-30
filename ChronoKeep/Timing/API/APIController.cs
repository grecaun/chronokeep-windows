using Chronokeep.Helpers;
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
    class APIController(IMainWindow mainWindow, IDBInterface database)
    {
        private static readonly Lock apiLock = new();
        private static readonly Semaphore waiter = new(0, 1);
        private static bool canUpload = true;
        private static bool isUploading = false;
        private static bool running = false;
        private static bool keepAlive = true;

        public int Errors { get; private set; } = 0;

        private static readonly int SleepSeconds = 30;

        public static async Task<AddResultsResponse> DeleteResults(APIObject api, string slug, string year, string distance)
        {
            AddResultsResponse response = null;
            try
            {
                if (distance != null && distance.Length > 0)
                {
                    response = await APIHandlers.DeleteDistanceResults(api, slug, year, distance);
                }
                else
                {
                    response = await APIHandlers.DeleteResults(api, slug, year);
                }
                Log.D("API.APIController", "API Controller response: " + response.Count);
            }
            catch { }
            return response;
        }

        public static async Task UploadResults(
            List<TimeResult> results,
            APIObject api,
            string[] event_ids,
            IDBInterface database,
            APIController controller,
            IMainWindow mainWindow,
            Event theEvent
            )
        {
            DateTime start = DateTime.SpecifyKind(DateTime.Parse(theEvent.Date), DateTimeKind.Local).AddSeconds(theEvent.StartSeconds).AddMilliseconds(theEvent.StartMilliseconds);
            Dictionary<string, DateTime> waveStartTimes = [];
            HashSet<string> uploadDistances = [];
            foreach (Distance d in database.GetDistances(theEvent.Identifier))
            {
                waveStartTimes[d.Name] = start.AddSeconds(d.StartOffsetSeconds).AddMilliseconds(d.StartOffsetMilliseconds);
                if (d.Upload && d.LinkedDistance == Constants.Timing.DISTANCE_NO_LINKED_ID)
                {
                    uploadDistances.Add(d.Name);
                }
            }
            string unique_pad = "";
            AppSetting uniqueID = database.GetAppSetting(Constants.Settings.PROGRAM_UNIQUE_MODIFIER);
            if (uniqueID != null)
            {
                unique_pad = uniqueID.Value;
            }
            Log.D("API.APIController", "Attempting to upload " + results.Count.ToString() + " results.");
            if (apiLock.TryEnter(3000))
            {
                try
                {
                    if (isUploading)
                    {
                        return;
                    }
                    isUploading = true;
                }
                finally
                {
                    apiLock.Exit();
                }
            }
            else
            {
                throw new Exception("error grabbing lock to signal start");
            }
            int total = 0;
            int loops = results.Count / Constants.Timing.API_LOOP_COUNT;
            AddResultsResponse response;
            bool loop_error = false;
            for (int i = 0; i < loops; i += 1)
            {
                Log.D("API.APIController", string.Format("Loop {0}", i));
                // Change TimeResults to APIResults - breaking this up into chunks so we can
                // properly update them with the UPLOADED field
                List<APIResult> upRes = [];
                List<TimeResult> uploaded = results.GetRange(i * Constants.Timing.API_LOOP_COUNT, Constants.Timing.API_LOOP_COUNT);
                foreach (TimeResult tr in uploaded)
                {
                    //tr.Uploaded = Constants.Timing.TIMERESULT_UPLOADED_TRUE;
                    DateTime trStart = waveStartTimes.TryGetValue(tr.RealDistanceName, out DateTime value) ? value : start;
                    // only add to upload list if we want to upload everything (NOT Specific)
                    // or we only want to upload specific distances and the distance is in the
                    // list of distances we want to upload
                    if (Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA != theEvent.EventType && (!theEvent.UploadSpecific || uploadDistances.Contains(tr.DistanceName)))
                    {
                        upRes.Add(new(theEvent, tr, trStart, unique_pad));
                    }
                    // Make sure that DNF entries are not uploaded when timing Backyard Ultra since multiples are generated and
                    // that info isn't useful to others
                    else if (Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA == theEvent.EventType && Constants.Timing.TIMERESULT_STATUS_DNF != tr.Status)
                    {
                        upRes.Add(new(theEvent, tr, trStart, unique_pad));
                    }
                }
                try
                {
                    response = await APIHandlers.UploadResults(api, event_ids[0], event_ids[1], upRes);
                }
                catch
                {
                    // Error uploading due to network issues most likely. Keep tally of these errors but continue running.
                    Log.D("API.APIController", "Unable to handle API response. Loop " + i);
                    response = null;
                    loop_error = true;
                    if (controller != null)
                    {
                        controller.Errors += 1;
                    }
                    mainWindow?.UpdateTiming();
                    break;
                }
                if (response != null)
                {
                    total += response.Count;
                    Log.D("API.APIController", "Total: " + total + " Count: " + response.Count);
                    if (response.Count == Constants.Timing.API_LOOP_COUNT)
                    {
                        // Updating uploaded value for uploaded results.
                        foreach (TimeResult res in uploaded)
                        {
                            res.Uploaded = Constants.Timing.TIMERESULT_UPLOADED_TRUE;
                        }
                        database.SetUploadedTimingResults(uploaded);
                    }
                }
            }
            int leftovers = results.Count - (loops * Constants.Timing.API_LOOP_COUNT);
            if (leftovers > 0 && !loop_error)
            {
                response = null;
                // Change TimeResults to APIResults
                List<APIResult> upRes = [];
                List<TimeResult> uploaded = results.GetRange(loops * Constants.Timing.API_LOOP_COUNT, leftovers);
                foreach (TimeResult tr in uploaded)
                {
                    //tr.Uploaded = Constants.Timing.TIMERESULT_UPLOADED_TRUE;
                    DateTime trStart = waveStartTimes.TryGetValue(tr.RealDistanceName, out DateTime value) ? value : start;
                    // only add to upload list if we want to upload everything (NOT Specific)
                    // or we only want to upload specific distances and the distance is in the
                    // list of distances we want to upload
                    if (Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA != theEvent.EventType && (!theEvent.UploadSpecific || uploadDistances.Contains(tr.DistanceName)))
                    {
                        upRes.Add(new(theEvent, tr, trStart, unique_pad));
                    }
                    // Make sure that DNF entries are not uploaded when timing Backyard Ultra since multiples are generated and
                    // that info isn't useful to others
                    else if (Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA == theEvent.EventType && Constants.Timing.TIMERESULT_STATUS_DNF != tr.Status)
                    {
                        upRes.Add(new(theEvent, tr, trStart, unique_pad));
                    }
                }
                try
                {
                    response = await APIHandlers.UploadResults(api, event_ids[0], event_ids[1], upRes);
                }
                catch
                {
                    // Error uploading due to network issues most likely. Keep tally of these errors but continue running.
                    Log.D("API.APIController", "Unable to handle API response. Leftovers");
                    loop_error = true;
                    if (controller != null)
                    {
                        controller.Errors += 1;
                    }
                    mainWindow?.UpdateTiming();
                }
                if (response != null)
                {
                    total += response.Count;
                    Log.D("API.APIController", "Total: " + total + " Count: " + response.Count);
                    if (response.Count == leftovers)
                    {
                        // Updating uploaded value for uploaded results;
                        foreach (TimeResult res in uploaded)
                        {
                            res.Uploaded = Constants.Timing.TIMERESULT_UPLOADED_TRUE;
                        }
                        database.SetUploadedTimingResults(uploaded);
                    }
                }
                Log.D("API.APIController", "Upload finished. Count total: " + total);
            }
            if (!loop_error && controller != null)
            {
                controller.Errors = 0;
            }
            if (apiLock.TryEnter(3000))
            {
                try
                {
                    isUploading = false;
                }
                finally
                {
                    apiLock.Exit();
                }
            }
            else
            {
                throw new Exception("error grabbing lock to signal completion");
            }
        }

        public static bool SetUploadableTrue(int millisecondsTimeout)
        {
            if (apiLock.TryEnter(millisecondsTimeout))
            {
                try
                {
                    canUpload = true;
                }
                finally
                {
                    apiLock.Exit();
                }
                return true;
            }
            return false;
        }

        public static bool SetUploadableFalse(int millisecondsTimeout)
        {
            if (apiLock.TryEnter(millisecondsTimeout))
            {
                try
                {
                    canUpload = false;
                }
                finally
                {
                    apiLock.Exit();
                }
                return true;
            }
            return false;
        }

        public static bool GetUploadable(int millisecondsTimeout)
        {
            bool output = false;
            if (apiLock.TryEnter(millisecondsTimeout))
            {
                try
                {
                    output = canUpload;
                }
                finally
                {
                    apiLock.Exit();
                }
            }
            return output;
        }

        public static bool IsUploading()
        {
            bool output = true;
            if (apiLock.TryEnter(3000))
            {
                try
                {
                    output = isUploading;
                }
                finally
                {
                    apiLock.Exit();
                }
            }
            return output;
        }

        public static bool IsRunning()
        {
            bool output = false;
            if (apiLock.TryEnter(6000))
            {
                try
                {
                    output = running;
                }
                finally
                {
                    apiLock.Exit();
                }
            }
            return output;
        }

        public static void Shutdown()
        {
            if (apiLock.TryEnter(6000))
            {
                try
                {
                    Log.D("API.APIController", "Shutting down API Auto Upload.");
                    keepAlive = false;
                    waiter.Release();
                }
                finally
                {
                    apiLock.Exit();
                }
            }
        }

        public async void Run()
        {
            Log.D("API.APIController", "API Controller is now running.");
            if (apiLock.TryEnter(6000))
            {
                try
                {
                    if (running)
                    {
                        Log.D("API.APIController", "API Controller thread is already running.");
                        return;
                    }
                    running = true;
                    keepAlive = true;
                }
                finally
                {
                    apiLock.Exit();
                }
            }
            else
            {
                Log.D("API.APIController", "Unable to acquire lock.");
                return;
            }
            mainWindow.UpdateTiming();
            // keep looping until told to stop
            while (true)
            {
                // Start upload of data to API.
                Event theEvent = database.GetCurrentEvent();
                // Get API to upload. Exit if not found
                if (theEvent.API_ID < 0 && theEvent.API_Event_ID.Length > 1)
                {
                    Log.D("API.APIController", "Unable to find API information.");
                    keepAlive = false;
                    running = false;
                    mainWindow.UpdateTiming();
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
                    keepAlive = false;
                    running = false;
                    mainWindow.UpdateTiming();
                    return;
                }
                // Get the event id values. Exit if not valid.
                string[] event_ids = theEvent.API_Event_ID.Split(',');
                if (event_ids.Length != 2)
                {
                    Log.D("API.APIController", "Event ID values for API upload not valid.");
                    keepAlive = false;
                    running = false;
                    mainWindow.UpdateTiming();
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
                bool upload = false;
                if (apiLock.TryEnter(3000))
                {
                    try
                    {
                        upload = canUpload;
                    }
                    finally
                    {
                        apiLock.Exit();
                    }
                }
                //Log.D("Timing.API.APIController", "We are " + (!upload ? "not " : "") + "able to upload right now.");
                if (results.Count > 0 && upload)
                {
                    // Change TimeResults to APIResults
                    List<APIResult> upRes = [];
                    DateTime start = DateTime.SpecifyKind(DateTime.Parse(theEvent.Date), DateTimeKind.Local).AddSeconds(theEvent.StartSeconds).AddMilliseconds(theEvent.StartMilliseconds);
                    Dictionary<string, DateTime> waveStartTimes = [];
                    HashSet<string> uploadDistances = [];
                    foreach (Distance d in database.GetDistances(theEvent.Identifier))
                    {
                        waveStartTimes[d.Name] = start.AddSeconds(d.StartOffsetSeconds).AddMilliseconds(d.StartOffsetMilliseconds);
                        if (d.Upload && d.LinkedDistance == Constants.Timing.DISTANCE_NO_LINKED_ID)
                        {
                            uploadDistances.Add(d.Name);
                        }
                    }
                    string unique_pad = "";
                    AppSetting uniqueID = database.GetAppSetting(Constants.Settings.PROGRAM_UNIQUE_MODIFIER);
                    if (uniqueID != null)
                    {
                        unique_pad = uniqueID.Value;
                    }
                    foreach (TimeResult tr in results)
                    {
                        //tr.Uploaded = Constants.Timing.TIMERESULT_UPLOADED_TRUE;
                        DateTime trStart = waveStartTimes.TryGetValue(tr.RealDistanceName, out DateTime value) ? value : start;
                        // only add to upload list if we want to upload everything (NOT Specific)
                        // or we only want to upload specific distances and the distance is in the
                        // list of distances we want to upload
                        if (!theEvent.UploadSpecific || uploadDistances.Contains(tr.DistanceName))
                        {
                            upRes.Add(new(theEvent, tr, trStart, unique_pad));
                        }
                    }
                    await UploadResults(results, api, event_ids, database, this, mainWindow, theEvent);
                    mainWindow.UpdateTiming();
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
                        mainWindow.UpdateTiming();
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
                if (apiLock.TryEnter(6000))
                {
                    try
                    {
                        Log.D("API.APIController", "Checking keep alive status.");
                        if (!keepAlive)
                        {
                            Log.D("API.APIController", "Exiting API thread.");
                            running = false;
                            mainWindow.UpdateTiming();
                            return;
                        }
                    }
                    finally
                    {
                        apiLock.Exit();
                    }
                }
                else
                {
                    Log.D("API.APIController", "Error with API lock.");
                    keepAlive = false;
                    running = false;
                    mainWindow.UpdateTiming();
                    return;
                }
            }
        }
    }
}
