using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EventDirector
{
    public class TimeResult
    {
        int eventId, eventspecificId, locationId, segmentId,
            occurrence, bib, readId;
        string time, locationName, segmentName, participantName,
            divisionName, unknownId, chipTime;
        DateTime systemTime;

        public static readonly Regex timeRegex = new Regex(@"(\d+):(\d{2}):(\d{2})\.(\d{3})");

        public static Dictionary<int, TimingLocation> locations = null;
        public static Dictionary<int, Segment> segments = null;

        public TimeResult(int eventId, int eventspecificId, int locationId, int segmentId,
            string time, int occurrence, string first, string last, string division, int bib,
            int readId, string unknownId, string systemTime, string chipTime)
        {
            this.eventId = eventId;
            this.eventspecificId = eventspecificId;
            this.locationId = locationId;
            this.segmentId = segmentId;
            this.time = time;
            this.occurrence = occurrence;
            this.locationName = locations != null ? locations.ContainsKey(this.locationId) ?
                locations[this.locationId].Name : "Unknown" : "Unknown";
            if (Constants.Timing.SEGMENT_FINISH == this.segmentId)
            {
                this.segmentName = "Finish";
            }
            else if (Constants.Timing.SEGMENT_START == this.segmentId)
            {
                this.segmentName = "Start";
            }
            else if (segments != null && segments.ContainsKey(this.segmentId))
            {
                this.segmentName = segments[this.segmentId].Name;
            }
            else
            {
                this.segmentName = "";
            }
            this.participantName = String.Format("{0} {1}", first, last).Trim();
            this.divisionName = division;
            this.bib = bib;
            this.unknownId = unknownId;
            this.readId = readId;
            this.systemTime = DateTime.Parse(systemTime);
            this.chipTime = chipTime;
        }

        public TimeResult(int eventId, int readId, int eventspecificId, int locationId,
            int segmentId, int occurrence, string time, string unknownId, string chipTime,
            DateTime systemTime, int bib)
        {
            this.eventId = eventId;
            this.readId = readId;
            this.eventspecificId = eventspecificId;
            this.locationId = locationId;
            this.segmentId = segmentId;
            this.occurrence = occurrence;
            this.time = time;
            this.unknownId = unknownId;
            this.chipTime = chipTime;
            this.systemTime = systemTime;
            this.bib = bib;
        }

        public static void SetupStaticVariables(IDBInterface database)
        {
            if (locations != null && segments != null)
            {
                return;
            }
            locations = new Dictionary<int, TimingLocation>();
            segments = new Dictionary<int, Segment>();
            Event theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            if (theEvent.CommonStartFinish != 1)
            {
                locations[Constants.Timing.LOCATION_FINISH] = new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin);
                locations[Constants.Timing.LOCATION_START] = new TimingLocation(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow);
            }
            else
            {
                locations[Constants.Timing.LOCATION_FINISH] = new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin);
            }
            foreach (TimingLocation loc in database.GetTimingLocations(theEvent.Identifier))
            {
                locations[loc.Identifier] = loc;
            }
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                segments[seg.Identifier] = seg;
            }
        }

        public int EventSpecificId { get => eventspecificId; set => eventspecificId = value; }
        public int LocationId { get => locationId; set => locationId = value; }
        public int EventIdentifier { get => eventId; set => eventId = value; }
        public int SegmentId { get => segmentId; set => segmentId = value; }
        public int Occurrence { get => occurrence; set => occurrence = value; }
        public string Time { get => time; set => time = value; }
        public string LocationName { get => locationName; set => locationName = value; }
        public string SegmentName { get => segmentName; set => segmentName = value; }
        public string ParticipantName { get => participantName; set => participantName = value; }
        public string DivisionName { get => divisionName; set => divisionName = value; }
        public int Bib { get => bib; set => bib = value; }
        public string UnknownId { get => unknownId; set => unknownId = value; }
        public int EventId { get => eventId; set => eventId = value; }
        public int EventspecificId { get => eventspecificId; set => eventspecificId = value; }
        public int ReadId { get => readId; set => readId = value; }
        public string Identifier {
            get
            {
                if (eventspecificId == Constants.Timing.TIMERESULT_DUMMYPERSON)
                {
                    return unknownId;
                }
                return bib.ToString();
            }
        }

        public DateTime SystemTime { get => systemTime; set => systemTime = value; }

        public string SysTime
        {
            get
            {
                return systemTime.ToString("MMM dd HH:mm:ss.fff");
            }
        }

        public string ChipTime { get => chipTime; set => chipTime = value; }

        public static int CompareByGunTime(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            Match oneMatch = timeRegex.Match(one.Time);
            Match twoMatch = timeRegex.Match(two.Time);
            if (oneMatch == null || twoMatch == null) return 1;
            long oneTime = (Convert.ToInt64(oneMatch.Groups[1].Value) * 3600)
                + (Convert.ToInt64(oneMatch.Groups[2].Value) * 60)
                + Convert.ToInt64(oneMatch.Groups[3].Value);
            long twoTime = (Convert.ToInt64(twoMatch.Groups[1].Value) * 3600)
                + (Convert.ToInt64(twoMatch.Groups[2].Value) * 60)
                + Convert.ToInt64(twoMatch.Groups[3].Value);
            return oneTime.CompareTo(twoTime);
        }

        public static int CompareBySystemTime(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            return one.systemTime.CompareTo(two.systemTime);
        }

        public static int CompareByBib(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            if (one.Bib == two.Bib)
            {
                one.systemTime.CompareTo(two.systemTime);
            }
            return one.Bib.CompareTo(two.Bib);
        }

        public static int CompareByDivision(TimeResult one, TimeResult two)
        {
            if (one == null || two == null) return 1;
            if (one.DivisionName.Equals(two.DivisionName))
            {
                return one.systemTime.CompareTo(two.systemTime);
            }
            return one.DivisionName.CompareTo(two.DivisionName);
        }

        public static bool IsNotKnown(TimeResult one)
        {
            return one.EventSpecificId == Constants.Timing.TIMERESULT_DUMMYPERSON;
        }

        public static bool IsNotStart(TimeResult one)
        {
            return one.SegmentId != Constants.Timing.SEGMENT_START;
        }

        public static bool IsNotFinish(TimeResult one)
        {
            return one.SegmentId != Constants.Timing.SEGMENT_FINISH;
        }

        public bool IsNotMatch(string value)
        {
            return this.Bib.ToString().IndexOf(value, StringComparison.OrdinalIgnoreCase) == -1 &&
                this.ParticipantName.IndexOf(value, StringComparison.OrdinalIgnoreCase) == -1;
        }
    }
}
