using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Chronokeep.Objects.ChronokeepPortal
{
    public class PortalRead
    {
        public static string READ_KIND_CHIP         = "reader";
        public static string READ_KIND_MANUAL       = "manual";
        public static string READ_IDENT_TYPE_CHIP   = "chip";
        public static string READ_IDENT_TYPE_BIB    = "bib";

        [JsonPropertyName("chip")]
        public string Chip { get; set; }
        [JsonPropertyName("seconds")]
        public ulong Seconds { get; set; }
        [JsonPropertyName("milliseconds")]
        public uint Milliseconds { get; set; }
        [JsonPropertyName("antenna")]
        public uint Antenna { get; set; }
        [JsonPropertyName("reader")]
        public string Reader { get; set; }
        [JsonPropertyName("rssi")]
        public string RSSI { get; set; }
        [JsonPropertyName("ident_type")]
        public string IdentType { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
