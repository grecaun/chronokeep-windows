﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Constants
{
    public class ResultsAPI
    {
        public static readonly string CHRONOKEEP = "CHRONOKEEP_V1";
        public static readonly string CHRONOKEEP_SELF = "CHRONOKEEP_V1_SELF";

        public static readonly int NULL_ID = -1;
        public static readonly string NULL_EVENT_ID = "";

        public static readonly string CHRONOKEEP_URL = "https://api.chronokeep.com/";

        public static readonly Dictionary<string, string> API_TYPE_NAMES = new Dictionary<string, string>()
        {
            { CHRONOKEEP, "Chronokeep" },
            { CHRONOKEEP_SELF, "Chronokeep (Self Hosted)" }
        };
    }
}
