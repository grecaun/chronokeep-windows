using Chronokeep.Interfaces;
using Chronokeep.Objects;
using System;
using System.Collections.Generic;
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
        // Keep track of what alerts have been sent out (EventID, Bib)
        internal static HashSet<(int, string)> AlertsSent = new HashSet<(int, string)>();


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
            Log.D("Timing.TimingWorker", "Mutex Wait 02");
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
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                if (dictionary.segmentDictionary.ContainsKey((seg.DistanceId, seg.LocationId, seg.Occurrence)))
                {
                    Log.E("Timing.TimingWorker", "Multiples of a segment found in segment set.");
                }
                dictionary.segmentDictionary[(seg.DistanceId, seg.LocationId, seg.Occurrence)] = seg;
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
            dictionary.apiURLs.Clear();
            foreach (APIObject api in database.GetAllAPI())
            {
                dictionary.apiURLs.Add(api.Identifier, api.URL);
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

        public void Run()
        {
            // get sms alerts from database
            int eventId = database.GetCurrentEvent().Identifier;
            foreach (string bib in database.GetSMSAlerts(eventId))
            {
                AlertsSent.Add((eventId, bib));
            }
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
                        foreach (TimeResult result in database.GetFinishTimes(theEvent.Identifier))
                        {
                            // verify the distance is set to allow sms alerts and the runner hasn't been notified already
                            if (dictionary.distanceNameDictionary.ContainsKey(result.RealDistanceName)
                                && true == dictionary.distanceNameDictionary[result.RealDistanceName].SMSEnabled
                                && false == AlertsSent.Contains((theEvent.Identifier, result.Bib)))
                            {
                                // Only send alert if participant wants it sent
                                if (dictionary.participantBibDictionary.ContainsKey(result.Bib) && true == dictionary.participantBibDictionary[result.Bib].EventSpecific.SMSEnabled)
                                {
                                    if (result.SendSMSAlert(theEvent, dictionary))
                                    {
                                        AlertsSent.Add((theEvent.Identifier, result.Bib));
                                        database.AddSMSAlert(theEvent.Identifier, result.Bib);
                                    }
                                }
                                else
                                {
                                    AlertsSent.Add((theEvent.Identifier, result.Bib));
                                    database.AddSMSAlert(theEvent.Identifier, result.Bib);
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
