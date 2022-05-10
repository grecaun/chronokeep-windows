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
                        long startSeconds = dictionary.distanceStartDict[0].Seconds;
                        int startMilliseconds = dictionary.distanceStartDict[0].Milliseconds;
                        long secondsDiff = read.TimeSeconds - startSeconds;
                        int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                        if (millisecDiff < 0)
                        {
                            secondsDiff--;
                        }
                        // Calculate the hour
                        int hour = (int)(secondsDiff / 3600);
                        dnfDictionary[read.Bib] = read;
                        dnfHourDictionary[TimeResult.BibToIdentifier(read.Bib)] = hour;
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
                        long startSeconds = dictionary.distanceStartDict[0].Seconds;
                        int startMilliseconds = dictionary.distanceStartDict[0].Milliseconds;
                        long secondsDiff = read.TimeSeconds - startSeconds;
                        int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                        if (millisecDiff < 0)
                        {
                            secondsDiff--;
                        }
                        // Calculate the hour
                        int hour = (int)(secondsDiff / 3600);
                        chipDnfDictionary[read.ChipNumber] = read;
                        dnfHourDictionary[TimeResult.ChipToIdentifier(read.ChipNumber)] = hour;
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
            // This algorithm assumes it is processing every chip read in chronological order.
            // Reads not input into the system in the correct order will require 
            List<TimeResult> newResults = new List<TimeResult>();
            // Keep a list of participants to update.
            HashSet<Participant> updateParticipants = new HashSet<Participant>();
            // Process reads that have a bib
            foreach (int bib in bibReadPairs.Keys)
            {
                Participant part = dictionary.participantBibDictionary.ContainsKey(bib) ?
                    dictionary.participantBibDictionary[bib] : null;
                Distance d = part != null ? dictionary.distanceDictionary[part.EventSpecific.DistanceIdentifier] : null;
                long startSeconds = dictionary.distanceStartDict[0].Seconds;
                int startMilliseconds = dictionary.distanceStartDict[0].Milliseconds;
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
                            if (!dnfHourDictionary.ContainsKey(TimeResult.BibToIdentifier(bib)) || dnfHourDictionary[TimeResult.BibToIdentifier(bib)] > hour)
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
                                    if (backyardResultDictionary.ContainsKey((hour, TimeResult.BibToIdentifier(bib))))
                                    {
                                        startResult = backyardResultDictionary[(hour, TimeResult.BibToIdentifier(bib))].start;
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
                                        TimeResult.BibToIdentifier(bib),
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
                                else if (Constants.Timing.LOCATION_FINISH != read.LocationID)
                                {
                                    // find the hour results
                                    (TimeResult start, TimeResult end) tmpRes = (null, null);
                                    if (backyardResultDictionary.ContainsKey((hour, TimeResult.BibToIdentifier(bib))))
                                    {
                                        tmpRes = backyardResultDictionary[(hour, TimeResult.BibToIdentifier(bib))];
                                    }
                                    // Check if this person has already finished in this hour.
                                    if (tmpRes.end != null)
                                    {
                                        read.Status = Constants.Timing.CHIPREAD_STATUS_WITHINIGN;
                                    }
                                    else
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
                                        // Get the minimum number of seconds we want to enforce between start time for a loop and finish time
                                        // Start with 0 because they may not have a start time for one reason or another
                                        long minSeconds = 0;
                                        long minMilliseconds = 0;
                                        if (lastReadDictionary.ContainsKey((bib, read.LocationID)))
                                        {
                                            occurrence = lastReadDictionary[(bib, read.LocationID)].Occurrence + 1;
                                            minSeconds = lastReadDictionary[(bib, read.LocationID)].Read.TimeSeconds % 3600 + occursWithin;
                                            minMilliseconds = lastReadDictionary[(bib, read.LocationID)].Read.Milliseconds;
                                        }
                                        if (Constants.Timing.LOCATION_FINISH == read.LocationID && tmpRes.start != null)
                                        {
                                            // get rid of the hours from the result time
                                            minSeconds = tmpRes.start.ChipSeconds % 3600 + occursWithin;
                                            minMilliseconds = tmpRes.start.ChipMilliseconds;
                                        }
                                        // Check if we're within the ignore period
                                        if (secondsNoHour < minSeconds || (secondsNoHour == minSeconds && millisecDiff <= minMilliseconds))
                                        {
                                            // and set it to ignore it
                                            read.Status = Constants.Timing.CHIPREAD_STATUS_WITHINIGN;
                                        }
                                        else
                                        {
                                            ChipRead lastRead = lastReadDictionary.ContainsKey((bib, read.LocationID)) ? lastReadDictionary[(bib, read.LocationID)].Read : null;
                                            lastReadDictionary[(bib, read.LocationID)] = (read, occurrence);
                                            long chipSecDiff = read.TimeSeconds - (lastRead == null ? hour * 3600 : lastRead.Seconds);
                                            int chipMillisecDiff = read.TimeMilliseconds - (lastRead == null ? 0 : lastRead.Milliseconds);
                                            if (chipMillisecDiff < 0)
                                            {
                                                chipSecDiff--;
                                                chipMillisecDiff += 1000;
                                            }
                                            // Check that we're not adding a time for a DNF person, we can use any other times
                                            // for information for that person.
                                            if (!dnfDictionary.ContainsKey(bib))
                                            {
                                                TimeResult newResult = new TimeResult(theEvent.Identifier,
                                                    read.ReadId,
                                                    part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                                                    read.LocationID,
                                                    Constants.Timing.SEGMENT_NONE,
                                                    occurrence,
                                                    Constants.Timing.ToTime(secondsDiff, millisecDiff),
                                                    TimeResult.BibToIdentifier(bib),
                                                    Constants.Timing.ToTime(chipSecDiff, chipMillisecDiff),
                                                    read.Time,
                                                    bib,
                                                    Constants.Timing.TIMERESULT_STATUS_NONE
                                                    );
                                                // Check if we're the finish location and set our finish time and occurence if so.
                                                if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                                                {
                                                    if (occurrence != hour * 2 + 1)
                                                    {
                                                        Log.E("Timing.Routimes.BackyardUltraRoutine", "Something went wrong and occurrence was not correct.");
                                                        newResult.Occurrence = hour * 2 + 1;
                                                    }
                                                    tmpRes.end = newResult;
                                                    backyardResultDictionary[(hour, TimeResult.BibToIdentifier(bib))] = tmpRes;
                                                }
                                                newResults.Add(newResult);
                                                if (part != null)
                                                {
                                                    // If they were marked as noshow previously, mark them as started
                                                    if (Constants.Timing.EVENTSPECIFIC_NOSHOW == part.Status
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
            // Process reads that don't have an associated bib but do have a chip.
            foreach (string chip in chipReadPairs.Keys)
            {
                long startSeconds, maxStartSeconds;
                int startMilliseconds;
                (startSeconds, startMilliseconds) = dictionary.distanceStartDict[0];
                maxStartSeconds = startSeconds + theEvent.StartWindow;// Go through each chipread
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
                            if (!dnfHourDictionary.ContainsKey(TimeResult.ChipToIdentifier(chip)) || dnfHourDictionary[TimeResult.ChipToIdentifier(chip)] > hour)
                            {
                                // Check if we're at the starting point and within a starting window
                                if ((Constants.Timing.LOCATION_START == read.LocationID || (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish))
                                    && (secondsNoHour < theEvent.StartWindow || (secondsNoHour == theEvent.StartWindow && millisecDiff == 0)))
                                {
                                    // check for a stored start chipread with the correct occurence (hour start)
                                    if (chipLastReadDictionary.ContainsKey((chip, read.LocationID)) && chipLastReadDictionary[(chip, read.LocationID)].Occurrence == (hour * 2))
                                    {
                                        chipLastReadDictionary[(chip, read.LocationID)].Read.Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                                    }
                                    // Update the last read we've seen at this location
                                    chipLastReadDictionary[(chip, read.LocationID)] = (Read: read, Occurrence: hour * 2);
                                    // check for start results in our list that we're pushing to the database and remove it if it is there
                                    TimeResult startResult = null;
                                    if (backyardResultDictionary.ContainsKey((hour, TimeResult.ChipToIdentifier(chip))))
                                    {
                                        startResult = backyardResultDictionary[(hour, TimeResult.ChipToIdentifier(chip))].start;
                                    }
                                    if (startResult != null && newResults.Contains(startResult))
                                    {
                                        newResults.Remove(startResult);
                                    }
                                    // Create a result for the start value.
                                    startResult = new TimeResult(theEvent.Identifier,
                                        read.ReadId,
                                        Constants.Timing.TIMERESULT_DUMMYPERSON,
                                        read.LocationID,
                                        hour == 0 ? Constants.Timing.SEGMENT_START : Constants.Timing.SEGMENT_NONE,
                                        hour * 2, // start reads are always set at their hour * 2 for occurence (0, 2, 4, 6, etc)
                                        Constants.Timing.ToTime(secondsDiff, millisecDiff),
                                        TimeResult.ChipToIdentifier(chip),
                                        "0:00:00.000",
                                        read.Time,
                                        read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib,
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
                                    // Finally, set the chipread status to STARTTIME
                                    read.Status = Constants.Timing.CHIPREAD_STATUS_STARTTIME;
                                }
                                // Possible reads at this point:
                                //      Reads at the start not within the StartWindow (IGNORE)
                                //      Start/Finish Location reads past the StartWindow (Valid Reads)
                                //          These could be BEFORE or AFTER the last occurrence at this spot
                                //      Reads at any other location
                                else if (Constants.Timing.LOCATION_FINISH != read.LocationID)
                                {
                                    // find the hour results
                                    (TimeResult start, TimeResult end) tmpRes = (null, null);
                                    if (backyardResultDictionary.ContainsKey((hour, TimeResult.ChipToIdentifier(chip))))
                                    {
                                        tmpRes = backyardResultDictionary[(hour, TimeResult.ChipToIdentifier(chip))];
                                    }
                                    // Check if this person has already finished in this hour.
                                    if (tmpRes.end != null)
                                    {
                                        read.Status = Constants.Timing.CHIPREAD_STATUS_WITHINIGN;
                                    }
                                    else
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
                                        // Get the minimum number of seconds we want to enforce between start time for a loop and finish time
                                        // Start with 0 because they may not have a start time for one reason or another
                                        // Make sure to remove the hour portion of the last read chip time
                                        long minSeconds = 0;
                                        long minMilliseconds = 0;
                                        if (chipLastReadDictionary.ContainsKey((chip, read.LocationID)))
                                        {
                                            occurrence = chipLastReadDictionary[(chip, read.LocationID)].Occurrence + 1;
                                            minSeconds = chipLastReadDictionary[(chip, read.LocationID)].Read.TimeSeconds % 3600 + occursWithin;
                                            minMilliseconds = chipLastReadDictionary[(chip, read.LocationID)].Read.Milliseconds;
                                        }
                                        if (Constants.Timing.LOCATION_FINISH == read.LocationID && tmpRes.start != null)
                                        {
                                            // get rid of the hours from the result time
                                            minSeconds = tmpRes.start.ChipSeconds % 3600 + occursWithin;
                                            minMilliseconds = tmpRes.start.ChipMilliseconds;
                                        }
                                        // Check if we're within the ignore period
                                        if (secondsNoHour < minSeconds || (secondsNoHour == minSeconds && millisecDiff <= minMilliseconds))
                                        {
                                            // and set it to ignore it
                                            read.Status = Constants.Timing.CHIPREAD_STATUS_WITHINIGN;
                                        }
                                        else
                                        {
                                            ChipRead lastRead = chipLastReadDictionary.ContainsKey((chip, read.LocationID)) ? chipLastReadDictionary[(chip, read.LocationID)].Read : null;
                                            chipLastReadDictionary[(chip, read.LocationID)] = (read, occurrence);
                                            long chipSecDiff = read.TimeSeconds - (lastRead == null ? hour * 3600 : lastRead.Seconds);
                                            int chipMillisecDiff = read.TimeMilliseconds - (lastRead == null ? 0 : lastRead.Milliseconds);
                                            if (chipMillisecDiff < 0)
                                            {
                                                chipSecDiff--;
                                                chipMillisecDiff += 1000;
                                            }
                                            // Check that we're not adding a time for a DNF person, we can use any other times
                                            // for information for that person.
                                            if (!chipDnfDictionary.ContainsKey(chip))
                                            {
                                                TimeResult newResult = new TimeResult(theEvent.Identifier,
                                                    read.ReadId,
                                                    Constants.Timing.TIMERESULT_DUMMYPERSON,
                                                    read.LocationID,
                                                    Constants.Timing.SEGMENT_NONE,
                                                    occurrence,
                                                    Constants.Timing.ToTime(secondsDiff, millisecDiff),
                                                    TimeResult.ChipToIdentifier(chip),
                                                    Constants.Timing.ToTime(chipSecDiff, chipMillisecDiff),
                                                    read.Time,
                                                    read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib,
                                                    Constants.Timing.TIMERESULT_STATUS_NONE
                                                    );
                                                // Check if we're the finish location and set our finish time and occurence if so.
                                                if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                                                {
                                                    if (occurrence != hour * 2 + 1)
                                                    {
                                                        Log.E("Timing.Routimes.BackyardUltraRoutine", "Something went wrong and occurrence was not correct.");
                                                        newResult.Occurrence = hour * 2 + 1;
                                                    }
                                                    tmpRes.end = newResult;
                                                    backyardResultDictionary[(hour, TimeResult.ChipToIdentifier(chip))] = tmpRes;
                                                }
                                                newResults.Add(newResult);
                                            }
                                            read.Status = Constants.Timing.CHIPREAD_STATUS_USED;
                                        }
                                    }
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
            // Process the intersection of unknown DNF people and Finish results:
            foreach (string chip in chipDnfDictionary.Keys)
            {
                ChipRead read = chipDnfDictionary[chip];
                long startSeconds = dictionary.distanceStartDict[0].Seconds;
                int startMilliseconds = dictionary.distanceStartDict[0].Milliseconds;
                long secondsDiff = read.TimeSeconds - startSeconds;
                int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                if (millisecDiff < 0)
                {
                    secondsDiff--;
                }
                // Calculate the hour
                int hour = (int)(secondsDiff / 3600);
                if (backyardResultDictionary.ContainsKey((hour, TimeResult.ChipToIdentifier(chip))))
                {
                    TimeResult finish = backyardResultDictionary[(hour, TimeResult.ChipToIdentifier(chip))].end;
                    if (newResults.Contains(finish))
                    {
                        newResults.Remove(finish);
                    }
                    finish.ReadId = read.ReadId;
                    finish.Time = "DNF";
                    finish.ChipTime = "DNF";
                    finish.Status = Constants.Timing.TIMERESULT_STATUS_DNF;
                    newResults.Add(finish);
                }
                else
                {
                    newResults.Add(new TimeResult(theEvent.Identifier,
                        read.ReadId,
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
            // Process the intersection of known DNF people and finish results.
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
                int occurrence = -1;
                if (lastReadDictionary.ContainsKey((bib, Constants.Timing.LOCATION_FINISH)))
                {
                    occurrence = lastReadDictionary[(bib, Constants.Timing.LOCATION_FINISH)].Occurrence + 1;
                }
                ChipRead read = dnfDictionary[bib];
                long startSeconds = dictionary.distanceStartDict[0].Seconds;
                int startMilliseconds = dictionary.distanceStartDict[0].Milliseconds;
                long secondsDiff = read.TimeSeconds - startSeconds;
                int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                if (millisecDiff < 0)
                {
                    secondsDiff--;
                }
                // Calculate the hour
                int hour = (int)(secondsDiff / 3600);
                if (backyardResultDictionary.ContainsKey((hour, TimeResult.BibToIdentifier(bib))))
                {
                    TimeResult finish = backyardResultDictionary[(hour, TimeResult.BibToIdentifier(bib))].end;
                    if (newResults.Contains(finish))
                    {
                        newResults.Remove(finish);
                    }
                    finish.ReadId = read.ReadId;
                    finish.Time = "DNF";
                    finish.ChipTime = "DNF";
                    finish.Status = Constants.Timing.TIMERESULT_STATUS_DNF;
                    newResults.Add(finish);
                }
                else
                {
                    newResults.Add(new TimeResult(theEvent.Identifier,
                        read.ReadId,
                        Constants.Timing.TIMERESULT_DUMMYPERSON,
                        Constants.Timing.LOCATION_FINISH,
                        Constants.Timing.SEGMENT_FINISH,
                        lastReadDictionary.ContainsKey((bib, Constants.Timing.LOCATION_FINISH)) ? lastReadDictionary[(bib, Constants.Timing.LOCATION_FINISH)].Occurrence + 1 : 1,
                        "DNF",
                        TimeResult.BibToIdentifier(bib),
                        "DNF",
                        dnfDictionary[bib].Time,
                        dnfDictionary[bib].ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? dnfDictionary[bib].ReadBib : dnfDictionary[bib].ChipBib,
                        Constants.Timing.TIMERESULT_STATUS_DNF
                        ));
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


        public static List<TimeResult> ProcessPlacements(Event theEvent, IDBInterface database, TimingDictionary dictionary)
        {
            return null;
        }
    }
}
