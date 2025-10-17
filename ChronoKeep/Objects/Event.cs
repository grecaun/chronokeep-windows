using System;

namespace Chronokeep.Objects
{
    public class Event : IEquatable<Event>, IComparable<Event>
    {
        private int identifier;
        private int common_age_groups = 1, common_start_finish = 1, distance_specific_segments = 0, rank_by_gun = 1;
        private int finish_max_occurrences = 1, finish_ignore_within = 0, start_window = -1, start_max_occurrences = 1;
        private int event_type = Constants.Timing.EVENT_TYPE_DISTANCE;
        private string name, date, yearcode = "";
        private long start_seconds = -1;
        private int start_milliseconds;
        private int api_id = Constants.APIConstants.NULL_ID;
        private string api_event_id = Constants.APIConstants.NULL_EVENT_ID;
        private int display_placements = 1, divisions_enabled = 0, days_allowed = 1, upload_specific = 0;

        public Event() { }

        public Event(string n, long d)
        {
            DateTime time = new(d);
            date = time.ToShortDateString();
            name = n;
            start_window = 600;
            finish_ignore_within = 600;
        }

        public Event(string n, long d, string yearcode)
        {
            DateTime time = new(d);
            date = time.ToShortDateString();
            name = n;
            this.yearcode = yearcode;
            start_window = 600;
            finish_ignore_within = 600;
        }

        public Event(int id, string n, long d)
        {
            identifier = id;
            name = n;
            DateTime time = new(d);
            date = time.ToShortDateString();
            start_window = 600;
            finish_ignore_within = 600;
        }

        public Event(string n, long d, int age, int start, int seg, int gun)
        {
            DateTime time = new(d);
            date = time.ToShortDateString();
            name = n;
            common_age_groups = age;
            common_start_finish = start;
            distance_specific_segments = seg;
            rank_by_gun = gun;
            start_window = 600;
            finish_ignore_within = 600;
        }

        public Event(int id, string n, long d, int age, int start, int seg, int gun)
        {
            identifier = id;
            name = n;
            DateTime time = new(d);
            date = time.ToShortDateString();
            common_age_groups = age;
            common_start_finish = start;
            distance_specific_segments = seg;
            rank_by_gun = gun;
            start_window = 600;
            finish_ignore_within = 600;
        }

        public Event(int id, string n, string d, int age, int start, int seg,
            int gun, string yearcode, int maxOcc, int ignWith, int window,
            long startsec, int startmill, int type, int api_id,
            string api_event_id, int display_placements, int age_groups_as_divisions,
            int days_allowed, int upload_specific, int start_max_occurrences)
        {
            identifier = id;
            name = n;
            DateTime time = DateTime.Parse(d);
            date = time.ToShortDateString();
            common_age_groups = age;
            common_start_finish = start;
            distance_specific_segments = seg;
            rank_by_gun = gun;
            this.yearcode = yearcode;
            finish_max_occurrences = maxOcc;
            finish_ignore_within = ignWith;
            start_window = window;
            start_seconds = startsec;
            start_milliseconds = startmill;
            event_type = type;
            this.api_id = api_id;
            this.api_event_id = api_event_id;
            this.display_placements = display_placements;
            divisions_enabled = age_groups_as_divisions;
            this.days_allowed = days_allowed;
            this.upload_specific = upload_specific;
            this.start_max_occurrences = start_max_occurrences;
        }

        public int Identifier { get => identifier; set => identifier = value; }
        public string Name { get => name; set => name = value; }
        public string Date { get => date; set => date = value; }
        public string LongDate { get => DateTime.Parse(date).ToString("MMMM d, yyyy"); }
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
        public int StartMaxOccurrences { get => start_max_occurrences; set => start_max_occurrences = value; }
        public int StartMilliseconds { get => start_milliseconds; set => start_milliseconds = value; }
        public int EventType { get => event_type; set => event_type = value; }
        public int API_ID { get => api_id; set => api_id = value; }
        public string API_Event_ID { get => api_event_id; set => api_event_id = value; }
        public bool DisplayPlacements { get => display_placements != 0; set => display_placements = value ? 1 : 0; }
        public bool DivisionsEnabled { get => divisions_enabled != 0; set => divisions_enabled = value ? 1 : 0; }
        public int DaysAllowed { get => days_allowed; set => days_allowed = value; }
        public bool UploadSpecific { get => upload_specific != 0; set => upload_specific = value ? 1 : 0; }

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
            DateTime thisDate = DateTime.Parse(Date);
            DateTime otherDate = DateTime.Parse(other.Date);
            return thisDate.CompareTo(otherDate) * -1;
        }

        public bool Equals(Event other)
        {
            if (other == null) return false;
            return Date == other.Date && name == other.name || Identifier == other.Identifier;
        }

        public void CopyFrom(Event other)
        {
            EventType = other.EventType;
            StartWindow = other.StartWindow;
            StartMaxOccurrences = other.StartMaxOccurrences;
            FinishIgnoreWithin = other.FinishIgnoreWithin;
            FinishMaxOccurrences = other.FinishMaxOccurrences;
            CommonAgeGroups = other.CommonAgeGroups;
            CommonStartFinish = other.CommonStartFinish;
            DistanceSpecificSegments = other.DistanceSpecificSegments;
            DisplayPlacements = other.DisplayPlacements;
            DivisionsEnabled = other.DivisionsEnabled;
            DaysAllowed = other.DaysAllowed;
            RankByGun = other.RankByGun;
        }
    }
}
