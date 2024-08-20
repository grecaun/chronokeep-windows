using Chronokeep.Objects;
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
            throw new System.NotImplementedException();
        }

        public void AddTimingResults(List<TimeResult> results)
        {
            throw new System.NotImplementedException();
        }

        public List<TimeResult> GetSegmentTimes(int eventId, int segmentId)
        {
            throw new System.NotImplementedException();
        }

        public List<TimeResult> GetFinishTimes(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<TimeResult> GetLastSeenResults(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<TimeResult> GetNonUploadedResults(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<TimeResult> GetStartTimes(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public List<TimeResult> GetTimingResults(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveTimingResult(TimeResult tr)
        {
            throw new System.NotImplementedException();
        }

        public void SetUploadedTimingResults(List<TimeResult> results)
        {
            throw new System.NotImplementedException();
        }

        public bool UnprocessedResultsExist(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void ResetTimingResultsEvent(int eventId)
        {
            throw new System.NotImplementedException();
        }

        public void ResetTimingResultsPlacements(int eventId)
        {
            throw new System.NotImplementedException();
        }
    }
}
