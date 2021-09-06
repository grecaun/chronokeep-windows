using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoKeep.Objects
{
    public class AgeGroup : IEquatable<AgeGroup>, IComparable<AgeGroup>
    {
        private int group_id, event_id, distance_id, start_age, end_age, last_group = Constants.Timing.AGEGROUPS_LASTGROUP_FALSE;

        private static Dictionary<(int, int), AgeGroup> CurrentGroups = null;
        private static Dictionary<int, AgeGroup> LastAgeGroup = null;
        private static Mutex AGMutex = new Mutex();

        public AgeGroup(int eventId, int distanceId, int startAge, int endAge)
        {
            this.group_id = -1;
            this.event_id = eventId;
            this.distance_id = distanceId;
            this.start_age = startAge;
            this.end_age = endAge;
            this.last_group = Constants.Timing.AGEGROUPS_LASTGROUP_FALSE;
        }

        public AgeGroup(int groupId, int eventId, int distanceId, int startAge, int endAge, int last_group)
        {
            this.group_id = groupId;
            this.event_id = eventId;
            this.distance_id = distanceId;
            this.start_age = startAge;
            this.end_age = endAge;
            this.last_group = last_group;
        }

        public int EventId { get => event_id; set => event_id = value; }
        public int DistanceId { get => distance_id; set => distance_id = value; }
        public int StartAge { get => start_age; set => start_age = value; }
        public int EndAge { get => end_age; set => end_age = value; }
        public int GroupId { get => group_id; set => group_id = value; }
        public bool LastGroup { get => last_group == Constants.Timing.AGEGROUPS_LASTGROUP_TRUE;
            set => last_group = value ? Constants.Timing.AGEGROUPS_LASTGROUP_TRUE : Constants.Timing.AGEGROUPS_LASTGROUP_FALSE; }
        public string Name { get => LastGroup ? string.Format("{0}+", start_age) : string.Format("{0}-{1}", start_age, end_age); }

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

        public static Dictionary<(int, int), AgeGroup> GetAgeGroups()
        {
            Dictionary<(int, int), AgeGroup> output = null;
            if (!AGMutex.WaitOne(3000))
            {
                return output;
            }
            output = CurrentGroups;
            AGMutex.ReleaseMutex();
            return output;
        }

        public static Dictionary<int, AgeGroup> GetLastAgeGroup()
        {
            Dictionary<int, AgeGroup> output = null;
            if (!AGMutex.WaitOne(3000))
            {
                return output;
            }
            output = LastAgeGroup;
            AGMutex.ReleaseMutex();
            return output;
        }

        public static void SetAgeGroups(List<AgeGroup> groups)
        {
            CurrentGroups = new Dictionary<(int,int), AgeGroup>();
            LastAgeGroup = new Dictionary<int, AgeGroup>();
            groups.Sort();
            foreach (AgeGroup group in groups)
            {
                for (int i = group.StartAge; i <= group.EndAge; i++)
                {
                    CurrentGroups[(group.DistanceId, i)] = group;
                }
                if (!LastAgeGroup.ContainsKey(group.DistanceId) || LastAgeGroup[group.DistanceId].StartAge < group.StartAge)
                {
                    LastAgeGroup[group.DistanceId] = group;
                }
            }
        }

        public bool Equals(AgeGroup that)
        {
            return this.event_id == that.event_id && this.distance_id == that.distance_id && this.start_age == that.start_age && this.end_age == that.start_age;
        }
    }
}
