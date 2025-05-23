using Chronokeep.Interfaces;
using Chronokeep.Objects;
using DocumentFormat.OpenXml.Drawing;
using System;
using System.Collections.Generic;

namespace Chronokeep.Timing.Routines
{
    internal class BackyardUltraRoutine
    {

        // Process chip reads
        public static List<TimeResult> ProcessRace(Event theEvent, IDBInterface database, TimingDictionary dictionary, IMainWindow window)
        {
            Log.D("Timing.Routines.BackyardUltraRoutine", "Processing chip reads for a backyard ultra.");
            // Pre-process information we'll need to fully process chip reads
            // Create a dictionary for hour starts and ends. (hour, identifier)
            Dictionary<(int, string), (TimeResult start, TimeResult end)> backyardResultDictionary = [];
            // The initial start times will always be hour 0 start times.
            foreach (TimeResult result in database.GetStartTimes(theEvent.Identifier))
            {
                // odd occurrences are going to be starts, divide by two to get the hour value
                backyardResultDictionary[(result.Occurrence / 2, result.Identifier)] = (start: result, end: null);
            }
            // Dictionary of timeresults for a specific identifier
            Dictionary<string, List<TimeResult>> finishTimes = [];
            List<TimeResult> toRemove = [];
            List<TimeResult> toAdd = [];
            // Get the rest of the times.
            foreach (TimeResult result in database.GetFinishTimes(theEvent.Identifier))
            {
                // Keep track of all finish times based upon an identifier.
                if (!finishTimes.TryGetValue(result.Identifier, out List<TimeResult> fTimes))
                {
                    fTimes = [];
                    finishTimes[result.Identifier] = fTimes;
                }
                fTimes.Add(result);
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
                        toRemove.Add(tmpRes.start);
                    }
                    tmpRes.start = result;
                }
                // If end time
                else if (result.Occurrence % 2 == 1)
                {
                    if (tmpRes.end == null)
                    {
                        tmpRes.end = result;
                    }
                    else
                    {
                        Log.E("Timing.Routines.BackyardUltraRoutine", "Found a duplicate end time for an hour.");
                        toRemove.Add(result);
                    }
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
            Dictionary<string, List<ChipRead>> bibReadPairs = [];
            Dictionary<string, List<ChipRead>> chipReadPairs = [];
            // Make sure we keep track of the
            // last occurrence for a person at a specific location.
            // (Bib, Location), Last Chip Read
            Dictionary<(string, int), (ChipRead Read, int Occurrence)> bibLastReadDictionary = [];
            Dictionary<(string, int), (ChipRead Read, int Occurrence)> chipLastReadDictionary = [];
            // Keep a list of DNF participants so we can mark them as DNF in results.
            // Keep a record of the DNF chipread so we can link it with the TimeResult.
            Dictionary<string, int> dnfHourDictionary = [];
            Dictionary<string, ChipRead> bibDNFDictionary = [];
            Dictionary<string, ChipRead> chipDnfDictionary = [];
            // Keep a list of DNS participants so we can mark them as DNS in results.
            // Keep a record of the DNS chipread so we can link it with the TimeResult
            Dictionary<string, ChipRead> bibDNSDictionary = [];
            Dictionary<string, ChipRead> chipDNSDictionary = [];

            // Get all useful chipreads.
            List<ChipRead> allChipReads = database.GetUsefulChipReads(theEvent.Identifier);
            allChipReads.Sort();
            List<ChipRead> setUnknown = [];

            // Get some variables to check if we need to sound an alarm.
            // Get a time value to check to ensure the chip read isn't too far in the past.
            DateTime before = DateTime.Now.AddMinutes(-5);
            (Dictionary<string, Alarm> bibAlarms, Dictionary<string, Alarm> chipAlarms) = Alarm.GetAlarmDictionaries();

            // Sort chipreads into proper piles.
            foreach (ChipRead read in allChipReads)
            {
                // This is a start time for a loop
                // Calculate the hour for getting the correct ocurrence.
                long startSeconds = dictionary.distanceStartDict[0].Seconds;
                int startMilliseconds = dictionary.distanceStartDict[0].Milliseconds;
                long secondsDiff = read.TimeSeconds - startSeconds;
                int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                if (millisecDiff < 0)
                {
                    secondsDiff--;
                }
                int hour = (int)(secondsDiff / 3600);
                // Check to set off an alarm.
                if (read.Time > before)
                {
                    // Bib set on the read, alarm exists and it hasn't went off.
                    if (read.Bib != Constants.Timing.CHIPREAD_DUMMYBIB
                        && bibAlarms.ContainsKey(read.Bib)
                        && bibAlarms[read.Bib].Enabled)
                    {
                        window.NotifyAlarm(read.Bib, "");
                    }
                    // Bib not set, chip is set, alarm exists and it hasn't went off.
                    else if (read.ChipNumber != Constants.Timing.CHIPREAD_DUMMYCHIP
                        && chipAlarms.ContainsKey(read.ChipNumber)
                        && chipAlarms[read.ChipNumber].Enabled)
                    {
                        window.NotifyAlarm("", read.ChipNumber);
                    }
                }
                // Process reads with known bib numbers.
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
                        else
                        {
                            if (!bibDNSDictionary.ContainsKey(read.Bib))
                            {
                                bibDNSDictionary.Add(read.Bib, read);
                            }
                        }
                    }
                    // if we process all the used reads before putting them in the list
                    // we can ensure that all of the reads we process are STATUS_NONE
                    // and then we can verify that we aren't inserting results BEFORE
                    // results we've already calculated
                    // Check if its a read we've used for a finish read.
                    else if (Constants.Timing.CHIPREAD_STATUS_USED == read.Status)
                    {
                        bibLastReadDictionary[(read.Bib, read.LocationID)] = (read, (hour * 2) + 1);
                    }
                    // Otherwise if its a start read at the proper location.
                    else if (Constants.Timing.CHIPREAD_STATUS_STARTTIME ==  read.Status &&
                        (Constants.Timing.LOCATION_START == read.LocationID ||
                        (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish)))
                    {
                        // This is a start time for a loop
                        bibLastReadDictionary[(read.Bib, read.LocationID)] = (read, hour * 2);
                    }
                    // If its a DNF read
                    else if (Constants.Timing.CHIPREAD_STATUS_DNF == read.Status)
                    {
                        bibDNFDictionary[read.Bib] = read;
                        dnfHourDictionary[TimeResult.BibToIdentifier(read.Bib)] = hour;
                    }
                    else
                    {
                        if (!bibReadPairs.TryGetValue(read.Bib, out List<ChipRead> readPairs))
                        {
                            readPairs = new List<ChipRead>();
                            bibReadPairs[read.Bib] = readPairs;
                        }

                        readPairs.Add(read);
                    }
                }
                // Process reads with unknown bib numbers but known chip numbers.
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
                        else
                        {
                            if (!chipDNSDictionary.ContainsKey(read.ChipNumber))
                            {
                                chipDNSDictionary.Add(read.ChipNumber, read);
                            }
                        }
                    }
                    // Otherwise check the status and everything as we did for Bib reads.
                    else if (Constants.Timing.CHIPREAD_STATUS_USED == read.Status)
                    {
                        chipLastReadDictionary[(read.ChipNumber.ToString(), read.LocationID)] = (read, (hour * 2) + 1);
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_STARTTIME == read.Status &&
                        (Constants.Timing.LOCATION_START == read.LocationID ||
                        (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish)))
                    {
                        chipLastReadDictionary[(read.ChipNumber.ToString(), read.LocationID)] = (read, hour * 2);
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_DNF == read.Status)
                    {
                        chipDnfDictionary[read.ChipNumber] = read;
                        dnfHourDictionary[TimeResult.ChipToIdentifier(read.ChipNumber)] = hour;
                    }
                    else
                    {
                        if (!chipReadPairs.TryGetValue(read.ChipNumber, out List<ChipRead> readPairs))
                        {
                            readPairs = new List<ChipRead>();
                            chipReadPairs[read.ChipNumber] = readPairs;
                        }
                        readPairs.Add(read);
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
            List<TimeResult> newResults = [];
            // Keep a list of participants to update.
            HashSet<Participant> updateParticipants = [];
            // Process reads that have a bib
            foreach (string bib in bibReadPairs.Keys)
            {
                Participant part = dictionary.participantBibDictionary.TryGetValue(bib, out Participant value) ? value : null;
                Distance d = part != null ? dictionary.distanceDictionary[part.EventSpecific.DistanceIdentifier] : null;
                // Go through each chipread
                foreach (ChipRead read in bibReadPairs[bib])
                {
                    // Calculate the hour for getting the correct ocurrence.
                    long startSeconds = dictionary.distanceStartDict[0].Seconds;
                    int startMilliseconds = dictionary.distanceStartDict[0].Milliseconds;
                    long secondsDiff = read.TimeSeconds - startSeconds;
                    int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                    if (millisecDiff < 0)
                    {
                        secondsDiff--;
                    }
                    int hour = (int)(secondsDiff / 3600);
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
                            long secondsNoHour = secondsDiff % 3600;
                            // Check if we've already included them in the DNF pile and we're past the hour when they DNF'ed.
                            // Process the reads if this isn't the case.
                            if (!dnfHourDictionary.ContainsKey(TimeResult.BibToIdentifier(bib)) || dnfHourDictionary[TimeResult.BibToIdentifier(bib)] > hour)
                            {
                                // Check if we're at the starting point and within a starting window
                                if ((Constants.Timing.LOCATION_START == read.LocationID || (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish))
                                    && (secondsNoHour < theEvent.StartWindow || (secondsNoHour == startSeconds && millisecDiff <= startMilliseconds)))
                                {
                                    // check for a stored start chipread with the correct occurence (hour start)
                                    if (bibLastReadDictionary.ContainsKey((bib, read.LocationID)) && bibLastReadDictionary[(bib, read.LocationID)].Occurrence == (hour * 2))
                                    {
                                        bibLastReadDictionary[(bib, read.LocationID)].Read.Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                                    }
                                    // Update the last read we've seen at this location
                                    bibLastReadDictionary[(bib, read.LocationID)] = (Read: read, Occurrence: hour * 2);
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
                                        Constants.Timing.SEGMENT_START,
                                        hour * 2, // start reads are always set at their hour * 2 for occurence (0, 2, 4, 6, etc)
                                        Constants.Timing.ToTime(secondsNoHour, millisecDiff),
                                        TimeResult.BibToIdentifier(bib),
                                        "0:00:00.000",
                                        read.Time,
                                        bib,
                                        Constants.Timing.TIMERESULT_STATUS_NONE,
                                        part == null ? "" : part.EventSpecific.Division
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
                                        (Constants.Timing.EVENTSPECIFIC_UNKNOWN == part.Status
                                        && !bibDNFDictionary.ContainsKey(bib)))
                                    {
                                        part.Status = Constants.Timing.EVENTSPECIFIC_STARTED;
                                        updateParticipants.Add(part);
                                    }
                                    // Finally, set the chipread status to STARTTIME
                                    read.Status = Constants.Timing.CHIPREAD_STATUS_STARTTIME;
                                }
                                // Possible reads at this point:
                                //      Reads at the start not within the StartWindow (IGNORE)
                                //      Finish Location reads past the StartWindow (Valid Reads)
                                //          These could be BEFORE or AFTER the last occurrence at this spot
                                //      Reads at any other location
                                else if (Constants.Timing.LOCATION_FINISH == read.LocationID)
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
                                    // Otherwise THIS is (potentially) a finish.
                                    else
                                    {
                                        bibLastReadDictionary[(bib, read.LocationID)] = (read, (hour * 2) + 1);
                                        long chipSecDiff = secondsNoHour - (tmpRes.start == null ? 0 : tmpRes.start.Seconds);
                                        int chipMillisecDiff = read.TimeMilliseconds - (tmpRes.start == null ? 0 : tmpRes.start.Milliseconds);
                                        if (chipMillisecDiff < 0)
                                        {
                                            chipSecDiff--;
                                            chipMillisecDiff += 1000;
                                        }
                                        // Check that we're not adding a time for a DNF person, we can use any other times
                                        // for information for that person.
                                        if (!bibDNFDictionary.ContainsKey(bib))
                                        {
                                            TimeResult newResult = new(theEvent.Identifier,
                                                read.ReadId,
                                                part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                                                read.LocationID,
                                                Constants.Timing.SEGMENT_FINISH,
                                                (hour * 2) + 1,
                                                Constants.Timing.ToTime(secondsNoHour, millisecDiff),
                                                TimeResult.BibToIdentifier(bib),
                                                Constants.Timing.ToTime(chipSecDiff, chipMillisecDiff),
                                                read.Time,
                                                bib,
                                                Constants.Timing.TIMERESULT_STATUS_NONE,
                                                part == null ? "" : part.EventSpecific.Division
                                                );
                                            tmpRes.end = newResult;
                                            backyardResultDictionary[(hour, TimeResult.BibToIdentifier(bib))] = tmpRes;
                                            newResults.Add(newResult);
                                            if (part != null)
                                            {
                                                // If they were marked as noshow previously, mark them as started
                                                if (Constants.Timing.EVENTSPECIFIC_UNKNOWN == part.Status
                                                    && !bibDNFDictionary.ContainsKey(bib))
                                                {
                                                    part.Status = Constants.Timing.EVENTSPECIFIC_STARTED;
                                                    updateParticipants.Add(part);
                                                }
                                            }
                                            read.Status = Constants.Timing.CHIPREAD_STATUS_USED;
                                        }
                                    }
                                }
                                // Possible reads at this point:
                                //      Start location reads not within a start window...
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
                                    // Otherwise assume this could be a result.
                                    else
                                    {
                                        int occurrence = 1;
                                        int ignoreWithin = 0;
                                        if (dictionary.locationDictionary.TryGetValue(read.LocationID, out TimingLocation loc))
                                        {
                                            ignoreWithin = loc.IgnoreWithin;
                                        }
                                        // Get the minimum number of seconds we want to enforce between sightings at a spot
                                        // Start with 0 because they may not have a start time for one reason or another
                                        long minSeconds = 0;
                                        long minMilliseconds = 0;
                                        if (bibLastReadDictionary.ContainsKey((bib, read.LocationID)))
                                        {
                                            occurrence = bibLastReadDictionary[(bib, read.LocationID)].Occurrence + 1;
                                            minSeconds = bibLastReadDictionary[(bib, read.LocationID)].Read.TimeSeconds + ignoreWithin;
                                            minMilliseconds = bibLastReadDictionary[(bib, read.LocationID)].Read.Milliseconds;
                                        }
                                        // Check if we're within the ignore period
                                        if (read.TimeSeconds < minSeconds || (read.TimeSeconds == minSeconds && millisecDiff <= minMilliseconds))
                                        {
                                            // and set it to ignore it
                                            read.Status = Constants.Timing.CHIPREAD_STATUS_WITHINIGN;
                                        }
                                        else
                                        {
                                            ChipRead lastRead = bibLastReadDictionary.ContainsKey((bib, read.LocationID)) ? bibLastReadDictionary[(bib, read.LocationID)].Read : null;
                                            bibLastReadDictionary[(bib, read.LocationID)] = (read, occurrence);
                                            long chipSecDiff = secondsNoHour - (tmpRes.start == null ? 0 : tmpRes.start.Seconds);
                                            int chipMillisecDiff = read.TimeMilliseconds - (tmpRes.start == null ? 0 : tmpRes.start.Milliseconds);
                                            if (chipMillisecDiff < 0)
                                            {
                                                chipSecDiff--;
                                                chipMillisecDiff += 1000;
                                            }
                                            // Check that we're not adding a time for a DNF person, we can use any other times
                                            // for information for that person.
                                            if (!bibDNFDictionary.ContainsKey(bib))
                                            {
                                                TimeResult newResult = new TimeResult(theEvent.Identifier,
                                                    read.ReadId,
                                                    part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                                                    read.LocationID,
                                                    Constants.Timing.SEGMENT_NONE,
                                                    occurrence,
                                                    Constants.Timing.ToTime(secondsNoHour, millisecDiff),
                                                    TimeResult.BibToIdentifier(bib),
                                                    Constants.Timing.ToTime(chipSecDiff, chipMillisecDiff),
                                                    read.Time,
                                                    bib,
                                                    Constants.Timing.TIMERESULT_STATUS_NONE,
                                                    part == null ? "" : part.EventSpecific.Division
                                                    );
                                                newResults.Add(newResult);
                                                if (part != null)
                                                {
                                                    // If they were marked as noshow previously, mark them as started
                                                    if (Constants.Timing.EVENTSPECIFIC_UNKNOWN == part.Status
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
                foreach (ChipRead read in chipReadPairs[chip])
                {
                    // Calculate the hour for getting the correct ocurrence.
                    long startSeconds = dictionary.distanceStartDict[0].Seconds;
                    int startMilliseconds = dictionary.distanceStartDict[0].Milliseconds;
                    long secondsDiff = read.TimeSeconds - startSeconds;
                    int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                    if (millisecDiff < 0)
                    {
                        secondsDiff--;
                    }
                    int hour = (int)(secondsDiff / 3600);
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
                            long secondsNoHour = secondsDiff % 3600;
                            // Check if we've already included them in the DNF pile and we're past the hour when they DNF'ed.
                            // Process the reads if this isn't the case.
                            if (!dnfHourDictionary.ContainsKey(TimeResult.ChipToIdentifier(chip)) && (!dnfHourDictionary.ContainsKey(TimeResult.ChipToIdentifier(chip)) || dnfHourDictionary[TimeResult.ChipToIdentifier(chip)] > hour))
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
                                        Constants.Timing.SEGMENT_START,
                                        hour * 2, // start reads are always set at their hour * 2 for occurence (0, 2, 4, 6, etc)
                                        Constants.Timing.ToTime(secondsNoHour, millisecDiff),
                                        TimeResult.ChipToIdentifier(chip),
                                        "0:00:00.000",
                                        read.Time,
                                        read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib,
                                        Constants.Timing.TIMERESULT_STATUS_NONE,
                                        ""
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
                                else if (Constants.Timing.LOCATION_FINISH == read.LocationID)
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
                                        chipLastReadDictionary[(chip, read.LocationID)] = (read, (hour * 2) + 1);
                                        long chipSecDiff = secondsNoHour - (tmpRes.start == null ? 0 : tmpRes.start.Seconds);
                                        int chipMillisecDiff = read.TimeMilliseconds - (tmpRes.start == null ? 0 : tmpRes.start.Milliseconds);
                                        if (chipMillisecDiff < 0)
                                        {
                                            chipSecDiff--;
                                            chipMillisecDiff += 1000;
                                        }
                                        // Check that we're not adding a time for a DNF person, we can use any other times
                                        // for information for that person.
                                        if (!chipDnfDictionary.ContainsKey(chip))
                                        {
                                            TimeResult newResult = new(theEvent.Identifier,
                                                read.ReadId,
                                                Constants.Timing.TIMERESULT_DUMMYPERSON,
                                                read.LocationID,
                                                Constants.Timing.SEGMENT_FINISH,
                                                (hour * 2) + 1,
                                                Constants.Timing.ToTime(secondsNoHour, millisecDiff),
                                                TimeResult.ChipToIdentifier(chip),
                                                Constants.Timing.ToTime(chipSecDiff, chipMillisecDiff),
                                                read.Time,
                                                read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib,
                                                Constants.Timing.TIMERESULT_STATUS_NONE,
                                                ""
                                                );
                                            tmpRes.end = newResult;
                                            backyardResultDictionary[(hour, TimeResult.ChipToIdentifier(chip))] = tmpRes;
                                            newResults.Add(newResult);
                                        }
                                        read.Status = Constants.Timing.CHIPREAD_STATUS_USED;
                                    }
                                }
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
                                        int ignoreWithin = 0;
                                        if (dictionary.locationDictionary.TryGetValue(read.LocationID, out TimingLocation loc))
                                        {
                                            ignoreWithin = loc.IgnoreWithin;
                                        }
                                        // Get the minimum number of seconds we want to enforce between start time for a loop and finish time
                                        // Start with 0 because they may not have a start time for one reason or another
                                        // Make sure to remove the hour portion of the last read chip time
                                        long minSeconds = 0;
                                        long minMilliseconds = 0;
                                        if (chipLastReadDictionary.ContainsKey((chip, read.LocationID)))
                                        {
                                            occurrence = chipLastReadDictionary[(chip, read.LocationID)].Occurrence + 1;
                                            minSeconds = chipLastReadDictionary[(chip, read.LocationID)].Read.TimeSeconds + ignoreWithin;
                                            minMilliseconds = chipLastReadDictionary[(chip, read.LocationID)].Read.Milliseconds;
                                        }
                                        // Check if we're within the ignore period
                                        if (read.TimeSeconds < minSeconds || (read.TimeSeconds == minSeconds && millisecDiff <= minMilliseconds))
                                        {
                                            // and set it to ignore it
                                            read.Status = Constants.Timing.CHIPREAD_STATUS_WITHINIGN;
                                        }
                                        else
                                        {
                                            chipLastReadDictionary[(chip, read.LocationID)] = (read, occurrence);
                                            long chipSecDiff = secondsNoHour - (tmpRes.start == null ? 0 : tmpRes.start.Seconds);
                                            int chipMillisecDiff = millisecDiff - (tmpRes.start == null ? 0 : tmpRes.start.Milliseconds);
                                            if (chipMillisecDiff < 0)
                                            {
                                                chipSecDiff--;
                                                chipMillisecDiff += 1000;
                                            }
                                            // Check that we're not adding a time for a DNF person, we can use any other times
                                            // for information for that person.
                                            if (!chipDnfDictionary.ContainsKey(chip))
                                            {
                                                TimeResult newResult = new(theEvent.Identifier,
                                                    read.ReadId,
                                                    Constants.Timing.TIMERESULT_DUMMYPERSON,
                                                    read.LocationID,
                                                    Constants.Timing.SEGMENT_NONE,
                                                    occurrence,
                                                    Constants.Timing.ToTime(secondsNoHour, millisecDiff),
                                                    TimeResult.ChipToIdentifier(chip),
                                                    Constants.Timing.ToTime(chipSecDiff, chipMillisecDiff),
                                                    read.Time,
                                                    read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib,
                                                    Constants.Timing.TIMERESULT_STATUS_NONE,
                                                    ""
                                                    );
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
                        (hour * 2) + 1,
                        "DNF",
                        TimeResult.ChipToIdentifier(chip),
                        "DNF",
                        chipDnfDictionary[chip].Time,
                        chipDnfDictionary[chip].ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? chipDnfDictionary[chip].ReadBib : chipDnfDictionary[chip].ChipBib,
                        Constants.Timing.TIMERESULT_STATUS_DNF,
                        ""
                        ));
                }
            }
            // Process the intersection of known DNF people and finish results.
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
                ChipRead read = bibDNFDictionary[bib];
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
                        (hour * 2) + 1,
                        "DNF",
                        TimeResult.BibToIdentifier(bib),
                        "DNF",
                        bibDNFDictionary[bib].Time,
                        bibDNFDictionary[bib].ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? bibDNFDictionary[bib].ReadBib : bibDNFDictionary[bib].ChipBib,
                        Constants.Timing.TIMERESULT_STATUS_DNF,
                        ""
                        ));
                }
            }
            // Process the intersection of unknown DNS people and Finish results:
            foreach (string chip in chipDNSDictionary.Keys)
            {
                if (finishTimes.ContainsKey(TimeResult.ChipToIdentifier(chip)))
                {
                    foreach (TimeResult finish in finishTimes[TimeResult.ChipToIdentifier(chip)])
                    {
                        finish.ReadId = chipDNSDictionary[chip].ReadId;
                        finish.Time = "DNS";
                        finish.ChipTime = "DNS";
                        finish.Status = Constants.Timing.TIMERESULT_STATUS_DNS;
                        finish.Occurrence = 0;
                        newResults.Add(finish);
                    }
                }
                else
                {
                    newResults.Add(new TimeResult(theEvent.Identifier,
                        chipDNSDictionary[chip].ReadId,
                        Constants.Timing.TIMERESULT_DUMMYPERSON,
                        Constants.Timing.LOCATION_FINISH,
                        Constants.Timing.SEGMENT_FINISH,
                        0,
                        "DNS",
                        TimeResult.ChipToIdentifier(chip),
                        "DNS",
                        chipDNSDictionary[chip].Time,
                        chipDNSDictionary[chip].ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? chipDNSDictionary[chip].ReadBib : chipDNSDictionary[chip].ChipBib,
                        Constants.Timing.TIMERESULT_STATUS_DNS,
                        ""
                        ));
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
                if (finishTimes.ContainsKey(TimeResult.BibToIdentifier(bib)))
                {
                    foreach (TimeResult finish in finishTimes[TimeResult.BibToIdentifier(bib)])
                    {
                        finish.ReadId = bibDNSDictionary[bib].ReadId;
                        finish.Time = "DNS";
                        finish.ChipTime = "DNS";
                        finish.Status = Constants.Timing.TIMERESULT_STATUS_DNS;
                        finish.Occurrence = 0;
                        newResults.Add(finish);
                    }
                }
                else
                {
                    newResults.Add(new TimeResult(theEvent.Identifier,
                        bibDNSDictionary[bib].ReadId,
                        part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                        Constants.Timing.LOCATION_FINISH,
                        Constants.Timing.SEGMENT_FINISH,
                        0,
                        "DNS",
                        TimeResult.BibToIdentifier(bib),
                        "DNS",
                        bibDNSDictionary[bib].Time,
                        bib,
                        Constants.Timing.TIMERESULT_STATUS_DNS,
                        part == null ? "" : part.EventSpecific.Division
                        ));
                }
            }
            // Go through and process every result.
            // Separate results by identifier
            Dictionary<string, List<TimeResult>> resultDictionary = [];
            foreach (TimeResult res in newResults)
            {
                if (!resultDictionary.TryGetValue(res.Identifier, out List<TimeResult> resultsList))
                {
                    resultsList = [];
                    resultDictionary[res.Identifier] = resultsList;
                }
                if (res.LocationId == Constants.Timing.LOCATION_FINISH)
                {
                    resultsList.Add(res);
                }
            }
            Dictionary<string, TimeResult> NewDNFDIctionary = [];
            foreach (string ident in resultDictionary.Keys)
            {
                int lastHourFinished = -1;
                List<TimeResult> results = resultDictionary[ident];
                results.Sort((a, b) => a.Occurrence.CompareTo(b.Occurrence));
                foreach (TimeResult finRes in results)
                {
                    if (finRes.Occurrence / 2 > lastHourFinished + 1)
                    {
                        toRemove.Add(finRes);
                        newResults.Remove(finRes);
                        if (!NewDNFDIctionary.TryGetValue(finRes.Identifier, out TimeResult dnfResult))
                        {
                            dnfResult = new TimeResult(theEvent.Identifier,
                                finRes.ReadId,
                                finRes.EventSpecificId,
                                Constants.Timing.LOCATION_FINISH,
                                Constants.Timing.SEGMENT_FINISH,
                                finRes.Occurrence,
                                "DNF",
                                finRes.Identifier,
                                "DNF",
                                finRes.SystemTime,
                                finRes.Bib,
                                Constants.Timing.TIMERESULT_STATUS_DNF,
                                finRes.Division
                                );
                            newResults.Add(dnfResult);
                        }
                    }
                    else if (finRes.Occurrence / 2 == lastHourFinished + 1)
                    {
                        lastHourFinished++;
                    }
                }
            }
            // process reads that need to be set to ignore
            foreach (ChipRead read in setUnknown)
            {
                read.Status = Constants.Timing.CHIPREAD_STATUS_UNKNOWN;
            }
            // Update database with information.
            foreach (TimeResult tRem in toRemove)
            {
                database.RemoveTimingResult(tRem);
            }
            database.AddTimingResults(newResults);
            database.SetChipReadStatuses(allChipReads);
            database.UpdateParticipants([.. updateParticipants]);
            return newResults;
        }


