﻿using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronoKeepAPI
{
    public class APISegment
    {
        [JsonPropertyName("locaton")]
        public string Location { get; set; }
        [JsonPropertyName("distance_name")]
        public string DistanceName { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("distance_value")]
        public double DistanceValue { get; set; }
        [JsonPropertyName("distance_unit")]
        public string DistanceUnit { get; set; }
        [JsonPropertyName("gps")]
        public string GPS { get; set; }
        [JsonPropertyName("map_link")]
        public string MapLink { get; set; }
    }
}
