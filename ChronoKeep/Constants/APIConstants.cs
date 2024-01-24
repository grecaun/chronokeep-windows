using System.Collections.Generic;

namespace Chronokeep.Constants
{
    public class APIConstants
    {
        public const string CHRONOKEEP_RESULTS        = "CHRONOKEEP_V1";
        public const string CHRONOKEEP_RESULTS_SELF   = "CHRONOKEEP_V1_SELF";
        public const string CHRONOKEEP_REMOTE         = "CHRONOKEEP_REMOTE_V1";
        public const string CHRONOKEEP_REMOTE_SELF    = "CHRONOKEEP_REMOTE_V1_SELF";

        public const int    NULL_ID         = -1;
        public const string NULL_EVENT_ID   = "";

        public const string CHRONOKEEP_EVENT_TYPE_DISTANCE          = "distance";
        public const string CHRONOKEEP_EVENT_TYPE_TIME              = "time";
        public const string CHRONOKEEP_EVENT_TYPE_BACKYARD_ULTRA    = "backyardultra";
        public const string CHRONOKEEP_EVENT_TYPE_UNKNOWN           = "unknown";

        public static readonly Dictionary<string, string> API_TYPE_NAMES = new()
        {
            { CHRONOKEEP_RESULTS,       "Chronokeep Results" },
            { CHRONOKEEP_RESULTS_SELF,  "Chronokeep Results (Self Hosted)" },
            { CHRONOKEEP_REMOTE,        "Chronokeep Remote" },
            { CHRONOKEEP_REMOTE_SELF,   "Chronokeep Remote (Self Hosted)" }
        };

        public static readonly Dictionary<string, bool> API_SELF_HOSTED = new()
        {
            { CHRONOKEEP_RESULTS,       false },
            { CHRONOKEEP_RESULTS_SELF,  true },
            { CHRONOKEEP_REMOTE,        false },
            { CHRONOKEEP_REMOTE_SELF,   true }
        };

        public static readonly Dictionary<string, string> API_URL = new()
        {
            { CHRONOKEEP_RESULTS,       "https://api.chronokeep.com/" },
            { CHRONOKEEP_REMOTE,        "https://remote.chronokeep.com/" },
            { CHRONOKEEP_RESULTS_SELF,  "" },
            { CHRONOKEEP_REMOTE_SELF,   "" }
        };

        public static readonly Dictionary<string, bool> API_RESULTS = new()
        {
            { CHRONOKEEP_RESULTS,       true },
            { CHRONOKEEP_RESULTS_SELF,  true },
            { CHRONOKEEP_REMOTE,        false },
            { CHRONOKEEP_REMOTE_SELF,   false }
        };

        public static readonly Dictionary<string, bool> API_REMOTE = new()
        {
            { CHRONOKEEP_RESULTS,       false },
            { CHRONOKEEP_RESULTS_SELF,  false },
            { CHRONOKEEP_REMOTE,        true },
            { CHRONOKEEP_REMOTE_SELF,   true }
        };
    }
}
