using Chronokeep.Interfaces;
using Chronokeep.Objects;
using System;
using System.Collections.Generic;

namespace Chronokeep.Timing.Routines
{
    internal class DistanceRoutine
    {
        public static List<TimeResult> ProcessRace(Event theEvent, IDBInterface database, TimingDictionary dictionary, IMainWindow window)
        {
            Log.D("Timing.Routines.DistanceRoutine", "Processing chip reads for a distance based event.");
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
            // Get the last known time we've seen each participant
            Dictionary<int, TimeResult> LastSeen = new Dictionary<int, TimeResult>();
            foreach (TimeResult result in database.GetTimingResults(theEvent.Identifier))
            {
                // if there is no time result
                // or we've seen the person but our time is BEFORE the one we're looking at
                // set the last seen as this result
                if (!LastSeen.ContainsKey(result.EventSpecificId)
                    || (LastSeen.ContainsKey(result.EventSpecificId)
                        && (LastSeen[result.EventSpecificId].Seconds < result.Seconds || (LastSeen[result.EventSpecificId].Seconds == result.Seconds && LastSeen[result.EventSpecificId].Milliseconds < result.Milliseconds))))
                {
                    LastSeen[result.EventSpecificId] = result;
                }
            }
            // Get all of the Chip Reads we find useful (Unprocessed, and those used as a result.)
            // and then sort them into groups based upon Bib, Chip, or put them in the ignore pile if
            // they have no bib or chip.
            Dictionary<string, List<ChipRead>> bibReadPairs = new Dictionary<string, List<ChipRead>>();
            Dictionary<string, List<ChipRead>> chipReadPairs = new Dictionary<string, List<ChipRead>>();
            // Make sure we keep track of the
            // last occurrence for a person at a specific location.
            // (Bib, Location), Last Chip Read
            Dictionary<(string, int), (ChipRead Read, int Occurrence)> bibLastReadDictionary = new Dictionary<(string, int), (ChipRead, int)>();
            Dictionary<(string, int), (ChipRead Read, int Occurrence)> chipLastReadDictionary = new Dictionary<(string, int), (ChipRead Read, int Occurrence)>();
            // Keep a list of DNF participants so we can mark them as DNF in results.
            // Keep a record of the DNF chipread so we can link it with the TimeResult
            Dictionary<string, ChipRead> bibDNFDictionary = new Dictionary<string, ChipRead>();
            Dictionary<string, ChipRead> chipDNFDictionary = new Dictionary<string, ChipRead>();
            // Keep a list of DNS participants so we can mark them as DNS in results.
            // Keep a record of the DNS chipread so we can link it with the TimeResult
            Dictionary<string, ChipRead> bibDNSDictionary = new Dictionary<string, ChipRead>();
            Dictionary<string, ChipRead> chipDNSDictionary = new Dictionary<string, ChipRead>();

            List<ChipRead> allChipReads = database.GetUsefulChipReads(theEvent.Identifier);
            allChipReads.Sort();
            List<ChipRead> setUnknown = [];

            // Create a dictionary for keeping track of all of our chipreads.
            Dictionary<int, ChipRead> chipReadDict = [];

            // Get some variables to check if we need to sound an alarm.
            // Get a time value to check to ensure the chip read isn't too far in the past.
            DateTime before = DateTime.Now.AddMinutes(-5);
            (Dictionary<string, Alarm> bibAlarms, Dictionary<string, Alarm> chipAlarms) = Alarm.GetAlarmDictionaries();

            foreach (ChipRead read in allChipReads)
            {
                chipReadDict[read.ReadId] = read;
                // Check to set off an alarm.
                if (read.Time > before)
                {
                    // Bib set on the read, alarm exists and it hasn't went off.
                    if (read.Bib != null && read.Bib.Length > 0 && read.Bib != Constants.Timing.CHIPREAD_DUMMYBIB
                        && bibAlarms.TryGetValue(read.Bib, out Alarm alarm1) && alarm1.Enabled)
                    {
                        window.NotifyAlarm(read.Bib, "");
                    }
                    // Bib not set, chip is set, alarm exists and it hasn't went off.
                    else if (read.ChipNumber != Constants.Timing.CHIPREAD_DUMMYCHIP
                        && chipAlarms.TryGetValue(read.ChipNumber, out Alarm alarm2) && alarm2.Enabled)
                    {
                        window.NotifyAlarm("", read.ChipNumber);
                    }
                }
                if (read.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
                {
                    // Start by checking if we've got a record of the person not starting.
                    // If they are, we set them to AFTER_DNS.
                    // This status can be ignored later and won't be changed to DNS_IGNORE
                    // which would keep it as a DNS entry forever.
                    if (dictionary.dnsBibs.Contains(read.Bib))
                    {
                        if (read.Status != Constants.Timing.CHIPREAD_STATUS_DNS)
                        {
                            read.Status = Constants.Timing.CHIPREAD_STATUS_AFTER_DNS;
                        }
                        else if (!bibDNSDictionary.ContainsKey(read.Bib))
                        {
                            bibDNSDictionary.Add(read.Bib, read);
                        }
                    }
                    // if we process all the used reads before putting them in the list
                    // we can ensure that all of the reads we process are STATUS_NONE
                    // and then we can verify that we aren't inserting results BEFORE
                    // results we've already calculated
                    else if (Constants.Timing.CHIPREAD_STATUS_USED == read.Status)
                    {
                        if (!bibLastReadDictionary.ContainsKey((read.Bib, read.LocationID)))
                        {
                            bibLastReadDictionary[(read.Bib, read.LocationID)] = (read, 1);
                        }
                        else
                        {
                            bibLastReadDictionary[(read.Bib, read.LocationID)] = (read, bibLastReadDictionary[(read.Bib, read.LocationID)].Occurrence + 1);
                        }
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_STARTTIME == read.Status &&
                        (Constants.Timing.LOCATION_START == read.LocationID ||
                        (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish)))
                    {
                        // If we haven't found anything, let us know what our start time was
                        if (!bibLastReadDictionary.ContainsKey((read.Bib, read.LocationID)))
                        {
                            bibLastReadDictionary[(read.Bib, read.LocationID)] = (read, 0);
                        }
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_DNF == read.Status)
                    {
                        bibDNFDictionary[read.Bib] = read;
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
                    // Start by checking if we've got a record of the person not starting.
                    // If they are, we set them to AFTER_DNS.
                    // This status can be ignored later and won't be changed to DNS_IGNORE
                    // which would keep it as a DNS entry forever.
                    if (dictionary.dnsChips.Contains(read.ChipNumber))
                    {
                        if (read.Status != Constants.Timing.CHIPREAD_STATUS_DNS)
                        {
                            read.Status = Constants.Timing.CHIPREAD_STATUS_AFTER_DNS;
                        }
                        else if (!chipDNSDictionary.ContainsKey(read.ChipNumber))
                        {
                            chipDNSDictionary.Add(read.ChipNumber, read);
                        }
                    }
                    // Otherwise check the status and everything as we did for Bib reads.
                    else if (Constants.Timing.CHIPREAD_STATUS_USED == read.Status)
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
                        chipDNFDictionary[read.ChipNumber] = read;
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_DNS == read.Status)
                    {
                        dictionary.dnsChips.Add(read.ChipNumber);
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
            List<TimeResult> newResults = new List<TimeResult>();
            // Keep a list of participants to update.
            HashSet<Participant> updateParticipants = new HashSet<Participant>();
            // process reads that have a bib
            foreach (string bib in bibReadPairs.Keys)
            {
                Participant part = dictionary.participantBibDictionary.ContainsKey(bib) ?
                    dictionary.participantBibDictionary[bib] :
                    null;
                Distance d = part != null ?
                    dictionary.distanceDictionary[part.EventSpecific.DistanceIdentifier] :
                    null;
                long startSeconds, maxStartSeconds;
                int startMilliseconds;
                TimeResult startResult = null;
                if (startTimes.ContainsKey(TimeResult.BibToIdentifier(bib)))
                {
                    startResult = startTimes[TimeResult.BibToIdentifier(bib)];
                }
                if (d == null || !dictionary.distanceStartDict.ContainsKey(d.Identifier))
                {
                    startSeconds = dictionary.distanceStartDict[0].Seconds;
                    startMilliseconds = dictionary.distanceStartDict[0].Milliseconds;
                }
                else
                {
                    startSeconds = dictionary.distanceStartDict[d.Identifier].Seconds;
                    startMilliseconds = dictionary.distanceStartDict[d.Identifier].Milliseconds;
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
                            if ((read.TimeSeconds < maxStartSeconds || (read.TimeSeconds == maxStartSeconds && read.TimeMilliseconds <= startMilliseconds))
                                && (Constants.Timing.LOCATION_START == read.LocationID || (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish)))
                            {
                                // check if we've stored a chipread as the start chipread, update it to unused if so
                                if (bibLastReadDictionary.ContainsKey((bib, read.LocationID)))
                                {
                                    bibLastReadDictionary[(bib, read.LocationID)].Read.Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                                }
                                // Update the last read we've seen at this location
                                bibLastReadDictionary[(bib, read.LocationID)] = (Read: read, Occurrence: 0);
                                // Check if we previously had a TimeResult for the start.
                                if (startResult != null && newResults.Contains(startResult))
                                {
                                    // Remove it if so.
                                    newResults.Remove(startResult);
                                }
                                // Create a result for the start value.
                                long secondsDiff = read.TimeSeconds - startSeconds;
                                int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                                // If the distance is linked as a late distance, use the linked distance's start time as the gun time.
                                if (d != null
                                    && d.Type == Constants.Timing.DISTANCE_TYPE_LATE
                                    && d.LinkedDistance != Constants.Timing.DISTANCE_DUMMYIDENTIFIER
                                    && dictionary.distanceStartDict.TryGetValue(d.LinkedDistance, out (long, int) tmp))
                                {
                                    secondsDiff = read.TimeSeconds - tmp.Item1;
                                    millisecDiff = read.TimeMilliseconds - tmp.Item2;
                                }
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
                                    secondsDiff,
                                    millisecDiff,
                                    TimeResult.BibToIdentifier(bib),
                                    0,
                                    0,
                                    read.Time,
                                    bib,
                                    Constants.Timing.TIMERESULT_STATUS_NONE,
                                    part == null ? "" : part.EventSpecific.Division
                                    );
                                startTimes[startResult.Identifier] = startResult;
                                newResults.Add(startResult);
                                if (part != null &&
                                    (Constants.Timing.EVENTSPECIFIC_UNKNOWN == part.Status
                                    && !bibDNFDictionary.ContainsKey(bib)))
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
                                    if (!dictionary.locationDictionary.ContainsKey(read.LocationID))
                                    {
                                        Log.E("Timing.Routines.DistanceRoutine", "Somehow the location was not found.");
                                    }
                                    else
                                    {
                                        maxOccurrences = dictionary.locationDictionary[read.LocationID].MaxOccurrences;
                                    }
                                }
                                int occurrence = 1;
                                int occursWithin = 0;
                                if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                                {
                                    occursWithin = theEvent.FinishIgnoreWithin;
                                }
                                else if (dictionary.locationDictionary.TryGetValue(read.LocationID, out TimingLocation loc))
                                {
                                    occursWithin = loc.IgnoreWithin;
                                }
                                // Minimum Time Value required to actually create a result
                                long minSeconds = startSeconds;
                                int minMilliseconds = startMilliseconds;
                                // Check if there's a previous read at this location.
                                if (bibLastReadDictionary.ContainsKey((bib, read.LocationID)))
                                {
                                    occurrence = bibLastReadDictionary[(bib, read.LocationID)].Occurrence + 1;
                                    minSeconds = bibLastReadDictionary[(bib, read.LocationID)].Read.TimeSeconds + occursWithin;
                                    minMilliseconds = bibLastReadDictionary[(bib, read.LocationID)].Read.TimeMilliseconds;
                                }
                                // Verify we know which occurrence we're supposed to be at
                                if (part != null && d != null)
                                {
                                    // The distanceId is either the participant's current distance
                                    int distanceId = d.Identifier;
                                    // the common distance ID
                                    if (theEvent.DistanceSpecificSegments == false)
                                    {
                                        distanceId = Constants.Timing.COMMON_SEGMENTS_DISTANCEID;
                                    }
                                    // or the main (linked) distance for the person's distance
                                    else if (dictionary.linkedDistanceIdentifierDictionary.TryGetValue(distanceId, out int linkedDistance))
                                    {
                                        distanceId = linkedDistance;
                                    }
                                    // Check if we know the last time the person was seen
                                    if (LastSeen.TryGetValue(part.EventSpecific.Identifier, out TimeResult lastResult)
                                        && dictionary.DistanceSegmentOrder.TryGetValue(distanceId, out List<Segment> distanceSegments)
                                        && dictionary.SegmentByIDDictionary.TryGetValue(lastResult.SegmentId, out Segment otherSeg))
                                    {
                                        foreach (Segment seg in distanceSegments)
                                        {
                                            // find the next segment at the location where the Cumulative (total) distance is greater than the distance
                                            // when we last saw the runner
                                            if (seg.LocationId == read.LocationID && seg.CumulativeDistance > otherSeg.CumulativeDistance)
                                            {
                                                // if we are set to set the occurrence too low
                                                if (occurrence < seg.Occurrence)
                                                {
                                                    // set it properly
                                                    occurrence = seg.Occurrence;
                                                }
                                                // break the loop since the occurrence is correct
                                                break;
                                            }
                                        }
                                    }
                                }
                                // Check if we're past the max occurances allowed for this spot.
                                // Also check if we've passed the finish occurrence for the finish line and that distance
                                // which requires an active distance and the person's information
                                if (occurrence > maxOccurrences ||
                                    (d != null && Constants.Timing.LOCATION_FINISH == read.LocationID && occurrence > d.FinishOccurrence))
                                {
                                    read.Status = Constants.Timing.CHIPREAD_STATUS_OVERMAX;
                                }
                                // occurrence is in [1,maxOccurrences], but can't be used because it's in the
                                // ignore period
                                else if (read.TimeSeconds < minSeconds || (read.TimeSeconds == minSeconds && read.TimeMilliseconds <= minMilliseconds))
                                {
                                    read.Status = Constants.Timing.CHIPREAD_STATUS_WITHINIGN;
                                }
                                // Check if part of the DNF list
                                // And if the read is AFTER they were marked as DNF
                                else if (bibDNFDictionary.TryGetValue(bib, out ChipRead dnfRead)
                                    && (dnfRead.TimeSeconds < read.TimeSeconds ||
                                        (dnfRead.TimeSeconds == read.TimeSeconds && dnfRead.TimeMilliseconds < read.TimeMilliseconds)))
                                {
                                    Log.D("Timing.Routines.DistanceRoutine", "bibDNFDictionary contains DNF for bib " + bib);
                                    read.Status = Constants.Timing.CHIPREAD_STATUS_OVERMAX;
                                }
                                // occurrence is in [1, maxOccurences] but not in the ignore period
                                else
                                {
                                    bibLastReadDictionary[(bib, read.LocationID)] = (read, occurrence);
                                    // Find if there's a segment associated with this combination
                                    int segId = Constants.Timing.SEGMENT_NONE;
                                    // With linked distances we want to ensure we use the Finish Occurence and Segments from the linked
                                    // distance instead of the actual distance since those aren't set.
                                    int distanceId = d == null ? 0 : d.Identifier, distanceFinOcc = d == null ? 0 : d.FinishOccurrence;
                                    if (d != null && d.LinkedDistance > 0)
                                    {
                                        distanceId = d.LinkedDistance;
                                        distanceFinOcc = dictionary.distanceDictionary.ContainsKey(d.LinkedDistance) ? dictionary.distanceDictionary[d.LinkedDistance].FinishOccurrence : d.FinishOccurrence;
                                    }
                                    // First check if we're using Distance specific segments
                                    if (!theEvent.DistanceSpecificSegments && dictionary.segmentDictionary.ContainsKey((Constants.Timing.COMMON_SEGMENTS_DISTANCEID, read.LocationID, occurrence)))
                                    {
                                        segId = dictionary.segmentDictionary[(Constants.Timing.COMMON_SEGMENTS_DISTANCEID, read.LocationID, occurrence)].Identifier;
                                    }
                                    // Then check if we can find a segment
                                    else if (d != null && dictionary.segmentDictionary.ContainsKey((distanceId, read.LocationID, occurrence)))
                                    {
                                        segId = dictionary.segmentDictionary[(distanceId, read.LocationID, occurrence)].Identifier;
                                    }
                                    // then check if it's the finish occurence. obviously this doesn't work if we can't find the distance
                                    else if (d != null && occurrence == distanceFinOcc && Constants.Timing.LOCATION_FINISH == read.LocationID)
                                    {
                                        segId = Constants.Timing.SEGMENT_FINISH;
                                    }
                                    string identifier = TimeResult.BibToIdentifier(bib);
                                    // Create a result for the start value.
                                    long secondsDiff = read.TimeSeconds - startSeconds;
                                    int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                                    // If the distance is linked as a late distance, use the linked distance's start time as the gun time.
                                    if (d != null
                                        && d.Type == Constants.Timing.DISTANCE_TYPE_LATE
                                        && d.LinkedDistance != Constants.Timing.DISTANCE_DUMMYIDENTIFIER
                                        && dictionary.distanceStartDict.TryGetValue(d.LinkedDistance, out (long, int) tmp))
                                    {
                                        secondsDiff = read.TimeSeconds - tmp.Item1;
                                        millisecDiff = read.TimeMilliseconds - tmp.Item2;
                                    }
                                    if (millisecDiff < 0)
                                    {
                                        secondsDiff--;
                                        millisecDiff += 1000;
                                    }
                                    long chipSecDiff = read.TimeSeconds - (startTimes.ContainsKey(identifier) ? Constants.Timing.RFIDDateToEpoch(startTimes[identifier].SystemTime) : startSeconds);
                                    int chipMillisecDiff = read.TimeMilliseconds - (startTimes.ContainsKey(identifier) ? startTimes[identifier].SystemTime.Millisecond : startMilliseconds);
                                    if (chipMillisecDiff < 0)
                                    {
                                        chipSecDiff--;
                                        chipMillisecDiff += 1000;
                                    }
                                    // Check that we're not adding a finish time for a DNF person, we can use any other times
                                    // for information for that person.
                                    if (Constants.Timing.SEGMENT_FINISH != segId || !bibDNFDictionary.ContainsKey(bib))
                                    {
                                        TimeResult newResult = new TimeResult(theEvent.Identifier,
                                            read.ReadId,
                                            part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                                            read.LocationID,
                                            segId,
                                            occurrence,
                                            secondsDiff,
                                            millisecDiff,
                                            identifier,
                                            chipSecDiff,
                                            chipMillisecDiff,
                                            read.Time,
                                            bib,
                                            Constants.Timing.TIMERESULT_STATUS_NONE,
                                            part == null ? "" : part.EventSpecific.Division
                                            );
                                        newResults.Add(newResult);
                                        if (Constants.Timing.SEGMENT_FINISH == segId)
                                        {
                                            finishTimes[identifier] = newResult;
                                        }
                                        if (part != null)
                                        {
                                            LastSeen[part.EventSpecific.Identifier] = newResult;
                                            // If they've finished, mark them as such.
                                            if (Constants.Timing.SEGMENT_FINISH == segId
                                                && !bibDNFDictionary.ContainsKey(bib))
                                            {
                                                part.Status = Constants.Timing.EVENTSPECIFIC_FINISHED;
                                                updateParticipants.Add(part);
                                            }
                                            // If they were marked as noshow previously, mark them as started
                                            else if (Constants.Timing.EVENTSPECIFIC_UNKNOWN == part.Status
                                                && !bibDNFDictionary.ContainsKey(bib))
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
            foreach (string chip in chipReadPairs.Keys)
            {
                long startSeconds, maxStartSeconds;
                int startMilliseconds;
                (startSeconds, startMilliseconds) = dictionary.distanceStartDict[0];
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
                                string identifier = TimeResult.ChipToIdentifier(chip);
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
                                    secondsDiff,
                                    millisecDiff,
                                    identifier,
                                    0,
                                    0,
                                    read.Time,
                                    read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib,
                                    Constants.Timing.TIMERESULT_STATUS_NONE,
                                    ""
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
                                    if (!dictionary.locationDictionary.ContainsKey(read.LocationID))
                                    {
                                        Log.E("Timing.Routines.DistanceRoutine", "Somehow the location was not found.");
                                    }
                                    else
                                    {
                                        maxOccurrences = dictionary.locationDictionary[read.LocationID].MaxOccurrences;
                                    }
                                }
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
                                // Check if part of the DNF list
                                // And if the read is AFTER they were marked as DNF
                                else if (chipDNFDictionary.TryGetValue(chip, out ChipRead dnfRead)
                                    && (dnfRead.TimeSeconds < read.TimeSeconds || 
                                        (dnfRead.TimeSeconds == read.TimeSeconds && dnfRead.TimeMilliseconds < read.TimeMilliseconds)))
                                {
                                    Log.D("Timing.Routines.DistanceRoutine", "chipDNFDictionary contains DNF for chip " + chip);
                                    read.Status = Constants.Timing.CHIPREAD_STATUS_OVERMAX;
                                }
                                // occurrence is in [1, maxOccurences] but not in the ignore period
                                else
                                {
                                    chipLastReadDictionary[(chip, read.LocationID)] = (read, occurrence);
                                    // Find if there's a segment associated with this combination
                                    int segId = Constants.Timing.SEGMENT_NONE;
                                    // First check if we're using Distance specific segments
                                    if (!theEvent.DistanceSpecificSegments && dictionary.segmentDictionary.ContainsKey((Constants.Timing.COMMON_SEGMENTS_DISTANCEID, read.LocationID, occurrence)))
                                    {
                                        segId = dictionary.segmentDictionary[(Constants.Timing.COMMON_SEGMENTS_DISTANCEID, read.LocationID, occurrence)].Identifier;
                                    }
                                    string identifier = TimeResult.ChipToIdentifier(chip).ToString();
                                    // Create a result for the start value.
                                    long secondsDiff = read.TimeSeconds - startSeconds;
                                    int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                                    if (millisecDiff < 0)
                                    {
                                        secondsDiff--;
                                        millisecDiff += 1000;
                                    }
                                    long chipSecDiff = read.TimeSeconds - (startTimes.ContainsKey(identifier) ? Constants.Timing.RFIDDateToEpoch(startTimes[identifier].SystemTime) : startSeconds);
                                    int chipMillisecDiff = read.TimeMilliseconds - (startTimes.ContainsKey(identifier) ? startTimes[identifier].SystemTime.Millisecond : startMilliseconds);
                                    if (chipMillisecDiff < 0)
                                    {
                                        chipSecDiff--;
                                        chipMillisecDiff += 1000;
                                    }
                                    // Check that we're not adding a finish time for a DNF person, we can use any other times
                                    // for information for that person.
                                    if (Constants.Timing.SEGMENT_FINISH != segId || !chipDNFDictionary.ContainsKey(chip))
                                    {
                                        TimeResult newResult = new(theEvent.Identifier,
                                            read.ReadId,
                                            Constants.Timing.TIMERESULT_DUMMYPERSON,
                                            read.LocationID,
                                            segId,
                                            occurrence,
                                            secondsDiff,
                                            millisecDiff,
                                            identifier,
                                            chipSecDiff,
                                            chipMillisecDiff,
                                            read.Time,
                                            read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib,
                                            Constants.Timing.TIMERESULT_STATUS_NONE,
                                            ""
                                            );
                                        read.Status = Constants.Timing.CHIPREAD_STATUS_USED;
                                        newResults.Add(newResult);
                                        if (Constants.Timing.SEGMENT_FINISH == segId)
                                        {
                                            finishTimes[identifier] = newResult;
                                        }
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
            // Process the intersection of unknown DNF people and Finish results:
            foreach (string chip in chipDNFDictionary.Keys)
            {
                if (finishTimes.ContainsKey(TimeResult.ChipToIdentifier(chip)))
                {
                    TimeResult finish = finishTimes[TimeResult.ChipToIdentifier(chip)];
                    finish.ReadId = chipDNFDictionary[chip].ReadId;
                    finish.Time = "DNF";
                    finish.ChipTime = "DNF";
                    finish.Status = Constants.Timing.TIMERESULT_STATUS_DNF;
                    finish.Occurrence = theEvent.FinishMaxOccurrences;
                    finishTimes[TimeResult.ChipToIdentifier(chip)] = finish;
                    newResults.Add(finish);
                }
                else
                {
                    TimeResult finish = new TimeResult(theEvent.Identifier,
                        chipDNFDictionary[chip].ReadId,
                        Constants.Timing.TIMERESULT_DUMMYPERSON,
                        Constants.Timing.LOCATION_FINISH,
                        Constants.Timing.SEGMENT_FINISH,
                        chipLastReadDictionary.ContainsKey((chip, Constants.Timing.LOCATION_FINISH)) ? chipLastReadDictionary[(chip, Constants.Timing.LOCATION_FINISH)].Occurrence + 1 : 1,
                        0,
                        0,
                        TimeResult.ChipToIdentifier(chip),
                        0,
                        0,
                        chipDNFDictionary[chip].Time,
                        chipDNFDictionary[chip].ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? chipDNFDictionary[chip].ReadBib : chipDNFDictionary[chip].ChipBib,
                        Constants.Timing.TIMERESULT_STATUS_DNF,
                        ""
                        );
                    finishTimes[TimeResult.ChipToIdentifier(chip)] = finish;
                    newResults.Add(finish);
                }
            }
            // Process the intersection of known DNF people and Finish results:
            foreach (string bib in bibDNFDictionary.Keys)
            {
                Participant part = dictionary.participantBibDictionary.ContainsKey(bib) ?
                    dictionary.participantBibDictionary[bib] :
                    null;
                if (part != null)
                {
                    part.Status = Constants.Timing.EVENTSPECIFIC_DNF;
                    updateParticipants.Add(part);
                }
                int occurrence = part == null ? 1 : dictionary.distanceDictionary.ContainsKey(part.EventSpecific.DistanceIdentifier) ? dictionary.distanceDictionary[part.EventSpecific.DistanceIdentifier].FinishOccurrence : 1;
                if (finishTimes.ContainsKey(TimeResult.BibToIdentifier(bib)))
                {
                    TimeResult finish = finishTimes[TimeResult.BibToIdentifier(bib)];
                    finish.ReadId = bibDNFDictionary[bib].ReadId;
                    finish.Time = "DNF";
                    finish.ChipTime = "DNF";
                    finish.Status = Constants.Timing.TIMERESULT_STATUS_DNF;
                    finish.Occurrence = occurrence;
                    finishTimes[TimeResult.BibToIdentifier(bib)] = finish;
                    newResults.Add(finish);
                }
                else
                {
                    TimeResult finish = new TimeResult(theEvent.Identifier,
                        bibDNFDictionary[bib].ReadId,
                        part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                        Constants.Timing.LOCATION_FINISH,
                        Constants.Timing.SEGMENT_FINISH,
                        occurrence,
                        0,
                        0,
                        TimeResult.BibToIdentifier(bib),
                        0,
                        0,
                        bibDNFDictionary[bib].Time,
                        bib,
                        Constants.Timing.TIMERESULT_STATUS_DNF,
                        part == null ? "" : part.EventSpecific.Division
                        );
                    finishTimes[TimeResult.BibToIdentifier(bib)] = finish;
                    newResults.Add(finish);
                }
            }
            // Process the intersection of unknown DNS people and Finish results:
            foreach (string chip in chipDNSDictionary.Keys)
            {
                if (finishTimes.ContainsKey(TimeResult.ChipToIdentifier(chip)))
                {
                    TimeResult finish = finishTimes[TimeResult.ChipToIdentifier(chip)];
                    finish.ReadId = chipDNSDictionary[chip].ReadId;
                    finish.Time = "DNS";
                    finish.ChipTime = "DNS";
                    finish.Status = Constants.Timing.TIMERESULT_STATUS_DNS;
                    finish.Occurrence = theEvent.FinishMaxOccurrences;
                    finishTimes[TimeResult.ChipToIdentifier(chip)] = finish;
                    newResults.Add(finish);
                }
                else
                {
                    TimeResult finish = new TimeResult(theEvent.Identifier,
                        chipDNSDictionary[chip].ReadId,
                        Constants.Timing.TIMERESULT_DUMMYPERSON,
                        Constants.Timing.LOCATION_FINISH,
                        Constants.Timing.SEGMENT_FINISH,
                        chipLastReadDictionary.ContainsKey((chip, Constants.Timing.LOCATION_FINISH)) ? chipLastReadDictionary[(chip, Constants.Timing.LOCATION_FINISH)].Occurrence + 1 : 1,
                        0,
                        0,
                        TimeResult.ChipToIdentifier(chip),
                        0,
                        0,
                        chipDNSDictionary[chip].Time,
                        chipDNSDictionary[chip].ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? chipDNSDictionary[chip].ReadBib : chipDNSDictionary[chip].ChipBib,
                        Constants.Timing.TIMERESULT_STATUS_DNS,
                        ""
                        );
                    finishTimes[TimeResult.ChipToIdentifier(chip)] = finish;
                    newResults.Add(finish);
                }
            }
            // Process the intersection of known DNS people and Finish results:
            foreach (string bib in bibDNSDictionary.Keys)
            {
                Participant part = dictionary.participantBibDictionary.ContainsKey(bib) ?
                    dictionary.participantBibDictionary[bib] :
                    null;
                if (part != null)
                {
                    part.Status = Constants.Timing.EVENTSPECIFIC_DNS;
                    updateParticipants.Add(part);
                }
                int occurrence = part == null ? 1 : dictionary.distanceDictionary.ContainsKey(part.EventSpecific.DistanceIdentifier) ? dictionary.distanceDictionary[part.EventSpecific.DistanceIdentifier].FinishOccurrence : 1;
                if (finishTimes.ContainsKey(TimeResult.BibToIdentifier(bib)))
                {
                    TimeResult finish = finishTimes[TimeResult.BibToIdentifier(bib)];
                    finish.ReadId = bibDNSDictionary[bib].ReadId;
                    finish.Time = "DNS";
                    finish.ChipTime = "DNS";
                    finish.Status = Constants.Timing.TIMERESULT_STATUS_DNS;
                    finish.Occurrence = occurrence;
                    finishTimes[TimeResult.BibToIdentifier(bib)] = finish;
                    newResults.Add(finish);
                }
                else
                {
                    TimeResult finish = new TimeResult(theEvent.Identifier,
                        bibDNSDictionary[bib].ReadId,
                        part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                        Constants.Timing.LOCATION_FINISH,
                        Constants.Timing.SEGMENT_FINISH,
                        occurrence,
                        0,
                        0,
                        TimeResult.BibToIdentifier(bib),
                        0,
                        0,
                        bibDNSDictionary[bib].Time,
                        bib,
                        Constants.Timing.TIMERESULT_STATUS_DNS,
                        part == null ? "" : part.EventSpecific.Division
                        );
                    finishTimes[TimeResult.BibToIdentifier(bib)] = finish;
                    newResults.Add(finish);
                }
            }
            // process reads that need to be set to ignore
            foreach (ChipRead read in setUnknown)
            {
                read.Status = Constants.Timing.CHIPREAD_STATUS_UNKNOWN;
            }
            // remove any results past the finish time
            List<TimeResult> toRemove = [];
            foreach (TimeResult res in newResults)
            {
                // Set all results that come after the finish to be removed
                if (finishTimes.TryGetValue(res.UnknownId, out TimeResult finish) && (finish.Seconds < res.Seconds || (finish.Seconds == res.Seconds && finish.Milliseconds < res.Milliseconds)))
                {
                    toRemove.Add(res);
                    if (chipReadDict.TryGetValue(res.ReadId, out ChipRead oldRead))
                    {
                        oldRead.Status = Constants.Timing.CHIPREAD_STATUS_AFTER_FINISH;
                    }
                }
            }
            newResults.RemoveAll(toRemove.Contains);
            // Update database with information.
            database.AddTimingResults(newResults);
            database.SetChipReadStatuses(allChipReads);
            database.UpdateParticipants([.. updateParticipants]);
            return newResults;
        }

        // Process timing placements for a distance based event.
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
                Log.D("Timing.Routines.DistanceRoutine", "Processing segment " + segment.Name);
                if (segmentDictionary.ContainsKey(segment.Identifier))
                {
                    output.AddRange(ProcessSegmentPlacements(theEvent, segmentDictionary[segment.Identifier], dictionary));
                }
            }
            Log.D("Timing.Routines.DistanceRoutine", "Processing finish results");
            if (segmentDictionary.ContainsKey(Constants.Timing.SEGMENT_FINISH))
            {
                output.AddRange(ProcessSegmentPlacements(theEvent, segmentDictionary[Constants.Timing.SEGMENT_FINISH], dictionary));
            }
            // Check if we should be re-uploading results because placements have changed.
            List<TimeResult> reUpload = new List<TimeResult>();
            Log.D("Timing.Routines.DistanceRoutine", "Checking for outdated placements.");
            foreach (TimeResult result in output)
            {
                if (PlacementDictionary.ContainsKey((result.Place, result.LocationId, result.Occurrence, result.DistanceName)) && PlacementDictionary[(result.Place, result.LocationId, result.Occurrence, result.DistanceName)].Bib != result.Bib)
                {
                    Log.D("Timing.Routines.DistanceRoutine", String.Format("Oudated placement found. {0} && {1}", result.ParticipantName, PlacementDictionary[(result.Place, result.LocationId, result.Occurrence, result.DistanceName)].ParticipantName));
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

        // Process segment placements.
        private static List<TimeResult> ProcessSegmentPlacements(Event theEvent,
            List<TimeResult> segmentResults,
            TimingDictionary dictionary)
        {
            if (theEvent.RankByGun)
            {
                segmentResults.Sort((x1, x2) =>
                {
                    if (x1 == null || x2 == null) return 1;
                    Distance distance1 = null, distance2 = null;
                    int rank1 = 0, rank2 = 0;
                    Log.D("Timing.Routines.DistanceRoutine", "x1 distance name: " + x1.RealDistanceName + " -- x2 distance name: " + x2.RealDistanceName);
                    // Get *linked* distances. (Could be that specific distance)
                    if (dictionary.linkedDistanceDictionary.ContainsKey(x1.RealDistanceName))
                    {
                        (distance1, rank1) = dictionary.linkedDistanceDictionary[x1.RealDistanceName];
                    }
                    if (dictionary.linkedDistanceDictionary.ContainsKey(x2.RealDistanceName))
                    {
                        (distance2, rank2) = dictionary.linkedDistanceDictionary[x2.RealDistanceName];
                    }
                    Log.D("Timing.Routines.DistanceRoutine", (distance1 == null || distance2 == null) ? "One of the distances not found." : "Rank 1: " + rank1 + " -- Rank 2: " + rank2);
                    // Check if they're in the same distance or a linked distance.
                    if (distance1 != null && distance2 != null && distance1.Identifier == distance2.Identifier)
                    {
                        // Sort based on rank.  This is the linked distance new sorting item.
                        if (rank1 == rank2)
                        {
                            Log.D("Timing.Routines.DistanceRoutine", "Ranks the same.");
                            if (theEvent.RankByGun)
                            {
                                if (x1.Seconds == x2.Seconds)
                                {
                                    return x1.Milliseconds.CompareTo(x2.Milliseconds);
                                }
                                Log.D("Timing.Routines.DistanceRoutine", "By Gun");
                                return x1.Seconds.CompareTo(x2.Seconds);
                            }
                            else
                            {
                                if (x1.ChipSeconds == x2.ChipSeconds)
                                {
                                    return x1.ChipMilliseconds.CompareTo(x2.ChipMilliseconds);
                                }
                                Log.D("Timing.Routines.DistanceRoutine", "By Chip");
                                return x1.ChipSeconds.CompareTo(x2.ChipSeconds);
                            }
                        }
                        Log.D("Timing.Routines.DistanceRoutine", "Ranks not the same.");
                        // Ranks not the same
                        return rank1.CompareTo(rank2);
                    }
                    return x1.DistanceName.CompareTo(x2.DistanceName);
                });
            }
            else
            {
                segmentResults.Sort((x1, x2) =>
                {
                    if (x1 == null || x2 == null) return 1;
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
                    // Check if they're in the same distance or a linked distance.
                    if (distance1 != null && distance2 != null && distance1.Identifier == distance2.Identifier)
                    {
                        // Sort based on rank.  This is the linked distance new sorting item.
                        if (rank1 == rank2)
                        {
                            // These are the old ways to sort before we've added linked distances.
                            // Check if we know the participants we're comparing
                            if (dictionary.participantEventSpecificDictionary.ContainsKey(x1.EventSpecificId) && dictionary.participantEventSpecificDictionary.ContainsKey(x2.EventSpecificId))
                            {
                                // Check if they're both either EARLY START or not EARLY START. (DEPRECATED METHOD)
                                return x1.CompareChip(x2);
                            }
                        }
                        // Ranks not the same
                        return rank1.CompareTo(rank2);
                    }
                    // Default to sorting by distance name.
                    return x1.DistanceName.CompareTo(x2.DistanceName);
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
            Log.D("Timing.Routines.DistanceRoutine", string.Format("{0} Result(s) in DNFResults - {1} Result(s) removed from segmentResults", DNFResults.Count, removed));
            // Get Dictionaries for storing the last known place (division, age group, gender, overall)
            // The key is as follows: (Distance ID, Division)
            Dictionary<(int, string), int> divisionPlaceDictionary = [];
            // The key is as follows: (Distance ID, Age Group ID, Gender)
            Dictionary<(int, int, string), int> ageGroupPlaceDictionary = [];
            // The key is as follows: (Distance ID, Gender)
            Dictionary<(int, string), int> genderPlaceDictionary = [];
            // The key is as follows: (Distance ID)
            Dictionary<int, int> placeDictionary = [];
            int ageGroupId;
            string gender, division;
            foreach (TimeResult result in segmentResults)
            {
                gender = "not specified";
                ageGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
                // Check if we know who the person is. Can't rank them if we don't know
                // what distance they're in, their age, or their gender
                if (dictionary.participantEventSpecificDictionary.TryGetValue(result.EventSpecificId, out Participant person))
                {
                    // Use a linked distance ID for ranking instead of a specific distance id.
                    if (!dictionary.linkedDistanceIdentifierDictionary.TryGetValue(person.EventSpecific.DistanceIdentifier, out int distanceId))
                    {
                        distanceId = person.EventSpecific.DistanceIdentifier;
                    }
                    gender = person.Gender.ToLower();
                    if (gender.Length < 1)
                    {
                        gender = "not specified";
                    }
                    ageGroupId = person.EventSpecific.AgeGroupId;
                    // Since Results were sorted before we started, let's assume that the first item
                    // is the fastest and if we can't find the key, add one starting at 0
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
                    if (ageGroupId != Constants.Timing.TIMERESULT_DUMMYAGEGROUP)
                    {
                        if (!ageGroupPlaceDictionary.ContainsKey((distanceId, ageGroupId, gender)))
                        {
                            ageGroupPlaceDictionary[(distanceId, ageGroupId, gender)] = 0;
                        }
                        result.AgePlace = ++ageGroupPlaceDictionary[(distanceId, ageGroupId, gender)];
                    }
                    division = person.EventSpecific.Division.ToLower();
                    if (division.Length > 0)
                    {
                        if (!divisionPlaceDictionary.ContainsKey((distanceId, division)))
                        {
                            divisionPlaceDictionary[(distanceId, division)] = 0;
                        }
                        result.DivisionPlace = ++divisionPlaceDictionary[(distanceId, division)];
                    }
                }
                result.Status = Constants.Timing.TIMERESULT_STATUS_PROCESSED;
            }
            segmentResults.AddRange(DNFResults);
            return segmentResults;
        }
    }
}
