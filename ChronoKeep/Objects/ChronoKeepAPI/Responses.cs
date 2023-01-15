using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chronokeep.Objects.API
{
    /*
     * 
     * Classes for dealing with ChronoKeep API responses.
     * 
     */

    // Event specific responses.
    public class GetEventsResponse
    {
        [JsonPropertyName("events")]
        public List<APIEvent> Events { get; set; }
    }

    public class ModifyEventResponse
    {
        [JsonPropertyName("event")]
        public APIEvent Event { get; set; }
    }

    // Event Year specific responses.
    public class GetEventYearsResponse
    {
        [JsonPropertyName("years")]
        public List<APIEventYear> EventYears { get; set; }
    }

    public class EventYearResponse
    {
        [JsonPropertyName("event")]
        public APIEvent Event { get; set; }
        [JsonPropertyName("event_year")]
        public APIEventYear EventYear { get; set; }
    }

    // Results specific responses.
    public class AddResultsResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    // Participant specific responses.
    public class GetParticipantsResponse
    {
        [JsonPropertyName("participants")]
        public List<APIPerson> Participants { get; set; }
    }

    // Error response.
    public class ErrorResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
