﻿using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronokeepPortal
{
    public class PortalParticipant
    {
        [JsonPropertyName("bib")]
        public string Bib { get; set; }
        [JsonPropertyName("first")]
        public string First { get; set; }
        [JsonPropertyName("last")]
        public string Last { get; set; }
        [JsonPropertyName("age")]
        public int Age { get; set; }
        [JsonPropertyName("gender")]
        public string Gender { get; set; }
        [JsonPropertyName("age_group")]
        public string AgeGroup { get; set; }
        [JsonPropertyName("distance")]
        public string Distance { get; set; }
        [JsonPropertyName("chip")]
        public string Chip { get; set; }
        [JsonPropertyName("anonymous")]
        public bool Anonymous { get; set; }
    }
}
