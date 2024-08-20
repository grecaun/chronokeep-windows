using System;

namespace Chronokeep
{
    public class Distance : IEquatable<Distance>, IComparable<Distance>
    {
        private string name;
        private int identifier, eventIdentifier;
        private double distance;
        private int distance_unit = Constants.Distances.MILES, finish_location = Constants.Timing.LOCATION_FINISH,
            finish_occurrence = 1, start_location = Constants.Timing.LOCATION_START, start_within = 5,
            end_seconds = 0;
        private int wave = 1, start_offset_seconds = 0, start_offset_milliseconds = 0;
        private int linked_distance = Constants.Timing.DISTANCE_NO_LINKED_ID, type = 0, ranking = 0;
        private bool sms_enabled = false;

        public Distance() { }

        public Distance(string name, int eventIdentifier)
        {
            this.name = name;
            this.eventIdentifier = eventIdentifier;
        }

        public Distance(string name, int eventIdentifier, int linkedIdentifier, int type, int ranking, int wave, int start_offset_seconds, int start_offset_milliseconds)
        {
            this.name = name;
            this.eventIdentifier = eventIdentifier;
            this.linked_distance = linkedIdentifier;
            this.type = type;
            this.ranking = ranking;
            this.wave = wave;
            this.start_offset_seconds = start_offset_seconds;
            this.start_offset_milliseconds = start_offset_milliseconds;
        }

        public Distance(int identifier, string name, int eventIdentifier,
            double distance, int distance_unit, int finish_location, int finish_occurrence,
            int start_location, int start_within, int wave, int start_offset_seconds, int start_offset_milliseconds,
            int endseconds, int linked_distance, int type, int ranking, bool sms_enabled)
        {
            this.identifier = identifier;
            this.name = name;
            this.eventIdentifier = eventIdentifier;
            this.distance = distance;
            this.distance_unit = distance_unit;
            this.finish_location = finish_location;
            this.finish_occurrence = finish_occurrence;
            this.start_location = start_location;
            this.start_within = start_within;
            this.wave = wave;
            this.start_offset_seconds = start_offset_seconds;
            this.start_offset_milliseconds = start_offset_milliseconds;
            this.end_seconds = endseconds;
            this.linked_distance = linked_distance;
            this.type = type;
            this.ranking = ranking;
            this.sms_enabled = sms_enabled;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; set => name = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
        public double DistanceValue { get => distance; set => distance = value; }
        public int DistanceUnit { get => distance_unit; set => distance_unit = value; }
        public int FinishLocation { get => finish_location; set => finish_location = value; }
        public int FinishOccurrence { get => finish_occurrence; set => finish_occurrence = value; }
        public int StartLocation { get => start_location; set => start_location = value; }
        public int StartWithin { get => start_within; set => start_within = value; }
        public int Wave { get => wave; set => wave = value; }
        public int StartOffsetSeconds { get => start_offset_seconds; set => start_offset_seconds = value; }
        public int StartOffsetMilliseconds { get => start_offset_milliseconds; set => start_offset_milliseconds = value; }
        public int EndSeconds { get => end_seconds; set => end_seconds = value; }
        public int LinkedDistance { get => linked_distance; set => linked_distance = value; }
        public int Type { get => type; set => type = value; }
        public int Ranking { get => ranking; set => ranking = value; }
        public bool SMSEnabled { get => sms_enabled; set => sms_enabled = value; }

        public int CompareTo(Distance other)
        {
            if (other == null) return 1;
            if (this.EventIdentifier == other.EventIdentifier)
            {
                return this.Name.CompareTo(other.Name);
            }
            return this.EventIdentifier.CompareTo(other.EventIdentifier);
        }

        public bool Equals(Distance other)
        {
            if (other == null) return false;
            return this.EventIdentifier == other.EventIdentifier && this.Identifier == other.Identifier;
        }

        public void Update(Distance other)
        {
            this.Name = other.Name;
            this.EventIdentifier = other.EventIdentifier;
            this.DistanceValue = other.DistanceValue;
            this.DistanceUnit = other.DistanceUnit;
            this.StartLocation = other.StartLocation;
            this.StartWithin = other.StartWithin;
            this.FinishLocation = other.FinishLocation;
            this.FinishOccurrence = other.FinishOccurrence;
            this.Wave = other.Wave;
            this.StartOffsetSeconds = other.StartOffsetSeconds;
            this.StartOffsetMilliseconds = other.StartOffsetMilliseconds;
            this.EndSeconds = other.EndSeconds;
            this.LinkedDistance = other.LinkedDistance;
            this.Type = other.Type;
            this.Ranking = other.Ranking;
            this.SMSEnabled = other.SMSEnabled;
        }

        public void SetWaveTime(int wave, long seconds, int milliseconds)
        {
            this.Wave = wave;
            this.StartOffsetSeconds = (int)seconds;
            this.StartOffsetMilliseconds = milliseconds;
        }
    }
}
