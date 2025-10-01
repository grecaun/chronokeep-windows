using System;

namespace Chronokeep.Objects
{
    public class TimingLocation : IEquatable<TimingLocation>, IComparable<TimingLocation>
    {
        private int identifier = -1, eventIdentifier, max_occurrences, ignore_within;
        private string name;

        public TimingLocation() { }

        public TimingLocation(int eventIdentifier, string nameString)
        {
            this.eventIdentifier = eventIdentifier;
            name = nameString ?? "";
            max_occurrences = 1;
            ignore_within = -1;
        }

        public TimingLocation(int identifier, int eventIdentifier, string nameString)
        {
            this.identifier = identifier;
            this.eventIdentifier = eventIdentifier;
            name = nameString ?? "";
            max_occurrences = 1;
            ignore_within = -1;
        }

        public TimingLocation(int id, int eventId, string name, int maxOcc, int ignore)
        {
            identifier = id;
            eventIdentifier = eventId;
            this.name = name ?? "";
            max_occurrences = maxOcc;
            ignore_within = ignore;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
        public string Name { get => name; set => name = value ?? ""; }
        public int MaxOccurrences { get => max_occurrences; set => max_occurrences = value; }
        public int IgnoreWithin { get => ignore_within; set => ignore_within = value; }

        public int CompareTo(TimingLocation other)
        {
            if (other == null) return 1;
            return Identifier.CompareTo(other.Identifier);
        }

        public bool Equals(TimingLocation other)
        {
            if (other == null) return false;
            return Identifier == other.Identifier && EventIdentifier == other.EventIdentifier;
        }

        public void CopyFrom(TimingLocation other)
        {
            EventIdentifier = other.EventIdentifier;
            Name = other.Name;
            MaxOccurrences = other.MaxOccurrences;
            IgnoreWithin = other.IgnoreWithin;
        }
    }
}
