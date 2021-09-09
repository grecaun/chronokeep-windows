using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Constants
{
    public class Timing
    {
        public static readonly int LOCATION_START = -1;
        public static readonly int LOCATION_FINISH = -2;
        public static readonly int LOCATION_ANNOUNCER = -3;
        public static readonly int LOCATION_DUMMY = -12;

        public static readonly int SEGMENT_FINISH = -1;
        public static readonly int SEGMENT_START = -2;
        public static readonly int SEGMENT_NONE = -3;

        public static readonly int CHIPREAD_STATUS_NONE = 0; // If this value changes, SQLiteInterface version must be updated to reflect said change.
        public static readonly int CHIPREAD_STATUS_UNUSEDSTART = 1;
        public static readonly int CHIPREAD_STATUS_FORCEIGNORE = 2;
        public static readonly int CHIPREAD_STATUS_PRESTART = 3;
        public static readonly int CHIPREAD_STATUS_USED = 4;
        public static readonly int CHIPREAD_STATUS_WITHINIGN = 5;  // Within the ignore period
        public static readonly int CHIPREAD_STATUS_OVERMAX = 6; // over max occurrences
        public static readonly int CHIPREAD_STATUS_UNKNOWN = 7; // Unknown chip read
        public static readonly int CHIPREAD_STATUS_STARTTIME = 8;
        public static readonly int CHIPREAD_STATUS_DNF = 9;
        public static readonly int CHIPREAD_STATUS_ANNOUNCER_SEEN = 10;
        public static readonly int CHIPREAD_STATUS_ANNOUNCER_USED = 11;

        public static readonly int TIMERESULT_STATUS_NONE = 0;
        public static readonly int TIMERESULT_STATUS_DNF = 1;
        public static readonly int TIMERESULT_UPLOADED_FALSE = 0;
        public static readonly int TIMERESULT_UPLOADED_TRUE = 1;

        public static readonly int CHIPREAD_TYPE_MANUAL = 1;
        public static readonly int CHIPREAD_TYPE_CHIP = 0;  // If this value changes, SQLiteInterface version must be updated to reflect said change.

        public static readonly string CHIPREAD_DUMMYCHIP = "-1";
        public static readonly int CHIPREAD_DUMMYBIB = -1;  // If this value changes, SQLiteInterface version must be updated to reflect said change.

        public static readonly int TIMERESULT_DUMMYPERSON = -1;
        public static readonly int TIMERESULT_DUMMYPLACE = -1;
        public static readonly int TIMERESULT_GENDER_MALE = 1;
        public static readonly int TIMERESULT_GENDER_FEMALE = 2;
        public static readonly int TIMERESULT_GENDER_UNKNOWN = 3;
        public static readonly int TIMERESULT_DUMMYAGEGROUP = -1;

        public static readonly int EVENTSPECIFIC_NOSHOW = 0;
        public static readonly int EVENTSPECIFIC_STARTED = 1;
        public static readonly int EVENTSPECIFIC_FINISHED = 2;
        public static readonly int EVENTSPECIFIC_NOFINISH = 3;

        public static readonly int COMMON_SEGMENTS_DISTANCEID = -1;
        public static readonly int COMMON_AGEGROUPS_DISTANCEID = -1;

        public static readonly int AGEGROUPS_CUSTOM_DISTANCEID = -8;
        public static readonly int AGEGROUPS_LASTGROUP_TRUE = 1;
        public static readonly int AGEGROUPS_LASTGROUP_FALSE = 0;

        public static readonly int EVENT_TYPE_DISTANCE = 0;
        public static readonly int EVENT_TYPE_TIME = 1;

        public static readonly int TIMINGSYSTEM_UNKNOWN = -1;

        public static readonly int PARTICIPANT_DUMMYIDENTIFIER = -1;
        public static readonly int DISTANCE_DUMMYIDENTIFIER = -1;
        public static readonly int DISTANCE_NO_LINKED_ID = -1;

        // These values are what are sent in the API Result and indicate the type of the result.
        public static readonly int DISTANCE_TYPE_NORMAL = 0;
        public static readonly int DISTANCE_TYPE_EARLY = 1;
        public static readonly int DISTANCE_TYPE_UNOFFICIAL = 2;
        public static readonly int API_TYPE_DNF = 3;
        public static readonly int API_TYPE_DNS = 4;

        // API Upload Count
        public static readonly int API_LOOP_COUNT = 20;

        // Announcer variables
        public static readonly int ANNOUNCER_LOOP_TIMER = 2;
        public static readonly int ANNOUNCER_DISPLAY_WINDOW = -45;

        public static long DateToEpoch(DateTime date)
        {
            var ticks = date.Ticks - new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            return ticks / TimeSpan.TicksPerSecond;
        }

        public static DateTime EpochToDate(long date)
        {
            return new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(date * TimeSpan.TicksPerSecond);
        }

        public static readonly Dictionary<string, string> SYSTEM_NAMES = new Dictionary<string, string>()
        {
            { Settings.TIMING_RFID, "RFID Timing Systems" },
            { Settings.TIMING_IPICO, "Ipico Elite Reader" },
            { Settings.TIMING_IPICO_LITE, "Ipico Lite Reader" }
        };

        public static readonly Dictionary<int, string> EVENTSPECIFIC_STATUS_NAMES = new Dictionary<int, string>()
        {
            { EVENTSPECIFIC_NOSHOW, "DNS" },
            { EVENTSPECIFIC_STARTED, "Started" },
            { EVENTSPECIFIC_FINISHED, "Finished" },
            { EVENTSPECIFIC_NOFINISH, "DNF" },
        };

        public static string SecondsToTime(long seconds)
        {
            return string.Format("{0}:{1:D2}:{2:D2}", seconds / 3600, (seconds % 3600) / 60, seconds % 60);
        }

        public static string ToTime(long seconds, int milliseconds)
        {
            return string.Format("{0:D}:{1:D2}:{2:D2}.{3:D3}", seconds / 3600, (seconds % 3600) / 60, seconds % 60, milliseconds);
        }
    }
}
