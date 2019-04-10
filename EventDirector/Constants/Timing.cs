using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector.Constants
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

        public static readonly int DEFAULT_BIB_GROUP = -1;

        public static readonly int COMMON_SEGMENTS_DIVISIONID = -1;
        public static readonly int COMMON_AGEGROUPS_DIVISIONID = -1;

        public static readonly int EVENT_TYPE_DISTANCE = 0;
        public static readonly int EVENT_TYPE_TIME = 1;

        public static readonly Dictionary<string, string> SYSTEM_NAMES = new Dictionary<string, string>()
        {
            { Constants.Settings.TIMING_MANUAL, "Manual" },
            { Constants.Settings.TIMING_RFID, "Chip: RFID" },
            { Constants.Settings.TIMING_IPICO, "Chip: Ipico" }
        };
    }
}
