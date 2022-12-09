using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Timing.Routines
{
    internal class DistanceRoutine
    {
        public static List<TimeResult> ProcessRace(Event theEvent, IDBInterface database, TimingDictionary dictionary)
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
                        if (!lastReadDictionary.ContainsKey((read.Bib, read.LocationID)))
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
                        if (!lastReadDictionary.ContainsKey((read.Bib, read.LocationID)))
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
            List<TimeResult> newResults = new List<TimeResult>();
            // Keep a list of participants to update.
            HashSet<Participant> updateParticipants = new HashSet<Participant>();
            // process reads that have a bib
            foreach (int bib in bibReadPairs.Keys)
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
                                    Constants.Timing.ToTime(secondsDiff, millisecDiff),
                                    TimeResult.BibToIdentifier(bib),
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
                                if (lastReadDictionary.ContainsKey((bib, read.LocationID)))
                                {
                                    occurrence = lastReadDictionary[(bib, read.LocationID)].Occurrence + 1;
                                    minSeconds = lastReadDictionary[(bib, read.LocationID)].Read.TimeSeconds + occursWithin;
                                    minMilliseconds = lastReadDictionary[(bib, read.LocationID)].Read.TimeMilliseconds;
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
                                // occurrence is in [1, maxOccurences] but not in the ignore period
                                else
                                {
                                    lastReadDictionary[(bib, read.LocationID)] = (read, occurrence);
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
                                    if (theEvent.DistanceSpecificSegments && dictionary.segmentDictionary.ContainsKey((Constants.Timing.COMMON_SEGMENTS_DISTANCEID, read.LocationID, occurrence)))
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
                                            Constants.Timing.ToTime(secondsDiff, millisecDiff),
                                            identifier,
                                            Constants.Timing.ToTime(chipSecDiff, chipMillisecDiff),
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
                                    Constants.Timing.ToTime(secondsDiff, millisecDiff),
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
                                // occurrence is in [1, maxOccurences] but not in the ignore period
                                else
                                {
                                    chipLastReadDictionary[(chip, read.LocationID)] = (read, occurrence);
                                    // Find if there's a segment associated with this combination
                                    int segId = Constants.Timing.SEGMENT_NONE;
                                    // First check if we're using Distance specific segments
                                    if (theEvent.DistanceSpecificSegments && dictionary.segmentDictionary.ContainsKey((Constants.Timing.COMMON_SEGMENTS_DISTANCEID, read.LocationID, occurrence)))
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
            foreach (string chip in chipDnfDictionary.Keys)
            {
                if (finishTimes.ContainsKey(TimeResult.ChipToIdentifier(chip)))
                {
                    TimeResult finish = finishTimes[TimeResult.ChipToIdentifier(chip)];
                    finish.ReadId = chipDnfDictionary[chip].ReadId;
                    finish.Time = "DNF";
                    finish.ChipTime = "DNF";
                    finish.Status = Constants.Timing.TIMERESULT_STATUS_DNF;
                    finish.Occurrence = theEvent.FinishMaxOccurrences;
                    newResults.Add(finish);
                }
                else
                {
                    newResults.Add(new TimeResult(theEvent.Identifier,
                        chipDnfDictionary[chip].ReadId,
                        Constants.Timing.TIMERESULT_DUMMYPERSON,
                        Constants.Timing.LOCATION_FINISH,
                        Constants.Timing.SEGMENT_FINISH,
                        chipLastReadDictionary.ContainsKey((chip, Constants.Timing.LOCATION_FINISH)) ? chipLastReadDictionary[(chip, Constants.Timing.LOCATION_FINISH)].Occurrence + 1 : 1,
                        "DNF",
                        TimeResult.ChipToIdentifier(chip),
                        "DNF",
                        chipDnfDictionary[chip].Time,
                        chipDnfDictionary[chip].ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? chipDnfDictionary[chip].ReadBib : chipDnfDictionary[chip].ChipBib,
                        Constants.Timing.TIMERESULT_STATUS_DNF
                        ));
                }
            }
            // Process the intersection of known DNF people and Finish results:
            foreach (int bib in dnfDictionary.Keys)
            {
                Participant part = dictionary.participantBibDictionary.ContainsKey(bib) ?
                    dictionary.participantBibDictionary[bib] :
                    null;
                if (part != null)
                {
                    part.Status = Constants.Timing.EVENTSPECIFIC_NOFINISH;
                    updateParticipants.Add(part);
                }
                int occurrence = dictionary.distanceDictionary.ContainsKey(part.EventSpecific.DistanceIdentifier) ? dictionary.distanceDictionary[part.EventSpecific.DistanceIdentifier].FinishOccurrence : 1;
                if (finishTimes.ContainsKey(TimeResult.BibToIdentifier(bib)))
                {
                    TimeResult finish = finishTimes[TimeResult.BibToIdentifier(bib)];
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
                        TimeResult.BibToIdentifier(bib),
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
            foreach (TimeResult result in segmentResults)
            {
                // Check if we know who the person is. Can't rank them if we don't know
                // what distance they're in, their age, or their gender
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
                        || person.Gender.Equals("NonBinary", StringComparison.OrdinalIgnoreCase))
                    {
                        gender = Constants.Timing.TIMERESULT_GENDER_NON_BINARY;
                    }
                    ageGroupId = person.EventSpecific.AgeGroupId;
                    // Since Results were sorted before we started, let's assume that the first item
                    // is the fastest and if we can't find the key, add one starting at 0
                    if (!placeDictionary.ContainsKey(distanceId))
                    {
                        placeDictionary[distanceId] = 0;
                    }
                    result.Place = ++(placeDictionary[distanceId]);
                    if (!genderPlaceDictionary.ContainsKey((distanceId, gender)))
                    {
                        genderPlaceDictionary[(distanceId, gender)] = 0;
                    }
                    result.GenderPlace = ++(genderPlaceDictionary[(distanceId, gender)]);
                    if (!ageGroupPlaceDictionary.ContainsKey((distanceId, ageGroupId, gender)))
                    {
                        ageGroupPlaceDictionary[(distanceId, ageGroupId, gender)] = 0;
                    }
                    result.AgePlace = ++(ageGroupPlaceDictionary[(distanceId, ageGroupId, gender)]);
                }
            }
            segmentResults.AddRange(DNFResults);
            return segmentResults;
        }
    }
}
