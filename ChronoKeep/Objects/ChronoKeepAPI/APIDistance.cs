using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronoKeepAPI
{
    public class APIDistance
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("certification")]
        public string Certification { get; set; }
    }
}
