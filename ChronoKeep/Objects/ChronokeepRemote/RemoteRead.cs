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
        public IdentType IdentType { get; set; }
        [JsonPropertyName("type")]
        public ReadType Type { get; set; }
        [JsonPropertyName("antenna")]
        public int Antenna { get; set; }
        [JsonPropertyName("reader")]
        public string Reader { get; set; }
        [JsonPropertyName("rssi")]
        public string RSSI { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum IdentType
    {
        bib,
        chip,
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReadType
    {
        reader,
        manual,
    }
}
