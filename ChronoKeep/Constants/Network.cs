using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Constants
{
    internal class Network
    {
        public const string DEFAULT_CHRONOKEEP_SERVER_NAME = "Chronokeep Registration";
        public const string CHRONOKEEP_ZCONF_MULTICAST_IP  = "224.0.44.88";
        public const string CHRONOKEEP_ZCONF_CONNECT_MSG   = "[DISCOVER_CHRONO_SERVER_REQUEST]";
        public const int    CHRONOKEEP_ZCONF_PORT          = 4488;

        public const string CHRONOKEEP_REGISTRATION_TYPE = "CHRONOKEEP_WINDOWS";
        public const int    CHRONOKEEP_REGISTRATION_VERS = 1;
    }
}
