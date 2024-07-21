using System;

namespace Chronokeep
{
    public class Segment : IEquatable<Segment>, IComparable<Segment>
    {
        private int id, event_id, distance_id, location_id, occurrence;
        private double distance_segment, distance_cumulative;
        private int distance_unit;
        private string name, gps, map_link;

        public Segment(Segment seg)
        {
            this.id = -1;
            this.event_id = seg.event_id;
            this.distance_id = seg.distance_id;
            this.location_id = seg.location_id;
            this.occurrence = seg.occurrence;
            this.distance_segment = seg.distance_segment;
            this.distance_cumulative = seg.distance_cumulative;
            this.distance_unit = seg.distance_unit;
            this.name = seg.name ?? "";
            this.gps = seg.gps;
            this.map_link = seg.map_link;
        }

        public Segment(int e, int d, int l, int occ, double dseg, double dcum, int dunit, string n, string gps, string ml)
        {
            this.id = -1;
            this.event_id = e;
            this.distance_id = d;
            this.location_id = l;
            this.occurrence = occ;
            this.distance_segment = dseg;
            this.distance_cumulative = dcum;
            this.distance_unit = dunit;
            this.name = n ?? "";
            this.gps = gps;
            this.map_link = ml;
        }

        public Segment(int id, int e, int d, int l, int occ, double dseg, double dcum, int dunit, string n, string gps, string ml)
        {
            this.id = id;
            this.event_id = e;
            this.distance_id = d;
            this.location_id = l;
            this.occurrence = occ;
            this.distance_segment = dseg;
            this.distance_cumulative = dcum;
            this.distance_unit = dunit;
            this.name = n ?? "";
            this.gps = gps;
            this.map_link = ml;
        }

        public string Name { get => name; set => name = value ?? ""; }
        public int DistanceUnit { get => distance_unit; set => distance_unit = value; }
        public double SegmentDistance { get => distance_segment; set => distance_segment = value; }
        public double CumulativeDistance { get => distance_cumulative; set => distance_cumulative = value; }
        public int EventId { get => event_id; set => event_id = value; }
        public int DistanceId { get => distance_id; set => distance_id = value; }
        public int LocationId { get => location_id; set => location_id = value; }
        public int Occurrence { get => occurrence; set => occurrence = value; }
        public int Identifier { get => id; set => id = value; }
        public string GPS { get => gps; set => gps = value; }
        public string MapLink { get => map_link; set => map_link = value; }

        public int CompareTo(Segment other)
        {
            if (other == null) return 1;
            if (this.event_id != other.event_id)
            {
                return this.event_id.CompareTo(other.event_id);
            } 
            if (other.distance_id != this.distance_id)
            {
                return this.distance_id.CompareTo(other.distance_id);
            }
            if (this.location_id != other.location_id)
            {
                return this.location_id.CompareTo(other.location_id);
            }
            return this.occurrence.CompareTo(other.occurrence);
        }

        public bool Equals(Segment other)
        {
            return this.event_id == other.event_id &&
                this.distance_id == other.distance_id &&
                this.location_id == other.location_id &&
                this.occurrence == other.occurrence;
        }
    }
}
