using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;
using System;
using System.Collections.Generic;

namespace Chronokeep.Timing.Routines
{
    internal class TimeRoutine
    {
        public static List<TimeResult> ProcessRace(Event theEvent, IDBInterface database, TimingDictionary dictionary, IMainWindow window)
        {
            Log.D("Timing.TimingWorker", "Processing chip reads for a time based event.");
            // Check if there's anything to process.
            // Get start TimeREsults
            Dictionary<string, TimeResult> startTimes = [];
            foreach (TimeResult result in database.GetStartTimes(theEvent.Identifier))
            {
                startTimes[result.Identifier] = result;
            }
            // Dictionary of timeresults for a specific identifier
            Dictionary<string, List<TimeResult>> finishTimes = [];
            foreach (TimeResult result in database.GetFinishTimes(theEvent.Identifier))
            {
                if (!finishTimes.TryGetValue(result.Identifier, out List<TimeResult> finResults))
                {
                    finResults = [];
                    finishTimes[result.Identifier] = finResults;
                }
                finResults.Add(result);
            }
            // Get all of the Chip Reads we find useful (Unprocessed, and those used
            // as results.) and sort them into groups based upon Bib, Chip, or put them
            // in the ignore pile if no chip/bib found.
            Dictionary<string, List<ChipRead>> bibReadPairs = [];
            Dictionary<string, List<ChipRead>> chipReadPairs = [];
            // Make sure we keep track of the last occurrence for a person at a location.
            // (Bib, Location), Last Chip Read
            Dictionary<(string, int), (ChipRead Read, int Occurrence)> bibLastReadDictionary = [];
            Dictionary<string, ChipRead> bibStartReadDictionary = [];
            Dictionary<(string, int), (ChipRead Read, int Occurrence)> chipLastReadDictionary = [];
            Dictionary<string, ChipRead> chipStartReadDictionary = [];
            // Keep a list of DNS participants so we can mark them as DNS in results.
            // Keep a record of the DNS chipread so we can link it with the TimeResult
            Dictionary<string, ChipRead> bibDNSDictionary = [];
            Dictionary<string, ChipRead> chipDNSDictionary = [];


            List<ChipRead> allChipReads = database.GetUsefulChipReads(theEvent.Identifier);
            allChipReads.Sort();
            List<ChipRead> setUnknown = [];

            // Get some variables to check if we need to sound an alarm.
            // Get a time value to check to ensure the chip read isn't too far in the past.
            DateTime before = DateTime.Now.AddMinutes(-5);
            (Dictionary<string, Alarm> bibAlarms, Dictionary<string, Alarm> chipAlarms) = Alarm.GetAlarmDictionaries();

            foreach (ChipRead read in allChipReads)
            {
                // Check to set off an alarm.
                if (read.Time > before)
                {
                    // Bib set on the read, alarm exists and it hasn't went off.
                    if (read.Bib != Constants.Timing.CHIPREAD_DUMMYBIB
                        && bibAlarms.TryGetValue(read.Bib, out Alarm bibAlarm)
                        && bibAlarm.Enabled)
                    {
                        window.NotifyAlarm(read.Bib, "");
                    }
                    // Bib not set, chip is set, alarm exists and it hasn't went off.
                    else if (read.ChipNumber != Constants.Timing.CHIPREAD_DUMMYCHIP
                        && chipAlarms.TryGetValue(read.ChipNumber, out Alarm chipAlarm)
                        && chipAlarm.Enabled)
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
                        else
                        {
                            bibDNSDictionary.TryAdd(read.Bib, read);
                        }
                    }
                    // if we process all the used reads before putting them in the list we can
                    // ensure that all of the reads we process are STATUS_NONE and then we can
                    // verify that we aren't inserting results BEFORE results we've already calculated.
                    else if (Constants.Timing.CHIPREAD_STATUS_USED == read.Status)
                    {
                        if (!bibLastReadDictionary.TryGetValue((read.Bib, read.LocationID), out (ChipRead Read, int Occurrence) bLastReads))
                        {
                            bLastReads = (read, 0);
                        }
                        bibLastReadDictionary[(read.Bib, read.LocationID)] = (read, bLastReads.Occurrence + 1);
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_STARTTIME == read.Status && (
                        Constants.Timing.LOCATION_START == read.LocationID ||
                            (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish)))
                    {
                        bibStartReadDictionary[read.Bib] = read;
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_NONE == read.Status || Constants.Timing.CHIPREAD_STATUS_DNF == read.Status)
                    {
                        if (!bibReadPairs.TryGetValue(read.Bib, out List<ChipRead> bReads))
                        {
                            bReads = [];
                            bibReadPairs[read.Bib] = bReads;
                        }

                        bReads.Add(read);
                    }
                }
                else if (Constants.Timing.CHIPREAD_DUMMYCHIP != read.ChipNumber)
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
                            chipDNSDictionary.TryAdd(read.ChipNumber, read);
                        }
                    }
                    // Otherwise check the status and everything as we did for Bib reads.
                    else if (Constants.Timing.CHIPREAD_STATUS_USED == read.Status)
                    {
                        if (!chipLastReadDictionary.TryGetValue((read.ChipNumber.ToString(), read.LocationID), out (ChipRead Read, int Occurrence) cLastReads))
                        {
                            cLastReads = (read, 0);
                        }
                        chipLastReadDictionary[(read.ChipNumber.ToString(), read.LocationID)] = (read, cLastReads.Occurrence + 1);
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_STARTTIME == read.Status
                        && (Constants.Timing.LOCATION_START == read.LocationID ||
                            (Constants.Timing.LOCATION_FINISH == read.LocationID && theEvent.CommonStartFinish)))
                    {
                        chipStartReadDictionary[read.ChipNumber.ToString()] = read;
                    }
                    else if (Constants.Timing.CHIPREAD_STATUS_NONE == read.Status || Constants.Timing.CHIPREAD_STATUS_DNF == read.Status)
                    {
                        if (!chipReadPairs.TryGetValue(read.ChipNumber.ToString(), out List<ChipRead> cReads))
                        {
                            cReads = [];
                        }
                        cReads.Add(read);
                        chipReadPairs[read.ChipNumber.ToString()] = cReads;
                    }
                }
                else
                {
                    setUnknown.Add(read);
                }
            }
            // Go through all of the chipreads we've marked and create new results.
            List<TimeResult> newResults = [];
            List<Participant> updateParticipants = [];
            // start with bibs
            foreach (string bib in bibReadPairs.Keys)
            {
                Participant part = dictionary.participantBibDictionary.TryGetValue(bib, out Participant oPart) ? oPart : null;
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
                if (d == null || !dictionary.distanceStartDict.TryGetValue(d.Identifier, out (long Seconds, int Milliseconds) oStart) || !dictionary.distanceEndDict.TryGetValue(d.Identifier, out (long Seconds, int Milliseconds) oEnd))
                {
                    (startSeconds, startMilliseconds) = dictionary.distanceStartDict[0];
                    endSeconds = dictionary.distanceEndDict[0].Seconds;
                }
                else
                {
                    (startSeconds, startMilliseconds) = oStart;
                    endSeconds = oEnd.Seconds;
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
                            if (bibStartReadDictionary.TryGetValue(bib, out ChipRead oStartRead))
                            {
                                oStartRead.Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                            }
                            // Update the last read we've seen at this location
                            bibStartReadDictionary[bib] = read;
                            if (startResult != null)
                            {
                                newResults.Remove(startResult);
                            }
                            // Create a result for the start time.
                            long secondsDiff = read.TimeSeconds - startSeconds;
                            int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                            while (millisecDiff < 0)
                            {
                                secondsDiff--;
                                millisecDiff += 1000;
                            }
                            while (millisecDiff >= 1000)
                            {
                                secondsDiff++;
                                millisecDiff -= 1000;
                            }
                            startResult = new(theEvent.Identifier,
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
                            else if (dictionary.locationDictionary.TryGetValue(read.LocationID, out TimingLocation oLoc))
                            {
                                occursWithin = oLoc.IgnoreWithin;
                            }
                            // Minimum time to create a result.
                            long minSeconds = startSeconds;
                            int minMilliseconds = startMilliseconds;
                            if (bibLastReadDictionary.TryGetValue((bib, read.LocationID), out (ChipRead Read, int Occurrence) bLastReads))
                            {
                                occurrence = bLastReads.Occurrence + 1;
                                minSeconds = bLastReads.Read.TimeSeconds + occursWithin;
                                minMilliseconds = bLastReads.Read.TimeMilliseconds;
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
                                bibLastReadDictionary[(bib, read.LocationID)] = (read, occurrence);
                                int segId = Constants.Timing.SEGMENT_NONE;
                                // Check for linked distance and set distanceId to the linked distance, or to the actual distance id.
                                // Segments are based on the linked distance.
                                int distanceId = d == null ? 0 : d.LinkedDistance > 0 ? d.LinkedDistance : d.Identifier;
                                // Check for Distance specific segments (Occurrence is always 1 for time based)
                                if (!theEvent.DistanceSpecificSegments && dictionary.segmentDictionary.TryGetValue((Constants.Timing.COMMON_SEGMENTS_DISTANCEID, read.LocationID, 1), out Segment oSeg))
                                {
                                    segId = oSeg.Identifier;
                                }
                                // Distance specific segments
                                else if (d != null && dictionary.segmentDictionary.TryGetValue((distanceId, read.LocationID, 1), out Segment tSeg))
                                {
                                    segId = tSeg.Identifier;
                                }
                                else if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                                {
                                    segId = Constants.Timing.SEGMENT_FINISH;
                                }
                                string identifier = TimeResult.BibToIdentifier(bib);
                                // Create a result for the start value
                                long secondsDiff = read.TimeSeconds - startSeconds;
                                int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                                while (millisecDiff < 0)
                                {
                                    secondsDiff--;
                                    millisecDiff += 1000;
                                }
                                while (millisecDiff >= 1000)
                                {
                                    secondsDiff++;
                                    millisecDiff -= 1000;
                                }
                                bool startExists = startTimes.TryGetValue(identifier, out TimeResult startRes);
                                long chipSecDiff = read.TimeSeconds - (startExists ? Constants.Timing.RFIDDateToEpoch(startRes.SystemTime) : startSeconds);
                                int chipMillisecDiff = read.TimeMilliseconds - (startExists ? startRes.SystemTime.Millisecond : startMilliseconds);
                                while (chipMillisecDiff < 0)
                                {
                                    chipSecDiff--;
                                    chipMillisecDiff += 1000;
                                }
                                while (chipMillisecDiff >= 1000)
                                {
                                    chipSecDiff++;
                                    chipMillisecDiff -= 1000;
                                }
                                newResults.Add(new(theEvent.Identifier,
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
                            if (chipStartReadDictionary.TryGetValue(chip, out ChipRead oStartRead))
                            {
                                oStartRead.Status = Constants.Timing.CHIPREAD_STATUS_UNUSEDSTART;
                            }
                            // Update the last read we've seen at this location
                            chipStartReadDictionary[chip] = read;
                            if (startResult != null)
                            {
                                newResults.Remove(startResult);
                            }
                            // Create a result for the start time.
                            long secondsDiff = read.TimeSeconds - startSeconds;
                            int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                            while (millisecDiff < 0)
                            {
                                secondsDiff--;
                                millisecDiff += 1000;
                            }
                            while (millisecDiff >= 1000)
                            {
                                secondsDiff++;
                                millisecDiff -= 1000;
                            }
                            startResult = new(theEvent.Identifier,
                                read.ReadId,
                                Constants.Timing.TIMERESULT_DUMMYPERSON,
                                read.LocationID,
                                Constants.Timing.SEGMENT_START,
                                0, // start reads are not an occurrence at the start line
                                secondsDiff,
                                millisecDiff,
                                TimeResult.ChipToIdentifier(chip).ToString(),
                                0,
                                0,
                                read.Time,
                                read.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? read.ReadBib : read.ChipBib,
                                Constants.Timing.TIMERESULT_STATUS_NONE,
                                ""
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
                            else if (dictionary.locationDictionary.TryGetValue(read.LocationID, out TimingLocation oLoc))
                            {
                                occursWithin = oLoc.IgnoreWithin;
                            }
                            // Minimum time to create a result.
                            long minSeconds = startSeconds;
                            int minMilliseconds = startMilliseconds;
                            if (chipLastReadDictionary.TryGetValue((chip, read.LocationID), out (ChipRead Read, int Occurrence) cLastReads))
                            {
                                occurrence = cLastReads.Occurrence + 1;
                                minSeconds = cLastReads.Read.TimeSeconds + occursWithin;
                                minMilliseconds = cLastReads.Read.TimeMilliseconds;
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
                                if (!theEvent.DistanceSpecificSegments && dictionary.segmentDictionary.TryGetValue((Constants.Timing.COMMON_SEGMENTS_DISTANCEID, read.LocationID, 1), out Segment oSeg))
                                {
                                    segId = oSeg.Identifier;
                                }
                                else if (Constants.Timing.LOCATION_FINISH == read.LocationID)
                                {
                                    segId = Constants.Timing.SEGMENT_FINISH;
                                }
                                string identifier = TimeResult.ChipToIdentifier(chip).ToString();
                                // Create a result for the start value
                                long secondsDiff = read.TimeSeconds - startSeconds;
                                int millisecDiff = read.TimeMilliseconds - startMilliseconds;
                                while (millisecDiff < 0)
                                {
                                    secondsDiff--;
                                    millisecDiff += 1000;
                                }
                                while (millisecDiff >= 1000)
                                {
                                    secondsDiff++;
                                    millisecDiff -= 1000;
                                }
                                bool startExists = startTimes.TryGetValue(identifier, out TimeResult startRes);
                                long chipSecDiff = read.TimeSeconds - (startExists ? Constants.Timing.RFIDDateToEpoch(startRes.SystemTime) : startSeconds);
                                int chipMillisecDiff = read.TimeMilliseconds - (startExists ? startRes.SystemTime.Millisecond : startMilliseconds);
                                while (chipMillisecDiff < 0)
                                {
                                    chipSecDiff--;
                                    chipMillisecDiff += 1000;
                                }
                                while (chipMillisecDiff >= 1000)
                                {
                                    chipSecDiff++;
                                    chipMillisecDiff -= 1000;
                                }
                                newResults.Add(new(theEvent.Identifier,
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
            // Process the intersection of unknown DNS people and Finish results:
            foreach (string chip in chipDNSDictionary.Keys)
            {
                if (finishTimes.TryGetValue(TimeResult.ChipToIdentifier(chip), out List<TimeResult> finResults))
                {
                    int dnsId = chipDNSDictionary[chip].ReadId;
                    foreach (TimeResult finish in finResults)
                    {
                        finish.ReadId = dnsId;
                        finish.Time = "DNS";
                        finish.ChipTime = "DNS";
                        finish.Status = Constants.Timing.TIMERESULT_STATUS_DNS;
                        finish.Occurrence = theEvent.FinishMaxOccurrences;
                        newResults.Add(finish);
                    }
                }
                else
                {
                    ChipRead chipDNS = chipDNSDictionary[chip];
                    newResults.Add(new(theEvent.Identifier,
                        chipDNS.ReadId,
                        Constants.Timing.TIMERESULT_DUMMYPERSON,
                        Constants.Timing.LOCATION_FINISH,
                        Constants.Timing.SEGMENT_FINISH,
                        chipLastReadDictionary.TryGetValue((chip, Constants.Timing.LOCATION_FINISH), out (ChipRead Read, int Occurrence) cLastReads) ? cLastReads.Occurrence + 1 : 1,
                        0,
                        0,
                        TimeResult.ChipToIdentifier(chip),
                        0,
                        0,
                        chipDNS.Time,
                        chipDNS.ChipBib == Constants.Timing.CHIPREAD_DUMMYBIB ? chipDNS.ReadBib : chipDNS.ChipBib,
                        Constants.Timing.TIMERESULT_STATUS_DNS,
                        ""
                        ));
                }
            }
            // Process the intersection of known DNS people and Finish results:
            foreach (string bib in bibDNSDictionary.Keys)
            {
                Participant part = dictionary.participantBibDictionary.TryGetValue(bib, out Participant oPart) ? oPart : null;
                if (part != null)
                {
                    part.Status = Constants.Timing.EVENTSPECIFIC_DNS;
                    updateParticipants.Add(part);
                }
                int occurrence = part == null ? 1 : dictionary.distanceDictionary.TryGetValue(part.EventSpecific.DistanceIdentifier, out Distance oDist) ? oDist.FinishOccurrence : 1;
                if (finishTimes.TryGetValue(TimeResult.BibToIdentifier(bib), out List<TimeResult> finResults))
                {
                    int dnsId = bibDNSDictionary[bib].ReadId;
                    foreach (TimeResult finish in finResults)
                    {
                        finish.ReadId = dnsId;
                        finish.Time = "DNS";
                        finish.ChipTime = "DNS";
                        finish.Status = Constants.Timing.TIMERESULT_STATUS_DNS;
                        finish.Occurrence = occurrence;
                        newResults.Add(finish);
                    }
                }
                else
                {
                    ChipRead bibDNS = bibDNSDictionary[bib];
                    newResults.Add(new(theEvent.Identifier,
                        bibDNS.ReadId,
                        part == null ? Constants.Timing.TIMERESULT_DUMMYPERSON : part.EventSpecific.Identifier,
                        Constants.Timing.LOCATION_FINISH,
                        Constants.Timing.SEGMENT_FINISH,
                        occurrence,
                        0,
                        0,
                        TimeResult.BibToIdentifier(bib),
                        0,
                        0,
                        bibDNS.Time,
                        bib,
                        Constants.Timing.TIMERESULT_STATUS_DNS,
                        part == null ? "" : part.EventSpecific.Division
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
            database.UpdateParticipants(updateParticipants);
            return newResults;
        }

        // Process lap times.
        public static void ProcessLapTimes(Event theEvent, IDBInterface database)
        {
            Dictionary<(string, int), TimeResult> RaceResults = [];
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
                if (RaceResults.TryGetValue((currentLap.Identifier, currentLap.Occurrence - 1), out TimeResult prevRes))
                {
                    sec = prevRes.ChipSeconds;
                    mill = prevRes.ChipMilliseconds;
                }
                sec = currentLap.ChipSeconds - sec;
                mill = currentLap.ChipMilliseconds - mill;
                while (mill < 0)
                {
                    sec--;
                    mill += 1000;
                }
                while (mill >= 1000)
                {
                    sec++;
                    mill -= 1000;
                }
                currentLap.LapTime = Constants.Timing.ToTime((int)sec, mill);
            }
            database.AddTimingResults(laps);
        }

        // Process placements for a time based race.
        public static List<TimeResult> ProcessPlacements(Event theEvent, IDBInterface database, TimingDictionary dictionary)
        {
            List<TimeResult> output = [];
            // Create a dictionary so we can check if placements have changed. (place, location, occurrence, distance)
            Dictionary<(int, int, int, string), TimeResult> PlacementDictionary = [];
            // Get a list of all segments
            List<Segment> segments = database.GetSegments(theEvent.Identifier);
            Dictionary<int, List<TimeResult>> segmentDictionary = [];
            foreach (TimeResult result in database.GetTimingResults(theEvent.Identifier))
            {
                // We probably have unprocessed results in there, so only worry about results with a place set.
                // Make sure we're checking based on segmentId as well.
                if (result.Place > 0)
                {
                    PlacementDictionary[(result.Place, result.LocationId, result.Occurrence, result.DistanceName)] = result;
                }
                if (!segmentDictionary.TryGetValue(result.SegmentId, out List<TimeResult> oSegResults))
                {
                    oSegResults = [];
                    segmentDictionary[result.SegmentId] = oSegResults;
                }
                oSegResults.Add(result);
            }
            // process results based upon the segment they're in
            foreach (Segment segment in segments)
            {
                Log.D("Timing.TimingWorker", "Processing segment " + segment.Name);
                if (segmentDictionary.TryGetValue(segment.Identifier, out List<TimeResult> tSegResults))
                {
                    output.AddRange(ProcessSegmentPlacements(theEvent, tSegResults, dictionary));
                }
            }
            Log.D("Timing.TimingWorker", "Processing finish results");
            if (segmentDictionary.TryGetValue(Constants.Timing.SEGMENT_FINISH, out List<TimeResult> finSegResults))
            {
                output.AddRange(ProcessSegmentPlacements(theEvent, finSegResults, dictionary));
            }
            // Check if we should be re-uploading results because placements have changed.
            List<TimeResult> reUpload = [];
            Log.D("Timing.TimingWorker", "Checking for outdated placements.");
            foreach (TimeResult result in output)
            {
                if (PlacementDictionary.TryGetValue((result.Place, result.LocationId, result.Occurrence, result.DistanceName), out TimeResult plResult) && plResult.Bib != result.Bib)
                {
                    Log.D("Timing.TimingWorker", string.Format("Oudated placement found. {0} && {1}", result.ParticipantName, plResult.ParticipantName));
                    result.Uploaded = Constants.Timing.TIMERESULT_UPLOADED_FALSE;
                    plResult.Uploaded = Constants.Timing.TIMERESULT_UPLOADED_FALSE;
                    reUpload.Add(result);
                    reUpload.Add(plResult);
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
            Dictionary<int, List<TimeResult>> personResults = [];
            Dictionary<int, TimeResult> personLastResult = [];
            foreach (TimeResult result in segmentResults)
            {
                // If we don't have a Top Result for the person, or the result we have
                // is lesser than the one we're looking at, set it to the best
                if (!personLastResult.TryGetValue(result.EventSpecificId, out TimeResult pLastRes) || pLastRes.Occurrence < result.Occurrence)
                {
                    pLastRes = result;
                    personLastResult[result.EventSpecificId] = pLastRes;
                }
                // Store a person's results
                if (!personResults.TryGetValue(result.EventSpecificId, out List<TimeResult> pResults))
                {
                    pResults = [];
                    personResults[result.EventSpecificId] = pResults;
                }
                pResults.Add(result);
                // Change any TIMERESULT_STATUS_NONE to TIMERESULT_STATUS_PROCESSED
                if (Constants.Timing.TIMERESULT_STATUS_NONE == result.Status)
                {
                    result.Status = Constants.Timing.TIMERESULT_STATUS_PROCESSED;
                }
            }
            // Get Dictionaries for storing the last known place (division, age group, gender, overall)
            // The key is as follows: (Distance ID, Division)
            Dictionary<(int, string), int> divisionPlaceDictionary = [];
            // The key is as follows: (Distance ID, Age Group ID, int - Gender)
            Dictionary<(int, int, string), int> ageGroupPlaceDictionary = [];
            // The key is as follows: (Distance ID, Gender)
            Dictionary<(int, string), int> genderPlaceDictionary = [];
            // The key is as follows: (Distance ID)
            Dictionary<int, int> placeDictionary = [];
            List<TimeResult> topResults = [.. personLastResult.Values];
            topResults.Sort((x1, x2) =>
            {
                Distance distance1 = null, distance2 = null;
                int rank1 = 0, rank2 = 0;
                // Get *linked* distances. (Could be that specific distance)
                if (dictionary.linkedDistanceDictionary.TryGetValue(x1.RealDistanceName, out (Distance, int) value1))
                {
                    (distance1, rank1) = value1;
                }
                if (dictionary.linkedDistanceDictionary.TryGetValue(x2.RealDistanceName, out (Distance, int) value2))
                {
                    (distance2, rank2) = value2;
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
                            Log.D("Timing.Routines.TimeRoutine", "By Clock");
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
            int ageGroupId;
            string gender, division;
            foreach (TimeResult result in topResults)
            {
                // Make sure we know who we're looking at. Can't rank otherwise.
                if (dictionary.participantEventSpecificDictionary.TryGetValue(result.EventSpecificId, out Participant person))
                {
                    // Use a linked distance ID for ranking instead of a specific distance id.
                    if (!dictionary.linkedDistanceIdentifierDictionary.TryGetValue(person.EventSpecific.DistanceIdentifier, out int distanceId))
                    {
                        distanceId = person.EventSpecific.DistanceIdentifier;
                    }
                    // Since Results were sorted before we started, let's assume that the first item
                    // is the fastest/best and if we can't find the key, add one starting at 0
                    if (!placeDictionary.TryGetValue(distanceId, out int pl))
                    {
                        pl = 0;
                    }
                    result.Place = ++pl;
                    placeDictionary[distanceId] = pl;
                    gender = person.Gender.ToLower();
                    if (gender.Length < 1)
                    {
                        gender = "not specified";
                    }
                    ageGroupId = person.EventSpecific.AgeGroupId;
                    if (!genderPlaceDictionary.TryGetValue((distanceId, gender), out int gendPl))
                    {
                        gendPl = 0;
                    }
                    result.GenderPlace = ++gendPl;
                    genderPlaceDictionary[(distanceId, gender)] = gendPl;
                    if (ageGroupId != Constants.Timing.TIMERESULT_DUMMYAGEGROUP)
                    {
                        if (!ageGroupPlaceDictionary.TryGetValue((distanceId, ageGroupId, gender), out int agPl))
                        {
                            agPl = 0;
                        }
                        result.AgePlace = ++agPl;
                        ageGroupPlaceDictionary[(distanceId, ageGroupId, gender)] = agPl;
                    }
                    division = person.EventSpecific.Division.ToLower();
                    if (division.Length > 0)
                    {
                        if (!divisionPlaceDictionary.TryGetValue((distanceId, division), out int divPl))
                        {
                            divPl = 0;
                        }
                        result.DivisionPlace = ++divPl;
                        divisionPlaceDictionary[(distanceId, division)] = divPl;
                    }
                    foreach (TimeResult otherResult in personResults[result.EventSpecificId])
                    {
                        otherResult.Place = result.Place;
                        otherResult.GenderPlace = result.GenderPlace;
                        otherResult.AgePlace = result.AgePlace;
                        otherResult.DivisionPlace = result.DivisionPlace;
                    }
                }
            }
            return segmentResults;
        }
    }
}
