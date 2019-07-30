using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Objects
{
    public class AgeGroup : IEquatable<AgeGroup>, IComparable<AgeGroup>
    {
        private int group_id, event_id, division_id, start_age, end_age;

        public AgeGroup(int eventId, int divisionId, int startAge, int endAge)
        {
            this.group_id = -1;
            this.event_id = eventId;
            this.division_id = divisionId;
            this.start_age = startAge;
            this.end_age = endAge;
        }

        public AgeGroup(int groupId, int eventId, int divisionId, int startAge, int endAge)
        {
            this.group_id = groupId;
            this.event_id = eventId;
            this.division_id = divisionId;
            this.start_age = startAge;
            this.end_age = endAge;
        }

        public int EventId { get => event_id; set => event_id = value; }
        public int DivisionId { get => division_id; set => division_id = value; }
        public int StartAge { get => start_age; set => start_age = value; }
        public int EndAge { get => end_age; set => end_age = value; }
        public int GroupId { get => group_id; set => group_id = value; }

        public int CompareTo(AgeGroup other)
        {
            if (other == null) return 1;
            if (this.event_id != other.event_id)
            {
                return this.event_id.CompareTo(other.event_id);
            }
            if (this.division_id != other.division_id)
            {
                return this.division_id.CompareTo(other.division_id);
            }
            return this.start_age.CompareTo(other.start_age);
        }

        public bool Equals(AgeGroup that)
        {
            return this.event_id == that.event_id && this.division_id == that.division_id && this.start_age == that.start_age && this.end_age == that.start_age;
        }
    }
}
