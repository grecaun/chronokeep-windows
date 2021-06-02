using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep
{
    public class Division : IEquatable<Division>, IComparable<Division>
    {
        private String name;
        private int identifier, eventIdentifier, cost;
        private double distance;
        private int distance_unit = Constants.Distances.MILES, finish_location = Constants.Timing.LOCATION_FINISH,
            finish_occurrence = 1, start_location = Constants.Timing.LOCATION_START, start_within = 5,
            end_seconds = 0, early_start_offset_seconds = 0;
        private int wave = 1, bib_group_number = -1, start_offset_seconds = 0, start_offset_milliseconds = 0;
        private int linked_division = -1, type = 0, ranking = 0;

        public Division() { }

        public Division(string name, int eventIdentifier, int cost)
        {
            this.name = name;
            this.eventIdentifier = eventIdentifier;
            this.cost = cost;
        }

        public Division(string name, int eventIdentifier, int linkedIdentifier, int type, int ranking, int wave, int start_offset_seconds, int start_offset_milliseconds)
        {
            this.name = name;
            this.eventIdentifier = eventIdentifier;
            this.linked_division = linkedIdentifier;
            this.type = type;
            this.ranking = ranking;
            this.wave = wave;
            this.start_offset_seconds = start_offset_seconds;
            this.start_offset_milliseconds = start_offset_milliseconds;
        }

        public Division(int identifier, string name, int eventIdentifier,
            int cost, double distance, int dunit, int finloc, int finocc,
            int startloc, int startwith, int wave, int bgn, int soffsec, int soffmill,
            int endseconds, int earlyoffset, int linked_division, int type, int ranking)
        {
            this.identifier = identifier;
            this.name = name;
            this.eventIdentifier = eventIdentifier;
            this.cost = cost;
            this.distance = distance;
            this.distance_unit = dunit;
            this.finish_location = finloc;
            this.finish_occurrence = finocc;
            this.start_location = startloc;
            this.start_within = startwith;
            this.wave = wave;
            this.bib_group_number = bgn;
            this.start_offset_seconds = soffsec;
            this.start_offset_milliseconds = soffmill;
            this.end_seconds = endseconds;
            this.early_start_offset_seconds = earlyoffset;
            this.linked_division = linked_division;
            this.type = type;
            this.ranking = ranking;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; set => name = value; }
        public int EventIdentifier { get => eventIdentifier; set => eventIdentifier = value; }
        public int Cost { get => cost; set => cost = value; }
        public double Distance { get => distance; set => distance = value; }
        public int DistanceUnit { get => distance_unit; set => distance_unit = value; }
        public int FinishLocation { get => finish_location; set => finish_location = value; }
        public int FinishOccurrence { get => finish_occurrence; set => finish_occurrence = value; }
        public int StartLocation { get => start_location; set => start_location = value; }
        public int StartWithin { get => start_within; set => start_within = value; }
        public int Wave { get => wave; set => wave = value; }
        public int BibGroupNumber { get => bib_group_number; set => bib_group_number = value; }
        public int StartOffsetSeconds { get => start_offset_seconds; set => start_offset_seconds = value; }
        public int StartOffsetMilliseconds { get => start_offset_milliseconds; set => start_offset_milliseconds = value; }
        public int EndSeconds { get => end_seconds; set => end_seconds = value; }
        public int EarlyStartOffsetSeconds { get => early_start_offset_seconds; set => early_start_offset_seconds = value; }
        public int LinkedDivision { get => linked_division; set => LinkedDivision = value; }
        public int Type { get => type; set => type = value; }
        public int Ranking { get => ranking; set => ranking = value; }

        public int CompareTo(Division other)
        {
            if (other == null) return 1;
            if (this.EventIdentifier == other.EventIdentifier)
            {
                return this.Name.CompareTo(other.Name);
            }
            return this.EventIdentifier.CompareTo(other.EventIdentifier);
        }

        public bool Equals(Division other)
        {
            if (other == null) return false;
            return this.EventIdentifier == other.EventIdentifier && this.Identifier == other.Identifier;
        }

        public string GetEarlyStartString()
        {
            int hours = early_start_offset_seconds / 3600;
            int minutes = (early_start_offset_seconds % 3600) / 60;
            int seconds = early_start_offset_seconds % 60;
            if (early_start_offset_seconds < 0)
            {
                hours = minutes = seconds = 0;
            }
            return String.Format("{0,2:D2}:{1,2:D2}:{2,2:D2}", hours, minutes, seconds);
        }
    }
}
