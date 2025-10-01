using Chronokeep.Objects;
using System.Collections.Generic;

namespace Chronokeep.Timing
{
    public class TimingDictionary
    {
        // Dictionaries for storing information about the race.
        public Dictionary<int, TimingLocation> locationDictionary = [];
        // (DistanceId, LocationId, Occurrence)
        public Dictionary<(int, int, int), Segment> segmentDictionary = [];
        // Participants are stored based upon BIB and EVENTSPECIFICIDENTIFIER because we use both
        public Dictionary<string, Participant> participantBibDictionary = [];
        public Dictionary<int, Participant> participantEventSpecificDictionary = [];
        // Start times. Item at 0 should always be 00:00:00.000. Key is Distance ID
        public Dictionary<int, (long Seconds, int Milliseconds)> distanceStartDict = [];
        public Dictionary<int, (long Seconds, int Milliseconds)> distanceEndDict = [];
        public Dictionary<int, Distance> distanceDictionary = [];
        public Dictionary<string, Distance> distanceNameDictionary = [];

        // Link bibs and chipreads for adding occurence to bib based dnf entry.
        // We changed the database to allow multiple chips per bib.
        public Dictionary<string, List<string>> bibToChipDictionary = [];
        public Dictionary<string, string> chipToBibDictionary = [];

        // Linked distance dictionaries
        public Dictionary<string, (Distance, int)> linkedDistanceDictionary = [];
        public Dictionary<int, int> linkedDistanceIdentifierDictionary = [];

        // HashSet for non-linked distances.
        public HashSet<Distance> mainDistances = [];
        public Dictionary<int, APIObject> apis = [];

        // Dictionaries for keeping track of Segments by distance
        public Dictionary<int, List<Segment>> DistanceSegmentOrder = [];
        public Dictionary<int, Segment> SegmentByIDDictionary = [];

        // HashSet to keep track of chips & bibs of DNS entries.
        public HashSet<string> dnsChips = [];
        public HashSet<string> dnsBibs = [];
        public int dnsEntryCount = 0;
    }
}
