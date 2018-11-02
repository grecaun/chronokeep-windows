using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class Segment
    {
        private int id, event_id, division_id, location_id, occurance;
        private double distance_segment, distance_cumulative;
        private int distance_unit;
        private string name;

        public Segment(int e, int d, int l, int occ, int dseg, int dcum, int dunit, string n)
        {
            this.id = -1;
            this.event_id = e;
            this.division_id = d;
            this.location_id = l;
            this.occurance = occ;
            this.distance_segment = dseg;
            this.distance_cumulative = dcum;
            this.distance_unit = dunit;
            this.name = n;
        }

        public Segment(int id, int e, int d, int l, int occ, int dseg, int dcum, int dunit, string n)
        {
            this.id = id;
            this.event_id = e;
            this.division_id = d;
            this.location_id = l;
            this.occurance = occ;
            this.distance_segment = dseg;
            this.distance_cumulative = dcum;
            this.distance_unit = dunit;
            this.name = n;
        }

        public string Name { get => name; set => name = value; }
        public int DistanceUnit { get => distance_unit; set => distance_unit = value; }
        public double SegmentDistance { get => distance_segment; set => distance_segment = value; }
        public double CumulativeDistance { get => distance_cumulative; set => distance_cumulative = value; }
        public int EventId { get => event_id; set => event_id = value; }
        public int DivisionId { get => division_id; set => division_id = value; }
        public int LocationId { get => location_id; set => location_id = value; }
        public int Occurance { get => occurance; set => occurance = value; }
        public int Identifier { get => id; set => id = value; }
    }
}
