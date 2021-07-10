﻿using ChronoKeep.Interfaces;
using ChronoKeep.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoKeep.Timing
{
    class TimingWorker
    {
        private readonly IDBInterface database;
        private readonly IMainWindow window;
        private static TimingWorker worker;

        private static readonly Semaphore semaphore = new Semaphore(0, 2);
        private static readonly Mutex mutex = new Mutex();
        private static readonly Mutex ResultsMutex = new Mutex();
        private static readonly Mutex ResetDictionariesMutex = new Mutex();
        private static bool QuittingTime = false;
        private static bool NewResults = false;
        private static bool ResetDictionariesBool = true;

        // Dictionaries for storing information about the race.
        private Dictionary<int, TimingLocation> locationDictionary = new Dictionary<int, TimingLocation>();
        // (DivisionId, LocationId, Occurrence)
        private Dictionary<(int, int, int), Segment> segmentDictionary = new Dictionary<(int, int, int), Segment>();
        // Participants are stored based upon BIB and EVENTSPECIFICIDENTIFIER because we use both
        private Dictionary<int, Participant> participantBibDictionary = new Dictionary<int, Participant>();
        private Dictionary<int, Participant> participantEventSpecificDictionary = new Dictionary<int, Participant>();
        // Start times. Item at 0 should always be 00:00:00.000. Key is Division ID
        private Dictionary<int, (long Seconds, int Milliseconds)> divisionStartDict = new Dictionary<int, (long, int)>();
        private Dictionary<int, (long Seconds, int Milliseconds)> divisionEndDict = new Dictionary<int, (long, int)>();
        private Dictionary<int, Distance> divisionDictionary = new Dictionary<int, Distance>();

        // Link bibs and chipreads for adding occurence to bib based dnf entry.
        Dictionary<int, string> bibChipDictionary = new Dictionary<int, string>();

        private Dictionary<string, (Distance, int)> linkedDivisionDictionary = new Dictionary<string, (Distance, int)>();
        private Dictionary<int, int> linkedDivIdentifierDictionary = new Dictionary<int, int>();

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

        public static void Shutdown()
        {
            if (mutex.WaitOne(3000))
            {
                QuittingTime = true;
                mutex.ReleaseMutex();
            }
        }

        public static bool NewResultsExist()
        {
            bool output = false;
            if (ResultsMutex.WaitOne(3000))
            {
                output = NewResults;
                ResultsMutex.ReleaseMutex();
            }
            return output;
        }

        public static void ResetNewResults()
        {
            if (ResultsMutex.WaitOne(3000))
            {
                NewResults = false;
                ResultsMutex.ReleaseMutex();
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
                Log.D("Unable to release, release is full.");
            }
        }

        public static void ResetDictionaries()
        {
            Log.D("Resetting dictionaries next go around.");
            if (ResetDictionariesMutex.WaitOne(3000))
            {
                ResetDictionariesBool = true;
                ResetDictionariesMutex.ReleaseMutex();
            }
        }

        private void RecalculateDictionaries(Event theEvent)
        {
            Log.D("Recalculating dictionaries.");
            // Locations for checking if we're past the maximum number of occurrences
            // Stored in a dictionary based upon the location ID for easier access.
            locationDictionary.Clear();
            foreach (TimingLocation loc in database.GetTimingLocations(theEvent.Identifier))
            {
                if (locationDictionary.ContainsKey(loc.Identifier))
                {
                    Log.E("Multiples of a location found in location set.");
                }
                locationDictionary[loc.Identifier] = loc;
            }
            // Segments so we can give a result a segment ID if it's at the right location
            // and occurrence. Stored in a dictionary for obvious reasons.
            segmentDictionary.Clear();
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                if (segmentDictionary.ContainsKey((seg.DivisionId, seg.LocationId, seg.Occurrence)))
                {
                    Log.E("Multiples of a segment found in segment set.");
                }
                segmentDictionary[(seg.DivisionId, seg.LocationId, seg.Occurrence)] = seg;
            }
            // Participants so we can check their Division.
            participantBibDictionary.Clear();
            participantEventSpecificDictionary.Clear();
            foreach (Participant part in database.GetParticipants(theEvent.Identifier))
            {
                if (participantBibDictionary.ContainsKey(part.Bib))
                {
                    Log.E("Multiples of a Bib found in participants set. " + part.Bib);
                }
                participantBibDictionary[part.Bib] = part;
                participantEventSpecificDictionary[part.EventSpecific.Identifier] = part;
            }
            // Get the start time for the event. (Net time of 0:00:00.000)
            divisionStartDict.Clear();
            DateTime startTime = DateTime.Parse(theEvent.Date).AddSeconds(theEvent.StartSeconds);
            divisionStartDict[0] = (Constants.Timing.DateToEpoch(startTime), theEvent.StartMilliseconds);
            // And the end time (for time based events)
            divisionEndDict.Clear();
            divisionEndDict[0] = divisionStartDict[0];
            // Divisions so we can get their start offset.
            divisionDictionary.Clear();
            List<Distance> divs = database.GetDistances(theEvent.Identifier);
            foreach (Distance div in divs)
            {
                if (divisionDictionary.ContainsKey(div.Identifier))
                {
                    Log.E("Multiples of a Division found in divisions set.");
                }
                divisionDictionary[div.Identifier] = div;
                Log.D("Division " + div.Name + " offsets are " + div.StartOffsetSeconds + " " + div.StartOffsetMilliseconds);
                divisionStartDict[div.Identifier] = (divisionStartDict[0].Seconds + div.StartOffsetSeconds, divisionStartDict[0].Milliseconds + div.StartOffsetMilliseconds);
                divisionEndDict[div.Identifier] = (divisionStartDict[div.Identifier].Seconds + div.EndSeconds, divisionStartDict[div.Identifier].Milliseconds);
                divisionEndDict[0] = (divisionEndDict[div.Identifier].Seconds, divisionEndDict[div.Identifier].Milliseconds);
            }
            // Set up bibChipDictionary so we can link bibs to chips
            List<BibChipAssociation> bibChips = database.GetBibChips(theEvent.Identifier);
            foreach (BibChipAssociation assoc in bibChips)
            {
                bibChipDictionary[assoc.Bib] = assoc.Chip;
            }
            // Dictionary for looking up linked divisions
            linkedDivisionDictionary.Clear();
            foreach (Distance div in divs)
            {
                // Check if its a linked division
                if (div.LinkedDivision > 0)
                {
                    Log.D("Linked division found. " + div.LinkedDivision);
                    // Verify we know the division its linked to.
                    if (!divisionDictionary.ContainsKey(div.LinkedDivision))
                    {
                        Log.E("Unable to find linked division.");
                    }
                    else
                    {
                        Log.D("Setting linked dictionaries. Ranking: " + div.Ranking);
                        // Set linked division for ranking as the linked division and set ranking int.
                        linkedDivisionDictionary[div.Name] = (divisionDictionary[div.LinkedDivision], div.Ranking);
                        linkedDivIdentifierDictionary[div.Identifier] = divisionDictionary[div.LinkedDivision].Identifier;
                        // Set end time for linked division to linked divisions end time.
                        divisionEndDict[div.Identifier] = (divisionStartDict[div.Identifier].Seconds + divisionDictionary[div.LinkedDivision].EndSeconds, divisionStartDict[div.Identifier].Milliseconds);
                    }
                }
                else
                {
                    Log.D("Setting linked dictionaries (no linked div found). Ranking: 0");
                    // No linked division found, use division and 0 as ranking int.
                    linkedDivisionDictionary[div.Name] = (div, 0);
                    linkedDivIdentifierDictionary[div.Identifier] = div.Identifier;
                }
            }
        }

        public void Run()
        {
            int counter = 1;
            do
            {
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
                Log.D("Entering loop " + counter++);
                Event theEvent = database.GetCurrentEvent();
                // ensure the event exists and we've got unprocessed reads
                Dictionary<int, Participant> participantDictionary = new Dictionary<int, Participant>();
                if (theEvent != null && theEvent.Identifier != -1)
                {
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
                    List<TimeResult> results = null;
                    if (database.UnprocessedReadsExist(theEvent.Identifier))
                    {
                        DateTime start = DateTime.Now;
                        // If RACETYPE is DISTANCE
                        if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                        {
                            results = ProcessDistanceBasedRace(theEvent);
                            touched = true;
                        }
                        // Else RACETYPE is TIME
                        else if (Constants.Timing.EVENT_TYPE_TIME == theEvent.EventType)
                        {
                            results = ProcessTimeBasedRace(theEvent);
                            touched = true;
                        }
                        DateTime end = DateTime.Now;
                        TimeSpan time = end - start;
                        Log.D(String.Format("Time to process all chip reads was: {0} hours {1} minutes {2} seconds {3} milliseconds", time.Hours, time.Minutes, time.Seconds, time.Milliseconds));
                    }
                    results = null;
                    if (database.UnprocessedResultsExist(theEvent.Identifier))
                    {
                        DateTime start = DateTime.Now;
                        // If RACETYPE if DISTANCE
                        if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                        {
                            results = ProcessPlacementsDistance(theEvent);
                            touched = true;
                        }
                        // Else RACETYPE is TIME
                        else if (Constants.Timing.EVENT_TYPE_TIME == theEvent.EventType)
                        {
                            ProcessLapTimes(theEvent);
                            results = ProcessPlacementsTime(theEvent);
                            touched = true;
                        }
                        DateTime end = DateTime.Now;
                        TimeSpan time = end - start;
                        Log.D(String.Format("Time to process placements was: {0} hours {1} minutes {2} seconds {3} milliseconds", time.Hours, time.Minutes, time.Seconds, time.Milliseconds));
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

        private void ProcessLapTimes(Event theEvent)
        {
            Dictionary<(string, int), TimeResult> RaceResults = new Dictionary<(string, int), TimeResult>();
            foreach (TimeResult startTime in database.GetSegmentTimes(theEvent.Identifier, Constants.Timing.SEGMENT_START))
            {
                RaceResults[(startTime.Identifier, 0)] = startTime;
            }
            List<TimeResult> laps = database.GetSegmentTimes(theEvent.Identifier, Constants.Timing.SEGMENT_FINISH);
            laps.Sort((x1, x2) =>
            {
                if (x1.Identifier.Equals(x2.Identifier))
                {
                    return x1.Occurrence.CompareTo(x2.Occurrence);
                }
                return x1.Identifier.CompareTo(x2.Identifier);
            });
            foreach (TimeResult currentLap in laps)
            {
                RaceResults[(currentLap.Identifier, currentLap.Occurrence)] = currentLap;
                long sec = 0;
                int mill = 0;
                if (RaceResults.ContainsKey((currentLap.Identifier, currentLap.Occurrence - 1)))
                {
                    sec = RaceResults[(currentLap.Identifier, currentLap.Occurrence - 1)].ChipSeconds;
                    mill = RaceResults[(currentLap.Identifier, currentLap.Occurrence - 1)].ChipMilliseconds;
                }
                sec = currentLap.ChipSeconds - sec;
                mill = currentLap.ChipMilliseconds - mill;
                if (mill < 0)
                {
                    sec--;
                    mill += 1000;
                }
                currentLap.LapTime = String.Format("{0}:{1:D2}:{2:D2}.{3:D3}", sec / 3600, (sec % 3600) / 60, sec % 60, mill);
            }
            database.AddTimingResults(laps);
        }

        private List<TimeResult> ProcessDistanceBasedRace(Event theEvent)
        {
            Log.D("Processing chip reads for a distance based event.");
            // Check if there's anything to process.
            // Pre-process information we'll need to fully process chip reads
            // Get start TimeResults
            Dictionary<string, TimeResult> startTimes = new Dictionary<string, TimeResult>();
            foreach (TimeResult result in database.GetStartTimes(theEvent.Identifier))
            {
                startTimes[result.Identifier] = result;
            }
            // Get finish TimeResults
            Dictionary<string, TimeResult> finishTimes = new Dictionary<string, TimeResult>();
            foreach (TimeResult result in database.GetFinishTimes(theEvent.Identifier))
            {
                finishTimes[result.Identifier] = result;
            }
            // Get all of the Chip Reads we find useful (Unprocessed, and those used as a result.)
            // and then sort them into groups based upon Bib, Chip, or put them in the ignore pile if
            // they have no bib or chip.
            Dictionary<int, List<ChipRead>> bibReadPairs = new Dictionary<int, List<ChipRead>>();
            Dictionary<string, List<ChipRead>> chipReadPairs = new Dictionary<string, List<ChipRead>>();
            // Make sure we keep track of the
            // last occurrence for a person at a specific location.
            // (Bib, Location), Last Chip Read
            Dictionary<(int, int), (ChipRead Read, int Occurrence)> lastReadDictionary = new Dictionary<(int, int), (ChipRead, int)>();
            Dictionary<(string, int), (ChipRead Read, int Occurrence)> chipLastReadDictionary = new Dictionary<(string, int), (ChipRead Read, int Occurrence)>();
            // Keep a list of DNF participants so we can mark them as DNF in results.
            // Keep a record of the DNF chipread so we can link it with the TimeResult
            Dictionary<int, ChipRead> dnfDictionary = new Dictionary<int, ChipRead>();
            Dictionary<string, ChipRead> chipDnfDictionary = new Dictionary<string, ChipRead>();
            List<ChipRead> allChipReads = database.GetUsefulChipReads(theEvent.Identifier);
            allChipReads.Sort();
            List<ChipRead> setUnknown = new List<ChipRead>();
            foreach (ChipRead read in allChipReads)
            {
                if (read.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
                {
                    // if we process all the used reads before putting them in the list
                    // we can ensure that all of the reads we process are STATUS_NONE
                    // and then we can verify that we aren't inserting results BEFORE
                    // results we've already calculated
                    if (Constants.Timing.CHIPREAD_STATUS_USED == read.Status)
                    {
                        if (!lastReadDictionary.ContainsKey((read.ChipBib, read.LocationID)))
                        {
                            lastReadDictionary[(read.Bib, read.LocationID)] = (read, 1);
                        }
                        else
                        {
                            lastReadDictionary[(read.Bib, read.LocationID)] = (read, lastReadDictionary[(read.Bib, read.LocationID)].Occurrence + 1);
                        }
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_STARTTIME == read.Status &&
                        (Constants.Timing.LOCATION_START == read.LocationID ||
                        (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish)))
                    {
                        // If we haven't found anything, let us know what our start time was
                        if (!lastReadDictionary.ContainsKey((read.ChipBib, read.LocationID)))
                        {
                            lastReadDictionary[(read.Bib, read.LocationID)] = (read, 0);
                        }
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_DNF == read.Status)
                    {
                        dnfDictionary[read.Bib] = read;
                    }
                    else
                    {
                        if (!bibReadPairs.ContainsKey(read.Bib))
                        {
                            bibReadPairs[read.Bib] = new List<ChipRead>();
                        }
                        bibReadPairs[read.Bib].Add(read);
                    }
                }
                else if (read.ChipNumber != Constants.Timing.CHIPREAD_DUMMYCHIP)
                {
                    if (Constants.Timing.CHIPREAD_STATUS_USED == read.Status)
                    {
                        if (!chipLastReadDictionary.ContainsKey((read.ChipNumber.ToString(), read.LocationID)))
                        {
                            chipLastReadDictionary[(read.ChipNumber.ToString(), read.LocationID)] = (read, 1);
                        }
                        else
                        {
                            chipLastReadDictionary[(read.ChipNumber.ToString(), read.LocationID)] = (read, chipLastReadDictionary[(read.ChipNumber.ToString(), read.LocationID)].Occurrence + 1);
                        }
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_STARTTIME == read.Status &&
                        (Constants.Timing.LOCATION_START == read.LocationID ||
                        (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish)))
                    {
                        // If we haven't found anything, let us know what our start time was
                        if (!chipLastReadDictionary.ContainsKey((read.ChipNumber.ToString(), read.LocationID)))
                        {
                            chipLastReadDictionary[(read.ChipNumber.ToString(), read.LocationID)] = (Read: read, Occurrence: 0);
                        }
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_DNF == read.Status)
                    {
                        chipDnfDictionary[read.ChipNumber] = read;
                    }
                    else
                    {
                        if (!chipReadPairs.ContainsKey(read.ChipNumber))
                        {
                            chipReadPairs[read.ChipNumber] = new List<ChipRead>();
                        }
                        chipReadPairs[read.ChipNumber].Add(read);
                    }
                }
                else
                {
                    setUnknown.Add(read);
                }
            }
            // Go through each chip read for a single person.
            // List<ChipRead> updateStatusReads = new List<ChipRead>();
            List<TimeResult> newResults = new List<TimeResult>();
            // Keep a list of participants to update.
            HashSet<Participant> updateParticipants = new HashSet<Participant>();
            // process reads that have a bib
            foreach (int bib in bibReadPairs.Keys)
            {
                Participant part = participantBibDictionary.ContainsKey(bib) ?
                    participantBibDictionary[bib] :
                    null;
                Distance div = part != null ?
                    divisionDictionary[part.EventSpecific.DivisionIdentifier] :
                    null;
                long startSeconds, maxStartSeconds;
                int startMilliseconds;
                TimeResult startResult = null;
                if (startTimes.ContainsKey("Bib:" + bib.ToString()))
                {
                    startResult = startTimes["Bib:" + bib.ToString()];
                }
                if (div == null || !divisionStartDict.ContainsKey(div.Identifier))
                {
                    startSeconds = divisionStartDict[0].Seconds;
                    startMilliseconds = divisionStartDict[0].Milliseconds;
                }
                else
                {
                    startSeconds = divisionStartDict[div.Identifier].Seconds;
                    startMilliseconds = divisionStartDict[div.Identifier].Milliseconds;
                }
                if (part != null && part.EventSpecific != null && part.EventSpecific.EarlyStart == 1 && div != null)
                {
                    startSeconds = startSeconds - div.EarlyStartOffsetSeconds;
                }
                maxStartSeconds = startSeconds + theEvent.StartWindow;
                foreach (ChipRead read in bibReadPairs[bib])
                {
                    // Check that we haven't processed the read yet
                    if (Constants.Timing.CHIPREAD_STATUS_NONE == read.Status)
                    {
                        // Check if we're before the start time.
                        if (read.TimeSeconds < startSeconds || (read.TimeSeconds == startSeconds && read.TimeMilliseconds < startMilliseconds))
                        {
                            read.Status = Constants.Timing.CHIPREAD_STATUS_PRESTART;
                        }
                        else
                        {
                            // If we're within the start period
                            // And the location is the Start, or we've got a combined start finish location
                            if ((read.TimeSeconds < maxStartSeconds || (read.TimeSeconds == maxStartSeconds && read.TimeMilliseconds <= startMilliseconds)) &&
                                (Constants.Timing.LOCATION_START == read.LocationID
                                    || (Constants.Timing.LOCATION_FINISH == read.LocationID
                                        && theEvent.CommonStartFinish)))
                            {
                                // check if we've stored a chipread as the start chipread, update it to unused if so
                                if (lastReadDictionary.ContainsKey((bib, read.LocationID)))
                                {
                                    lastReadDictionary[(bib, read.LocationID)].Read.Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                                }
                                // Update the last read we've seen at this location
                                lastReadDictionary[(bib, read.LocationID)] = (Read: read, Occurrence: 0);
                                // Check if we previously had a TimeResult for the start.
                                if (startResult != null && newResults.Contains(startResult))
                                {
                                    // Remove it if so.
                                    newResults.Remove(startResult);
                                }
                                // Create a result for the start value.
                                long secondsDiff = read.TimeSeconds - startSeconds;
                                int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                                if (millisecDiff < 0)
                                {
                                    secondsDiff--;
                                    millisecDiff = 1000 + millisecDiff;
                                }
                                startResult = new TimeResult(theEvent.Identifier,
                                    read.ReadId,
                                    part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                                    read.LocationID,
                                    Constants.Timing.SEGMENT_START,
                                    0, // start reads are not an occurrence at the start line
                                    String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", secondsDiff / 3600, (secondsDiff % 3600) / 60, secondsDiff % 60, millisecDiff),
                                    "Bib:" + bib.ToString(),
                                    "0:00:00.000",
                                    read.Time,
                                    bib,
                                    Constants.Timing.TIMERESULT_STATUS_NONE
                                    );
                                startTimes[startResult.Identifier] = startResult;
                                newResults.Add(startResult);
                                if (part != null &&
                                    (Constants.Timing.EVENTSPECIFIC_NOSHOW == part.Status
                                    && !dnfDictionary.ContainsKey(bib)))
                                {
                                    part.Status = Constants.Timing.EVENTSPECIFIC_STARTED;
                                    updateParticipants.Add(part);
                                }
                                // Finally, set the chipread status to STARTTIME.
                                read.Status = Constants.Timing.CHIPREAD_STATUS_STARTTIME;
                            }
                            // Possible reads at this point:
                            //      Start Location reads past the StartWindow (IGNORE)
                            //      Start/Finish Location reads past the StartWindow (Valid reads)
                            //          These could be BEFORE or AFTER the last occurrence at this spot
                            //      Reads at any other location
                            else if (Constants.Timing.LOCATION_START != read.LocationID)
                            {
                                int maxOccurrences = 0;
                                if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                                {
                                    maxOccurrences = theEvent.FinishMaxOccurrences;
                                }
                                else
                                {
                                    if (!locationDictionary.ContainsKey(read.LocationID))
                                    {
                                        Log.E("Somehow the location was not found.");
                                    }
                                    else
                                    {
                                        maxOccurrences = locationDictionary[read.LocationID].MaxOccurrences;
                                    }
                                }
                                int occurrence = 1;
                                int occursWithin = 0;
                                if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                                {
                                    occursWithin = theEvent.FinishIgnoreWithin;
                                }
                                else if (locationDictionary.ContainsKey(read.LocationID))
                                {
                                    occursWithin = locationDictionary[read.LocationID].IgnoreWithin;
                                }
                                // Minimum Time Value required to actually create a result
                                long minSeconds = startSeconds;
                                int minMilliseconds = startMilliseconds;
                                // Check if there's a previous read at this location.
                                if (lastReadDictionary.ContainsKey((bib, read.LocationID)))
                                {
                                    occurrence = lastReadDictionary[(bib, read.LocationID)].Occurrence + 1;
                                    minSeconds = lastReadDictionary[(bib, read.LocationID)].Read.TimeSeconds + occursWithin;
                                    minMilliseconds = lastReadDictionary[(bib, read.LocationID)].Read.TimeMilliseconds;
                                }
                                // Check if we're past the max occurances allowed for this spot.
                                // Also check if we've passed the finish occurrence for the finish line and that division
                                // which requires an active division and the person's information
                                if (occurrence > maxOccurrences ||
                                    (div != null && Constants.Timing.LOCATION_FINISH == read.LocationID && occurrence > div.FinishOccurrence))
                                {
                                    read.Status = Constants.Timing.CHIPREAD_STATUS_OVERMAX;
                                }
                                // occurrence is in [1,maxOccurrences], but can't be used because it's in the
                                // ignore period
                                else if (read.TimeSeconds < minSeconds || (read.TimeSeconds == minSeconds && read.TimeMilliseconds <= minMilliseconds))
                                {
                                    read.Status = Constants.Timing.CHIPREAD_STATUS_WITHINIGN;
                                }
                                // occurrence is in [1, maxOccurences] but not in the ignore period
                                else
                                {
                                    lastReadDictionary[(bib, read.LocationID)] = (read, occurrence);
                                    // Find if there's a segment associated with this combination
                                    int segId = Constants.Timing.SEGMENT_NONE;
                                    // With linked divisions we want to ensure we use the Finish Occurence and Segments from the linked
                                    // division instead of the actual division since those aren't set.
                                    int divId = div == null ? 0 : div.Identifier, divFinOcc = div == null ? 0 : div.FinishOccurrence;
                                    if (div != null && div.LinkedDivision > 0)
                                    {
                                        divId = div.LinkedDivision;
                                        divFinOcc = divisionDictionary.ContainsKey(div.LinkedDivision) ? divisionDictionary[div.LinkedDivision].FinishOccurrence : div.FinishOccurrence;
                                    }
                                    // First check if we're using Division specific segments
                                    if (theEvent.DivisionSpecificSegments && segmentDictionary.ContainsKey((Constants.Timing.COMMON_SEGMENTS_DIVISIONID, read.LocationID, occurrence)))
                                    {
                                        segId = segmentDictionary[(Constants.Timing.COMMON_SEGMENTS_DIVISIONID, read.LocationID, occurrence)].Identifier;
                                    }
                                    // Then check if we can find a segment
                                    else if (div != null && segmentDictionary.ContainsKey((divId, read.LocationID, occurrence)))
                                    {
                                        segId = segmentDictionary[(divId, read.LocationID, occurrence)].Identifier;
                                    }
                                    // then check if it's the finish occurence. obviously this doesn't work if we can't find the division
                                    else if (div != null && occurrence == divFinOcc && Constants.Timing.LOCATION_FINISH == read.LocationID)
                                    {
                                        segId = Constants.Timing.SEGMENT_FINISH;
                                    }
                                    string identifier = "Bib:" + bib.ToString();
                                    // Create a result for the start value.
                                    long secondsDiff = read.TimeSeconds - startSeconds;
                                    int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                                    if (millisecDiff < 0)
                                    {
                                        secondsDiff--;
                                        millisecDiff += 1000;
                                    }
                                    long chipSecDiff = read.TimeSeconds - (startTimes.ContainsKey(identifier) ? Constants.Timing.DateToEpoch(startTimes[identifier].SystemTime) : startSeconds);
                                    int chipMillisecDiff = read.TimeMilliseconds - (startTimes.ContainsKey(identifier) ? startTimes[identifier].SystemTime.Millisecond : startMilliseconds);
                                    if (chipMillisecDiff < 0)
                                    {
                                        chipSecDiff--;
                                        chipMillisecDiff += 1000;
                                    }
                                    // Check that we're not adding a finish time for a DNF person, we can use any other times
                                    // for information for that person.
                                    if (Constants.Timing.SEGMENT_FINISH != segId || !dnfDictionary.ContainsKey(bib))
                                    {
                                        newResults.Add(new TimeResult(theEvent.Identifier,
                                            read.ReadId,
                                            part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                                            read.LocationID,
                                            segId,
                                            occurrence,
                                            String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", secondsDiff / 3600, (secondsDiff % 3600) / 60, secondsDiff % 60, millisecDiff),
                                            identifier,
                                            String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", chipSecDiff / 3600, (chipSecDiff % 3600) / 60, chipSecDiff % 60, chipMillisecDiff),
                                            read.Time,
                                            bib,
                                            Constants.Timing.TIMERESULT_STATUS_NONE
                                            ));
                                        if (part != null)
                                        {
                                            // If they've finished, mark them as such.
                                            if (Constants.Timing.SEGMENT_FINISH == segId
                                                && !dnfDictionary.ContainsKey(bib))
                                            {
                                                part.Status = Constants.Timing.EVENTSPECIFIC_FINISHED;
                                                updateParticipants.Add(part);
                                            }
                                            // If they were marked as noshow previously, mark them as started
                                            else if (Constants.Timing.EVENTSPECIFIC_NOSHOW == part.Status
                                                && !dnfDictionary.ContainsKey(bib))
                                            {
                                                part.Status = Constants.Timing.EVENTSPECIFIC_STARTED;
                                                updateParticipants.Add(part);
                                            }
                                        }
                                    }
                                    read.Status = Constants.Timing.CHIPREAD_STATUS_USED;
                                }
                            }
                            // Possible reads at this point:
                            //      Start Location reads past the StartWindow (Set status to ignore)
                            else
                            {
                                read.Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                            }
                        }
                    }
                }
            }
            // process reads that have a chip
            Dictionary<string, ChipRead> chipStartReadDictionary = new Dictionary<string, ChipRead>();
            foreach (string chip in chipReadPairs.Keys)
            {
                long startSeconds, maxStartSeconds;
                int startMilliseconds;
                (startSeconds, startMilliseconds) = divisionStartDict[0];
                maxStartSeconds = startSeconds + theEvent.StartWindow;
                TimeResult startResult = null;
                foreach (ChipRead read in chipReadPairs[chip])
                {
                    // Check that we haven't processed the read yet
                    if (Constants.Timing.CHIPREAD_STATUS_NONE == read.Status)
                    {
                        // Check if we're before the start time.
                        if (read.TimeSeconds < startSeconds || (read.TimeSeconds == startSeconds && read.TimeMilliseconds < startMilliseconds))
                        {
                            read.Status = Constants.Timing.CHIPREAD_STATUS_PRESTART;
                        }
                        else
                        {
                            // If we're within the start period
                            // And the location is the Start, or we've got a combined start finish location
                            if ((read.TimeSeconds < maxStartSeconds || (read.TimeSeconds == maxStartSeconds && read.TimeMilliseconds <= startMilliseconds)) &&
                                (Constants.Timing.LOCATION_START == read.LocationID
                                    || (Constants.Timing.LOCATION_FINISH == read.LocationID
                                        && theEvent.CommonStartFinish)))
                            {
                                // check if we've stored a chipread as the start chipread, update it to unused if so
                                if (chipLastReadDictionary.ContainsKey((chip, read.LocationID)))
                                {
                                    chipLastReadDictionary[(chip, read.LocationID)].Read.Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                                }
                                // Update the last read we've seen at this location
                                chipLastReadDictionary[(chip, read.LocationID)] = (Read: read, Occurrence: 0);
                                // Check if we previously had a TimeResult for the start.
                                if (startResult != null)
                                {
                                    // Remove it if so.
                                    newResults.Remove(startResult);
                                }
                                string identifier = "Chip:" + chip;
                                // Create a result for the start value.
                                long secondsDiff = read.TimeSeconds - startSeconds;
                                int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                                if (millisecDiff < 0)
                                {
                                    secondsDiff--;
                                    millisecDiff += 1000;
                                }
                                startResult = new TimeResult(theEvent.Identifier,
                                    read.ReadId,
                                    Constants.Timing.TIMERESULT_DUMMYPERSON,
                                    read.LocationID,
                                    Constants.Timing.SEGMENT_START,
                                    0, // start reads are not an occurrence at the start line
                                    String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", secondsDiff / 3600, (secondsDiff % 3600) / 60, secondsDiff % 60, millisecDiff),
                                    identifier,
                                    "0:00:00.000",
                                    read.Time,
                                    read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib,
                                    Constants.Timing.TIMERESULT_STATUS_NONE
                                    );
                                newResults.Add(startResult);
                                startTimes[startResult.Identifier] = startResult;
                                // Finally, set the chipread status to USED.
                                read.Status = Constants.Timing.CHIPREAD_STATUS_USED;
                            }
                            // Possible reads at this point:
                            //      Start Location reads past the StartWindow (IGNORE)
                            //      Start/Finish Location reads past the StartWindow (Valid reads)
                            //      Reads at any other location
                            else if (Constants.Timing.LOCATION_START != read.LocationID)
                            {
                                int maxOccurrences = 0;
                                if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                                {
                                    maxOccurrences = theEvent.FinishMaxOccurrences;
                                }
                                else
                                {
                                    if (!locationDictionary.ContainsKey(read.LocationID))
                                    {
                                        Log.E("Somehow the location was not found.");
                                    }
                                    else
                                    {
                                        maxOccurrences = locationDictionary[read.LocationID].MaxOccurrences;
                                    }
                                }
                                int occurrence = 1;
                                int occursWithin = 0;
                                if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                                {
                                    occursWithin = theEvent.FinishIgnoreWithin;
                                }
                                else if (locationDictionary.ContainsKey(read.LocationID))
                                {
                                    occursWithin = locationDictionary[read.LocationID].IgnoreWithin;
                                }
                                // Minimum Time Value required to actually create a result
                                long minSeconds = startSeconds;
                                int minMilliseconds = startMilliseconds;
                                // Check if there's a previous read at this location.
                                if (chipLastReadDictionary.ContainsKey((chip, read.LocationID)))
                                {
                                    occurrence = chipLastReadDictionary[(chip, read.LocationID)].Occurrence + 1;
                                    minSeconds = chipLastReadDictionary[(chip, read.LocationID)].Read.TimeSeconds + occursWithin;
                                    minMilliseconds = chipLastReadDictionary[(chip, read.LocationID)].Read.TimeMilliseconds;
                                }
                                // Check if we're past the max occurances allowed for this spot.
                                if (occurrence > maxOccurrences)
                                {
                                    read.Status = Constants.Timing.CHIPREAD_STATUS_OVERMAX;
                                }
                                // occurrence is in [1,maxOccurrences], but can't be used because it's in the
                                // ignore period
                                else if (read.TimeSeconds < minSeconds || (read.TimeSeconds == minSeconds && read.TimeMilliseconds < minMilliseconds))
                                {
                                    read.Status = Constants.Timing.CHIPREAD_STATUS_WITHINIGN;
                                }
                                // occurrence is in [1, maxOccurences] but not in the ignore period
                                else
                                {
                                    chipLastReadDictionary[(chip, read.LocationID)] = (read, occurrence);
                                    // Find if there's a segment associated with this combination
                                    int segId = Constants.Timing.SEGMENT_NONE;
                                    // First check if we're using Division specific segments
                                    if (theEvent.DivisionSpecificSegments && segmentDictionary.ContainsKey((Constants.Timing.COMMON_SEGMENTS_DIVISIONID, read.LocationID, occurrence)))
                                    {
                                        segId = segmentDictionary[(Constants.Timing.COMMON_SEGMENTS_DIVISIONID, read.LocationID, occurrence)].Identifier;
                                    }
                                    string identifier = "Chip:" + chip.ToString();
                                    // Create a result for the start value.
                                    long secondsDiff = read.TimeSeconds - startSeconds;
                                    int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                                    if (millisecDiff < 0)
                                    {
                                        secondsDiff--;
                                        millisecDiff += 1000;
                                    }
                                    long chipSecDiff = read.TimeSeconds - (startTimes.ContainsKey(identifier) ? Constants.Timing.DateToEpoch(startTimes[identifier].SystemTime) : startSeconds);
                                    int chipMillisecDiff = read.TimeMilliseconds - (startTimes.ContainsKey(identifier) ? startTimes[identifier].SystemTime.Millisecond : startMilliseconds);
                                    if (chipMillisecDiff < 0)
                                    {
                                        chipSecDiff--;
                                        chipMillisecDiff += 1000;
                                    }
                                    // Check that we're not adding a finish time for a DNF person, we can use any other times
                                    // for information for that person.
                                    if (Constants.Timing.SEGMENT_FINISH != segId || !chipDnfDictionary.ContainsKey(chip))
                                    {
                                        newResults.Add(new TimeResult(theEvent.Identifier,
                                        read.ReadId,
                                        Constants.Timing.TIMERESULT_DUMMYPERSON,
                                        read.LocationID,
                                        segId,
                                        occurrence,
                                        String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", secondsDiff / 3600, (secondsDiff % 3600) / 60, secondsDiff % 60, millisecDiff),
                                        identifier,
                                        String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", chipSecDiff / 3600, (chipSecDiff % 3600) / 60, chipSecDiff % 60, chipMillisecDiff),
                                        read.Time,
                                        read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib,
                                        Constants.Timing.TIMERESULT_STATUS_NONE
                                        ));
                                        read.Status = Constants.Timing.CHIPREAD_STATUS_USED;
                                    }
                                }
                            }
                            // Possible reads at this point:
                            //      Start Location reads past the StartWindow (Set status to ignore)
                            else
                            {
                                read.Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                            }
                        }
                    }
                }
            }
            // Process the intersection of DNF people and Finish results:
            foreach (string chip in chipDnfDictionary.Keys)
            {
                if (finishTimes.ContainsKey("Chip:" + chip))
                {
                    TimeResult finish = finishTimes["Chip:" + chip];
                    finish.ReadId = chipDnfDictionary[chip].ReadId;
                    finish.Time = "DNF";
                    finish.ChipTime = "DNF";
                    finish.Status = Constants.Timing.TIMERESULT_STATUS_DNF;
                    finish.Occurrence = chipLastReadDictionary.ContainsKey((chip, Constants.Timing.LOCATION_FINISH)) ? chipLastReadDictionary[(chip, Constants.Timing.LOCATION_FINISH)].Occurrence + 1 : -1;
                    newResults.Add(finish);
                }
                else
                {
                    newResults.Add(new TimeResult(theEvent.Identifier,
                        chipDnfDictionary[chip].ReadId,
                        Constants.Timing.TIMERESULT_DUMMYPERSON,
                        Constants.Timing.LOCATION_FINISH,
                        Constants.Timing.SEGMENT_FINISH,
                        chipLastReadDictionary.ContainsKey((chip, Constants.Timing.LOCATION_FINISH)) ? chipLastReadDictionary[(chip, Constants.Timing.LOCATION_FINISH)].Occurrence + 1 : -1,
                        "DNF",
                        "Chip:" + chip,
                        "DNF",
                        chipDnfDictionary[chip].Time,
                        chipDnfDictionary[chip].ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? chipDnfDictionary[chip].ReadBib : chipDnfDictionary[chip].ChipBib,
                        Constants.Timing.TIMERESULT_STATUS_DNF
                        ));
                }
            }
            // Process the intersection of DNF people and Finish results:
            foreach (int bib in dnfDictionary.Keys)
            {
                Participant part = participantBibDictionary.ContainsKey(bib) ?
                    participantBibDictionary[bib] :
                    null;
                if (part != null)
                {
                    part.Status = Constants.Timing.EVENTSPECIFIC_NOFINISH;
                    updateParticipants.Add(part);
                }
                int occurrence = -1;
                if (lastReadDictionary.ContainsKey((bib, Constants.Timing.LOCATION_FINISH)))
                {
                    occurrence = lastReadDictionary[(bib, Constants.Timing.LOCATION_FINISH)].Occurrence + 1;
                }
                else if (bibChipDictionary.ContainsKey(bib) && chipLastReadDictionary.ContainsKey((bibChipDictionary[bib], Constants.Timing.LOCATION_FINISH)))
                {
                    occurrence = chipLastReadDictionary[(bibChipDictionary[bib], Constants.Timing.LOCATION_FINISH)].Occurrence + 1;
                }
                if (finishTimes.ContainsKey("Bib:" + bib.ToString()))
                {
                    TimeResult finish = finishTimes["Bib:" + bib.ToString()];
                    finish.ReadId = dnfDictionary[bib].ReadId;
                    finish.Time = "DNF";
                    finish.ChipTime = "DNF";
                    finish.Status = Constants.Timing.TIMERESULT_STATUS_DNF;
                    finish.Occurrence = occurrence;
                    newResults.Add(finish);
                }
                else
                {
                    newResults.Add(new TimeResult(theEvent.Identifier,
                        dnfDictionary[bib].ReadId,
                        part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                        Constants.Timing.LOCATION_FINISH,
                        Constants.Timing.SEGMENT_FINISH,
                        occurrence,
                        "DNF",
                        "Bib:" + bib.ToString(),
                        "DNF",
                        dnfDictionary[bib].Time,
                        bib,
                        Constants.Timing.TIMERESULT_STATUS_DNF));
                }
            }
            // process reads that need to be set to ignore
            foreach (ChipRead read in setUnknown)
            {
                read.Status = Constants.Timing.CHIPREAD_STATUS_UNKNOWN;
            }
            // Update database with information.
            database.AddTimingResults(newResults);
            database.SetChipReadStatuses(allChipReads);
            database.UpdateParticipants(new List<Participant>(updateParticipants));
            return newResults;
        }

        private List<TimeResult> ProcessTimeBasedRace(Event theEvent)
        {
            Log.D("Processing chip reads for a time based event.");
            // Check if there's anything to process.
            // Get start TimeREsults
            Dictionary<string, TimeResult> startTimes = new Dictionary<string, TimeResult>();
            foreach (TimeResult result in database.GetStartTimes(theEvent.Identifier))
            {
                startTimes[result.Identifier] = result;
            }
            // Get all of the Chip Reads we find useful (Unprocessed, and those used
            // as results.) and sort them into groups based upon Bib, Chip, or put them
            // in the ignore pile if no chip/bib found.
            Dictionary<int, List<ChipRead>> bibReadPairs = new Dictionary<int, List<ChipRead>>();
            Dictionary<string, List<ChipRead>> chipReadPairs = new Dictionary<string, List<ChipRead>>();
            // Make sure we keep track of the last occurrence for a person at a location.
            // (Bib, Location), Last Chip Read
            Dictionary<(int, int), (ChipRead Read, int Occurrence)> lastReadDictionary = new Dictionary<(int, int), (ChipRead Read, int Occurrence)>();
            Dictionary<int, ChipRead> startReadDictionary = new Dictionary<int, ChipRead>();
            Dictionary <(string, int), (ChipRead Read, int Occurrence)> chipLastReadDictionary = new Dictionary<(string, int), (ChipRead Read, int Occurrence)>();
            Dictionary<string, ChipRead> chipStartReadDictionary = new Dictionary<string, ChipRead>();
            List<ChipRead> allChipReads = database.GetUsefulChipReads(theEvent.Identifier);
            allChipReads.Sort();
            List<ChipRead> setUnknown = new List<ChipRead>();
            foreach (ChipRead read in allChipReads)
            {
                if (read.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
                {
                    // if we process all the used reads before putting them in the list we can
                    // ensure that all of the reads we process are STATUS_NONE and then we can
                    // verify that we aren't inserting results BEFORE results we've already calculated.
                    if (Constants.Timing.CHIPREAD_STATUS_USED == read.Status)
                    {
                        if (!lastReadDictionary.ContainsKey((read.Bib, read.LocationID)))
                        {
                            lastReadDictionary[(read.Bib, read.LocationID)] = (read, 1);
                        }
                        else
                        {
                            lastReadDictionary[(read.Bib, read.LocationID)] = (read, lastReadDictionary[(read.Bib, read.LocationID)].Occurrence + 1);
                        }
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_STARTTIME == read.Status && (
                        Constants.Timing.LOCATION_START == read.LocationID ||
                            (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish)))
                    {
                        startReadDictionary[read.Bib] = read;
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_NONE == read.Status || Constants.Timing.CHIPREAD_STATUS_DNF == read.Status)
                    {
                        if (!bibReadPairs.ContainsKey(read.Bib))
                        {
                            bibReadPairs[read.Bib] = new List<ChipRead>();
                        }
                        bibReadPairs[read.Bib].Add(read);
                    }
                }
                else if (Constants.Timing.CHIPREAD_DUMMYCHIP != read.ChipNumber)
                {
                    if (Constants.Timing.CHIPREAD_STATUS_USED == read.Status)
                    {
                        if (!chipLastReadDictionary.ContainsKey((read.ChipNumber.ToString(), read.LocationID)))
                        {
                            chipLastReadDictionary[(read.ChipNumber.ToString(), read.LocationID)] = (read, 1);
                        }
                        else
                        {
                            chipLastReadDictionary[(read.ChipNumber.ToString(), read.LocationID)] = (read, chipLastReadDictionary[(read.ChipNumber.ToString(), read.LocationID)].Occurrence + 1);
                        }
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_STARTTIME == read.Status && (
                        Constants.Timing.LOCATION_START == read.LocationID ||
                            (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish)))
                    {
                        chipStartReadDictionary[read.ChipNumber.ToString()] = read;
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_NONE == read.Status || Constants.Timing.CHIPREAD_STATUS_DNF == read.Status)
                    {
                        if (!chipReadPairs.ContainsKey(read.ChipNumber.ToString()))
                        {
                            chipReadPairs[read.ChipNumber.ToString()] = new List<ChipRead>();
                        }
                        chipReadPairs[read.ChipNumber.ToString()].Add(read);
                    }
                }
                else
                {
                    setUnknown.Add(read);
                }
            }
            // Go through all of the chipreads we've marked and create new results.
            List<TimeResult> newResults = new List<TimeResult>();
            List<Participant> updateParticipants = new List<Participant>();
            // start with bibs
            foreach (int bib in bibReadPairs.Keys)
            {
                Participant part = participantBibDictionary.ContainsKey(bib) ?
                    participantBibDictionary[bib] :
                    null;
                if (part != null)
                {
                    part.Status = Constants.Timing.EVENTSPECIFIC_STARTED;
                    updateParticipants.Add(part);
                }
                Distance div = part != null ?
                    divisionDictionary[part.EventSpecific.DivisionIdentifier] :
                    null;
                long startSeconds, maxStartSeconds, endSeconds;
                int startMilliseconds;
                TimeResult startResult = null;
                if (div == null || !divisionStartDict.ContainsKey(div.Identifier) || !divisionEndDict.ContainsKey(div.Identifier))
                {
                    (startSeconds, startMilliseconds) = divisionStartDict[0];
                    endSeconds = divisionEndDict[0].Seconds;
                }
                else
                {
                    (startSeconds, startMilliseconds) = divisionStartDict[div.Identifier];
                    endSeconds = divisionEndDict[div.Identifier].Seconds;
                }
                maxStartSeconds = startSeconds + theEvent.StartWindow;
                bool finished = false;
                foreach (ChipRead read in bibReadPairs[bib])
                {
                    // pre-start
                    if (read.TimeSeconds < startSeconds || (read.TimeSeconds == startSeconds && read.TimeMilliseconds < startMilliseconds))
                    {
                        read.Status = Constants.Timing.CHIPREAD_STATUS_PRESTART;
                    }
                    else if (read.TimeSeconds > endSeconds || (read.TimeSeconds == endSeconds && read.TimeMilliseconds > startMilliseconds))
                    {
                        read.Status = Constants.Timing.CHIPREAD_STATUS_OVERMAX;
                    }
                    else
                    {
                        // check if we're in the starting window and at the start line
                        if ((read.TimeSeconds < maxStartSeconds || (read.TimeSeconds == maxStartSeconds && read.TimeMilliseconds <= startMilliseconds)) &&
                            (Constants.Timing.LOCATION_START == read.LocationID ||
                            (Constants.Timing.LOCATION_FINISH == read.LocationID
                            && theEvent.CommonStartFinish)))
                        {
                            // check if we've stored a chipread as the start chipread, update it to unused if so
                            if (startReadDictionary.ContainsKey(bib))
                            {
                                startReadDictionary[bib].Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                            }
                            // Update the last read we've seen at this location
                            startReadDictionary[bib] = read;
                            if (startResult != null && newResults.Contains(startResult))
                            {
                                newResults.Remove(startResult);
                            }
                            // Create a result for the start time.
                            long secondsDiff = read.TimeSeconds - startSeconds;
                            int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                            if (millisecDiff < 0)
                            {
                                secondsDiff--;
                                millisecDiff += 1000;
                            }
                            startResult = new TimeResult(theEvent.Identifier,
                                read.ReadId,
                                part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                                read.LocationID,
                                Constants.Timing.SEGMENT_START,
                                0, // start reads are not an occurrence at the start line
                                String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", secondsDiff / 3600, (secondsDiff % 3600) / 60, secondsDiff % 60, millisecDiff),
                                "Bib:" + bib.ToString(),
                                "0:00:00.000",
                                read.Time,
                                bib,
                                Constants.Timing.TIMERESULT_STATUS_NONE
                                );
                            startTimes[startResult.Identifier] = startResult;
                            newResults.Add(startResult);
                            read.Status = Constants.Timing.CHIPREAD_STATUS_STARTTIME;
                        }
                        // Possible reads at this point:
                        //      Start Location reads past the StartWindow (IGNORE)
                        //      Start/Finish Location reads past the StartWindow (valid reads)
                        //          These could be BEFORE or AFTER the last occurrence at this spot
                        //      Reads at any other location
                        else if (Constants.Timing.LOCATION_START != read.LocationID)
                        {
                            int occurrence = 1;
                            int occursWithin = 0;
                            if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                            {
                                occursWithin = theEvent.FinishIgnoreWithin;
                            }
                            else if (locationDictionary.ContainsKey(read.LocationID))
                            {
                                occursWithin = locationDictionary[read.LocationID].IgnoreWithin;
                            }
                            // Minimum time to create a result.
                            long minSeconds = startSeconds;
                            int minMilliseconds = startMilliseconds;
                            if (lastReadDictionary.ContainsKey((bib, read.LocationID)))
                            {
                                occurrence = lastReadDictionary[(bib, read.LocationID)].Occurrence + 1;
                                minSeconds = lastReadDictionary[(bib, read.LocationID)].Read.TimeSeconds + occursWithin;
                                minMilliseconds = lastReadDictionary[(bib, read.LocationID)].Read.TimeMilliseconds;
                            }
                            // Check if this is a 'dnf' read.  If it is we set the flag to the previous finish time and
                            // IGNORE ANY SUBSEQUENT READS. PERIOD.
                            if (Constants.Timing.CHIPREAD_STATUS_DNF == read.Status)
                            {
                                finished = true;
                            }
                            // Check if we've marked the person as finished;
                            if (finished == true)
                            {
                                read.Status = Constants.Timing.CHIPREAD_STATUS_OVERMAX;
                            }
                            // Check if we're in the ignore within period.
                            else if (read.TimeSeconds < minSeconds || (read.TimeSeconds == minSeconds && read.TimeMilliseconds < minMilliseconds))
                            {
                                read.Status = Constants.Timing.CHIPREAD_STATUS_WITHINIGN;
                            }
                            else
                            {
                                lastReadDictionary[(bib, read.LocationID)] = (read, occurrence);
                                int segId = Constants.Timing.SEGMENT_NONE;
                                // Check for linked division and set divId to the linked division, or to the actual div id.
                                // Segments are based on the linked division.
                                int divId = div == null ? 0 : div.LinkedDivision > 0 ? div.LinkedDivision : div.Identifier;
                                // Check for Division specific segments (Occurrence is always 1 for time based)
                                if (theEvent.DivisionSpecificSegments && segmentDictionary.ContainsKey((Constants.Timing.COMMON_SEGMENTS_DIVISIONID, read.LocationID, 1)))
                                {
                                    segId = segmentDictionary[(Constants.Timing.COMMON_SEGMENTS_DIVISIONID, read.LocationID, 1)].Identifier;
                                }
                                // Division specific segments
                                else if (div != null && segmentDictionary.ContainsKey((divId, read.LocationID, 1)))
                                {
                                    segId = segmentDictionary[(divId, read.LocationID, 1)].Identifier;
                                }
                                else if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                                {
                                    segId = Constants.Timing.SEGMENT_FINISH;
                                }
                                string identifier = "Bib:" + bib.ToString();
                                // Create a result for the start value
                                long secondsDiff = read.TimeSeconds - startSeconds;
                                int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                                if (millisecDiff < 0)
                                {
                                    secondsDiff--;
                                    millisecDiff += 1000;
                                }
                                long chipSecDiff = read.TimeSeconds - (startTimes.ContainsKey(identifier) ? Constants.Timing.DateToEpoch(startTimes[identifier].SystemTime) : startSeconds);
                                int chipMillisecDiff = read.TimeMilliseconds - (startTimes.ContainsKey(identifier) ? startTimes[identifier].SystemTime.Millisecond : startMilliseconds);
                                if (chipMillisecDiff < 0)
                                {
                                    chipSecDiff--;
                                    chipMillisecDiff += 1000;
                                }
                                newResults.Add(new TimeResult(theEvent.Identifier,
                                    read.ReadId,
                                    part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                                    read.LocationID,
                                    segId,
                                    occurrence,
                                    String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", secondsDiff / 3600, (secondsDiff % 3600) / 60, secondsDiff % 60, millisecDiff),
                                    identifier,
                                    String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", chipSecDiff / 3600, (chipSecDiff % 3600) / 60, chipSecDiff % 60, chipMillisecDiff),
                                    read.Time,
                                    bib,
                                    Constants.Timing.TIMERESULT_STATUS_NONE
                                    ));
                                read.Status = Constants.Timing.CHIPREAD_STATUS_USED;
                            }
                        }
                        // Possible reads at this point:
                        //      Start Location reads past the StartWindow (set status to ignore)
                        else
                        {
                            read.Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                        }
                    }
                }
            }
            foreach (string chip in chipReadPairs.Keys)
            {
                long startSeconds, maxStartSeconds, endSeconds;
                int startMilliseconds;
                (startSeconds, startMilliseconds) = divisionStartDict[0];
                endSeconds = divisionEndDict[0].Seconds;
                maxStartSeconds = startSeconds + theEvent.StartWindow;
                TimeResult startResult = null;
                // keep a boolean so we can notify ourselves if we've marked a person as finished
                bool finished = false;
                foreach (ChipRead read in chipReadPairs[chip])
                {
                    // pre-start
                    if (read.TimeSeconds < startSeconds || (read.TimeSeconds == startSeconds && read.TimeMilliseconds < startMilliseconds))
                    {
                        read.Status = Constants.Timing.CHIPREAD_STATUS_PRESTART;
                    }
                    else if (read.TimeSeconds > endSeconds || (read.TimeSeconds == endSeconds && read.TimeMilliseconds > startMilliseconds))
                    {
                        read.Status = Constants.Timing.CHIPREAD_STATUS_OVERMAX;
                    }
                    else
                    {
                        // check if we're in the starting window and at the start line
                        if ((read.TimeSeconds < maxStartSeconds || (read.TimeSeconds == maxStartSeconds && read.TimeMilliseconds <= startMilliseconds)) &&
                            (Constants.Timing.LOCATION_START == read.LocationID ||
                            (Constants.Timing.LOCATION_FINISH == read.LocationID
                            && theEvent.CommonStartFinish)))
                        {
                            // check if we've stored a chipread as the start chipread, update it to unused if so
                            if (chipStartReadDictionary.ContainsKey(chip))
                            {
                                chipStartReadDictionary[chip].Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                            }
                            // Update the last read we've seen at this location
                            chipStartReadDictionary[chip] = read;
                            if (startResult != null && newResults.Contains(startResult))
                            {
                                newResults.Remove(startResult);
                            }
                            // Create a result for the start time.
                            long secondsDiff = read.TimeSeconds - startSeconds;
                            int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                            if (millisecDiff < 0)
                            {
                                secondsDiff--;
                                millisecDiff += 1000;
                            }
                            startResult = new TimeResult(theEvent.Identifier,
                                read.ReadId,
                                Constants.Timing.TIMERESULT_DUMMYPERSON,
                                read.LocationID,
                                Constants.Timing.SEGMENT_START,
                                0, // start reads are not an occurrence at the start line
                                String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", secondsDiff / 3600, (secondsDiff % 3600) / 60, secondsDiff % 60, millisecDiff),
                                "Chip:" + chip.ToString(),
                                "0:00:00.000",
                                read.Time,
                                read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib,
                                Constants.Timing.TIMERESULT_STATUS_NONE
                                );
                            startTimes[startResult.Identifier] = startResult;
                            newResults.Add(startResult);
                            read.Status = Constants.Timing.CHIPREAD_STATUS_STARTTIME;
                        }
                        // Possible reads at this point:
                        //      Start Location reads past the StartWindow (IGNORE)
                        //      Start/Finish Location reads past the StartWindow (valid reads)
                        //          These could be BEFORE or AFTER the last occurrence at this spot
                        //      Reads at any other location
                        else if (Constants.Timing.LOCATION_START != read.LocationID)
                        {
                            int occurrence = 1;
                            int occursWithin = 0;
                            if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                            {
                                occursWithin = theEvent.FinishIgnoreWithin;
                            }
                            else if (locationDictionary.ContainsKey(read.LocationID))
                            {
                                occursWithin = locationDictionary[read.LocationID].IgnoreWithin;
                            }
                            // Minimum time to create a result.
                            long minSeconds = startSeconds;
                            int minMilliseconds = startMilliseconds;
                            if (chipLastReadDictionary.ContainsKey((chip, read.LocationID)))
                            {
                                occurrence = chipLastReadDictionary[(chip, read.LocationID)].Occurrence + 1;
                                minSeconds = chipLastReadDictionary[(chip, read.LocationID)].Read.TimeSeconds + occursWithin;
                                minMilliseconds = chipLastReadDictionary[(chip, read.LocationID)].Read.TimeMilliseconds;
                            }
                            // Check if this is a 'dnf' read.  If it is we set the flag to the previous finish time and
                            // IGNORE ANY SUBSEQUENT READS. PERIOD.
                            if (Constants.Timing.CHIPREAD_STATUS_DNF == read.Status)
                            {
                                finished = true;
                            }
                            // Check if we've marked the person as finished;
                            if (finished == true)
                            {
                                read.Status = Constants.Timing.CHIPREAD_STATUS_OVERMAX;
                            }
                            // Check if we're in the ignore within period.
                            else if (read.TimeSeconds < minSeconds || (read.TimeSeconds == minSeconds && read.TimeMilliseconds < minMilliseconds))
                            {
                                read.Status = Constants.Timing.CHIPREAD_STATUS_WITHINIGN;
                            }
                            else
                            {
                                chipLastReadDictionary[(chip, read.LocationID)] = (read, occurrence);
                                int segId = Constants.Timing.SEGMENT_NONE;
                                // Check for Division specific segments (Occurrence is always 1 for time based)
                                if (theEvent.DivisionSpecificSegments && segmentDictionary.ContainsKey((Constants.Timing.COMMON_SEGMENTS_DIVISIONID, read.LocationID, 1)))
                                {
                                    segId = segmentDictionary[(Constants.Timing.COMMON_SEGMENTS_DIVISIONID, read.LocationID, 1)].Identifier;
                                }
                                else if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                                {
                                    segId = Constants.Timing.SEGMENT_FINISH;
                                }
                                string identifier = "Chip:" + chip.ToString();
                                // Create a result for the start value
                                long secondsDiff = read.TimeSeconds - startSeconds;
                                int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                                if (millisecDiff < 0)
                                {
                                    secondsDiff--;
                                    millisecDiff += 1000;
                                }
                                long chipSecDiff = read.TimeSeconds - (startTimes.ContainsKey(identifier) ? Constants.Timing.DateToEpoch(startTimes[identifier].SystemTime) : startSeconds);
                                int chipMillisecDiff = read.TimeMilliseconds - (startTimes.ContainsKey(identifier) ? startTimes[identifier].SystemTime.Millisecond : startMilliseconds);
                                if (chipMillisecDiff < 0)
                                {
                                    chipSecDiff--;
                                    chipMillisecDiff += 1000;
                                }
                                newResults.Add(new TimeResult(theEvent.Identifier,
                                    read.ReadId,
                                    Constants.Timing.TIMERESULT_DUMMYPERSON,
                                    read.LocationID,
                                    segId,
                                    occurrence,
                                    String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", secondsDiff / 3600, (secondsDiff % 3600) / 60, secondsDiff % 60, millisecDiff),
                                    identifier,
                                    String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", chipSecDiff / 3600, (chipSecDiff % 3600) / 60, chipSecDiff % 60, chipMillisecDiff),
                                    read.Time,
                                    read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib,
                                    Constants.Timing.TIMERESULT_STATUS_NONE
                                    ));
                                read.Status = Constants.Timing.CHIPREAD_STATUS_USED;
                            }
                        }
                        // Possible reads at this point:
                        //      Start Location reads past the StartWindow (set status to ignore)
                        else
                        {
                            read.Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                        }
                    }
                }
            }
            // process reads that need to be set to ignore
            foreach (ChipRead read in setUnknown)
            {
                read.Status = Constants.Timing.CHIPREAD_STATUS_UNKNOWN;
            }
            // Update database with information.
            database.AddTimingResults(newResults);
            database.SetChipReadStatuses(allChipReads);
            database.UpdateParticipants(updateParticipants);
            return newResults;
        }

        private List<TimeResult> ProcessPlacementsDistance(Event theEvent)
        {
            List<TimeResult> output = new List<TimeResult>();
            // Get a list of all segments
            List<Segment> segments = database.GetSegments(theEvent.Identifier);
            Dictionary<int, List<TimeResult>> segmentDictionary = new Dictionary<int, List<TimeResult>>();
            foreach (TimeResult result in database.GetTimingResults(theEvent.Identifier))
            {
                if (!segmentDictionary.ContainsKey(result.SegmentId))
                {
                    segmentDictionary[result.SegmentId] = new List<TimeResult>();
                }
                segmentDictionary[result.SegmentId].Add(result);
            }
            // process results based upon the segment they're in
            foreach (Segment segment in segments)
            {
                Log.D("Processing segment " + segment.Name);
                if (segmentDictionary.ContainsKey(segment.Identifier))
                {
                    output.AddRange(ProcessSegmentPlacementsDistance(theEvent, segmentDictionary[segment.Identifier], participantEventSpecificDictionary));
                }
            }
            Log.D("Processing finish results");
            if (segmentDictionary.ContainsKey(Constants.Timing.SEGMENT_FINISH))
            {
                output.AddRange(ProcessSegmentPlacementsDistance(theEvent, segmentDictionary[Constants.Timing.SEGMENT_FINISH], participantEventSpecificDictionary));
            }
            database.AddTimingResults(output);
            return output;
        }

        private List<TimeResult> ProcessPlacementsTime(Event theEvent)
        {
            List<TimeResult> output = new List<TimeResult>();
            // Get a list of all segments
            List<Segment> segments = database.GetSegments(theEvent.Identifier);
            Dictionary<int, List<TimeResult>> segmentDictionary = new Dictionary<int, List<TimeResult>>();
            foreach (TimeResult result in database.GetTimingResults(theEvent.Identifier))
            {
                if (!segmentDictionary.ContainsKey(result.SegmentId))
                {
                    segmentDictionary[result.SegmentId] = new List<TimeResult>();
                }
                segmentDictionary[result.SegmentId].Add(result);
            }
            // process results based upon the segment they're in
            foreach (Segment segment in segments)
            {
                Log.D("Processing segment " + segment.Name);
                if (segmentDictionary.ContainsKey(segment.Identifier))
                {
                    output.AddRange(ProcessSegmentPlacementsTime(theEvent, segmentDictionary[segment.Identifier], participantEventSpecificDictionary));
                }
            }
            Log.D("Processing finish results");
            if (segmentDictionary.ContainsKey(Constants.Timing.SEGMENT_FINISH))
            {
                output.AddRange(ProcessSegmentPlacementsTime(theEvent, segmentDictionary[Constants.Timing.SEGMENT_FINISH], participantEventSpecificDictionary));
            }
            database.AddTimingResults(output);
            return output;
        }

        private List<TimeResult> ProcessSegmentPlacementsTime(Event theEvent,
            List<TimeResult> segmentResults,
            Dictionary<int, Participant> participantEventSpecificDictionary)
        {
            Dictionary<int, List<TimeResult>> personResults = new Dictionary<int, List<TimeResult>>();
            Dictionary<int, TimeResult> personLastResult = new Dictionary<int, TimeResult>();
            foreach (TimeResult result in segmentResults)
            {
                // If we don't have a Top Result for the person, or the result we have
                // is lesser than the one we're looking at, set it to the best
                if (!personLastResult.ContainsKey(result.EventSpecificId) || personLastResult[result.EventSpecificId].Occurrence < result.Occurrence)
                {
                    personLastResult[result.EventSpecificId] = result;
                }
                // Store a person's results
                if (!personResults.ContainsKey(result.EventSpecificId))
                {
                    personResults[result.EventSpecificId] = new List<TimeResult>();
                }
                personResults[result.EventSpecificId].Add(result);
            }
            // Get Dictionaries for storing the last known place (age group, gender)
            // The key is as follows: (Division ID, Age Group ID, int - Gender ID (M=1,F=2))
            // The value stored is the last place given
            Dictionary<(int, int, int), int> ageGroupPlaceDictionary = new Dictionary<(int, int, int), int>();
            // The key is as follows: (Division ID, Gender ID (M=1, F=2))
            // The value stored is the last place given
            Dictionary<(int, int), int> genderPlaceDictionary = new Dictionary<(int, int), int>();
            // The key is as follows: (Division ID)
            // The value stored is the last place given
            Dictionary<int, int> placeDictionary = new Dictionary<int, int>();
            int ageGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
            int divisionId = -1;
            int age = -1;
            int gender = -1;
            Participant person = null;
            List<TimeResult> topResults = personLastResult.Values.ToList<TimeResult>();
            topResults.Sort((x1, x2) =>
            {
                Distance div1 = null, div2 = null;
                int rank1 = 0, rank2 = 0;
                // Get *linked* divisions. (Could be that specific division)
                if (linkedDivisionDictionary.ContainsKey(x1.RealDivisionName))
                {
                    (div1, rank1) = linkedDivisionDictionary[x1.RealDivisionName];
                }
                if (linkedDivisionDictionary.ContainsKey(x2.RealDivisionName))
                {
                    (div2, rank2) = linkedDivisionDictionary[x2.RealDivisionName];
                }
                if (rank1 == rank2)
                {
                    if (x1.Occurrence == x2.Occurrence)
                    {
                        return x1.SystemTime.CompareTo(x2.SystemTime);
                    }
                    return x2.Occurrence.CompareTo(x1.Occurrence);
                }
                return rank1.CompareTo(rank2);
            });
            foreach (TimeResult result in topResults)
            {
                // Make sure we know who we're looking at. Can't rank otherwise.
                if (participantEventSpecificDictionary.ContainsKey(result.EventSpecificId))
                {
                    person = participantEventSpecificDictionary[result.EventSpecificId];
                    // Use a linked division ID for ranking instead of a specific division id.
                    if (linkedDivIdentifierDictionary.ContainsKey(person.EventSpecific.DivisionIdentifier))
                    {
                        divisionId = linkedDivIdentifierDictionary[person.EventSpecific.DivisionIdentifier];
                    }
                    else
                    {
                        divisionId = person.EventSpecific.DivisionIdentifier;
                    }
                    divisionId = person.EventSpecific.DivisionIdentifier;
                    age = person.GetAge(theEvent.Date);
                    gender = Constants.Timing.TIMERESULT_GENDER_UNKNOWN;
                    if (person.Gender.Equals("M", StringComparison.OrdinalIgnoreCase)
                        || person.Gender.Equals("Male", StringComparison.OrdinalIgnoreCase))
                    {
                        gender = Constants.Timing.TIMERESULT_GENDER_MALE;
                    }
                    else if (person.Gender.Equals("F", StringComparison.OrdinalIgnoreCase)
                        || person.Gender.Equals("Female", StringComparison.OrdinalIgnoreCase))
                    {
                        gender = Constants.Timing.TIMERESULT_GENDER_FEMALE;
                    }
                    ageGroupId = person.EventSpecific.AgeGroupId;
                    // Since Results were sorted before we started, let's assume that the first item
                    // is the fastest/best and if we can't find the key, add one starting at 0
                    if (!placeDictionary.ContainsKey(divisionId))
                    {
                        placeDictionary[divisionId] = 0;
                    }
                    result.Place = ++placeDictionary[divisionId];
                    if (!genderPlaceDictionary.ContainsKey((divisionId, gender)))
                    {
                        genderPlaceDictionary[(divisionId, gender)] = 0;
                    }
                    result.GenderPlace = ++genderPlaceDictionary[(divisionId, gender)];
                    if (!ageGroupPlaceDictionary.ContainsKey((divisionId, ageGroupId, gender)))
                    {
                        ageGroupPlaceDictionary[(divisionId, ageGroupId, gender)] = 0;
                    }
                    result.AgePlace = ++ageGroupPlaceDictionary[(divisionId, ageGroupId, gender)];
                    foreach (TimeResult otherResult in personResults[result.EventSpecificId])
                    {
                        otherResult.Place = result.Place;
                        otherResult.GenderPlace = result.GenderPlace;
                        otherResult.AgePlace = result.AgePlace;
                    }
                }
            }
            return segmentResults;
        }

        private List<TimeResult> ProcessSegmentPlacementsDistance(Event theEvent,
            List<TimeResult> segmentResults,
            Dictionary<int, Participant> participantEventSpecificDictionary)
        {
            if (theEvent.RankByGun)
            {
                //segmentResults.Sort(TimeResult.CompareByDivision);
                segmentResults.Sort((x1, x2) =>
                {
                    if (x1 == null || x2 == null) return 1;
                    Distance div1 = null, div2 = null;
                    int rank1 = 0, rank2 = 0;
                    Log.D("x1 division name: " + x1.RealDivisionName + " -- x2 division name: " + x2.RealDivisionName);
                    // Get *linked* divisions. (Could be that specific division)
                    if (linkedDivisionDictionary.ContainsKey(x1.RealDivisionName))
                    {
                        (div1, rank1) = linkedDivisionDictionary[x1.RealDivisionName];
                    }
                    if (linkedDivisionDictionary.ContainsKey(x2.RealDivisionName))
                    {
                        (div2, rank2) = linkedDivisionDictionary[x2.RealDivisionName];
                    }
                    Log.D((div1 == null || div2 == null) ? "One of the divs not found." : "Rank 1: " + rank1 + " -- Rank 2: " + rank2);
                    // Check if they're in the same division or a linked division.
                    if (div1 != null && div2 != null && div1.Identifier == div2.Identifier)
                    {
                        // Sort based on rank.  This is the linked division new sorting item.
                        if (rank1 == rank2)
                        {
                            Log.D("Ranks the same.");
                            // These are the old ways to sort before we've added linked divisions.
                            // Check if we know the participants we're comparing
                            if (participantEventSpecificDictionary.ContainsKey(x1.EventSpecificId) && participantEventSpecificDictionary.ContainsKey(x2.EventSpecificId))
                            {
                                // Check if they're both either EARLY START or not EARLY START. (DEPRECATED METHOD)
                                if (participantEventSpecificDictionary[x1.EventSpecificId].IsEarlyStart == participantEventSpecificDictionary[x2.EventSpecificId].IsEarlyStart)
                                {
                                    return x1.SystemTime.CompareTo(x2.SystemTime);
                                }
                                // Sort early starts below non early starts
                                return participantEventSpecificDictionary[x1.EventSpecificId].IsEarlyStart.CompareTo(participantEventSpecificDictionary[x2.EventSpecificId].IsEarlyStart);
                            }
                            // Sort by early start values
                            if (participantEventSpecificDictionary.ContainsKey(x2.EventSpecificId))
                            {
                                return participantEventSpecificDictionary[x2.EventSpecificId].IsEarlyStart.CompareTo(false);
                            }
                            if (participantEventSpecificDictionary.ContainsKey(x1.EventSpecificId))
                            {
                                return participantEventSpecificDictionary[x1.EventSpecificId].IsEarlyStart.CompareTo(false);
                            }
                        }
                        Log.D("Ranks not the same.");
                        // Ranks not the same
                        return rank1.CompareTo(rank2);
                    }
                    return x1.DivisionName.CompareTo(x2.DivisionName);
                });
            }
            else
            {
                //segmentResults.Sort(TimeResult.CompareByDivisionChip);
                segmentResults.Sort((x1, x2) =>
                {
                    if (x1 == null || x2 == null) return 1;
                    Distance div1 = null, div2 = null;
                    int rank1 = 0, rank2 = 0;
                    // Get *linked* divisions. (Could be that specific division)
                    if (linkedDivisionDictionary.ContainsKey(x1.RealDivisionName))
                    {
                        (div1, rank1) = linkedDivisionDictionary[x1.RealDivisionName];
                    }
                    if (linkedDivisionDictionary.ContainsKey(x2.RealDivisionName))
                    {
                        (div2, rank2) = linkedDivisionDictionary[x2.RealDivisionName];
                    }
                    // Check if they're in the same division or a linked division.
                    if (div1 != null && div2 != null && div1.Identifier == div2.Identifier)
                    {
                        // Sort based on rank.  This is the linked division new sorting item.
                        if (rank1 == rank2)
                        {
                            // These are the old ways to sort before we've added linked divisions.
                            // Check if we know the participants we're comparing
                            if (participantEventSpecificDictionary.ContainsKey(x1.EventSpecificId) && participantEventSpecificDictionary.ContainsKey(x2.EventSpecificId))
                            {
                            // Check if they're both either EARLY START or not EARLY START. (DEPRECATED METHOD)
                                if (participantEventSpecificDictionary[x1.EventSpecificId].IsEarlyStart == participantEventSpecificDictionary[x2.EventSpecificId].IsEarlyStart)
                                {
                                    return x1.CompareChip(x2);
                                }
                                // Sort early starts below non early starts
                                return participantEventSpecificDictionary[x1.EventSpecificId].IsEarlyStart.CompareTo(participantEventSpecificDictionary[x2.EventSpecificId].IsEarlyStart);
                            }
                            // Check if we know one of the two participants and sort based upon their early start value.
                            if (participantEventSpecificDictionary.ContainsKey(x2.EventSpecificId))
                            {
                                return participantEventSpecificDictionary[x2.EventSpecificId].IsEarlyStart.CompareTo(false);
                            }
                            if (participantEventSpecificDictionary.ContainsKey(x1.EventSpecificId))
                            {
                                return participantEventSpecificDictionary[x1.EventSpecificId].IsEarlyStart.CompareTo(false);
                            }
                        }
                        // Ranks not the same
                        return rank1.CompareTo(rank2);
                    }
                    // Default to sorting by division name.
                    return x1.DivisionName.CompareTo(x2.DivisionName);
                });
            }
            List<TimeResult> DNFResults = segmentResults.FindAll(x => x.IsDNF());
            foreach (TimeResult res in DNFResults)
            {
                res.Place = Constants.Timing.TIMERESULT_DUMMYPLACE;
                res.AgePlace = Constants.Timing.TIMERESULT_DUMMYPLACE;
                res.GenderPlace = Constants.Timing.TIMERESULT_DUMMYPLACE;
            }
            int removed = segmentResults.RemoveAll(x => x.IsDNF());
            Log.D(String.Format("{0} Result(s) in DNFResults - {1} Result(s) removed from segmentResults", DNFResults.Count, removed));
            // Get Dictionaries for storing the last known place (age group, gender)
            // The key is as follows: (Division ID, Age Group ID, int - Gender ID (M=1,F=2))
            // The value stored is the last place given
            Dictionary<(int, int, int), int> ageGroupPlaceDictionary = new Dictionary<(int, int, int), int>();
            // The key is as follows: (Division ID, Gender ID (M=1, F=2))
            // The value stored is the last place given
            Dictionary<(int, int), int> genderPlaceDictionary = new Dictionary<(int, int), int>();
            // The key is as follows: (Division ID)
            // The value stored is the last place given
            Dictionary<int, int> placeDictionary = new Dictionary<int, int>();
            int ageGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
            int divisionId = -1;
            int age = -1;
            int gender = -1;
            Participant person = null;
            foreach (TimeResult result in segmentResults)
            {
                // Check if we know who the person is. Can't rank them if we don't know
                // what division they're in, their age, or their gender
                if (participantEventSpecificDictionary.ContainsKey(result.EventSpecificId))
                {
                    person = participantEventSpecificDictionary[result.EventSpecificId];
                    // Use a linked division ID for ranking instead of a specific division id.
                    if (linkedDivIdentifierDictionary.ContainsKey(person.EventSpecific.DivisionIdentifier))
                    {
                        divisionId = linkedDivIdentifierDictionary[person.EventSpecific.DivisionIdentifier];
                    }
                    else
                    {
                        divisionId = person.EventSpecific.DivisionIdentifier;
                    }
                    age = person.GetAge(theEvent.Date);
                    gender = Constants.Timing.TIMERESULT_GENDER_UNKNOWN;
                    if (person.Gender.Equals("M", StringComparison.OrdinalIgnoreCase)
                        || person.Gender.Equals("Male", StringComparison.OrdinalIgnoreCase))
                    {
                        gender = Constants.Timing.TIMERESULT_GENDER_MALE;
                    }
                    else if (person.Gender.Equals("F", StringComparison.OrdinalIgnoreCase)
                        || person.Gender.Equals("Female", StringComparison.OrdinalIgnoreCase))
                    {
                        gender = Constants.Timing.TIMERESULT_GENDER_FEMALE;
                    }
                    ageGroupId = person.EventSpecific.AgeGroupId;
                    // Since Results were sorted before we started, let's assume that the first item
                    // is the fastest and if we can't find the key, add one starting at 0
                    if (!placeDictionary.ContainsKey(divisionId))
                    {
                        placeDictionary[divisionId] = 0;
                    }
                    result.Place = ++(placeDictionary[divisionId]);
                    if (!genderPlaceDictionary.ContainsKey((divisionId, gender)))
                    {
                        genderPlaceDictionary[(divisionId, gender)] = 0;
                    }
                    result.GenderPlace = ++(genderPlaceDictionary[(divisionId, gender)]);
                    if (!ageGroupPlaceDictionary.ContainsKey((divisionId, ageGroupId, gender)))
                    {
                        ageGroupPlaceDictionary[(divisionId, ageGroupId, gender)] = 0;
                    }
                    result.AgePlace = ++(ageGroupPlaceDictionary[(divisionId, ageGroupId, gender)]);
                }
            }
            segmentResults.AddRange(DNFResults);
            return segmentResults;
        }
    }
}
