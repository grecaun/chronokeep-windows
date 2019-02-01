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

        public static readonly int CHIPREAD_STATUS_NONE = 0;// If this value changes, SQLiteInterface version must be updated to reflect said change.
        public static readonly int CHIPREAD_STATUS_TOUCHED = 1;
        public static readonly int CHIPREAD_STATUS_IGNORE = 2;

        public static readonly int CHIPREAD_TYPE_MANUAL = 1;
        public static readonly int CHIPREAD_TYPE_CHIP = 0;  // If this value changes, SQLiteInterface version must be updated to reflect said change.

        public static readonly int CHIPREAD_DUMMYCHIP = -1;
        public static readonly int CHIPREAD_DUMMYBIB = -1;  // If this value changes, SQLiteInterface version must be updated to reflect said change.

        public static readonly int DEFAULT_BIB_GROUP = -1;

        public static readonly Dictionary<string, string> SYSTEM_NAMES = new Dictionary<string, string>()
        {
            { Constants.Settings.TIMING_MANUAL, "Manual" },
            { Constants.Settings.TIMING_RFID, "Chip: RFID" },
            { Constants.Settings.TIMING_IPICO, "Chip: Ipico" }
        };
    }
}
