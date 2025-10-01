using System;

namespace Chronokeep.Objects
{
    public class Segment : IEquatable<Segment>, IComparable<Segment>
    {
        private int id, event_id, distance_id, location_id, occurrence;
        private double distance_segment, distance_cumulative;
        private int distance_unit;
        private string name, gps, map_link;

        public Segment(Segment seg)
        {
            id = -1;
            event_id = seg.event_id;
            distance_id = seg.distance_id;
            location_id = seg.location_id;
            occurrence = seg.occurrence;
            distance_segment = seg.distance_segment;
            distance_cumulative = seg.distance_cumulative;
            distance_unit = seg.distance_unit;
            name = seg.name ?? "";
            gps = seg.gps;
            map_link = seg.map_link;
        }

        public Segment(
            int e,
            int d,
            int l,
            int occ,
            double dseg,
            double dcum,
            int dunit,
            string n,
            string gps,
            string ml
            )
        {
            id = -1;
            event_id = e;
            distance_id = d;
            location_id = l;
            occurrence = occ;
            distance_segment = dseg;
            distance_cumulative = dcum;
            distance_unit = dunit;
            name = n ?? "";
            this.gps = gps;
            map_link = ml;
        }

        public Segment(
            int id,
            int e,
            int d,
            int l,
            int occ,
            double dseg,
            double dcum,
            int dunit,
            string n,
            string gps,
            string ml
            )
        {
            this.id = id;
            event_id = e;
            distance_id = d;
            location_id = l;
            occurrence = occ;
            distance_segment = dseg;
            distance_cumulative = dcum;
            distance_unit = dunit;
            name = n ?? "";
            this.gps = gps;
            map_link = ml;
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
            if (event_id != other.event_id)
            {
                return event_id.CompareTo(other.event_id);
            } 
            if (other.distance_id != distance_id)
            {
                return distance_id.CompareTo(other.distance_id);
            }
            if (distance_cumulative != other.distance_cumulative)
            {
                return distance_cumulative.CompareTo(other.distance_cumulative);
            }
            if (location_id != other.location_id)
            {
                return location_id.CompareTo(other.location_id);
            }
            return occurrence.CompareTo(other.occurrence);
        }

        public bool Equals(Segment other)
        {
            return event_id == other.event_id &&
                distance_id == other.distance_id &&
                location_id == other.location_id &&
                occurrence == other.occurrence;
        }

        public void CopyFrom(Segment other)
        {
            EventId = other.EventId;
            DistanceId = other.DistanceId;
            LocationId = other.LocationId;
            Occurrence = other.Occurrence;
            Name = other.Name;
            SegmentDistance = other.SegmentDistance;
            CumulativeDistance = other.CumulativeDistance;
            DistanceUnit = other.DistanceUnit;
            GPS = other.GPS;
            MapLink = other.MapLink;
        }
    }
}
