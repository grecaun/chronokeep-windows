using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chronokeep.Objects.API
{
    /*
     * 
     * Classes for dealing with ChronoKeep API requests.
     * 
     */

    // Event specific requests
    public class GetEventRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
    }

    public class ModifyEventRequest
    {
        [JsonPropertyName("event")]
        public APIEvent Event { get; set; }
    }

    // Event Year specific requests.
    public class GetEventYearRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    public class ModifyEventYearRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("event_year")]
        public APIEventYear Year { get; set; }
    }

    // Result specific requests
    public class GetResultsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    public class AddResultsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
        [JsonPropertyName("results")]
        public List<APIResult> Results { get; set; }
    }

    // Participant specific requests
    public class GetParticipantsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    public class AddParticipantsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
        [JsonPropertyName("participants")]
        public List<APIPerson> Participants { get; set; }
    }

    public class DeleteParticipantsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
        [JsonPropertyName("bibs")]
        public List<string> Bibs { get; set; }
    }
}
