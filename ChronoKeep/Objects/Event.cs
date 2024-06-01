using System;

namespace Chronokeep
{
    public class Event : IEquatable<Event>, IComparable<Event>
    {
        private int identifier;
        private int common_age_groups = 1, common_start_finish = 1, distance_specific_segments = 0, rank_by_gun = 1;
        private int finish_max_occurrences = 1, finish_ignore_within = 0, start_window = -1;
        private int event_type = Constants.Timing.EVENT_TYPE_DISTANCE;
        private string name, date, yearcode = "", long_date;
        private long start_seconds = -1;
        private int start_milliseconds;
        private int api_id = Constants.APIConstants.NULL_ID;
        private string api_event_id = Constants.APIConstants.NULL_EVENT_ID;
        private int display_placements = 1, age_groups_as_divisions = 0;

        public Event() { }

        public Event(string n, long d)
        {
            DateTime time = new DateTime(d);
            this.date = time.ToShortDateString();
            this.long_date = time.ToString("MMMM d, yyyy");
            this.name = n;
            this.start_window = 600;
            this.finish_ignore_within = 600;
        }

        public Event(string n, long d, string yearcode)
        {
            DateTime time = new DateTime(d);
            this.date = time.ToShortDateString();
            this.long_date = time.ToString("MMMM d, yyyy");
            this.name = n;
            this.yearcode = yearcode;
            this.start_window = 600;
            this.finish_ignore_within = 600;
        }

        public Event(int id, string n, long d)
        {
            this.identifier = id;
            this.name = n;
            DateTime time = new DateTime(d);
            this.date = time.ToShortDateString();
            this.long_date = time.ToString("MMMM d, yyyy");
            this.start_window = 600;
            this.finish_ignore_within = 600;
        }

        public Event(string n, long d, int age, int start, int seg, int gun)
        {
            DateTime time = new DateTime(d);
            this.date = time.ToShortDateString();
            this.long_date = time.ToString("MMMM d, yyyy");
            this.name = n;
            this.common_age_groups = age;
            this.common_start_finish = start;
            this.distance_specific_segments = seg;
            this.rank_by_gun = gun;
            this.start_window = 600;
            this.finish_ignore_within = 600;
        }

        public Event(int id, string n, long d, int age, int start, int seg, int gun)
        {
            this.identifier = id;
            this.name = n;
            DateTime time = new DateTime(d);
            this.date = time.ToShortDateString();
            this.long_date = time.ToString("MMMM d, yyyy");
            this.common_age_groups = age;
            this.common_start_finish = start;
            this.distance_specific_segments = seg;
            this.rank_by_gun = gun;
            this.start_window = 600;
            this.finish_ignore_within = 600;
        }

        public Event(int id, string n, string d, int age, int start, int seg,
            int gun, string yearcode, int maxOcc, int ignWith, int window,
            long startsec, int startmill, int type, int api_id,
            string api_event_id, int display_placements, int age_groups_as_divisions)
        {
            this.identifier = id;
            this.name = n;
            DateTime time = DateTime.Parse(d);
            this.date = time.ToShortDateString();
            this.long_date = time.ToString("MMMM d, yyyy");
            this.common_age_groups = age;
            this.common_start_finish = start;
            this.distance_specific_segments = seg;
            this.rank_by_gun = gun;
            this.yearcode = yearcode;
            this.finish_max_occurrences = maxOcc;
            this.finish_ignore_within = ignWith;
            this.start_window = window;
            this.start_seconds = startsec;
            this.start_milliseconds = startmill;
            this.event_type = type;
            this.api_id = api_id;
            this.api_event_id = api_event_id;
            this.display_placements = display_placements;
            this.age_groups_as_divisions = age_groups_as_divisions;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; set => name = value; }
        public string Date { get => date; set => date = value; }
        public string LongDate { get => long_date; set => long_date = value; }
        public bool CommonAgeGroups { get => common_age_groups != 0; set => common_age_groups = value ? 1 : 0; }
        public bool CommonStartFinish { get => common_start_finish != 0; set => common_start_finish = value ? 1 : 0; }
        public bool DistanceSpecificSegments { get => distance_specific_segments != 0; set => distance_specific_segments = value ? 1 : 0; }
        public bool RankByGun { get => rank_by_gun != 0; set => rank_by_gun = value ? 1 : 0; }
        public string YearCode { get => yearcode; set => yearcode = value; }
        public string Year { get => date.Split('/').Length == 3 ? date.Split('/')[2] : ""; }
        public int StartWindow { get => start_window; set => start_window = value; }
        public int FinishMaxOccurrences { get => finish_max_occurrences; set => finish_max_occurrences = value; }
        public int FinishIgnoreWithin { get => finish_ignore_within; set => finish_ignore_within = value; }
        public long StartSeconds { get => start_seconds; set => start_seconds = value; }
        public int StartMilliseconds { get => start_milliseconds; set => start_milliseconds = value; }
        public int EventType { get => event_type; set => event_type = value; }
        public int API_ID { get => api_id; set => api_id = value; }
        public string API_Event_ID { get => api_event_id; set => api_event_id = value; }
        public bool DisplayPlacements { get => display_placements != 0; set => display_placements = value ? 1 : 0; }
        public bool AgeGroupDivision { get => age_groups_as_divisions != 0; set => age_groups_as_divisions = value ? 1 : 0; }

        public string EventTypeString
        {
            get
            {
                if (event_type == Constants.Timing.EVENT_TYPE_TIME)
                {
                    return "Time Based";
                }
                else if (event_type == Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA)
                {
                    return "Backyard Ultra";
                }
                return "Distance Based";
            }
        }

        public int CompareTo(Event other)
        {
            if (other == null) return 1;
            DateTime thisDate = DateTime.Parse(this.Date);
            DateTime otherDate = DateTime.Parse(other.Date);
            return thisDate.CompareTo(otherDate) * -1;
        }

        public bool Equals(Event other)
        {
            if (other == null) return false;
            return (this.Date == other.Date && this.name == other.name) || this.Identifier == other.Identifier;
        }
    }
}
