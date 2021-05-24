using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Constants
{
    public class ResultsAPI
    {
        public const string CHRONOKEEP = "CHRONOKEEP_V1";
        public const string CHRONOKEEP_SELF = "CHRONOKEEP_V1_SELF";

        public const string CHRONOKEEP_URL = "https://api.chronokeep.com/";

        public static readonly Dictionary<string, string> API_TYPE_NAMES = new Dictionary<string, string>()
        {
            { CHRONOKEEP, "Chronokeep" },
            { CHRONOKEEP_SELF, "Chronokeep (Self Hosted)" }
        };
    }
}
