using Chronokeep.Objects;
using System.Collections.Generic;

namespace Chronokeep.Timing
{
    public class TimingDictionary
    {
        // Dictionaries for storing information about the race.
        public Dictionary<int, TimingLocation> locationDictionary = new Dictionary<int, TimingLocation>();
        // (DistanceId, LocationId, Occurrence)
        public Dictionary<(int, int, int), Segment> segmentDictionary = new Dictionary<(int, int, int), Segment>();
        // Participants are stored based upon BIB and EVENTSPECIFICIDENTIFIER because we use both
        public Dictionary<string, Participant> participantBibDictionary = new Dictionary<string, Participant>();
        public Dictionary<int, Participant> participantEventSpecificDictionary = new Dictionary<int, Participant>();
        // Start times. Item at 0 should always be 00:00:00.000. Key is Distance ID
        public Dictionary<int, (long Seconds, int Milliseconds)> distanceStartDict = new Dictionary<int, (long, int)>();
        public Dictionary<int, (long Seconds, int Milliseconds)> distanceEndDict = new Dictionary<int, (long, int)>();
        public Dictionary<int, Distance> distanceDictionary = new Dictionary<int, Distance>();
        public Dictionary<string, Distance> distanceNameDictionary = new Dictionary<string, Distance>();

        // Link bibs and chipreads for adding occurence to bib based dnf entry.
        // We changed the database to allow multiple chips per bib.
        public Dictionary<string, List<string>> bibToChipDictionary = new Dictionary<string, List<string>>();
        public Dictionary<string, string> chipToBibDictionary = new Dictionary<string, string>();

        // Linked distance dictionaries
        public Dictionary<string, (Distance, int)> linkedDistanceDictionary = new Dictionary<string, (Distance, int)>();
        public Dictionary<int, int> linkedDistanceIdentifierDictionary = new Dictionary<int, int>();

        // HashSet for non-linked distances.
        public HashSet<Distance> mainDistances = new HashSet<Distance>();
        public Dictionary<int, string> apiURLs = new Dictionary<int, string>();

        // Dictionaries for keeping track of Segments by distance
        public Dictionary<int, List<Segment>> DistanceSegmentOrder = new Dictionary<int, List<Segment>>();
        public Dictionary<int, Segment> SegmentByIDDictionary = new Dictionary<int, Segment>();

        // HashSet to keep track of chips & bibs of DNS entries.
        public HashSet<string> dnsChips = new HashSet<string>();
        public HashSet<string> dnsBibs = new HashSet<string>();
        public int dnsEntryCount = 0;
    }
}
