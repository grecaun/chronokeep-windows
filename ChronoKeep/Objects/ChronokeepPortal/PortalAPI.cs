using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Chronokeep.Objects.ChronokeepPortal
{
    public class PortalAPI
    {
        public static readonly string API_TYPE_CHRONOKEEP_RESULTS       = "CHRONOKEEP_RESULTS";
        public static readonly string API_TYPE_CHRONOKEEP_RESULTS_SELF  = "CHRONOKEEP_RESULTS_SELF";
        public static readonly string API_TYPE_CHRONOKEEP_REMOTE        = "CHRONOKEEP_REMOTE";
        public static readonly string API_TYPE_CHRONOKEEP_REMOTE_SELF   = "CHRONOKEEP_REMOTE_SELF";
        public static readonly string API_URI_CHRONOKEEP_RESULTS        = @"https://api.chronokeep.com/";
        public static readonly string API_URI_CHRONOKEEP_REMOTE         = @"https://remote.chronokeep.com/";

        [JsonPropertyName("id")]
        public uint Id { get; set; }
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }
        [JsonPropertyName("kind")]
        public string Kind { get; set; }
        [JsonPropertyName("token")]
        public string Token { get; set; }
        [JsonPropertyName("uri")]
        public string Uri { get; set; }
    }
}
