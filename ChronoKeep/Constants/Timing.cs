using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Constants
{
    public class Timing
    {
        public static readonly int LOCATION_FINISH = -2;
        public static readonly int LOCATION_START = -1;
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

        public static readonly int TIMERESULT_STATUS_NONE = 0;
        public static readonly int TIMERESULT_STATUS_DNF = 1;

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

        public static readonly int DEFAULT_BIB_GROUP = -1;

        public static readonly int COMMON_SEGMENTS_DIVISIONID = -1;
        public static readonly int COMMON_AGEGROUPS_DIVISIONID = -1;

        public static readonly int AGEGROUPS_CUSTOM_DIVISIONID = -8;
        public static readonly int AGEGROUPS_LASTGROUP_TRUE = 1;
        public static readonly int AGEGROUPS_LASTGROUP_FALSE = 0;

        public static readonly int EVENT_TYPE_DISTANCE = 0;
        public static readonly int EVENT_TYPE_TIME = 1;

        public static readonly int TIMINGSYSTEM_UNKNOWN = -1;

        public static readonly int PARTICIPANT_DUMMYIDENTIFIER = -1;

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
    }
}
