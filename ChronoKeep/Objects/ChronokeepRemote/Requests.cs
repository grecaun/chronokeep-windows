using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronokeepRemote
{
    /*
     * Reads requests
     */
    public class GetReadsRequest
    {
        [JsonPropertyName("reader")]
        public string ReaderName { get; set; }
        [JsonPropertyName("start")]
        public long Start { get; set; }
        [JsonPropertyName("end")]
        public long End { get; set; }
    }

    public class DeleteReadsRequest
    {
        [JsonPropertyName("reader")]
        public string ReaderName { get; set; }
        [JsonPropertyName("start")]
        public long Start { get; set; }
        [JsonPropertyName("end")]
        public long End { get; set; }
    }
}
