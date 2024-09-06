using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * Segment Functions
         */

        public int AddSegment(Segment seg)
        {
            Log.D("MemStore", "AddSegment");
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                segmentLock.AcquireWriterLock(lockTimeout);
                int output = database.AddSegment(seg);
                if (theEvent != null && seg.EventId == theEvent.Identifier && seg.Identifier > 0)
                {
                    seg.Identifier = output;
                }
                segments[seg.Identifier] = seg;
                eventLock.ReleaseReaderLock();
                segmentLock.ReleaseWriterLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
        }

        public List<Segment> AddSegments(List<Segment> segs)
        {
            Log.D("MemStore", "AddSegments");
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                segmentLock.AcquireWriterLock(lockTimeout);
                List<Segment> output = database.AddSegments(segs);
                foreach (Segment seg in output)
                {
                    if (theEvent != null && seg.EventId == theEvent.Identifier && seg.Identifier > 0)
                    {
                        segments[seg.Identifier] = seg;
                    }
                }
                eventLock.ReleaseReaderLock();
                segmentLock.ReleaseWriterLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
        }

        public int GetSegmentId(Segment seg)
        {
            Log.D("MemStore", "GetSegmentId");
            try
            {
                segmentLock.AcquireReaderLock(lockTimeout);
                int output = -1;
                foreach (Segment s in segments.Values)
                {
                    if (s.EventId == seg.EventId
                        && s.DistanceId == seg.DistanceId
                        && s.LocationId == seg.LocationId
                        && s.Occurrence == seg.Occurrence)
                    {
                        output = s.Identifier;
                        break;
                    }
                }
                segmentLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
        }

        public List<Segment> GetSegments(int eventId)
        {
            Log.D("MemStore", "GetSegments");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
                return database.GetSegments(eventId);
            }
            try
            {
                segmentLock.AcquireReaderLock(lockTimeout);
                List<Segment> output = new();
                output.AddRange(segments.Values);
                segmentLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
        }

        public int GetMaxSegments(int eventId)
        {
            Log.D("MemStore", "GetMaxSegments");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
                return database.GetMaxSegments(eventId);
            }
            try
            {
                int output = 0;
                Dictionary<int, int> maxSegmentsPerDistance = new();
                segmentLock.AcquireReaderLock(lockTimeout);
                foreach (Segment s in segments.Values)
                {
                    if (!maxSegmentsPerDistance.TryGetValue(s.DistanceId, out int count))
                    {
                        maxSegmentsPerDistance[s.DistanceId] = 0;
                    }
                    count++;
                    if (count > output)
                    {
                        output = count;
                    }
                }
                segmentLock.ReleaseReaderLock();
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
        }

        public void RemoveSegment(Segment seg)
        {
            Log.D("MemStore", "RemoveSegment");
            try
            {
                segmentLock.AcquireWriterLock(lockTimeout);
                database.RemoveSegment(seg);
                segments.Remove(seg.Identifier);
                segmentLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
        }

        public void RemoveSegment(int identifier)
        {
            Log.D("MemStore", "RemoveSegment");
            try
            {
                segmentLock.AcquireWriterLock(lockTimeout);
                database.RemoveSegment(identifier);
                segments.Remove(identifier);
                segmentLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
        }

        public void RemoveSegments(List<Segment> segs)
        {
            Log.D("MemStore", "RemoveSegments");
            try
            {
                segmentLock.AcquireWriterLock(lockTimeout);
                database.RemoveSegments(segs);
                foreach (Segment s in segs)
                {
                    segments.Remove(s.Identifier);
                }
                segmentLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
        }

        public void ResetSegments(int eventId)
        {
            Log.D("MemStore", "RemoveSegments");
            bool invalidEvent = false;
            try
            {
                eventLock.AcquireReaderLock(lockTimeout);
                if (theEvent == null || theEvent.Identifier != eventId)
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
                segmentLock.AcquireWriterLock(lockTimeout);
                database.ResetSegments(eventId);
                segments.Clear();
                segmentLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
        }

        public void UpdateSegment(Segment seg)
        {
            Log.D("MemStore", "UpdateSegment");
            try
            {
                segmentLock.AcquireWriterLock(lockTimeout);
                database.UpdateSegment(seg);
                if (segments.TryGetValue(seg.Identifier, out Segment oldSeg))
                {
                    oldSeg.CopyFrom(seg);
                }
                segmentLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
        }

        public void UpdateSegments(List<Segment> segs)
        {
            Log.D("MemStore", "UpdateSegments");
            try
            {
                segmentLock.AcquireWriterLock(lockTimeout);
                database.UpdateSegments(segs);
                foreach (Segment s in segs)
                {
                    if (segments.TryGetValue(s.Identifier, out Segment oldSeg))
                    {
                        oldSeg.CopyFrom(s);
                    }
                }
                segmentLock.ReleaseWriterLock();
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
        }
    }
}
