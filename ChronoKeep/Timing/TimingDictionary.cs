using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Timing
{
    internal class TimingDictionary
    {
        // Dictionaries for storing information about the race.
        public Dictionary<int, TimingLocation> locationDictionary = new Dictionary<int, TimingLocation>();
        // (DistanceId, LocationId, Occurrence)
        public Dictionary<(int, int, int), Segment> segmentDictionary = new Dictionary<(int, int, int), Segment>();
        // Participants are stored based upon BIB and EVENTSPECIFICIDENTIFIER because we use both
        public Dictionary<int, Participant> participantBibDictionary = new Dictionary<int, Participant>();
        public Dictionary<int, Participant> participantEventSpecificDictionary = new Dictionary<int, Participant>();
        // Start times. Item at 0 should always be 00:00:00.000. Key is Distance ID
        public Dictionary<int, (long Seconds, int Milliseconds)> distanceStartDict = new Dictionary<int, (long, int)>();
        public Dictionary<int, (long Seconds, int Milliseconds)> distanceEndDict = new Dictionary<int, (long, int)>();
        public Dictionary<int, Distance> distanceDictionary = new Dictionary<int, Distance>();

        // Link bibs and chipreads for adding occurence to bib based dnf entry.
        public Dictionary<int, string> bibChipDictionary = new Dictionary<int, string>();

        public Dictionary<string, (Distance, int)> linkedDistanceDictionary = new Dictionary<string, (Distance, int)>();
        public Dictionary<int, int> linkedDistanceIdentifierDictionary = new Dictionary<int, int>();

        // HashSet to keep track of chips of DNS entries.
        public HashSet<string> dnsParticipants = new HashSet<string>();
    }
}
