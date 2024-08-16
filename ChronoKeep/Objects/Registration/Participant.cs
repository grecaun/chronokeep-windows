using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Chronokeep.Objects.Registration
{
    public class Participant
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("bib")]
        public string Bib { get; set; }
        [JsonPropertyName("first")]
        public string FirstName { get; set; }
        [JsonPropertyName("last")]
        public string LastName { get; set; }
        [JsonPropertyName("birthdate")]
        public string Birthdate { get; set; }
        [JsonPropertyName("gender")]
        public string Gender { get; set; }
        [JsonPropertyName("distance")]
        public string Distance { get; set; }
        [JsonPropertyName("mobile")]
        public string Mobile { get; set; }
        [JsonPropertyName("sms")]
        public bool SMSEnabled { get; set; }
        [JsonPropertyName("apparel")]
        public string Apparel { get; set; }
    }
}
