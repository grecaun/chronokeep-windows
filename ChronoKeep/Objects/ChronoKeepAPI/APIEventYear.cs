using System.Text.Json.Serialization;

namespace Chronokeep.Objects.API
{
    public class APIEventYear
    {
        [JsonPropertyName("year")]
        public string Year { get; set; }
        [JsonPropertyName("date_time")]
        public string DateTime { get; set; }
        [JsonPropertyName("live")]
        public bool Live { get; set; }
    }
}
