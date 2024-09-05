using Chronokeep.Objects;
using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * TimingResult Functions
         */

        public void AddTimingResult(TimeResult tr)
        {
            Log.D("MemStore", "AddTimingResult");
            try
            {
                resultsLock.AcquireWriterLock(lockTimeout);
                distanceLock.AcquireReaderLock(lockTimeout);
                participantsLock.AcquireReaderLock(lockTimeout);
                chipReadsLock.AcquireReaderLock(lockTimeout);
                database.AddTimingResult(tr);
                string bib = "";
                if (participants.TryGetValue(tr.EventSpecificId, out Participant p))
                {
                    bib = p.Bib;
                    tr.SetParticipant(p);
                    if (distances.TryGetValue(p.EventSpecific.DistanceIdentifier, out Distance distance))
                    {
                        tr.SetResultType(distance.Type);
                        if (distance.LinkedDistance != Constants.Timing.DISTANCE_NO_LINKED_ID && distances.TryGetValue(distance.LinkedDistance, out Distance linked))
                        {
                            tr.SetLinkedDistanceName(linked.Name);
                        }
                    }
                }
                else
                {
                    tr.SetBlankParticipant();
                }
                if (chipReads.TryGetValue(tr.ReadId, out ChipRead chipRead))
                {
                    if (bib.Length < 1)
                    {
                        bib = chipRead.Bib;
                    }
                    tr.SetChipRead(chipRead.ChipNumber, bib, chipRead.TimeSeconds, chipRead.TimeMilliseconds);
                }
                else
                {
                    tr.SetChipRead("", bib, 0, 0);
                }
                tr.FinalizeSetup();
                timingResults[(tr.EventSpecificId, tr.LocationId, tr.Occurrence, tr.UnknownId)] = tr;
                resultsLock.ReleaseWriterLock();
                distanceLock.ReleaseReaderLock();
                participantsLock.ReleaseReaderLock();
                chipReadsLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring resultsLock. " + e.Message);
                throw new MutexLockException("resultsLock");
            }
        }

        public void AddTimingResults(List<TimeResult> results)
        {
            Log.D("MemStore", "AddTimingResults");
            try
            {
                resultsLock.AcquireWriterLock(lockTimeout);
                database.AddTimingResults(results);
                foreach (TimeResult tr in results)
                {
                    string bib = "";
                    if (participants.TryGetValue(tr.EventSpecificId, out Participant p))
                    {
                        bib = p.Bib;
                        tr.SetParticipant(p);
                        if (distances.TryGetValue(p.EventSpecific.DistanceIdentifier, out Distance distance))
                        {
                            tr.SetResultType(distance.Type);
                            if (distance.LinkedDistance != Constants.Timing.DISTANCE_NO_LINKED_ID && distances.TryGetValue(distance.LinkedDistance, out Distance linked))
                            {
                                tr.SetLinkedDistanceName(linked.Name);
                            }
                        }
                    }
                    else
                    {
                        tr.SetBlankParticipant();
                    }
                    if (chipReads.TryGetValue(tr.ReadId, out ChipRead chipRead))
                    {
                        if (bib.Length < 1)
                        {
                            bib = chipRead.Bib;
                        }
                        tr.SetChipRead(chipRead.ChipNumber, bib, chipRead.TimeSeconds, chipRead.TimeMilliseconds);
                    }
                    else
                    {
                        tr.SetChipRead("", bib, 0, 0);
                    }
                    tr.FinalizeSetup();
                    timingResults[(tr.EventSpecificId, tr.LocationId, tr.Occurrence, tr.UnknownId)] = tr;
                }
                resultsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring resultsLock. " + e.Message);
                throw new MutexLockException("resultsLock");
            }
        }

        public List<TimeResult> GetSegmentTimes(int eventId, int segmentId)
        {
            Log.D("MemStore", "GetSegmentTimes");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            try
            {
                resultsLock.AcquireReaderLock(lockTimeout);
                List<TimeResult> output = new();
                foreach (TimeResult tr in timingResults.Values)
                {
                    if (tr.EventIdentifier == eventId && tr.SegmentId == segmentId)
                    {
                        output.Add(tr);
                    }
                }
                resultsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring resultsLock. " + e.Message);
                throw new MutexLockException("resultsLock");
            }
        }

        public List<TimeResult> GetFinishTimes(int eventId)
        {
            Log.D("MemStore", "GetFinishTimes");
            return GetSegmentTimes(eventId, Constants.Timing.SEGMENT_FINISH);
        }

        public List<TimeResult> GetLastSeenResults(int eventId)
        {
            Log.D("MemStore", "GetLastSeenResults");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            try
            {
                resultsLock.AcquireReaderLock(lockTimeout);
                Dictionary<int, TimeResult> trDict = new();
                foreach (TimeResult tr in timingResults.Values)
                {
                    if (trDict.TryGetValue(tr.EventSpecificId, out TimeResult result))
                    {
                        if (result.Seconds < tr.Seconds || (result.Seconds == tr.Seconds && result.Milliseconds < tr.Milliseconds))
                        {
                            trDict[tr.EventSpecificId] = tr;
                        }
                    }
                    else
                    {
                        trDict[tr.EventSpecificId] = tr;
                    }
                }
                resultsLock.ReleaseReaderLock();
                List<TimeResult> output = new();
                output.AddRange(trDict.Values);
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring resultsLock. " + e.Message);
                throw new MutexLockException("resultsLock");
            }
        }

        public List<TimeResult> GetNonUploadedResults(int eventId)
        {
            Log.D("MemStore", "GetNonUploadedResults");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            try
            {
                resultsLock.AcquireReaderLock(lockTimeout);
                List<TimeResult> output = new();
                foreach (TimeResult tr in timingResults.Values)
                {
                    if (tr.EventIdentifier == eventId && !tr.IsUploaded())
                    {
                        output.Add(tr);
                    }
                }
                resultsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring resultsLock. " + e.Message);
                throw new MutexLockException("resultsLock");
            }
        }

        public List<TimeResult> GetStartTimes(int eventId)
        {
            Log.D("MemStore", "GetStartTimes");
            return GetSegmentTimes(eventId, Constants.Timing.SEGMENT_START);
        }

        public List<TimeResult> GetTimingResults(int eventId)
        {
            Log.D("MemStore", "GetTimingResults");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            try
            {
                resultsLock.AcquireReaderLock(lockTimeout);
                List<TimeResult> output = new();
                output.AddRange(timingResults.Values);
                resultsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring resultsLock. " + e.Message);
                throw new MutexLockException("resultsLock");
            }
        }

        public void RemoveTimingResult(TimeResult tr)
        {
            Log.D("MemStore", "RemoveTimingResult");
            try
            {
                resultsLock.AcquireWriterLock(lockTimeout);
                database.RemoveTimingResult(tr);
                timingResults.Remove((tr.EventSpecificId, tr.LocationId, tr.Occurrence, tr.UnknownId));
                resultsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring resultsLock. " + e.Message);
                throw new MutexLockException("resultsLock");
            }
        }

        public void SetUploadedTimingResults(List<TimeResult> results)
        {
            Log.D("MemStore", "SetUploadedTimingResults");
            try
            {
                resultsLock.AcquireWriterLock(lockTimeout);
                database.SetUploadedTimingResults(results);
                foreach (TimeResult changed in results)
                {
                    foreach (TimeResult toChange in timingResults.Values)
                    {
                        if (changed.Equals(toChange))
                        {
                            toChange.Uploaded = changed.Uploaded;
                            break;
                        }
                    }
                }
                resultsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring resultsLock. " + e.Message);
                throw new MutexLockException("resultsLock");
            }
        }

        public bool UnprocessedResultsExist(int eventId)
        {
            Log.D("MemStore", "UnprocessedResultsExist");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            try
            {
                resultsLock.AcquireReaderLock(lockTimeout);
                bool output = false;
                foreach (TimeResult result in timingResults.Values)
                {
                    if (result.Status == Constants.Timing.TIMERESULT_STATUS_NONE)
                    {
                        output = true;
                        break;
                    }
                }
                resultsLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring resultsLock. " + e.Message);
                throw new MutexLockException("resultsLock");
            }
        }

        // Reset Timing Results clears all timing results
        // But it also changes ChipReads statuses to None except where
        // statuses were set to IGNORE/DNF/DNS/DNF_IGNORE/DNS_IGNORE
        // and it also sets Participant EVENTSPECIFIC_STATUS to UNKNOWN
        public void ResetTimingResultsEvent(int eventId)
        {
            Log.D("MemStore", "ResetTimingResultsEvent");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            try
            {
                resultsLock.AcquireWriterLock(lockTimeout);
                chipReadsLock.AcquireWriterLock(lockTimeout);
                participantsLock.AcquireWriterLock(lockTimeout);
                database.ResetTimingResultsEvent(eventId);
                timingResults.Clear();
                foreach (ChipRead cr in chipReads.Values)
                {
                    if (cr.CanBeReset())
                    {
                        cr.Status = Constants.Timing.CHIPREAD_STATUS_NONE;
                    }
                }
                foreach (Participant p in participants.Values)
                {
                    p.EventSpecific.Status = Constants.Timing.EVENTSPECIFIC_UNKNOWN;
                }
                resultsLock.ReleaseWriterLock();
                chipReadsLock.ReleaseWriterLock();
                participantsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring resultsLock/chipReadsLock/participantsLock. " + e.Message);
                throw new MutexLockException("resultsLock/chipReadsLock/participantsLock");
            }
        }

        public void ResetTimingResultsPlacements(int eventId)
        {
            Log.D("MemStore", "ResetTimingResultsPlacements");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent.Identifier != eventId)
                {
                    invalidEvent = true;
                }
                eventLock.ReleaseReaderLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring eventLock. " + e.Message);
                throw new MutexLockException("eventLock");
            }
            if (invalidEvent)
            {
                throw new InvalidEventID("Expected different event id.");
            }
            try
            {
                resultsLock.AcquireWriterLock(lockTimeout);
                database.ResetTimingResultsPlacements(eventId);
                foreach (TimeResult tr in timingResults.Values)
                {
                    tr.Status = Constants.Timing.CHIPREAD_STATUS_NONE;
                    tr.Place = -1;
                    tr.AgePlace = -1;
                    tr.GenderPlace = -1;
                    tr.Uploaded = Constants.Timing.TIMERESULT_UPLOADED_FALSE;
                }
                resultsLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring resultsLock. " + e.Message);
                throw new MutexLockException("resultsLock");
            }
        }
    }
}
