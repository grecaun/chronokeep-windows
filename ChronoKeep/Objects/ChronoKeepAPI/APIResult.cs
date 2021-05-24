using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Objects.API
{
    public class APIResult
    {
        [JsonProperty("bib")]
        public string Bib { get; set; }
        [JsonProperty("first")]
        public string First { get; set; }
        [JsonProperty("last")]
        public string Last { get; set; }
        [JsonProperty("age")]
        public int Age { get; set; }
        [JsonProperty("gender")]
        public string Gender { get; set; }
        [JsonProperty("age_group")]
        public string AgeGroup { get; set; }
        [JsonProperty("distance")]
        public string Distance { get; set; }
        [JsonProperty("seconds")]
        public int Seconds { get; set; }
        [JsonProperty("milliseconds")]
        public int Milliseconds { get; set; }
        [JsonProperty("segment")]
        public string Segment { get; set; }
        [JsonProperty("location")]
        public string Location { get; set; }
        [JsonProperty("occurence")]
        public int Occurence { get; set; }
        [JsonProperty("ranking")]
        public int Ranking { get; set; }
        [JsonProperty("age_ranking")]
        public int AgeRanking { get; set; }
        [JsonProperty("gender_ranking")]
        public int GenderRanking { get; set; }
        [JsonProperty("finish")]
        public bool Finish { get; set; }
    }
}
