using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronokeepRemote
{
    /**
     * Responses for read requests
     */
    public class GetReadsResponse
    {
        [JsonPropertyName("count")]
        public long Count { get; set; }
        [JsonPropertyName("reads")]
        public List<RemoteRead> Reads { get; set; }
    }

    public class DeleteReadsResponse
    {
        [JsonPropertyName("count")]
        public long Count { get; set; }
    }

    /*
     * Response for readers request.
     */
    public class GetReadersResponse
    {
        [JsonPropertyName("readers")]
        public List<RemoteReader> Readers { get; set; }
    }
}
