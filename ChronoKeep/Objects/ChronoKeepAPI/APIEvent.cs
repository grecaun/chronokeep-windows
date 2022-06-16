using System.Text.Json.Serialization;

namespace Chronokeep.Objects.API
{
    public class APIEvent
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("website")]
        public string Website { get; set; }
        [JsonPropertyName("image")]
        public string Image { get; set; }
        [JsonPropertyName("contact_email")]
        public string ContactEmail { get; set; }
        [JsonPropertyName("access_restricted")]
        public bool AccessRestricted { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
