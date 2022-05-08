using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Timing.Routines
{
    internal class BackyardUltraRoutine
    {

        // Process chip reads
        public static List<TimeResult> ProcessRace(Event theEvent, IDBInterface database, TimingDictionary dictionary)
        {
            Log.D("Timing.Routines.BackyardUltraRoutine", "Processing chip reads for a backyard ultra.");
            // Pre-process information we'll need to fully process chip reads
            // Create a dictionary for hour starts and ends.
            Dictionary<(int, string), (TimeResult start, TimeResult end)> backyardResultDictionary = new Dictionary<(int, string), (TimeResult start, TimeResult end)>();
            Dictionary<(int, string, int), TimeResult> backyardOccurenceDictionary = new Dictionary<(int, string, int), TimeResult>();
            // The initial start times will always be hour 0 start times.
            foreach (TimeResult result in database.GetStartTimes(theEvent.Identifier))
            {
                backyardResultDictionary[(0, result.Identifier)] = (start: result, end: null);
            }
            // Get the rest of the times.
            foreach (TimeResult result in database.GetFinishTimes(theEvent.Identifier))
            {
                // Pull out old results if they're in the dictionary already.
                (TimeResult start, TimeResult end) tmpRes = (null, null);
                if (backyardResultDictionary.ContainsKey((result.Occurrence / 2, result.Identifier)))
                {
                    tmpRes = backyardResultDictionary[(result.Occurrence / 2, result.Identifier)];
                }
                // If start time
                if (result.Occurrence % 2 == 0)
                {
                    if (tmpRes.start != null)
                    {
                        Log.E("Timing.Routines.BackyardUltraRoutine", "Found a duplicate start time for an hour.");
                    }
                    tmpRes.start = result;
                }
                // If end time
                else if (result.Occurrence % 2 == 1)
                {
                    if (tmpRes.end != null)
                    {
                        Log.E("Timing.Routines.BackyardUltraRoutine", "Found a duplicate end time for an hour.");
                    }
                    tmpRes.end = result;
                }
                // Modification 2 should result in either a 0 or a 1, this code should be unreachable.
                else
                {
                    Log.E("Timing.Routines.BackyardUltraRoutine", "Made it to code that should be unreachable somehow.");
                }
                // Update dictionary.
                backyardResultDictionary[(result.Occurrence / 2, result.Identifier)] = tmpRes;
            }
            // Get all of the Chip Reads we find useful (Unprocessed, and those used as a result.)
            // and then sort them into groups based upon Bib, Chip, or put them in the ignore pile if
            // they have no bib or chip.
            Dictionary<int, List<ChipRead>> bibReadPairs = new Dictionary<int, List<ChipRead>>();
            Dictionary<string, List<ChipRead>> chipReadPairs = new Dictionary<string, List<ChipRead>>();
            // Make sure we keep track of the
            // last occurrence for a person at a specific location.
            // (Bib, Location), Last Chip Read
            Dictionary<(int, int), (ChipRead Read, int Occurrence)> lastReadDictionary = new Dictionary<(int, int), (ChipRead Read, int Occurrence)>();
            Dictionary<(string, int), (ChipRead Read, int Occurrence)> chipLastReadDictionary = new Dictionary<(string, int), (ChipRead Read, int Occurrence)>();
            // Keep a list of DNF participants so we can mark them as DNF in results.
            // Keep a record of the DNF chipread so we can link it with the TimeResult.
            Dictionary<string, int> dnfHourDictionary = new Dictionary<string, int>();
            Dictionary<int, ChipRead> dnfDictionary = new Dictionary<int, ChipRead>();
            Dictionary<string, ChipRead> chipDnfDictionary = new Dictionary<string, ChipRead>();
            // Get all useful chipreads.
            List<ChipRead> allChipReads = database.GetUsefulChipReads(theEvent.Identifier);
            allChipReads.Sort();
            List<ChipRead> setUnknown = new List<ChipRead>();

            // Sort chipreads into proper piles.
            foreach (ChipRead read in allChipReads)
            {
                // Process reads with known bib numbers.
                if (read.Bib != Constants.Timing.CHIPREAD_DUMMYBIB)
                {
                    // if we process all the used reads before putting them in the list
                    // we can ensure that all of the reads we process are STATUS_NONE
                    // and then we can verify that we aren't inserting results BEFORE
                    // results we've already calculated
                    // Check if its a read we've used for a finish read.
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
                    // Otherwise if its a start read at the proper location.
                    else if (Constants.Timing.CHIPREAD_STATUS_STARTTIME ==  read.Status &&
                        (Constants.Timing.LOCATION_START == read.LocationID ||
                        (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish)))
                    {
                        // If we haven't found anything, let us know what our start time was
                        if (!lastReadDictionary.ContainsKey((read.Bib, read.LocationID)))
                        {
                            lastReadDictionary[(read.Bib, read.LocationID)] = (read, 0);
                        }
                    }
                    // If its a DNF read
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
                // Process reads with unknown bib numbers but known chip numbers.
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
                // Set all other reads to unknown.
                else
                {
                    setUnknown.Add(read);
                }
            }

            // Go through each chip read for a single person.
            List<TimeResult> newResults = new List<TimeResult>();
            // Keep a list of participants to update.
            HashSet<Participant> updateParticipants = new HashSet<Participant>();
            // Process reads that have a bib
            foreach (int bib in bibReadPairs.Keys)
            {
                Participant part = dictionary.participantBibDictionary.ContainsKey(bib) ?
                    dictionary.participantBibDictionary[bib] : null;
                Distance d = part != null ? dictionary.distanceDictionary[part.EventSpecific.DistanceIdentifier] : null;
                long startSeconds;
                int startMilliseconds;
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
                // Go through each chipread
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
                            // Calculate the time value for this read.
                            long secondsDiff = read.TimeSeconds - startSeconds;
                            int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                            if (millisecDiff < 0)
                            {
                                secondsDiff--;
                                millisecDiff = 1000 + millisecDiff;
                            }
                            // Calculate the hour
                            int hour = (int)(secondsDiff / 3600);
                            long secondsNoHour = secondsDiff % 3600;
                            // Check if we've already included them in the DNF pile and we're past the hour when they DNF'ed.
                            // Process the reads if this isn't the case.
                            if (!dnfHourDictionary.ContainsKey("Bib:" + bib.ToString()) || dnfHourDictionary["Bib:" + bib.ToString()] > hour)
                            {
                                // Check if we're at the starting point and within a starting window
                                if ((Constants.Timing.LOCATION_START == read.LocationID || (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish))
                                    && (secondsNoHour < theEvent.StartWindow || (secondsNoHour == theEvent.StartWindow && millisecDiff == 0)))
                                {
                                    // check for a stored start chipread with the correct occurence (hour start)
                                    if (lastReadDictionary.ContainsKey((bib, read.LocationID)) && lastReadDictionary[(bib, read.LocationID)].Occurrence == (hour * 2))
                                    {
                                        lastReadDictionary[(bib, read.LocationID)].Read.Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                                    }
                                    // Update the last read we've seen at this location
                                    lastReadDictionary[(bib, read.LocationID)] = (Read: read, Occurrence: hour * 2);
                                    // check for start results in our list that we're pushing to the database and remove it if it is there
                                    TimeResult startResult = null;
                                    if (backyardResultDictionary.ContainsKey((hour, "Bib:" + bib.ToString())))
                                    {
                                        startResult = backyardResultDictionary[(hour, "Bib:" + bib.ToString())].start;
                                    }
                                    if (startResult != null && newResults.Contains(startResult))
                                    {
                                        newResults.Remove(startResult);
                                    }
                                    // Create a result for the start value.
                                    startResult = new TimeResult(theEvent.Identifier,
                                        read.ReadId,
                                        part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                                        read.LocationID,
                                        hour == 0 ? Constants.Timing.SEGMENT_START : Constants.Timing.SEGMENT_NONE,
                                        hour * 2, // start reads are always set at their hour * 2 for occurence (0, 2, 4, 6, etc)
                                        Constants.Timing.ToTime(secondsDiff, millisecDiff),
                                        "Bib:" + bib.ToString(),
                                        "0:00:00.000",
                                        read.Time,
                                        bib,
                                        Constants.Timing.TIMERESULT_STATUS_NONE
                                        );
                                    if (!backyardResultDictionary.ContainsKey((hour, startResult.Identifier)))
                                    {
                                        backyardResultDictionary[(hour, startResult.Identifier)] = (start: null, end: null);
                                    }
                                    var tmpVal = backyardResultDictionary[(hour, startResult.Identifier)];
                                    tmpVal.start = startResult;
                                    backyardResultDictionary[(hour, startResult.Identifier)] = tmpVal;
                                    newResults.Add(startResult);
                                    // Check if we should update the status of the person.
                                    if (part != null &&
                                        (Constants.Timing.EVENTSPECIFIC_NOSHOW == part.Status
                                        && !dnfDictionary.ContainsKey(bib)))
                                    {
                                        part.Status = Constants.Timing.EVENTSPECIFIC_STARTED;
                                        updateParticipants.Add(part);
                                    }
                                    // Finally, set the chipread status to STARTTIME
                                    read.Status = Constants.Timing.CHIPREAD_STATUS_STARTTIME;
                                }
                                // Possible reads at this point:
                                //      Reads at the start not within the StartWindow (IGNORE)
                                //      Start/Finish Location reads past the StartWindow (Valid Reads)
                                //          These could be BEFORE or AFTER the last occurrence at this spot
                                //      Reads at any other location
                                else if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                                {
                                    int occursWithin = theEvent.FinishIgnoreWithin;
                                    // find the hour results
                                    (TimeResult start, TimeResult end) tmpRes = (null, null);
                                    if (backyardResultDictionary.ContainsKey((hour, "Bib:" + bib.ToString())))
                                    {
                                        tmpRes = backyardResultDictionary[(hour, "Bib:" + bib.ToString())];
                                    }
                                    // Get the minimum number of seconds we want to enforce between start time for a loop and finish time
                                    // Start with 0 because they may not have a start time for one reason or another
                                    long minSeconds = 0;
                                    long minMilliseconds = 0;
                                    if (tmpRes.start != null)
                                    {
                                        // get rid of the hours from the result time
                                        minSeconds = tmpRes.start.ChipSeconds % 3600 + occursWithin;
                                        minMilliseconds = tmpRes.start.ChipMilliseconds;
                                    }
                                    // Check if there's a finish time already for this hour:
                                    if (tmpRes.end != null ||
                                        // or we're within the ignore period
                                        (secondsNoHour < minSeconds || (secondsNoHour == minSeconds && millisecDiff < minMilliseconds)))
                                    {
                                        // and set it to ignore it -- this assumes all entries received and processed are in chronological order
                                        read.Status = Constants.Timing.CHIPREAD_STATUS_WITHINIGN;
                                    }
                                    else
                                    {
                                        lastReadDictionary[(bib, read.LocationID)] = (read, hour * 2 + 1);// Create a result for the start value.
                                        long chipSecDiff = read.TimeSeconds - (tmpRes.start == null ? 0 : tmpRes.start.ChipSeconds % 3600);
                                        int chipMillisecDiff = read.TimeMilliseconds - (tmpRes.start == null ? 0 : tmpRes.start.ChipMilliseconds);
                                        if (chipMillisecDiff < 0)
                                        {
                                            chipSecDiff--;
                                            chipMillisecDiff += 1000;
                                        }
                                        // Check that we're not adding a finish time for a DNF person, we can use any other times
                                        // for information for that person.
                                        // TODO check previous hour for result and set DNF if none found
                                        // then upload result for this hour if they've got one
                                        if (!dnfDictionary.ContainsKey(bib))
                                        {
                                            newResults.Add(new TimeResult(theEvent.Identifier,
                                                read.ReadId,
                                                part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                                                read.LocationID,
                                                Constants.Timing.SEGMENT_NONE,
                                                hour * 2 + 1,
                                                Constants.Timing.ToTime(secondsDiff, millisecDiff),
                                                "Bib:" + bib.ToString(),
                                                Constants.Timing.ToTime(chipSecDiff, chipMillisecDiff),
                                                read.Time,
                                                bib,
                                                Constants.Timing.TIMERESULT_STATUS_NONE
                                                ));
                                            if (part != null)
                                            {
                                                // If they've finished, mark them as such.
                                                if (Constants.Timing.SEGMENT_FINISH == 10918
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
                                //      Reads at the start not within the StartWindow (IGNORE)
                                //      Reads at any location other than the start/finish
                                else if (Constants.Timing.LOCATION_START != read.LocationID)
                                {

                                }
                                // Possible reads at this point:
                                //      Start location reads not within a start window...
                                else
                                {
                                    read.Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                                }
                            }
                            // Otherwise just ignore the reads.
                            else
                            {
                                read.Status = Constants.Timing.CHIPREAD_STATUS_OVERMAX;
                            }
                        }
                    }
                }
            }
            return null;
        }


        public static List<TimeResult> ProcessPlacements(Event theEvent, IDBInterface database, TimingDictionary dictionary)
        {
            return null;
        }
    }
}
