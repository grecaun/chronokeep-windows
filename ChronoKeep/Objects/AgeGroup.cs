using System;
using System.Collections.Generic;
using System.Threading;

namespace Chronokeep.Objects
{
    public class AgeGroup : IEquatable<AgeGroup>, IComparable<AgeGroup>
    {
        private int group_id, event_id, distance_id, start_age, end_age, last_group = Constants.Timing.AGEGROUPS_LASTGROUP_FALSE;
        private string custom_name;

        public AgeGroup(int eventId, int distanceId, int startAge, int endAge, string custom_name = "")
        {
            this.group_id = -1;
            this.event_id = eventId;
            this.distance_id = distanceId;
            this.start_age = startAge;
            this.end_age = endAge;
            this.last_group = Constants.Timing.AGEGROUPS_LASTGROUP_FALSE;
            this.custom_name = custom_name;
        }

        public AgeGroup(int groupId, int eventId, int distanceId, int startAge, int endAge, int last_group, string custom_name)
        {
            this.group_id = groupId;
            this.event_id = eventId;
            this.distance_id = distanceId;
            this.start_age = startAge;
            this.end_age = endAge;
            this.last_group = last_group;
            this.custom_name = custom_name;
        }

        public int EventId { get => event_id; set => event_id = value; }
        public int DistanceId { get => distance_id; set => distance_id = value; }
        public int StartAge { get => start_age; set => start_age = value; }
        public int EndAge { get => end_age; set => end_age = value; }
        public int GroupId { get => group_id; set => group_id = value; }
        public bool LastGroup { get => last_group == Constants.Timing.AGEGROUPS_LASTGROUP_TRUE;
            set => last_group = value ? Constants.Timing.AGEGROUPS_LASTGROUP_TRUE : Constants.Timing.AGEGROUPS_LASTGROUP_FALSE; }
        public string Name { get => LastGroup ? string.Format("Over {0}", start_age) : string.Format("{0}-{1}", start_age, end_age); }
        public string CustomName { get => custom_name; set => custom_name = value; }
        public string PrettyName()
        {
            if (custom_name.Length > 0)
            {
                return custom_name;
            }
            if (start_age < 1 && end_age > 0)
            {
                return string.Format("Under {0}", end_age + 1);
            }
            else if (end_age >= 99)
            {
                return string.Format("Over {0}", start_age);
            }
            return Name;
        }

        public int CompareTo(AgeGroup other)
        {
            if (other == null) return 1;
            if (this.event_id != other.event_id)
            {
                return this.event_id.CompareTo(other.event_id);
            }
            if (this.distance_id != other.distance_id)
            {
                return this.distance_id.CompareTo(other.distance_id);
            }
            return this.start_age.CompareTo(other.start_age);
        }

        public bool Equals(AgeGroup that)
        {
            return this.event_id == that.event_id && this.distance_id == that.distance_id && this.start_age == that.start_age && this.end_age == that.start_age;
        }
    }
}
