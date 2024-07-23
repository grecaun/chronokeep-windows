using Chronokeep.Interfaces;
using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.API;
using Chronokeep.Objects.ChronoKeepAPI;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace Chronokeep.Timing
{
    class TimingWorker
    {
        private readonly IDBInterface database;
        private readonly IMainWindow window;
        private static TimingWorker worker;

        private static readonly Semaphore semaphore = new Semaphore(0, 2);
        private static readonly Mutex mutex = new Mutex();
        private static readonly Mutex ResetDictionariesMutex = new Mutex();
        private static readonly Mutex ResultsMutex = new();
        private static bool QuittingTime = false;
        private static bool NewResults = false;
        private static bool ResetDictionariesBool = true;

        private static TimingDictionary dictionary = new TimingDictionary();

        private static Regex alphaOnly = new Regex("[^A-Za-z]");
        private static DateTime lastSubscriptionFetch = DateTime.Now.AddMinutes(-1);

        private TimingWorker(IMainWindow window, IDBInterface database)
        {
            this.window = window;
            this.database = database;
        }

        public static TimingWorker NewWorker(IMainWindow window, IDBInterface database)
        {
            if (worker == null)
            {
                worker = new TimingWorker(window, database);
            }
            return worker;
        }

        public static bool NewResultsExist()
        {
            bool output = false;
            //Log.D("Timing.TimingWorker", "Mutex Wait 02");
            if (ResultsMutex.WaitOne(3000))
            {
                output = NewResults;
                NewResults = false;
                ResultsMutex.ReleaseMutex();
            }
            return output;
        }

        public static void Shutdown()
        {
            Log.D("Timing.TimingWorker", "Mutex Wait 01");
            if (mutex.WaitOne(3000))
            {
                QuittingTime = true;
                mutex.ReleaseMutex();
            }
        }

        public static void Notify()
        {
            try
            {
                semaphore.Release();
            }
            catch
            {
                Log.D("Timing.TimingWorker", "Unable to release, release is full.");
            }
        }

        public static void ResetDictionaries()
        {
            Log.D("Timing.TimingWorker", "Resetting dictionaries next go around.");
            Log.D("Timing.TimingWorker", "Mutex Wait 04");
            if (ResetDictionariesMutex.WaitOne(3000))
            {
                ResetDictionariesBool = true;
                ResetDictionariesMutex.ReleaseMutex();
            }
        }

        private void RecalculateDictionaries(Event theEvent)
        {
            Log.D("Timing.TimingWorker", "Recalculating dictionaries.");
            // Locations for checking if we're past the maximum number of occurrences
            // Stored in a dictionary based upon the location ID for easier access.
            dictionary.locationDictionary.Clear();
            foreach (TimingLocation loc in database.GetTimingLocations(theEvent.Identifier))
            {
                if (dictionary.locationDictionary.ContainsKey(loc.Identifier))
                {
                    Log.E("Timing.TimingWorker", "Multiples of a location found in location set.");
                }
                dictionary.locationDictionary[loc.Identifier] = loc;
            }
            // Segments so we can give a result a segment ID if it's at the right location
            // and occurrence. Stored in a dictionary for obvious reasons.
            dictionary.segmentDictionary.Clear();
            // Keep track of the list of Segments by distance
            dictionary.DistanceSegmentOrder.Clear();
            dictionary.SegmentByIDDictionary.Clear();
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                if (dictionary.segmentDictionary.ContainsKey((seg.DistanceId, seg.LocationId, seg.Occurrence)))
                {
                    Log.E("Timing.TimingWorker", "Multiples of a segment found in segment set.");
                }
                dictionary.segmentDictionary[(seg.DistanceId, seg.LocationId, seg.Occurrence)] = seg;
                if (!dictionary.DistanceSegmentOrder.ContainsKey(seg.DistanceId))
                {
                    dictionary.DistanceSegmentOrder[seg.DistanceId] = new List<Segment>();
                }
                dictionary.DistanceSegmentOrder[seg.DistanceId].Add(seg);
                dictionary.SegmentByIDDictionary[seg.Identifier] = seg;
            }
            // Add finish segments to DistanceSegmentOrder if distance is specified
            foreach (Distance d in database.GetDistances(theEvent.Identifier))
            {
                if (d.DistanceValue > 0)
                {
                    if (!dictionary.DistanceSegmentOrder.ContainsKey(d.Identifier))
                    {
                        dictionary.DistanceSegmentOrder[d.Identifier] = new List<Segment>();
                    }
                    dictionary.DistanceSegmentOrder[d.Identifier].Add(
                        new Segment(
                            Constants.Timing.SEGMENT_FINISH,
                            theEvent.Identifier,
                            d.Identifier,
                            Constants.Timing.LOCATION_FINISH,
                            d.FinishOccurrence,
                            0.0,
                            d.DistanceValue,
                            d.DistanceUnit,
                            "Finish",
                            "",
                            "")
                        );
                }
            }
            // Participants so we can check their Distance.
            dictionary.participantBibDictionary.Clear();
            dictionary.participantEventSpecificDictionary.Clear();
            foreach (Participant part in database.GetParticipants(theEvent.Identifier))
            {
                if (dictionary.participantBibDictionary.ContainsKey(part.Bib))
                {
                    Log.E("Timing.TimingWorker", "Multiples of a Bib found in participants set. " + part.Bib);
                }
                dictionary.participantBibDictionary[part.Bib] = part;
                dictionary.participantEventSpecificDictionary[part.EventSpecific.Identifier] = part;
            }
            // Get the start time for the event. (Net time of 0:00:00.000)
            dictionary.distanceStartDict.Clear();
            DateTime startTime = DateTime.Parse(theEvent.Date).AddSeconds(theEvent.StartSeconds);
            dictionary.distanceStartDict[0] = (Constants.Timing.RFIDDateToEpoch(startTime), theEvent.StartMilliseconds);
            // And the end time (for time based events)
            dictionary.distanceEndDict.Clear();
            dictionary.distanceEndDict[0] = dictionary.distanceStartDict[0];
            // Distances so we can get their start offset.
            dictionary.distanceDictionary.Clear();
            dictionary.distanceNameDictionary.Clear();
            List<Distance> distances = database.GetDistances(theEvent.Identifier);
            foreach (Distance d in distances)
            {
                if (dictionary.distanceDictionary.ContainsKey(d.Identifier))
                {
                    Log.E("Timing.TimingWorker", "Multiples of a Distance found in distances set.");
                }
                dictionary.distanceDictionary[d.Identifier] = d;
                dictionary.distanceNameDictionary[d.Name] = d;
                Log.D("Timing.TimingWorker", "Distance " + d.Name + " offsets are " + d.StartOffsetSeconds + " " + d.StartOffsetMilliseconds);
                dictionary.distanceStartDict[d.Identifier] = (dictionary.distanceStartDict[0].Seconds + d.StartOffsetSeconds, dictionary.distanceStartDict[0].Milliseconds + d.StartOffsetMilliseconds);
                dictionary.distanceEndDict[d.Identifier] = (dictionary.distanceStartDict[d.Identifier].Seconds + d.EndSeconds, dictionary.distanceStartDict[d.Identifier].Milliseconds);
                dictionary.distanceEndDict[0] = (dictionary.distanceEndDict[d.Identifier].Seconds, dictionary.distanceEndDict[d.Identifier].Milliseconds);
            }
            // Set up bibToChipDictionary so we can link bibs to chips
            List<BibChipAssociation> bibChips = database.GetBibChips(theEvent.Identifier);
            foreach (BibChipAssociation assoc in bibChips)
            {
                dictionary.chipToBibDictionary[assoc.Chip] = assoc.Bib;
                if (!dictionary.bibToChipDictionary.ContainsKey(assoc.Bib))
                {
                    dictionary.bibToChipDictionary[assoc.Bib] = new List<string>();
                }
                dictionary.bibToChipDictionary[assoc.Bib].Add(assoc.Chip);
            }
            // Dictionary for looking up linked distances
            dictionary.linkedDistanceDictionary.Clear();
            dictionary.linkedDistanceIdentifierDictionary.Clear();
            dictionary.mainDistances.Clear();
            foreach (Distance d in distances)
            {
                // Check if its a linked distance
                if (d.LinkedDistance > 0)
                {
                    Log.D("Timing.TimingWorker", "Linked distance found. " + d.LinkedDistance);
                    // Verify we know the distance its linked to.
                    if (!dictionary.distanceDictionary.ContainsKey(d.LinkedDistance))
                    {
                        Log.E("Timing.TimingWorker", "Unable to find linked distance.");
                    }
                    else
                    {
                        Log.D("Timing.TimingWorker", "Setting linked dictionaries. Ranking: " + d.Ranking);
                        // Set linked distance for ranking as the linked distance and set ranking int.
                        dictionary.linkedDistanceDictionary[d.Name] = (dictionary.distanceDictionary[d.LinkedDistance], d.Ranking);
                        dictionary.linkedDistanceIdentifierDictionary[d.Identifier] = dictionary.distanceDictionary[d.LinkedDistance].Identifier;
                        // Set end time for linked distance to linked distances end time.
                        dictionary.distanceEndDict[d.Identifier] = (dictionary.distanceStartDict[d.Identifier].Seconds + dictionary.distanceDictionary[d.LinkedDistance].EndSeconds, dictionary.distanceStartDict[d.Identifier].Milliseconds);
                    }
                }
                else
                {
                    Log.D("Timing.TimingWorker", "Setting linked dictionaries (no linked distance found). Ranking: 0");
                    // No linked distance found, use distance and 0 as ranking int.
                    dictionary.linkedDistanceDictionary[d.Name] = (d, 0);
                    dictionary.linkedDistanceIdentifierDictionary[d.Identifier] = d.Identifier;
                    // not a linked distance, add it to mainDistances so we can check if there's only one distance
                    dictionary.mainDistances.Add(d);
                }
            }
            dictionary.apis.Clear();
            foreach (APIObject api in database.GetAllAPI())
            {
                dictionary.apis[api.Identifier] = api;
            }
            // Clear distance segment list if no distance values are set
            List<int> distanceNotSet = new List<int>();
            // Sort the segments in our dictionary.
            foreach (List<Segment> segments in dictionary.DistanceSegmentOrder.Values)
            {
                int distanceCount = 0;
                int distanceId = -1;
                foreach (Segment segment in segments)
                {
                    distanceId = segment.DistanceId;
                    if (segment.CumulativeDistance > 0)
                    {
                        distanceCount += 1;
                    }
                }
                if (distanceCount == segments.Count)
                {
                    segments.Sort((x1, x2) =>
                    {
                        return x1.CumulativeDistance.CompareTo(x2.CumulativeDistance);
                    });
                }
                else
                {
                    distanceNotSet.Add(distanceId);
                }
            }
            // remove all that we didn't find with distances specified
            foreach (int distanceId in distanceNotSet)
            {
                dictionary.DistanceSegmentOrder.Remove(distanceId);
            }
            RecalculateDNS(theEvent);
        }

        private void RecalculateDNS(Event theEvent)
        {
            // Get a list of DNS entries.
            dictionary.dnsChips.Clear();
            dictionary.dnsBibs.Clear();
            List<ChipRead> dnsReads = database.GetDNSChipReads(theEvent.Identifier);
            foreach (ChipRead read in dnsReads)
            {
                dictionary.dnsChips.Add(read.ChipNumber);
                if (dictionary.chipToBibDictionary.ContainsKey(read.ChipNumber))
                {
                    dictionary.dnsBibs.Add(dictionary.chipToBibDictionary[read.ChipNumber]);
                }
            }
            dictionary.dnsEntryCount = dnsReads.Count;
        }

        public async void Run()
        {
            int counter = 1;
            do
            {
                Log.D("Timing.TimingWorker", "Mutex Wait 05");
                semaphore.WaitOne();        // Wait for work.
                if (mutex.WaitOne(3000))    // Check if we've been told to quit.
                {                           // Do that here so we don't try to process another loop after being told to quit.
                    if (QuittingTime)
                    {
                        mutex.ReleaseMutex();
                        break;
                    }
                    mutex.ReleaseMutex();
                }
                else
                {
                    break;
                }
                Log.D("Timing.TimingWorker", "Entering loop " + counter++);
                Event theEvent = database.GetCurrentEvent();
                // ensure the event exists and we've got unprocessed reads
                if (theEvent != null && theEvent.Identifier != -1)
                {
                    Log.D("Timing.TimingWorker", "Mutex Wait 06");
                    if (ResetDictionariesMutex.WaitOne(3000))
                    {
                        if (ResetDictionariesBool)
                        {
                            RecalculateDictionaries(theEvent);
                        }
                        ResetDictionariesBool = false;
                        ResetDictionariesMutex.ReleaseMutex();
                    }
                    bool touched = false;
                    // Check if we have new DNS entries and reset if necessary.
                    if (database.GetDNSChipReads(theEvent.Identifier).Count > dictionary.dnsEntryCount)
                    {
                        RecalculateDNS(theEvent);
                    }
                    // Process chip reads first.
                    if (database.UnprocessedReadsExist(theEvent.Identifier))
                    {
                        Log.D("Timing.TimingWorker", "Unprocessed reads exist.");
#if DEBUG
                        DateTime start = DateTime.Now;
#endif
                        // If RACETYPE is DISTANCE
                        if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                        {
                            _ = Routines.DistanceRoutine.ProcessRace(theEvent, database, dictionary, window);
                            touched = true;
                        }
                        // Else RACETYPE is TIME
                        else if (Constants.Timing.EVENT_TYPE_TIME == theEvent.EventType)
                        {
                            _ = Routines.TimeRoutine.ProcessRace(theEvent, database, dictionary, window);
                            touched = true;
                        }
                        // Else if RACETYPE if BACKYARD_ULTRA
                        else if (Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA == theEvent.EventType)
                        {
                            _ = Routines.BackyardUltraRoutine.ProcessRace(theEvent, database, dictionary, window);
                            touched = true;
                        }
#if DEBUG
                        DateTime end = DateTime.Now;
                        TimeSpan time = end - start;
                        Log.D("Timing.TimingWorker", string.Format("Time to process all chip reads was: {0} hours {1} minutes {2} seconds {3} milliseconds", time.Hours, time.Minutes, time.Seconds, time.Milliseconds));
#endif
                    }
                    // Now process Results that aren't ranked.
                    if (database.UnprocessedResultsExist(theEvent.Identifier))
                    {
                        Log.D("Timing.TimingWorker", "Unprocessed results exist.");
#if DEBUG
                        DateTime start = DateTime.Now;
#endif
                        // If RACETYPE if DISTANCE
                        if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                        {
                            _ = Routines.DistanceRoutine.ProcessPlacements(theEvent, database, dictionary);
                            touched = true;
                        }
                        // Else if RACETYPE is TIME
                        else if (Constants.Timing.EVENT_TYPE_TIME == theEvent.EventType)
                        {
                            Routines.TimeRoutine.ProcessLapTimes(theEvent, database);
                            _ = Routines.TimeRoutine.ProcessPlacements(theEvent, database, dictionary);
                            touched = true;
                        }
                        // Else if RACETYPE is BACKYARD_ULTRA
                        else if (Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA == theEvent.EventType)
                        {
                            _ = Routines.BackyardUltraRoutine.ProcessPlacements(theEvent, database, dictionary);
                            touched = true;
                        }
#if DEBUG
                        DateTime end = DateTime.Now;
                        TimeSpan time = end - start;
                        Log.D("Timing.TimingWorker", string.Format("Time to process placements was: {0} hours {1} minutes {2} seconds {3} milliseconds", time.Hours, time.Minutes, time.Seconds, time.Milliseconds));
#endif
                        window.NetworkUpdateResults();
                    }
                    if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType) // && SMS set up && SMS enabled on event)
                    {
                        List<(int, int)> alerts = database.GetSMSAlerts(theEvent.Identifier);
                        DateTime now = DateTime.Now;
                        if (lastSubscriptionFetch.AddSeconds(30).CompareTo(now) < 0)
                        {
                            APIObject api = database.GetAPI(theEvent.API_ID);
                            string[] event_ids = theEvent.API_Event_ID.Split(',');
                            if (event_ids.Length == 2)
                            {
                                try
                                {
                                    GetSmsSubscriptionsResponse subscriptionResponse = await APIHandlers.GetSmsSubscriptions(api, event_ids[0], event_ids[1]);
                                    if (subscriptionResponse != null && subscriptionResponse.Subscriptions != null)
                                    {
                                        // delete old then upload all the new subscriptions
                                        // this is just to make sure that we remove anyone who may have unsubscribed
                                        database.DeleteSmsSubscriptions(theEvent.Identifier);
                                        database.AddSmsSubscriptions(theEvent.Identifier, subscriptionResponse.Subscriptions);
                                    }
                                }
                                catch
                                {
                                    Log.E("Timing.TimingWorker", "Exception getting sms subscriptions.");
                                }
                            }
                        }
                        if (alerts != null)
                        {
                            // Changing alerts hashset to locally based and pulled from the database each time we try to send alerts
                            HashSet<(TimeResult, string)> toSendTo = new HashSet<(TimeResult, string)>();
                            Dictionary<string, string> nameToBibDict = new();
                            foreach (Participant p in database.GetParticipants(theEvent.Identifier))
                            {
                                string name = p.FirstName.ToLower() + p.LastName.ToLower();
                                name = alphaOnly.Replace(name, string.Empty);
                                nameToBibDict[name] = p.Bib;
                            }
                            Dictionary<string, HashSet<string>> bibToPhonesDict = new();
                            foreach (APISmsSubscription sub in database.GetSmsSubscriptions(theEvent.Identifier))
                            {
                                if (sub.Bib.Length > 0 && sub.Phone.Length > 0)
                                {
                                    if (!bibToPhonesDict.ContainsKey(sub.Bib))
                                    {
                                        bibToPhonesDict[sub.Bib] = new HashSet<string>();
                                    }
                                    bibToPhonesDict[sub.Bib].Add(Constants.Globals.GetValidPhone(sub.Phone));
                                }
                                else if (sub.First.Length + sub.Last.Length > 0 && sub.Phone.Length > 0)
                                {
                                    string name = sub.First.ToLower() + sub.Last.ToLower();
                                    name = alphaOnly.Replace(name, string.Empty);
                                    if (nameToBibDict.ContainsKey(name))
                                    {
                                        if (!bibToPhonesDict.ContainsKey(nameToBibDict[name]))
                                        {
                                            bibToPhonesDict[nameToBibDict[name]] = new HashSet<string>();
                                        }
                                        bibToPhonesDict[nameToBibDict[name]].Add(Constants.Globals.GetValidPhone(sub.Phone));
                                    }
                                }
                            }
                            // Check the finish results for results we can send SMS messages for.
                            foreach (TimeResult result in database.GetTimingResults(theEvent.Identifier))
                            {
                                // Deal with finish results for runners who want to be notified of their finish time.
                                // verify the distance is set to allow sms alerts and the runner hasn't been notified already
                                if (Constants.Timing.SEGMENT_FINISH == result.SegmentId
                                    && dictionary.distanceNameDictionary.ContainsKey(result.RealDistanceName)
                                    && true == dictionary.distanceNameDictionary[result.RealDistanceName].SMSEnabled
                                    && Constants.Timing.EVENTSPECIFIC_UNKNOWN != result.EventSpecificId
                                    && false == alerts.Contains((result.EventSpecificId, result.SegmentId)))
                                {
                                    // If we can send a message to them (valid phones, credentials valid, sms opted in)
                                    // add them to the list to send sms messages
                                    if (result.SMSCanBeSent(dictionary) && dictionary.participantBibDictionary.ContainsKey(result.Bib))
                                    {
                                        string phone = Constants.Globals.GetValidPhone(dictionary.participantBibDictionary[result.Bib].Mobile);
                                        if (phone.Length == 0)
                                        {
                                            phone = Constants.Globals.GetValidPhone(dictionary.participantBibDictionary[result.Bib].Phone);
                                        }
                                        toSendTo.Add((result, phone));
                                    }
                                }
                                // Now deal with sms subcriptions
                                // verify the distance is set to allow sms alerts and we haven't sent notifications for that segment yet
                                if (Constants.Timing.SEGMENT_START != result.SegmentId
                                    && Constants.Timing.SEGMENT_NONE != result.SegmentId
                                    && dictionary.distanceNameDictionary.ContainsKey(result.RealDistanceName)
                                    && true == dictionary.distanceNameDictionary[result.RealDistanceName].SMSEnabled
                                    && Constants.Timing.EVENTSPECIFIC_UNKNOWN != result.EventSpecificId
                                    && false == alerts.Contains((result.EventSpecificId, result.SegmentId))
                                    )
                                {
                                    if (bibToPhonesDict.ContainsKey(result.Bib))
                                    {
                                        foreach (string phone in bibToPhonesDict[result.Bib])
                                        {
                                            toSendTo.Add((result, phone));
                                        }
                                    }
                                }
                            }
                            // Only check banned phones or try to send texts if there is something to send.
                            string resultsURL = "";
                            if (dictionary.apis.ContainsKey(theEvent.API_ID) && dictionary.apis[theEvent.API_ID].WebURL.Length > 0)
                            {
                                string[] event_ids = theEvent.API_Event_ID.Split(',');
                                if (event_ids.Length == 2)
                                {
                                    resultsURL = string.Format(" More results @ {0}results/{1}/{2}.", dictionary.apis[theEvent.API_ID].WebURL, event_ids[0], event_ids[1]);
                                }
                                else
                                {
                                    resultsURL = string.Format(" More results @ {0}.", dictionary.apis[theEvent.API_ID].WebURL);
                                }
                            }
                            if (toSendTo.Count > 0)
                            {
                                // Update banned phones list.
                                Constants.Globals.UpdateBannedPhones();
                                foreach ((TimeResult result, string phone) in toSendTo)
                                {
                                    // Only send alert if participant wants it sent
                                    // Do not add to the AlertsSent database because they
                                    // may change their mind later and
                                    // we still want to be able to send a SMS to them
                                    // Only add to the database/dictionary if successful.
                                    string sms;
                                    if (Constants.Timing.SEGMENT_FINISH != result.SegmentId)
                                    {
                                        if (dictionary.mainDistances.Count > 1)
                                        {
                                            sms = string.Format("{0} {1} has finished the {2} {3} {4} in {5}.{6} Reply STOP to opt-out.", result.First, result.Last, theEvent.Year, theEvent.Name, result.DistanceName, result.ChipTimeNoMilliseconds, resultsURL);
                                        }
                                        else
                                        {
                                            sms = string.Format("{0} {1} has finished the {2} {3} in {4}.{5} Reply STOP to opt-out.", result.First, result.Last, theEvent.Year, theEvent.Name, result.ChipTimeNoMilliseconds, resultsURL);
                                        }
                                    }
                                    else
                                    {
                                        sms = string.Format("{0} {1} has has reached {2} in {3}.{4} Reply STOP to opt-out.", result.First, result.Last, result.SegmentName, result.ChipTimeNoMilliseconds, resultsURL);
                                    }
                                    if (result.EventSpecificId != Constants.Timing.EVENTSPECIFIC_UNKNOWN && result.SendSMSAlert(theEvent, phone, sms))
                                    {
                                        database.AddSMSAlert(theEvent.Identifier, result.EventSpecificId, result.SegmentId);
                                    }
                                }
                            }
                        }
                    }
                    if (touched)
                    {
                        if (ResultsMutex.WaitOne(3000))
                        {
                            NewResults = true;
                            ResultsMutex.ReleaseMutex();
                        }
                    }
                }
            } while (true);
        }
    }
}
