using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * Segment Functions
         */

        public void AddSegment(Segment seg)
        {
            throw new System.NotImplementedException();
        }

        public void AddSegments(List<Segment> segments)
        {
            throw new System.NotImplementedException();
        }

        public int GetSegmentId(Segment seg)
        {
            throw new System.NotImplementedException();
        }

        public List<Segment> GetSegments(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public int GetMaxSegments(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveSegment(Segment seg)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveSegment(int identifier)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveSegments(List<Segment> segments)
        {
            throw new System.NotImplementedException();
        }

        public void ResetSegments(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateSegment(Segment seg)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateSegments(List<Segment> segments)
        {
            throw new System.NotImplementedException();
        }
    }
}
