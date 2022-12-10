using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Timing.Routines
{
    internal class TimeRoutine
    {
        public static List<TimeResult> ProcessRace(Event theEvent, IDBInterface database, TimingDictionary dictionary)
        {
            Log.D("Timing.TimingWorker", "Processing chip reads for a time based event.");
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
            Dictionary<(string, int), (ChipRead Read, int Occurrence)> chipLastReadDictionary = new Dictionary<(string, int), (ChipRead Read, int Occurrence)>();
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
                Participant part = dictionary.participantBibDictionary.ContainsKey(bib) ?
                    dictionary.participantBibDictionary[bib] :
                    null;
                if (part != null)
                {
                    part.Status = Constants.Timing.EVENTSPECIFIC_STARTED;
                    updateParticipants.Add(part);
                }
                Distance d = part != null ?
                    dictionary.distanceDictionary[part.EventSpecific.DistanceIdentifier] :
                    null;
                long startSeconds, maxStartSeconds, endSeconds;
                int startMilliseconds;
                TimeResult startResult = null;
                if (d == null || !dictionary.distanceStartDict.ContainsKey(d.Identifier) || !dictionary.distanceEndDict.ContainsKey(d.Identifier))
                {
                    (startSeconds, startMilliseconds) = dictionary.distanceStartDict[0];
                    endSeconds = dictionary.distanceEndDict[0].Seconds;
                }
                else
                {
                    (startSeconds, startMilliseconds) = dictionary.distanceStartDict[d.Identifier];
                    endSeconds = dictionary.distanceEndDict[d.Identifier].Seconds;
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
                                Constants.Timing.ToTime(secondsDiff, millisecDiff),
                                TimeResult.BibToIdentifier(bib),
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
                            else if (dictionary.locationDictionary.ContainsKey(read.LocationID))
                            {
                                occursWithin = dictionary.locationDictionary[read.LocationID].IgnoreWithin;
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
                                read.Status = Constants.Timing.CHIPREAD_STATUS_DNF == read.Status ? read.Status : Constants.Timing.CHIPREAD_STATUS_OVERMAX;
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
                                // Check for linked distance and set distanceId to the linked distance, or to the actual distance id.
                                // Segments are based on the linked distance.
                                int distanceId = d == null ? 0 : d.LinkedDistance > 0 ? d.LinkedDistance : d.Identifier;
                                // Check for Distance specific segments (Occurrence is always 1 for time based)
                                if (theEvent.DistanceSpecificSegments && dictionary.segmentDictionary.ContainsKey((Constants.Timing.COMMON_SEGMENTS_DISTANCEID, read.LocationID, 1)))
                                {
                                    segId = dictionary.segmentDictionary[(Constants.Timing.COMMON_SEGMENTS_DISTANCEID, read.LocationID, 1)].Identifier;
                                }
                                // Distance specific segments
                                else if (d != null && dictionary.segmentDictionary.ContainsKey((distanceId, read.LocationID, 1)))
                                {
                                    segId = dictionary.segmentDictionary[(distanceId, read.LocationID, 1)].Identifier;
                                }
                                else if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                                {
                                    segId = Constants.Timing.SEGMENT_FINISH;
                                }
                                string identifier = TimeResult.BibToIdentifier(bib);
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
                                    Constants.Timing.ToTime(secondsDiff, millisecDiff),
                                    identifier,
                                    Constants.Timing.ToTime(chipSecDiff, chipMillisecDiff),
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
                (startSeconds, startMilliseconds) = dictionary.distanceStartDict[0];
                endSeconds = dictionary.distanceEndDict[0].Seconds;
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
                                Constants.Timing.ToTime(secondsDiff, millisecDiff),
                                TimeResult.ChipToIdentifier(chip).ToString(),
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
                            else if (dictionary.locationDictionary.ContainsKey(read.LocationID))
                            {
                                occursWithin = dictionary.locationDictionary[read.LocationID].IgnoreWithin;
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
                                read.Status = Constants.Timing.CHIPREAD_STATUS_DNF == read.Status ? read.Status : Constants.Timing.CHIPREAD_STATUS_OVERMAX;
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
                                // Check for Distance specific segments (Occurrence is always 1 for time based)
                                if (theEvent.DistanceSpecificSegments && dictionary.segmentDictionary.ContainsKey((Constants.Timing.COMMON_SEGMENTS_DISTANCEID, read.LocationID, 1)))
                                {
                                    segId = dictionary.segmentDictionary[(Constants.Timing.COMMON_SEGMENTS_DISTANCEID, read.LocationID, 1)].Identifier;
                                }
                                else if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                                {
                                    segId = Constants.Timing.SEGMENT_FINISH;
                                }
                                string identifier = TimeResult.ChipToIdentifier(chip).ToString();
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
                                    Constants.Timing.ToTime(secondsDiff, millisecDiff),
                                    identifier,
                                    Constants.Timing.ToTime(chipSecDiff, chipMillisecDiff),
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

        // Process lap times.
        public static void ProcessLapTimes(Event theEvent, IDBInterface database)
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
                currentLap.LapTime = Constants.Timing.ToTime((int)sec, mill);
            }
            database.AddTimingResults(laps);
        }

        // Process placements for a time based race.
        public static List<TimeResult> ProcessPlacements(Event theEvent, IDBInterface database, TimingDictionary dictionary)
        {
            List<TimeResult> output = new List<TimeResult>();
            // Create a dictionary so we can check if placements have changed. (place, location, occurrence, distance)
            Dictionary<(int, int, int, string), TimeResult> PlacementDictionary = new Dictionary<(int, int, int, string), TimeResult>();
            // Get a list of all segments
            List<Segment> segments = database.GetSegments(theEvent.Identifier);
            Dictionary<int, List<TimeResult>> segmentDictionary = new Dictionary<int, List<TimeResult>>();
            foreach (TimeResult result in database.GetTimingResults(theEvent.Identifier))
            {
                // We probably have unprocessed results in there, so only worry about results with a place set.
                // Make sure we're checking based on segmentId as well.
                if (result.Place > 0)
                {
                    PlacementDictionary[(result.Place, result.LocationId, result.Occurrence, result.DistanceName)] = result;
                }
                if (!segmentDictionary.ContainsKey(result.SegmentId))
                {
                    segmentDictionary[result.SegmentId] = new List<TimeResult>();
                }
                segmentDictionary[result.SegmentId].Add(result);
            }
            // process results based upon the segment they're in
            foreach (Segment segment in segments)
            {
                Log.D("Timing.TimingWorker", "Processing segment " + segment.Name);
                if (segmentDictionary.ContainsKey(segment.Identifier))
                {
                    output.AddRange(ProcessSegmentPlacements(theEvent, segmentDictionary[segment.Identifier], dictionary));
                }
            }
            Log.D("Timing.TimingWorker", "Processing finish results");
            if (segmentDictionary.ContainsKey(Constants.Timing.SEGMENT_FINISH))
            {
                output.AddRange(ProcessSegmentPlacements(theEvent, segmentDictionary[Constants.Timing.SEGMENT_FINISH], dictionary));
            }
            // Check if we should be re-uploading results because placements have changed.
            List<TimeResult> reUpload = new List<TimeResult>();
            Log.D("Timing.TimingWorker", "Checking for outdated placements.");
            foreach (TimeResult result in output)
            {
                if (PlacementDictionary.ContainsKey((result.Place, result.LocationId, result.Occurrence, result.DistanceName)) && PlacementDictionary[(result.Place, result.LocationId, result.Occurrence, result.DistanceName)].Bib != result.Bib)
                {
                    Log.D("Timing.TimingWorker", String.Format("Oudated placement found. {0} && {1}", result.ParticipantName, PlacementDictionary[(result.Place, result.LocationId, result.Occurrence, result.DistanceName)].ParticipantName));
                    result.Uploaded = Constants.Timing.TIMERESULT_UPLOADED_FALSE;
                    PlacementDictionary[(result.Place, result.LocationId, result.Occurrence, result.DistanceName)].Uploaded = Constants.Timing.TIMERESULT_UPLOADED_FALSE;
                    reUpload.Add(result);
                    reUpload.Add(PlacementDictionary[(result.Place, result.LocationId, result.Occurrence, result.DistanceName)]);
                }
            }
            database.AddTimingResults(output);
            database.SetUploadedTimingResults(reUpload);
            return output;
        }

        // Process segment placement times.
        private static List<TimeResult> ProcessSegmentPlacements(Event theEvent,
            List<TimeResult> segmentResults, TimingDictionary dictionary)
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
            // The key is as follows: (Distance ID, Age Group ID, int - Gender ID (M=1,F=2, U=3, NB=4))
            // The value stored is the last place given
            Dictionary<(int, int, int), int> ageGroupPlaceDictionary = new Dictionary<(int, int, int), int>();
            // The key is as follows: (Distance ID, Gender ID (M=1, F=2, U=3, NB=4))
            // The value stored is the last place given
            Dictionary<(int, int), int> genderPlaceDictionary = new Dictionary<(int, int), int>();
            // The key is as follows: (Distance ID)
            // The value stored is the last place given
            Dictionary<int, int> placeDictionary = new Dictionary<int, int>();
            int ageGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
            int distanceId = -1;
            int age = -1;
            int gender = -1;
            Participant person = null;
            List<TimeResult> topResults = personLastResult.Values.ToList<TimeResult>();
            topResults.Sort((x1, x2) =>
            {
                Distance distance1 = null, distance2 = null;
                int rank1 = 0, rank2 = 0;
                // Get *linked* distances. (Could be that specific distance)
                if (dictionary.linkedDistanceDictionary.ContainsKey(x1.RealDistanceName))
                {
                    (distance1, rank1) = dictionary.linkedDistanceDictionary[x1.RealDistanceName];
                }
                if (dictionary.linkedDistanceDictionary.ContainsKey(x2.RealDistanceName))
                {
                    (distance2, rank2) = dictionary.linkedDistanceDictionary[x2.RealDistanceName];
                }
                Log.D("Timing.Routines.TimeRoutine", string.Format("rank 1 {0} - rank 2 {1}", rank1, rank2));
                if (rank1 == rank2)
                {
                    if (x1.Occurrence == x2.Occurrence)
                    {
                        if (theEvent.RankByGun)
                        {
                            if (x1.Seconds == x2.Seconds)
                            {
                                return x1.Milliseconds.CompareTo(x2.Milliseconds);
                            }
                            Log.D("Timing.Routines.TimeRoutine", "By Gun");
                            return x1.Seconds.CompareTo(x2.Seconds);
                        }
                        else
                        {
                            if (x1.ChipSeconds == x2.ChipSeconds)
                            {
                                return x1.ChipMilliseconds.CompareTo(x2.ChipMilliseconds);
                            }
                            Log.D("Timing.Routines.TimeRoutine", "By Chip");
                            return x1.ChipSeconds.CompareTo(x2.ChipSeconds);
                        }
                    }
                    Log.D("Timing.Routines.TimeRoutine", "By Occurrence");
                    return x2.Occurrence.CompareTo(x1.Occurrence);
                }
                Log.D("Timing.Routines.TimeRoutine", "By Rank");
                return rank1.CompareTo(rank2);
            });
            foreach (TimeResult result in topResults)
            {
                // Make sure we know who we're looking at. Can't rank otherwise.
                if (dictionary.participantEventSpecificDictionary.ContainsKey(result.EventSpecificId))
                {
                    person = dictionary.participantEventSpecificDictionary[result.EventSpecificId];
                    // Use a linked distance ID for ranking instead of a specific distance id.
                    if (dictionary.linkedDistanceIdentifierDictionary.ContainsKey(person.EventSpecific.DistanceIdentifier))
                    {
                        distanceId = dictionary.linkedDistanceIdentifierDictionary[person.EventSpecific.DistanceIdentifier];
                    }
                    else
                    {
                        distanceId = person.EventSpecific.DistanceIdentifier;
                    }
                    distanceId = person.EventSpecific.DistanceIdentifier;
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
                    else if (person.Gender.Equals("NB", StringComparison.OrdinalIgnoreCase)
                        || person.Gender.Equals("Non-Binary", StringComparison.OrdinalIgnoreCase))
                    {
                        gender = Constants.Timing.TIMERESULT_GENDER_NON_BINARY;
                    }
                    ageGroupId = person.EventSpecific.AgeGroupId;
                    // Since Results were sorted before we started, let's assume that the first item
                    // is the fastest/best and if we can't find the key, add one starting at 0
                    if (!placeDictionary.ContainsKey(distanceId))
                    {
                        placeDictionary[distanceId] = 0;
                    }
                    result.Place = ++placeDictionary[distanceId];
                    if (!genderPlaceDictionary.ContainsKey((distanceId, gender)))
                    {
                        genderPlaceDictionary[(distanceId, gender)] = 0;
                    }
                    result.GenderPlace = ++genderPlaceDictionary[(distanceId, gender)];
                    if (!ageGroupPlaceDictionary.ContainsKey((distanceId, ageGroupId, gender)))
                    {
                        ageGroupPlaceDictionary[(distanceId, ageGroupId, gender)] = 0;
                    }
                    result.AgePlace = ++ageGroupPlaceDictionary[(distanceId, ageGroupId, gender)];
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
    }
}
