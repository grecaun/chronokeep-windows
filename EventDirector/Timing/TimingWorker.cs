using EventDirector.Interfaces;
using EventDirector.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventDirector.Timing
{
    class TimingWorker
    {
        private readonly IDBInterface database;
        private readonly IMainWindow window;
        private static TimingWorker worker;

        private static readonly Semaphore semaphore = new Semaphore(0, 2);
        private static readonly Mutex mutex = new Mutex();
        private static readonly Mutex ageGroupMutex = new Mutex();
        private static readonly Mutex ResultsMutex = new Mutex();
        private static readonly Mutex ResetDictionariesMutex = new Mutex();
        private static bool QuittingTime = false;
        private static bool RecalculateAgeGroupsBool = true;
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
        private Dictionary<int, Division> divisionDictionary = new Dictionary<int, Division>();
        // (DivisionId, Age)
        private Dictionary<(int, int), int> divisionAgeGroups = new Dictionary<(int, int), int>();

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

        public static void RecalculateAgeGroups()
        {
            if (ageGroupMutex.WaitOne(3000))
            {
                RecalculateAgeGroupsBool = true;
                ageGroupMutex.ReleaseMutex();
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
            divisionStartDict[0] = (RFIDUltraInterface.DateToEpoch(startTime), theEvent.StartMilliseconds);
            // And the end time (for time based events)
            divisionEndDict.Clear();
            divisionEndDict[0] = divisionStartDict[0];
            // Divisions so we can get their start offset.
            divisionDictionary.Clear();
            foreach (Division div in database.GetDivisions(theEvent.Identifier))
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
            // Dictionary containing Age groups based upon their (Division, Age in Years)
            divisionAgeGroups.Clear();
            // process them into lists based upon divisions (in case there are division specific age groups)
            foreach (AgeGroup group in database.GetAgeGroups(theEvent.Identifier))
            {
                Log.D(String.Format("Age group {0} - Div {3} - {1} - {2}", group.GroupId, group.StartAge, group.EndAge, group.DivisionId));
                for (int age = group.StartAge; age <= group.EndAge; age++)
                {
                    divisionAgeGroups[(group.DivisionId, age)] = group.GroupId;
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
                    if (results != null && results.Count > 0)
                    {
                        window.NetworkAddResults(theEvent.Identifier, results);
                    }
                    results = null;
                    if (database.UnprocessedResultsExist(theEvent.Identifier))
                    {
                        if (ageGroupMutex.WaitOne(3000))
                        {
                            if (RecalculateAgeGroupsBool)
                            {
                                Log.D("Updating Age Groups.");
                                RecalculateAgeGroupsBool = false;
                                ageGroupMutex.ReleaseMutex();
                                UpdateAgeGroups(theEvent);
                            }
                            else
                            {
                                ageGroupMutex.ReleaseMutex();
                            }
                        }
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
                    if (results != null && results.Count > 0)
                    {
                        window.NetworkUpdateResults(theEvent.Identifier, results);
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
            List<ChipRead> allChipReads = database.GetUsefulChipReads(theEvent.Identifier);
            allChipReads.Sort();
            List<ChipRead> setUnknown = new List<ChipRead>();
            DateTime start = DateTime.Now;
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
                        (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish == 1)))
                    {
                        // If we haven't found anything, let us know what our start time was
                        if (!lastReadDictionary.ContainsKey((read.ChipBib, read.LocationID)))
                        {
                            lastReadDictionary[(read.Bib, read.LocationID)] = (read, 0);
                        }
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
                        (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish == 1)))
                    {
                        // If we haven't found anything, let us know what our start time was
                        if (!chipLastReadDictionary.ContainsKey((read.ChipNumber.ToString(), read.LocationID)))
                        {
                            chipLastReadDictionary[(read.ChipNumber.ToString(), read.LocationID)] = (Read: read, Occurrence: 0);
                        }
                    }
                    else
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
            DateTime end = DateTime.Now;
            TimeSpan first = end - start;
            start = DateTime.Now;
            // Go through each chip read for a single person.
            // List<ChipRead> updateStatusReads = new List<ChipRead>();
            List<TimeResult> newResults = new List<TimeResult>();
            // process reads that have a bib
            foreach (int bib in bibReadPairs.Keys)
            {
                Participant part = participantBibDictionary.ContainsKey(bib) ?
                    participantBibDictionary[bib] :
                    null;
                Division div = part != null ?
                    divisionDictionary[part.EventSpecific.DivisionIdentifier] :
                    null;
                long startSeconds, maxStartSeconds;
                int startMilliseconds;
                TimeResult startResult = null;
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
                if (part != null && part.EventSpecific != null && part.EventSpecific.EarlyStart == 1)
                {
                    startSeconds = startSeconds - theEvent.EarlyStartDifference;
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
                                        && theEvent.CommonStartFinish == 1)))
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
                                    bib);
                                startTimes[startResult.Identifier] = startResult;
                                newResults.Add(startResult);
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
                                    // First check if we're using Division specific segments
                                    if (theEvent.DivisionSpecificSegments == 1 && segmentDictionary.ContainsKey((Constants.Timing.COMMON_SEGMENTS_DIVISIONID, read.LocationID, occurrence)))
                                    {
                                        segId = segmentDictionary[(Constants.Timing.COMMON_SEGMENTS_DIVISIONID, read.LocationID, occurrence)].Identifier;
                                    }
                                    // Then check if we can find a segment
                                    else if (div != null && segmentDictionary.ContainsKey((div.Identifier, read.LocationID, occurrence)))
                                    {
                                        segId = segmentDictionary[(div.Identifier, read.LocationID, occurrence)].Identifier;
                                    }
                                    // then check if it's the finish occurence. obviously this doesn't work if we can't find the division
                                    else if (div != null && occurrence == div.FinishOccurrence && Constants.Timing.LOCATION_FINISH == read.LocationID)
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
                                    long chipSecDiff = read.TimeSeconds - (startTimes.ContainsKey(identifier) ? RFIDUltraInterface.DateToEpoch(startTimes[identifier].SystemTime) : startSeconds);
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
                                        bib));
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
                                        && theEvent.CommonStartFinish == 1)))
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
                                string identifier = "Chip:" + chip.ToString();
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
                                    read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib);
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
                                    if (theEvent.DivisionSpecificSegments == 1 && segmentDictionary.ContainsKey((Constants.Timing.COMMON_SEGMENTS_DIVISIONID, read.LocationID, occurrence)))
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
                                    long chipSecDiff = read.TimeSeconds - (startTimes.ContainsKey(identifier) ? RFIDUltraInterface.DateToEpoch(startTimes[identifier].SystemTime) : startSeconds);
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
                                        read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib));
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
            end = DateTime.Now;
            TimeSpan second = end - start;
            start = DateTime.Now;
            // process reads that need to be set to ignore
            foreach (ChipRead read in setUnknown)
            {
                read.Status = Constants.Timing.CHIPREAD_STATUS_UNKNOWN;
            }
            // Update database with information.
            database.AddTimingResults(newResults);
            database.SetChipReadStatuses(allChipReads);
            end = DateTime.Now;
            TimeSpan third = end - start;
            Log.D(String.Format("Done. Splitting into bib/chip: {0} - Creating Results: {1} - Putting Results in DB: {2}", first.ToString("c"), second.ToString("c"), third.ToString("c")));
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
            DateTime start = DateTime.Now;
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
                            (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish == 1)))
                    {
                        startReadDictionary[read.Bib] = read;
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_NONE == read.Status)
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
                            (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish == 1)))
                    {
                        chipStartReadDictionary[read.ChipNumber.ToString()] = read;
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_NONE == read.Status)
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
            DateTime end = DateTime.Now;
            TimeSpan first = end - start;
            start = DateTime.Now;
            // Go through all of the chipreads we've marked and create new results.
            List<TimeResult> newResults = new List<TimeResult>();
            // start with bibs
            foreach (int bib in bibReadPairs.Keys)
            {
                Participant part = participantBibDictionary.ContainsKey(bib) ?
                    participantBibDictionary[bib] :
                    null;
                Division div = part != null ?
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
                            && theEvent.CommonStartFinish == 1)))
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
                                bib);
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
                            // Check if we're in the ignore within period.
                            if (read.TimeSeconds < minSeconds || (read.TimeSeconds == minSeconds && read.TimeMilliseconds < minMilliseconds))
                            {
                                read.Status = Constants.Timing.CHIPREAD_STATUS_WITHINIGN;
                            }
                            else
                            {
                                lastReadDictionary[(bib, read.LocationID)] = (read, occurrence);
                                int segId = Constants.Timing.SEGMENT_NONE;
                                // Check for Division specific segments (Occurrence is always 1 for time based)
                                if (theEvent.DivisionSpecificSegments == 1 && segmentDictionary.ContainsKey((Constants.Timing.COMMON_SEGMENTS_DIVISIONID, read.LocationID, 1)))
                                {
                                    segId = segmentDictionary[(Constants.Timing.COMMON_SEGMENTS_DIVISIONID, read.LocationID, 1)].Identifier;
                                }
                                // Division specific segments
                                else if (div != null & segmentDictionary.ContainsKey((div.Identifier, read.LocationID, 1)))
                                {
                                    segId = segmentDictionary[(div.Identifier, read.LocationID, 1)].Identifier;
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
                                long chipSecDiff = read.TimeSeconds - (startTimes.ContainsKey(identifier) ? RFIDUltraInterface.DateToEpoch(startTimes[identifier].SystemTime) : startSeconds);
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
                                    bib));
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
                            && theEvent.CommonStartFinish == 1)))
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
                                read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib);
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
                            // Check if we're in the ignore within period.
                            if (read.TimeSeconds < minSeconds || (read.TimeSeconds == minSeconds && read.TimeMilliseconds < minMilliseconds))
                            {
                                read.Status = Constants.Timing.CHIPREAD_STATUS_WITHINIGN;
                            }
                            else
                            {
                                chipLastReadDictionary[(chip, read.LocationID)] = (read, occurrence);
                                int segId = Constants.Timing.SEGMENT_NONE;
                                // Check for Division specific segments (Occurrence is always 1 for time based)
                                if (theEvent.DivisionSpecificSegments == 1 && segmentDictionary.ContainsKey((Constants.Timing.COMMON_SEGMENTS_DIVISIONID, read.LocationID, 1)))
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
                                long chipSecDiff = read.TimeSeconds - (startTimes.ContainsKey(identifier) ? RFIDUltraInterface.DateToEpoch(startTimes[identifier].SystemTime) : startSeconds);
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
                                    read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib));
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
            end = DateTime.Now;
            TimeSpan second = end - start;
            start = DateTime.Now;
            // process reads that need to be set to ignore
            foreach (ChipRead read in setUnknown)
            {
                read.Status = Constants.Timing.CHIPREAD_STATUS_UNKNOWN;
            }
            // Update database with information.
            database.AddTimingResults(newResults);
            database.SetChipReadStatuses(allChipReads);
            end = DateTime.Now;
            TimeSpan third = end - start;
            Log.D(String.Format("Done. Splitting into bib/chip: {0} - Creating Results: {1} - Putting Results in DB: {2}", first.ToString("c"), second.ToString("c"), third.ToString("c")));
            return newResults;
        }

        private void UpdateAgeGroups(Event theEvent)
        {
            List<Participant> participants = database.GetParticipants(theEvent.Identifier);
            // Dictionary containing Age groups based upon their (Division, Age in Years)
            Dictionary<(int, int), int> divisionAgeGroups = new Dictionary<(int, int), int>();
            // process them into lists based upon divisions (in case there are division specific age groups)
            foreach (AgeGroup group in database.GetAgeGroups(theEvent.Identifier))
            {
                for (int age = group.StartAge; age <= group.EndAge; age++)
                {
                    divisionAgeGroups[(group.DivisionId, age)] = group.GroupId;
                }
            }
            int ageGroupDivisionId = Constants.Timing.COMMON_AGEGROUPS_DIVISIONID;
            foreach (Participant part in participants)
            {
                if (theEvent.CommonAgeGroups != 1)
                {
                    ageGroupDivisionId = part.EventSpecific.DivisionIdentifier;
                }
                int age = part.GetAge(theEvent.Date);
                if (divisionAgeGroups.ContainsKey((ageGroupDivisionId, age)))
                {
                    part.EventSpecific.AgeGroup = divisionAgeGroups[(ageGroupDivisionId, age)];
                }
            }
            database.UpdateParticipants(participants);
            if (ageGroupMutex.WaitOne(3000))
            {
                RecalculateAgeGroupsBool = false;
                ageGroupMutex.ReleaseMutex();
            }
        }

        private List<TimeResult> ProcessPlacementsDistance(Event theEvent)
        {
            List<TimeResult> output = new List<TimeResult>();
            // Get a list of all segments
            List<Segment> segments = database.GetSegments(theEvent.Identifier);
            // process results based upon the segment they're in
            foreach (Segment segment in segments)
            {
                Log.D("Processing segment " + segment.Name);
                List<TimeResult> segmentResults = database.GetSegmentTimes(theEvent.Identifier, segment.Identifier);
                ProcessSegmentPlacementsDistance(theEvent, segmentResults, participantEventSpecificDictionary);
            }
            Log.D("Processing finish results");
            ProcessSegmentPlacementsDistance(theEvent, database.GetSegmentTimes(theEvent.Identifier, Constants.Timing.SEGMENT_FINISH), participantEventSpecificDictionary);
            return output;
        }

        private List<TimeResult> ProcessPlacementsTime(Event theEvent)
        {
            List<TimeResult> output = new List<TimeResult>();
            // Get a list of all segments
            List<Segment> segments = database.GetSegments(theEvent.Identifier);
            foreach (Segment segment in segments)
            {
                Log.D("Processing segment " + segment.Name);
                List<TimeResult> segmentResults = database.GetSegmentTimes(theEvent.Identifier, segment.Identifier);
                output.AddRange(ProcessSegmentPlacementsTime(theEvent, segmentResults, participantEventSpecificDictionary));
            }
            Log.D("Processing finish results");
            output.AddRange(ProcessSegmentPlacementsTime(theEvent, database.GetSegmentTimes(theEvent.Identifier, Constants.Timing.SEGMENT_FINISH), participantEventSpecificDictionary));
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
                if (x1.Occurrence == x2.Occurrence)
                {
                    return x1.SystemTime.CompareTo(x2.SystemTime);
                }
                return x2.Occurrence.CompareTo(x1.Occurrence);
            });
            foreach (TimeResult result in topResults)
            {
                // Make sure we know who we're looking at. Can't rank otherwise.
                if (participantEventSpecificDictionary.ContainsKey(result.EventSpecificId))
                {
                    person = participantEventSpecificDictionary[result.EventSpecificId];
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
                    ageGroupId = person.EventSpecific.AgeGroup;
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
            database.AddTimingResults(segmentResults);
            return segmentResults;
        }

        private List<TimeResult> ProcessSegmentPlacementsDistance(Event theEvent,
            List<TimeResult> segmentResults,
            Dictionary<int, Participant> participantEventSpecificDictionary)
        {
            if (theEvent.RankByGun != 0)
            {
                //segmentResults.Sort(TimeResult.CompareByDivision);
                segmentResults.Sort((x1, x2) =>
                {
                    if (x1 == null || x2 == null) return 1;
                    if (x1.DivisionName.Equals(x2.DivisionName))
                    {
                        if (participantEventSpecificDictionary[x1.EventSpecificId].IsEarlyStart == participantEventSpecificDictionary[x2.EventSpecificId].IsEarlyStart)
                        {
                            return x1.SystemTime.CompareTo(x2.SystemTime);
                        }
                        return participantEventSpecificDictionary[x1.EventSpecificId].IsEarlyStart.CompareTo(participantEventSpecificDictionary[x2.EventSpecificId].IsEarlyStart);
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
                    if (x1.DivisionName.Equals(x2.DivisionName))
                    {
                        if (participantEventSpecificDictionary[x1.EventSpecificId].IsEarlyStart == participantEventSpecificDictionary[x2.EventSpecificId].IsEarlyStart)
                        {
                            return x1.CompareChip(x2);
                        }
                        return participantEventSpecificDictionary[x1.EventSpecificId].IsEarlyStart.CompareTo(participantEventSpecificDictionary[x2.EventSpecificId].IsEarlyStart);
                    }
                    return x1.DivisionName.CompareTo(x2.DivisionName);
                });
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
            foreach (TimeResult result in segmentResults)
            {
                // Check if we know who the person is. Can't rank them if we don't know
                // what division they're in, their age, or their gender
                if (participantEventSpecificDictionary.ContainsKey(result.EventSpecificId))
                {
                    person = participantEventSpecificDictionary[result.EventSpecificId];
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
                    ageGroupId = person.EventSpecific.AgeGroup;
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
            database.AddTimingResults(segmentResults);
            return segmentResults;
        }
    }
}
