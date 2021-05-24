using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Objects.API
{
    /*
     * 
     * Classes for dealing with ChronoKeep API requests.
     * 
     */

    // General request with only the key
    public class GeneralRequest
    {
        [JsonProperty("key")]
        public string Key { get; set; }
    }

    // Event specific requests
    public class GetEventRequest
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
    }

    public class ModifyEventRequest
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("event")]
        public APIEvent Event { get; set; }
    }

    // Event Year specific requests.
    public class GetEventYearRequest
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("year")]
        public string Year { get; set; }
    }

    public class ModifyEventYearRequest
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("event_year")]
        public APIEventYear Year { get; set; }
    }

    // Result specific requests
    public class GetResultsRequest
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("year")]
        public string Year { get; set; }
    }

    public class AddResultsRequest
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("year")]
        public string Year { get; set; }
        [JsonProperty("results")]
        public List<APIResult> Results { get; set; }
    }
}
