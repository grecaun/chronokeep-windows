using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Constants
{
    class Readers
    {
        public const string SYSTEM_RFID = "RFID";
        public const string SYSTEM_IPICO = "IPICO";
        public const string SYSTEM_IPICO_LITE = "IPICO_LITE";
        public const string SYSTEM_CHRONOKEEP_PORTAL = "CHRONOKEEP_PORTAL";

        public const string DEFAULT_TIMING_SYSTEM = SYSTEM_RFID;

        public const int RFID_DEFAULT_PORT = 23;
        public const int IPICO_DEFAULT_PORT = 10000;
        public const int IPICO_CONTROL_PORT = 9999;
        public const int CHRONO_PORTAL_ZCONF_PORT = 4488;

        public const string CHRONO_PORTAL_ZCONF_IP = "224.0.44.88";
        public const string CHRONO_PORTAL_CONNECT_MSG = "[DISCOVER_CHRONO_SERVER_REQUEST]";

        public static readonly Dictionary<string, string> SYSTEM_NAMES = new Dictionary<string, string>()
        {
            { SYSTEM_RFID, "RFID Timing Systems" },
            { SYSTEM_IPICO, "Ipico Elite Reader" },
            { SYSTEM_IPICO_LITE, "Ipico Lite Reader" },
            { SYSTEM_CHRONOKEEP_PORTAL, "Chronokeep Portal"},
        };

    }
}
