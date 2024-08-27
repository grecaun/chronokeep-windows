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
                Dictionary<string, DistanceStat> distStatDict = new();
                DistanceStat allstats = new()
                {
                    DistanceName = "All",
                    DistanceID = -1,
                    Active = 0,
                    DNF = 0,
                    DNS = 0,
                    Finished = 0
                };
                Dictionary<string, int> distanceIdentifierDict = new();
                foreach (Distance d in distances.Values)
                {
                    distanceIdentifierDict[d.Name] = d.Identifier;
                }
                foreach (TimeResult result in timingResults)
                {
                    if (!distStatDict.TryGetValue(result.RealDistanceName, out DistanceStat distStats))
                    {
                        if (distanceIdentifierDict.TryGetValue(result.RealDistanceName, out int distanceID))
                        {
                            distStats = new()
                            {
                                DistanceName = result.RealDistanceName,
                                DistanceID = distanceID,
                                Active = 0,
                                DNF = 0,
                                DNS = 0,
                                Finished = 0
                            };
                        }
                        else
                        {
                            distStats = new()
                            {
                                DistanceName = result.RealDistanceName,
                                DistanceID = -5,
                                Active = 0,
                                DNF = 0,
                                DNS = 0,
                                Finished = 0
                            };
                        }
                    }
                    switch (result.Status)
                    {
                        case Constants.Timing.EVENTSPECIFIC_DNS:
                        case Constants.Timing.EVENTSPECIFIC_UNKNOWN:
                            distStats.DNS++;
                            allstats.DNS++;
                            break;
                        case Constants.Timing.EVENTSPECIFIC_FINISHED:
                            distStats.Finished++;
                            allstats.Finished++;
                            break;
                        case Constants.Timing.EVENTSPECIFIC_STARTED:
                            distStats.Active++;
                            allstats.Active++;
                            break;
                        case Constants.Timing.EVENTSPECIFIC_DNF:
                            distStats.DNF++;
                            allstats.DNF++;
                            break;
                    }
                }
                distanceLock.ReleaseReaderLock();
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
