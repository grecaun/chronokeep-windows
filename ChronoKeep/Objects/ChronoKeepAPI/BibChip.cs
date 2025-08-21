using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronoKeepAPI
{
    public class BibChip
    {
        [JsonPropertyName("bib")]
        public string Bib { get; set; }
        [JsonPropertyName("chip")]
        public string Chip { get; set; }
    }
}
