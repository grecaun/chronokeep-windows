using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector.Constants
{
    public class DefaultTiming
    {
        public static readonly int LOCATION_FINISH = -2;
        public static readonly int LOCATION_START = -1;
        public static readonly int SEGMENT_FINISH = -1;

        public static readonly int DEFAULT_BIB_GROUP = -1;

        public static readonly Dictionary<string, string> SYSTEM_NAMES = new Dictionary<string, string>()
        {
            { Constants.Settings.TIMING_MANUAL, "Manual" },
            { Constants.Settings.TIMING_RFID, "Chip: RFID" },
            { Constants.Settings.TIMING_IPICO, "Chip: Ipico" }
        };
    }
}