        public static List<TimeResult> ProcessPlacements(Event theEvent, IDBInterface database, TimingDictionary dictionary)
        {
            List<TimeResult> output = database.GetTimingResults(theEvent.Identifier);
            // Dictionary for keeping track of hourly placements.
            Dictionary<(int, string), (TimeResult start, TimeResult end)> HourlyDictionary = new Dictionary<(int, string), (TimeResult, TimeResult)>();
            // Dictionaries for keeping track of placements.
            Dictionary<string, TimeResult> LastLapDictionary = [];

            // Create a dictionary so we can check if placements have changed. (place, location, occurrence, distance)
            Dictionary<(int, int, int, string), TimeResult> PlacementDictionary = new Dictionary<(int, int, int, string), TimeResult>();
            // Dictionary for converting result identifiers into eventspecific id's.
            Dictionary<string, int> EventSpecificDictionary = [];

            HashSet<string> Finished = [];
            HashSet<string> Participants = [];
            foreach (TimeResult result in output)
            {
                EventSpecificDictionary[result.Identifier] = result.EventSpecificId;
                // This check is to ensure we only flag for upload results whose placements change.
                if (result.Place > 0)
                {
                    PlacementDictionary[(result.Place, result.LocationId, result.Occurrence, result.DistanceName)] = result;
                }
                Participants.Add(result.Identifier);
                if (Constants.Timing.LOCATION_START == result.LocationId || Constants.Timing.LOCATION_FINISH == result.LocationId)
                { /*
                    // finish occurrence
                    if (result.Occurrence % 2 == 1)
                    {
                        (TimeResult start, TimeResult end) time = HourlyDictionary[(result.Occurrence / 2, result.Identifier)];
                        time.end = result;
                        HourlyDictionary[(result.Occurrence / 2, result.Identifier)] = time;
                    }
                    // start occurrence
                    else if (result.Occurrence % 2 == 0)
                    {
                        (TimeResult start, TimeResult end) time = HourlyDictionary[(result.Occurrence / 2, result.Identifier)];
                        time.start = result;
                        HourlyDictionary[(result.Occurrence / 2, result.Identifier)] = time;
                    } */
                }
            }
            // Calculate the current hour.
            int hour = (int)((Constants.Timing.RFIDDateToEpoch(DateTime.Now) - dictionary.distanceStartDict[0].Seconds) / 3600);
            List<TimeResult> invalid = [];
            // Process every hour from the start of the event until we don't have any more finishers for that hour
            for (int i = 0; i <= hour; i++)
            {
                // Check to make sure that every participant finished/started the last hour.
                if (i > 0)
                {
                    foreach (string participant in Participants)
                    {
                        // check if they've started the previous hour
                        if (!HourlyDictionary.ContainsKey((i - 1, participant)))
                        {
                            // they have not, so add them to the finished pile from now on
                            if (EventSpecificDictionary.TryGetValue(participant, out int eventSpecId) && eventSpecId != Constants.Timing.TIMERESULT_DUMMYPERSON)
                            {
                                TimeResult dnsEntry = new(
                                    theEvent.Identifier,
                                    Constants.Timing.TIMERESULT_DUMMYREAD,
                                    eventSpecId,
                                    theEvent.CommonStartFinish ? Constants.Timing.LOCATION_FINISH : Constants.Timing.LOCATION_START,
                                    Constants.Timing.SEGMENT_NONE,
                                    i * 2,
                                    String.Format("{0}:00:01.000", i-1),
                                    participant,
                                    String.Format("{0}:00:01.000", i-1),
                                    DateTime.Now,
                                    dictionary.participantEventSpecificDictionary[eventSpecId].Bib,
                                    Constants.Timing.TIMERESULT_STATUS_DNS,
                                    ""
                                    );
                                database.AddTimingResult(dnsEntry);
                                HourlyDictionary[(i - 1, participant)] = (dnsEntry, null);
                            }
                            Finished.Add(participant);
                        }
                        else
                        {
                            // check to make sure they finished the previous hour
                            (TimeResult start, TimeResult end) = HourlyDictionary[(i - 1, participant)];
                            if (end == null)
                            {
                                // No finish time, so they're done
                                // TODO maybe create a chipread? ensure that we can use a DUMMYREAD id
                                if (EventSpecificDictionary.TryGetValue(participant, out int eventSpecId) && eventSpecId != Constants.Timing.TIMERESULT_DUMMYPERSON)
                                {
                                    TimeResult dnfEntry = new(
                                        theEvent.Identifier,
                                        Constants.Timing.TIMERESULT_DUMMYREAD,
eventSpecId,
                                        theEvent.CommonStartFinish ? Constants.Timing.LOCATION_FINISH : Constants.Timing.LOCATION_START,
                                        Constants.Timing.SEGMENT_NONE,
                                        i * 2,
                                        String.Format("{0}:59:59.000", i-1),
                                        participant,
                                        String.Format("{0}:59:59.000", i-1),
                                        DateTime.Now,
                                        dictionary.participantEventSpecificDictionary[eventSpecId].Bib,
                                        Constants.Timing.TIMERESULT_STATUS_DNS,
                                        ""
                                        );
                                    database.AddTimingResult(dnfEntry);
                                    HourlyDictionary[(i - 1, participant)] = (start, dnfEntry);
                                }
                                Finished.Add(participant);
                            }
                        }
                    }
                }
                bool hourlyFinisher = false;
                foreach (string participant in Participants)
                {
                    // Check if they finished this hour
                    if (HourlyDictionary.ContainsKey((i, participant)))
                    {
                        (TimeResult start, TimeResult end) results = HourlyDictionary[(i, participant)];
                        if (Finished.Contains(participant))
                        {
                            Log.E("Timing.Routines.BackyardUltraRoutine", "Participant has finish/start times when they didn't finish the previous hour.");
                            if (results.start != null)
                            {
                                invalid.Add(results.start);
                            }
                            if (results.end != null)
                            {
                                invalid.Add(results.end);
                            }
                        }
                        if (results.end != null && Constants.Timing.TIMERESULT_STATUS_DNF != results.end.Status)
                        {
                            hourlyFinisher = true;
                            LastLapDictionary[participant] = results.end;
                        }
                    }
                }
                // check if we had an hourly finisher in this hour processing period.
                // since we could run this many hours after finishing, lets make sure we stop processing once we've got past the winner period.
                if (!hourlyFinisher)
                {
                    break;
                }
            }
            List<TimeResult> placementCalculations = new List<TimeResult>(LastLapDictionary.Values);
            placementCalculations.Sort(TimeResult.CompareByOccurrence);
            // Get Dictionaries for storing the last known place (age group, gender)
            // The key is as follows: Division
            Dictionary<string, int> divisionPlaceDictionary = [];
            // The key is as follows: (Age Group ID, Gender)
            Dictionary<(int, string), int> ageGroupPlaceDictionary = [];
            // The key is as follows: Gender
            Dictionary<string, int> genderPlaceDictionary = [];
            int place = 0;
            int ageGroupId;
            string gender, division;
            // Use the sorted list of results to calculate placements
            foreach (TimeResult result in placementCalculations)
            {
                gender = "not specified";
                ageGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
                if (dictionary.participantEventSpecificDictionary.TryGetValue(result.EventSpecificId, out Participant person))
                {
                    gender = person.Gender.ToLower();
                    if (gender.Length < 1)
                    {
                        gender = "not specified";
                    }
                    result.Place = ++place;
                    if (!genderPlaceDictionary.ContainsKey(gender))
                    {
                        genderPlaceDictionary[gender] = 0;
                    }
                    result.GenderPlace = ++genderPlaceDictionary[gender];
                    ageGroupId = person.EventSpecific.AgeGroupId;
                    if (ageGroupId != Constants.Timing.TIMERESULT_DUMMYAGEGROUP)
                    {
                        if (!ageGroupPlaceDictionary.ContainsKey((ageGroupId, gender)))
                        {
                            ageGroupPlaceDictionary[(ageGroupId, gender)] = 0;
                        }
                        result.AgePlace = ++ageGroupPlaceDictionary[(ageGroupId, gender)];
                    }
                    division = person.EventSpecific.Division.ToLower();
                    if (division.Length > 0)
                    {
                        if (!divisionPlaceDictionary.ContainsKey(division))
                        {
                            divisionPlaceDictionary[division] = 0;
                        }
                        result.DivisionPlace = ++divisionPlaceDictionary[division];
                    }
                }
            }
            // Update every result we're outputting with calculated places.
            foreach (TimeResult result in output)
            {
                if (LastLapDictionary.ContainsKey(result.Identifier))
                {
                    TimeResult placeResult = LastLapDictionary[result.Identifier];
                    result.Place = placeResult.Place;
                    result.GenderPlace = placeResult.GenderPlace;
                    result.AgePlace = placeResult.AgePlace;
                }
                // Change any TIMERESULT_STATUS_NONE to TIMERESULT_STATUS_PROCESSED
                if (Constants.Timing.TIMERESULT_STATUS_NONE == result.Status)
                {
                    result.Status = Constants.Timing.TIMERESULT_STATUS_PROCESSED;
                }
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
    }
}
