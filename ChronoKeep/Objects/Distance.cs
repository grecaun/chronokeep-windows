using System;

namespace Chronokeep.Objects
{
    public class Distance : IEquatable<Distance>, IComparable<Distance>
    {
        private string name, certification = "";
        private int identifier, eventIdentifier;
        private double distance;
        private int distance_unit = Constants.Distances.MILES, finish_location = Constants.Timing.LOCATION_FINISH,
            finish_occurrence = 1, start_location = Constants.Timing.LOCATION_START, start_within = 0,
            end_seconds = 0;
        private int wave = 1, start_offset_seconds = 0, start_offset_milliseconds = 0;
        private int linked_distance = Constants.Timing.DISTANCE_NO_LINKED_ID, type = 0, ranking = 0;
        private bool sms_enabled = false, upload = false;

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
            linked_distance = linkedIdentifier;
            this.type = type;
            this.ranking = ranking;
            this.wave = wave;
            this.start_offset_seconds = start_offset_seconds;
            this.start_offset_milliseconds = start_offset_milliseconds;
            upload = false;
        }

        public Distance(int identifier, string name, int eventIdentifier,
            double distance, int distance_unit, int finish_location, int finish_occurrence,
            int start_location, int start_within, int wave, int start_offset_seconds, int start_offset_milliseconds,
            int endseconds, int linked_distance, int type, int ranking, bool sms_enabled, bool upload, string certification)
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
            end_seconds = endseconds;
            this.linked_distance = linked_distance;
            this.type = type;
            this.ranking = ranking;
            this.sms_enabled = sms_enabled;
            this.upload = upload;
            this.certification = certification;
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
        public bool Upload { get => upload; set => upload = value; }
        public string Certification { get => certification; set => certification = value; }

        public int CompareTo(Distance other)
        {
            if (other == null) return 1;
            if (EventIdentifier == other.EventIdentifier)
            {
                return Name.CompareTo(other.Name);
            }
            return EventIdentifier.CompareTo(other.EventIdentifier);
        }

        public bool Equals(Distance other)
        {
            if (other == null) return false;
            return EventIdentifier == other.EventIdentifier && Identifier == other.Identifier;
        }

        public void Update(Distance other)
        {
            Name = other.Name;
            EventIdentifier = other.EventIdentifier;
            DistanceValue = other.DistanceValue;
            DistanceUnit = other.DistanceUnit;
            StartLocation = other.StartLocation;
            StartWithin = other.StartWithin;
            FinishLocation = other.FinishLocation;
            FinishOccurrence = other.FinishOccurrence;
            Wave = other.Wave;
            StartOffsetSeconds = other.StartOffsetSeconds;
            StartOffsetMilliseconds = other.StartOffsetMilliseconds;
            EndSeconds = other.EndSeconds;
            LinkedDistance = other.LinkedDistance;
            Type = other.Type;
            Ranking = other.Ranking;
            SMSEnabled = other.SMSEnabled;
            Certification = other.Certification;
        }

        public void SetWaveTime(int wave, long seconds, int milliseconds)
        {
            Wave = wave;
            StartOffsetSeconds = (int)seconds;
            StartOffsetMilliseconds = milliseconds;
        }
    }
}
