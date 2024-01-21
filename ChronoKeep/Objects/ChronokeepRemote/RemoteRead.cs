using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronokeepRemote
{
    public class RemoteRead
    {
        // Identifier is either a BIB or a CHIP value.
        [JsonPropertyName("identifier")]
        public string Identifier { get; set; }
        [JsonPropertyName("seconds")]
        public long Seconds { get; set; }
        [JsonPropertyName("milliseconds")]
        public int Milliseconds { get; set; }
        [JsonPropertyName("ident_type")]
        public string IdentType { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("antenna")]
        public int Antenna { get; set; }
        [JsonPropertyName("reader")]
        public string Reader { get; set; }
        [JsonPropertyName("rssi")]
        public string RSSI { get; set; }
    }
}
