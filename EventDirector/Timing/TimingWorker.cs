using EventDirector.Interfaces;
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
        private static bool QuittingTime = false;

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
                if (theEvent != null && theEvent.Identifier != -1)
                {
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
                    Dictionary<int, DateTime> waveStartTimesDict = new Dictionary<int, DateTime>();
                    Dictionary<int, DateTime> maxStartTimesDict = new Dictionary<int, DateTime>();
                    waveStartTimesDict[0] = DateTime.Parse(theEvent.Date)
                                                    .AddSeconds(theEvent.StartSeconds)
                                                    .AddMilliseconds(theEvent.StartMilliseconds);
                    maxStartTimesDict[0] = waveStartTimesDict[0].AddSeconds(theEvent.StartWindow);
                    // Divisions so we can get their start offset.
                    Dictionary<int, Division> divisionDictionary = new Dictionary<int, Division>();
                    foreach (Division div in database.GetDivisions(theEvent.Identifier))
                    {
                        if (divisionDictionary.ContainsKey(div.Identifier))
                        {
                            Log.E("Multiples of a Division found in divisions set.");
                        }
                        divisionDictionary[div.Identifier] = div;
                        // add wave to the list if it isn't there
                        if (!waveStartTimesDict.ContainsKey(div.Wave))
                        {
                            waveStartTimesDict[div.Wave] = waveStartTimesDict[0]
                                .AddSeconds(div.StartOffsetSeconds)
                                .AddMilliseconds(div.StartOffsetMilliseconds);
                            maxStartTimesDict[div.Wave] = waveStartTimesDict[div.Wave]
                                .AddSeconds(theEvent.StartWindow);
                        }
                    }
                    // Get all of the Chip Reads we find useful (Unprocessed, and those used as a result.)
                    // and then sort them into groups based upon Bib, Chip, or put them in the ignore pile if
                    // they have no bib or chip.
                    string startTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff   ");
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
                    List<ChipRead> updateStatusReads = new List<ChipRead>();
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
                        if (div == null || !waveStartTimesDict.ContainsKey(div.Wave))
                        {
                            start = waveStartTimesDict[0];
                            maxStart = maxStartTimesDict[0];
                        }
                        else
                        {
                            start = waveStartTimesDict[div.Wave];
                            maxStart = maxStartTimesDict[div.Wave];
                        }
                        foreach (ChipRead read in bibReadPairs[bib])
                        {
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
                                        if (startResult != null)
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
                                            1,  // occurrence, start time has a single possible start occurrence
                                            String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", netTime.Days * 24 + netTime.Hours, netTime.Minutes, netTime.Seconds, netTime.Milliseconds),
                                            part == null ? "Bib:" + bib.ToString() : "");
                                        newResults.Add(startResult);
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
                                        if (lastReadDictionary.ContainsKey((bib, read.LocationID)))
                                        {
                                            occurrence = lastReadDictionary[(bib, read.LocationID)].Occurrence + 1;
                                            minTime = lastReadDictionary[(bib, read.LocationID)].Read.Time.AddSeconds(occursWithin);
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
                                            // Create a result for the start value.
                                            TimeSpan netTime = read.Time - start;
                                            startResult = new TimeResult(theEvent.Identifier,
                                                read.ReadId,
                                                part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                                                read.LocationID,
                                                segId,
                                                occurrence,
                                                String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", netTime.Days * 24 + netTime.Hours, netTime.Minutes, netTime.Seconds, netTime.Milliseconds),
                                                part == null ? "Bib:" + bib.ToString() : "");
                                            newResults.Add(startResult);
                                            read.Status = Constants.Timing.CHIPREAD_STATUS_USED;
                                        }
                                    }
                                    // Possible reads at this point:
                                    //      Start Location reads past the StartWindow (Set status to ignore)
                                    else
                                    {
                                        read.Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                                    }
                                    updateStatusReads.Add(read);
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
                        start = waveStartTimesDict[0];
                        maxStart = maxStartTimesDict[0];
                        TimeResult startResult = null;
                        foreach (ChipRead read in chipReadPairs[chip])
                        {
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
                                        // Create a result for the start value.
                                        TimeSpan netTime = read.Time - start;
                                        startResult = new TimeResult(theEvent.Identifier,
                                            read.ReadId,
                                            Constants.Timing.TIMERESULT_DUMMYPERSON,
                                            read.LocationID,
                                            Constants.Timing.SEGMENT_START,
                                            1,  // occurrence, start time has a single possible start occurrence
                                            String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", netTime.Days * 24 + netTime.Hours, netTime.Minutes, netTime.Seconds, netTime.Milliseconds),
                                            "Chip:" + chip.ToString());
                                        newResults.Add(startResult);
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
                                            // Create a result for the start value.
                                            TimeSpan netTime = read.Time - start;
                                            startResult = new TimeResult(theEvent.Identifier,
                                                read.ReadId,
                                                Constants.Timing.TIMERESULT_DUMMYPERSON,
                                                read.LocationID,
                                                segId,
                                                occurrence,
                                                String.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", netTime.Days * 24 + netTime.Hours, netTime.Minutes, netTime.Seconds, netTime.Milliseconds),
                                                "Chip:" + chip.ToString());
                                            newResults.Add(startResult);
                                            read.Status = Constants.Timing.CHIPREAD_STATUS_USED;
                                        }
                                    }
                                    // Possible reads at this point:
                                    //      Start Location reads past the StartWindow (Set status to ignore)
                                    else
                                    {
                                        read.Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                                    }
                                    updateStatusReads.Add(read);
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
                        updateStatusReads.Add(read);
                    }
                    // Update database with information.
                    database.SetChipReadStatuses(updateStatusReads);
                    database.AddTimingResults(newResults);
                    // Write to log test information.
                    string endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string outfilepath = System.IO.Path.Combine(database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value, "TimingWorkerTestFile.txt");
                    List<string> messages = new List<string>
                    {
                        String.Format("{0} - {1} : {2}", startTime, endTime, allChipReads.Count)
                    };
                    foreach (int BibKey in bibReadPairs.Keys)
                    {
                        foreach (ChipRead read in bibReadPairs[BibKey])
                        {
                            messages.Add(String.Format(" {4,5} {0,10} - {3,30} - Time {1,25} - Status {2,10}", BibKey, read.Time.ToString("yyyy-MM-dd HH:mm:ss.fff"), read.StatusName, read.Name, "Bib"));
                        }
                    }
                    foreach (string ChipKey in chipReadPairs.Keys)
                    {
                        foreach (ChipRead read in chipReadPairs[ChipKey])
                        {
                            messages.Add(String.Format(" {4,5} {0,10} - {3,30} - Time {1,25} - Status {2,10}", ChipKey, read.Time.ToString("yyyy-MM-dd HH:mm:ss.fff"), read.StatusName, read.Name, "Chip"));
                        }
                    }
                    foreach (ChipRead read in setUnknown)
                    {
                        messages.Add(String.Format(" {4,5} {0,10} - {3,30} - Time {1,25} - Status {2,10}", "", read.Time.ToString("yyyy-MM-dd HH:mm:ss.fff"), read.StatusName, read.Name, "Bib"));
                    }
                    Log.WriteFile(outfilepath, messages.ToArray()); //*/
                    ((INewMainWindow)window).UpdateTimingWindow();
                }
            } while (true);
        }
    }
}
