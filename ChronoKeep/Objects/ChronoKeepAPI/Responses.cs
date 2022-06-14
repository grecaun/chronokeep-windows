using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        [JsonProperty("events")]
        public List<APIEvent> Events { get; set; }
    }

    public class ModifyEventResponse
    {
        [JsonProperty("event")]
        public APIEvent Event { get; set; }
    }

    // Event Year specific responses.
    public class GetEventYearsResponse
    {
        [JsonProperty("years")]
        public List<APIEventYear> EventYears { get; set; }
    }

    public class EventYearResponse
    {
        [JsonProperty("event")]
        public APIEvent Event { get; set; }
        [JsonProperty("event_year")]
        public APIEventYear EventYear { get; set; }
    }

    // Results specific responses.
    public class AddResultsResponse
    {
        [JsonProperty("count")]
        public int Count { get; set; }
    }

    // Error response.
    public class ErrorResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
