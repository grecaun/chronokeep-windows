using Chronokeep.Objects;
using System;
using System.Collections.Generic;

namespace Chronokeep.MemStore
{
    internal partial class MemStore : IDBInterface
    {
        /**
         * DistanceStat Functions
         */

        public List<DistanceStat> GetDistanceStats(int eventId)
        {
            Log.D("MemStore", "GetTimingSystems");
            try
            {
                distanceLock.AcquireReaderLock(lockTimeout);
                participantsLock.AcquireReaderLock(lockTimeout);
                Dictionary<int, DistanceStat> distStatDict = new();
                DistanceStat allstats = new()
                {
                    DistanceName = "All",
                    DistanceID = -1,
                    Active = 0,
                    DNF = 0,
                    DNS = 0,
                    Finished = 0
                };
                foreach (Participant p in participants.Values)
                {
                    string distName = distances.TryGetValue(p.EventSpecific.DistanceIdentifier, out Distance dist) ? dist.Name : "";
                    if (!distStatDict.TryGetValue(p.EventSpecific.DistanceIdentifier, out DistanceStat distStats))
                    {
                        distStats = new()
                        {
                            DistanceName = distName,
                            DistanceID = p.EventSpecific.DistanceIdentifier,
                            Active = 0,
                            DNF = 0,
                            DNS = 0,
                            Finished = 0
                        };
                        distStatDict[p.EventSpecific.DistanceIdentifier] = distStats;
                    }
                    if (Constants.Timing.EVENTSPECIFIC_DNF == p.Status)
                    {
                        distStats.DNF += 1;
                        allstats.DNF += 1;
                    }
                    else if (Constants.Timing.EVENTSPECIFIC_FINISHED == p.Status)
                    {
                        distStats.Finished += 1;
                        allstats.Finished += 1;
                    }
                    else if (Constants.Timing.EVENTSPECIFIC_STARTED == p.Status)
                    {
                        distStats.Active += 1;
                        allstats.Active += 1;
                    }
                    else if (Constants.Timing.EVENTSPECIFIC_DNS == p.Status || Constants.Timing.EVENTSPECIFIC_UNKNOWN == p.Status)
                    {
                        distStats.DNS += 1;
                        allstats.DNS += 1;
                    }
                }
                distanceLock.ReleaseReaderLock();
                participantsLock.ReleaseReaderLock();
                List<DistanceStat> output = new()
                {
                    allstats
                };
                output.AddRange(distStatDict.Values);
                return output;
            }
            catch (Exception e)
            {
                Log.D("MemStore", "Exception acquiring distanceLock. " + e.Message);
                throw new MutexLockException("distanceLock");
            }
        }

        public Dictionary<int, List<Participant>> GetDistanceParticipantsStatus(int eventId, int distanceId)
        {
            Dictionary<int, List<Participant>> output = new();
            List<Participant> dbParts = (distanceId == -1) ? GetParticipants(eventId) : GetParticipants(eventId, distanceId);
            foreach (Participant person in dbParts)
            {
                if (!output.TryGetValue(person.Status, out List<Participant> localParts))
                {
                    localParts = new List<Participant>();
                    output[person.Status] = localParts;
                }
                localParts.Add(person);
            }
            return output;
        }
    }
}
