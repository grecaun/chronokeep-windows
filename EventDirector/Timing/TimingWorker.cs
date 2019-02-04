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
        private static bool QuittingTime = false;
        private static bool RecalculateAgeGroupsBool = true;

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
            semaphore.Release();
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
                if (theEvent != null && theEvent.Identifier != -1)
                {
                    if (database.UnprocessedReadsExist(theEvent.Identifier))
                    {
                        // If RACETYPE is DISTANCE
                        ProcessDistanceBasedRace(theEvent);
                        // Else RACETYPE is TIME
                        // ProcessTimeBasedRace(theEvent);
                    }
                    if (database.UnprocessedResultsExist(theEvent.Identifier))
                    {
                        if (ageGroupMutex.WaitOne(3000))
                        {
                            if (RecalculateAgeGroupsBool)
                            {
                                Log.D("Updating Age Groups.");
                                ageGroupMutex.ReleaseMutex();
                                UpdateAgeGroups(theEvent);
                            }
                            else
                            {
                                ageGroupMutex.ReleaseMutex();
                            }
                        }
                        // If RACETYPE if DISTANCE
                        ProcessPlacementsDistance(theEvent);
                        // Else RACETYPE is TIME
                        // ProcessPlacementsTime(theEvent);
                    }
                    window.NonUIUpdate();
                }
            } while (true);
        }

        private void ProcessDistanceBasedRace(Event theEvent)
        {
            // Check if there's anything to process.
            // Pre-process information we'll need to fully process chip reads
            // Locations for checking if we're past the maximum number of occurrences
            // Stored in a dictionary based upon the location ID for easier access.
            Dictionary<int, TimingLocation> locationDictionary = new Dictionary<int, TimingLocation>();
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
            Dictionary<(int, int, int), Segment> segmentDictionary = new Dictionary<(int, int, int), Segment>();
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                if (segmentDictionary.ContainsKey((seg.DivisionId, seg.LocationId, seg.Occurrence)))
                {
                    Log.E("Multiples of a segment found in segment set.");
                }
                segmentDictionary[(seg.DivisionId, seg.LocationId, seg.Occurrence)] = seg;
            }
            // Participants so we can check their Division.
            Dictionary<int, Participant> participantDictionary = new Dictionary<int, Participant>();
            foreach (Participant part in database.GetParticipants(theEvent.Identifier))
            {
                if (participantDictionary.ContainsKey(part.Bib))
                {
                    Log.E("Multiples of a Bib found in participants set. " + part.Bib);
                }
                participantDictionary[part.Bib] = part;
            }
            // Get the start time for the event. (Net time of 0:00:00.000)
            Dictionary<int, DateTime> divisionStartDict = new Dictionary<int, DateTime>
            {
                [0] = DateTime.Parse(theEvent.Date)
                                            .AddSeconds(theEvent.StartSeconds)
                                            .AddMilliseconds(theEvent.StartMilliseconds)
            };
            // Divisions so we can get their start offset.
            Dictionary<int, Division> divisionDictionary = new Dictionary<int, Division>();
            foreach (Division div in database.GetDivisions(theEvent.Identifier))
            {
                if (divisionDictionary.ContainsKey(div.Identifier))
                {
                    Log.E("Multiples of a Division found in divisions set.");
                }
                divisionDictionary[div.Identifier] = div;
                Log.D("Division " + div.Name + " offsets are " + div.StartOffsetSeconds + " " + div.StartOffsetMilliseconds);
                divisionStartDict[div.Identifier] = divisionStartDict[0].AddSeconds(div.StartOffsetSeconds).AddMilliseconds(div.StartOffsetMilliseconds);
            }
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
            List<ChipRead> allChipReads = database.GetUsefulChipReads(theEvent.Identifier);
            List<ChipRead> setUnknown = new List<ChipRead>();
            foreach (ChipRead read in allChipReads)
            {
                if (read.ChipBib != Constants.Timing.CHIPREAD_DUMMYBIB)
                {
                    if (!bibReadPairs.ContainsKey(read.ChipBib))
                    {
                        bibReadPairs[read.ChipBib] = new List<ChipRead>();
                    }
                    bibReadPairs[read.ChipBib].Add(read);
                }
                else if (read.ReadBib != Constants.Timing.CHIPREAD_DUMMYBIB)
                {
                    if (!bibReadPairs.ContainsKey(read.ReadBib))
                    {
                        bibReadPairs[read.ReadBib] = new List<ChipRead>();
                    }
                    bibReadPairs[read.ReadBib].Add(read);
                }
                else if (read.ChipNumber != Constants.Timing.CHIPREAD_DUMMYCHIP)
                {
                    if (!chipReadPairs.ContainsKey(read.ChipNumber.ToString()))
                    {
                        chipReadPairs[read.ChipNumber.ToString()] = new List<ChipRead>();
                    }
                    chipReadPairs[read.ChipNumber.ToString()].Add(read);
                }
                else
                {
                    setUnknown.Add(read);
                }
            }
            // Go through each chip read for a single person. Make sure we keep track of the
            // last occurrence for a person at a specific location.
            // (Bib, Location), Last Chip Read
            Dictionary<(int, int), (ChipRead Read, int Occurrence)> lastReadDictionary = new Dictionary<(int, int), (ChipRead, int)>();
            //List<ChipRead> updateStatusReads = new List<ChipRead>();
            List<TimeResult> newResults = new List<TimeResult>();
            // process reads that have a bib
            foreach (int bib in bibReadPairs.Keys)
            {
                bibReadPairs[bib].Sort();
                Participant part = participantDictionary.ContainsKey(bib) ?
                    participantDictionary[bib] :
                    null;
                Division div = participantDictionary.ContainsKey(bib) ?
                    divisionDictionary[participantDictionary[bib].EventSpecific.DivisionIdentifier] :
                    null;
                DateTime start, maxStart;
                TimeResult startResult = null;
                if (div == null || !divisionStartDict.ContainsKey(div.Identifier))
                {
                    start = divisionStartDict[0];
                }
                else
                {
                    start = divisionStartDict[div.Identifier];
                }
                maxStart = start.AddSeconds(theEvent.StartWindow);
                foreach (ChipRead read in bibReadPairs[bib])
                {
                    TimeSpan diff = read.Time - start;
                    // Check that we haven't processed the read yet
                    if (Constants.Timing.CHIPREAD_STATUS_NONE == read.Status)
                    {
                        Log.D("Processing new chipread.");
                        // Check if we're before the start time.
                        if (read.Time < start)
                        {
                            read.Status = Constants.Timing.CHIPREAD_STATUS_PRESTART;
                        }
                        else
                        {
                            // If we're within the start period
                            // And the location is the Start, or we've got a combined start finish location
                            if (read.Time <= maxStart &&
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
                                TimeSpan netTime = read.Time - start;
                                startResult = new TimeResult(theEvent.Identifier,
                                    read.ReadId,
                                    part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                                    read.LocationID,
                                    Constants.Timing.SEGMENT_START,
                                    0, // start reads are not an occurrence at the start line
                                    String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", netTime.Days * 24 + netTime.Hours, netTime.Minutes, netTime.Seconds, netTime.Milliseconds),
                                    part == null ? "Bib:" + bib.ToString() : "",
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
                                DateTime minTime = start.AddSeconds(occursWithin);
                                // Check if there's a previous read at this location.
                                if (lastReadDictionary.ContainsKey((bib, read.LocationID)))
                                {
                                    occurrence = lastReadDictionary[(bib, read.LocationID)].Occurrence + 1;
                                    minTime = lastReadDictionary[(bib, read.LocationID)].Read.Time.AddSeconds(occursWithin);
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
                                else if (read.Time < minTime)
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
                                    string identifier = part == null ? "Bib:" + bib.ToString() : bib.ToString();
                                    // Create a result for the start value.
                                    TimeSpan netTime = read.Time - start;
                                    TimeSpan chipTime = read.Time - (startTimes.ContainsKey(identifier) ? startTimes[identifier].SystemTime : start);
                                    newResults.Add(new TimeResult(theEvent.Identifier,
                                        read.ReadId,
                                        part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                                        read.LocationID,
                                        segId,
                                        occurrence,
                                        String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", netTime.Days * 24 + netTime.Hours, netTime.Minutes, netTime.Seconds, netTime.Milliseconds),
                                        part == null ? "Bib:" + bib.ToString() : "",
                                        String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", chipTime.Days * 24 + chipTime.Hours, chipTime.Minutes, chipTime.Seconds, chipTime.Milliseconds),
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
                    else // read was already processed
                    {
                        Log.D("Read already processed.");
                        if (!lastReadDictionary.ContainsKey((bib, read.LocationID)))
                        {
                            lastReadDictionary[(bib, read.LocationID)] = (read, 1);
                        }
                        else
                        {
                            lastReadDictionary[(bib, read.LocationID)] = (read, lastReadDictionary[(bib, read.LocationID)].Occurrence + 1);
                        }
                    }
                }
            }
            // process reads that have a chip
            Dictionary<(string, int), (ChipRead Read, int Occurrence)> chipLastReadDictionary = new Dictionary<(string, int), (ChipRead Read, int Occurrence)>();
            Dictionary<string, ChipRead> chipStartReadDictionary = new Dictionary<string, ChipRead>();
            foreach (string chip in chipReadPairs.Keys)
            {
                chipReadPairs[chip].Sort();
                DateTime start, maxStart;
                start = divisionStartDict[0];
                maxStart = start.AddSeconds(theEvent.StartWindow);
                TimeResult startResult = null;
                foreach (ChipRead read in chipReadPairs[chip])
                {
                    TimeSpan diff = read.Time - start;
                    // Check that we haven't processed the read yet
                    if (Constants.Timing.CHIPREAD_STATUS_NONE == read.Status)
                    {
                        Log.D("Processing new chipread.");
                        // Check if we're before the start time.
                        if (read.Time < start)
                        {
                            read.Status = Constants.Timing.CHIPREAD_STATUS_PRESTART;
                        }
                        else
                        {
                            // If we're within the start period
                            // And the location is the Start, or we've got a combined start finish location
                            if (read.Time <= maxStart &&
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
                                string unknownId = "Chip:" + chip.ToString();
                                // Create a result for the start value.
                                TimeSpan netTime = read.Time - start;
                                startResult = new TimeResult(theEvent.Identifier,
                                    read.ReadId,
                                    Constants.Timing.TIMERESULT_DUMMYPERSON,
                                    read.LocationID,
                                    Constants.Timing.SEGMENT_START,
                                    0, // start reads are not an occurrence at the start line
                                    String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", netTime.Days * 24 + netTime.Hours, netTime.Minutes, netTime.Seconds, netTime.Milliseconds),
                                    unknownId,
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
                                DateTime minTime = start.AddSeconds(occursWithin);
                                // Check if there's a previous read at this location.
                                if (chipLastReadDictionary.ContainsKey((chip, read.LocationID)))
                                {
                                    occurrence = chipLastReadDictionary[(chip, read.LocationID)].Occurrence + 1;
                                    minTime = chipLastReadDictionary[(chip, read.LocationID)].Read.Time.AddSeconds(occursWithin);
                                }
                                // Check if we're past the max occurances allowed for this spot.
                                if (occurrence > maxOccurrences)
                                {
                                    read.Status = Constants.Timing.CHIPREAD_STATUS_OVERMAX;
                                }
                                // occurrence is in [1,maxOccurrences], but can't be used because it's in the
                                // ignore period
                                else if (read.Time < minTime)
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
                                    string unknownId = "Chip:" + chip.ToString();
                                    // Create a result for the start value.
                                    TimeSpan netTime = read.Time - start;
                                    TimeSpan chipTime = read.Time - (startTimes.ContainsKey(unknownId) ? startTimes[unknownId].SystemTime : start);
                                    newResults.Add(new TimeResult(theEvent.Identifier,
                                        read.ReadId,
                                        Constants.Timing.TIMERESULT_DUMMYPERSON,
                                        read.LocationID,
                                        segId,
                                        occurrence,
                                        String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", netTime.Days * 24 + netTime.Hours, netTime.Minutes, netTime.Seconds, netTime.Milliseconds),
                                        unknownId,
                                        String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", chipTime.Days * 24 + chipTime.Hours, chipTime.Minutes, chipTime.Seconds, chipTime.Milliseconds),
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
                    else // read was already processed
                    {
                        Log.D("Read already processed.");
                        if (!chipLastReadDictionary.ContainsKey((chip, read.LocationID)))
                        {
                            chipLastReadDictionary[(chip, read.LocationID)] = (read, 1);
                        }
                        else
                        {
                            chipLastReadDictionary[(chip, read.LocationID)] = (read, chipLastReadDictionary[(chip, read.LocationID)].Occurrence + 1);
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
            foreach (int BibKey in bibReadPairs.Keys)
            {
                database.SetChipReadStatuses(bibReadPairs[BibKey]);
            }
            foreach (string ChipKey in chipReadPairs.Keys)
            {
                database.SetChipReadStatuses(chipReadPairs[ChipKey]);
            }
            foreach (ChipRead read in setUnknown)
            {
                database.SetChipReadStatuses(setUnknown);
            }
        }

        private void ProcessTimeBasedRace(Event theEvent)
        {

        }

        private void UpdateAgeGroups(Event theEvent)
        {
            List<Participant> participants = database.GetParticipants(theEvent.Identifier);
            // Dictionary containing Age groups based upon their (Division, Age in Years)
            Dictionary<(int, int), int> divisionAgeGroups = new Dictionary<(int, int), int>();
            // process them into lists based upon divisions (in case there are division specific age groups)
            foreach (AgeGroup group in database.GetAgeGroups(theEvent.Identifier))
            {
                Log.D(String.Format("Age group {0} - Div {3} - {1} - {2}", group.GroupId, group.StartAge, group.EndAge, group.DivisionId));
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

        private void ProcessPlacementsDistance(Event theEvent)
        {
            // Get participants
            Dictionary<int, Participant> participantDictionary = new Dictionary<int, Participant>();
            List<Participant> participants = database.GetParticipants(theEvent.Identifier);
            foreach (Participant person in participants)
            {
                participantDictionary[person.EventSpecific.Identifier] = person;
            }
            // Dictionary containing Age groups based upon their (Division, Age in Years)
            Dictionary<(int, int), int> divisionAgeGroups = new Dictionary<(int, int), int>();
            // process them into lists based upon divisions (in case there are division specific age groups)
            foreach (AgeGroup group in database.GetAgeGroups(theEvent.Identifier))
            {
                Log.D(String.Format("Age group {0} - Div {3} - {1} - {2}", group.GroupId, group.StartAge, group.EndAge, group.DivisionId));
                for (int age = group.StartAge; age <= group.EndAge; age++)
                {
                    divisionAgeGroups[(group.DivisionId, age)] = group.GroupId;
                }
            }
            // Get a list of all segments
            List<Segment> segments = database.GetSegments(theEvent.Identifier);
            // process results based upon the segment they're in
            foreach (Segment segment in segments)
            {
                List<TimeResult> segmentResults = database.GetSegmentTimes(theEvent.Identifier, segment.Identifier);
                ProcessSegmentPlacements(theEvent, segmentResults, participantDictionary);
            }
            ProcessSegmentPlacements(theEvent, database.GetSegmentTimes(theEvent.Identifier, Constants.Timing.SEGMENT_FINISH), participantDictionary);
        }

        private void ProcessSegmentPlacements(Event theEvent,
            List<TimeResult> segmentResults,
            Dictionary<int, Participant> participantDictionary)
        {
            if (theEvent.RankByGun != 0)
            {
                segmentResults.Sort(TimeResult.CompareByDivision);
            }
            else
            {
                segmentResults.Sort(TimeResult.CompareByDivisionChip);
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
                if (participantDictionary.ContainsKey(result.EventSpecificId))
                {
                    person = participantDictionary[result.EventSpecificId];
                    // DivisionID is the person's actual DivisionId, whereas ageGroupDivisionID might
                    // be the dummy divisionID because of common age groups
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
                    result.Status = Constants.Timing.CHIPREAD_STATUS_USED;
                }
            }
            database.AddTimingResults(segmentResults);
        }
    }
}
