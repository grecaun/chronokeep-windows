using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    public class Segment
    {
        private int event_id, division_id, location_id, occurance;
        private double distance_segment, distance_cumulative;
        private int distance_unit;
        private string name;

        public string Name { get => name; set => name = value; }
        public int DistanceUnit { get => distance_unit; set => distance_unit = value; }
        public double SegmentDistance { get => distance_segment; set => distance_segment = value; }
        public double CumulativeDistance { get => distance_cumulative; set => distance_cumulative = value; }
        public int EventId { get => event_id; set => event_id = value; }
        public int DivisionId { get => division_id; set => division_id = value; }
        public int LocationId { get => location_id; set => location_id = value; }
        public int Occurance { get => occurance; set => occurance = value; }
    }
}
