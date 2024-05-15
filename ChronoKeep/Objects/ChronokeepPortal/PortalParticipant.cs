using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronokeepPortal
{
    public class PortalParticipant
    {
        [JsonPropertyName("id")]
        public string Identifier { get; set; }
        [JsonPropertyName("bib")]
        public string Bib { get; set; }
        [JsonPropertyName("first")]
        public string First { get; set; }
        [JsonPropertyName("last")]
        public string Last { get; set; }
        [JsonPropertyName("birthdate")]
        public string Birthdate { get; set; }
        [JsonPropertyName("gender")]
        public string Gender { get; set; }
        [JsonPropertyName("age_group")]
        public string AgeGroup { get; set; }
        [JsonPropertyName("distance")]
        public string Distance { get; set; }
        [JsonPropertyName("anonymous")]
        public bool Anonymous { get; set; }
        [JsonPropertyName("sms_enabled")]
        public bool SMSEnabled { get; set; }
        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }
        [JsonPropertyName("apparel")]
        public string Apparel { get; set; }
    }
}
