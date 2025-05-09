﻿using System;
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
            int output = database.AddSegment(seg);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && seg.EventId == theEvent.Identifier && seg.Identifier > 0)
                    {
                        seg.Identifier = output;
                    }
                    segments[seg.Identifier] = seg;
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
            return output;
        }

        public List<Segment> AddSegments(List<Segment> segs)
        {
            Log.D("MemStore", "AddSegments");
            List<Segment> output = database.AddSegments(segs);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    foreach (Segment seg in output)
                    {
                        if (theEvent != null && seg.EventId == theEvent.Identifier && seg.Identifier > 0)
                        {
                            segments[seg.Identifier] = seg;
                        }
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
            return output;
        }

        public int GetSegmentId(Segment seg)
        {
            Log.D("MemStore", "GetSegmentId");
            int output = -1;
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
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
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
            return output;
        }

        public List<Segment> GetSegments(int eventId)
        {
            Log.D("MemStore", "GetSegments");
            List<Segment> output = new();
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        output.AddRange(segments.Values);
                    }
                    else
                    {
                        output.AddRange(database.GetSegments(eventId));
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
            return output;
        }

        public int GetMaxSegments(int eventId)
        {
            Log.D("MemStore", "GetMaxSegments");
            int output = 0;
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        Dictionary<int, int> maxSegmentsPerDistance = new();
                        foreach (Segment s in segments.Values)
                        {
                            if (!maxSegmentsPerDistance.TryGetValue(s.DistanceId, out int count))
                            {
                                count = 0;
                            }
                            count++;
                            maxSegmentsPerDistance[s.DistanceId] = count;
                            if (count > output)
                            {
                                output = count;
                            }
                        }
                    }
                    else
                    {
                        output = database.GetMaxSegments(eventId);
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
            return output;
        }

        public void RemoveSegment(Segment seg)
        {
            Log.D("MemStore", "RemoveSegment");
            database.RemoveSegment(seg);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    segments.Remove(seg.Identifier);
                    memStoreLock.ReleaseMutex();
                }
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
            database.RemoveSegment(identifier);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    segments.Remove(identifier);
                    memStoreLock.ReleaseMutex();
                }
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
            database.RemoveSegments(segs);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    foreach (Segment s in segs)
                    {
                        segments.Remove(s.Identifier);
                    }
                    memStoreLock.ReleaseMutex();
                }
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
            database.ResetSegments(eventId);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (theEvent != null && theEvent.Identifier == eventId)
                    {
                        segments.Clear();
                    }
                    memStoreLock.ReleaseMutex();
                }
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
            database.UpdateSegment(seg);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    if (segments.TryGetValue(seg.Identifier, out Segment oldSeg))
                    {
                        oldSeg.CopyFrom(seg);
                    }
                    memStoreLock.ReleaseMutex();
                }
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
            database.UpdateSegments(segs);
            try
            {
                if (memStoreLock.WaitOne(lockTimeout))
                {
                    foreach (Segment s in segs)
                    {
                        if (segments.TryGetValue(s.Identifier, out Segment oldSeg))
                        {
                            oldSeg.CopyFrom(s);
                        }
                    }
                    memStoreLock.ReleaseMutex();
                }
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring segmentLock. " + e.Message);
                throw new MutexLockException("segmentLock");
            }
        }
    }
}
