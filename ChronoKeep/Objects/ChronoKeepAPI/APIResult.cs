﻿using Chronokeep.Helpers;
using System;
using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronoKeepAPI
{
    public class APIResult
    {
        public APIResult(Event theEvent, TimeResult result, DateTime start, string unique_pad)
        {
            PersonId = string.Format("{0}-{1}", result.EventSpecificId, unique_pad);
            Bib = result.Bib.ToString();
            First = result.Anonymous ? "" : result.First;
            Last = result.Anonymous ? "" : result.Last;
            Age = result.Age(theEvent.Date);
            Gender = result.Gender;
            AgeGroup = result.PrettyAgeGroupName();
            Distance = result.DistanceName;
            ChipSeconds = (int)result.ChipSeconds;
            ChipMilliseconds = result.ChipMilliseconds;
            Segment = result.SegmentName;
            Location = result.LocationName;
            Occurence = result.Occurrence;
            Ranking = result.Place;
            AgeRanking = result.AgePlace;
            GenderRanking = result.GenderPlace;
            Finish = result.SegmentId == Constants.Timing.SEGMENT_FINISH;
            Type = result.Type;
            Anonymous = result.Anonymous;
            LocalTime = start.AddSeconds(result.Seconds).AddMilliseconds(result.Milliseconds).ToLocalTime().ToString("o");
            Division = result.Division;
            DivisionRanking = result.DivisionPlace;
            if (result.Status == Constants.Timing.TIMERESULT_STATUS_DNF)
            {
                Type = Constants.Timing.API_TYPE_DNF;
                Seconds = 0;
                Milliseconds = 0;
            }
            else
            {
                string[] split1 = result.Time.Split('.');
                string[] split2 = split1[0].Split(':');
                Log.D("Objects.API.APIResult", string.Format("Time is {0} - Split1 {1} - Split2 length {2}", result.Time, split1[0], split2.Length));
                switch (split2.Length)
                {
                    case 3:
                        // HOURS : MINUTES : SECONDS -- Seconds * 1 + Minutes * 60 + Hours * 60 * 60
                        Seconds = Convert.ToInt32(split2[2]) + Convert.ToInt32(split2[1]) * 60 + Convert.ToInt32(split2[0]) * 60 * 60;
                        break;
                    case 2:
                        // MINUTES : SECONDS -- Seconds * 1 + Minutes * 60
                        Seconds = Convert.ToInt32(split2[1]) + Convert.ToInt32(split2[0]) * 60;
                        break;
                    case 1:
                        // SECONDS
                        Seconds = Convert.ToInt32(split2[0]);
                        break;
                    default:
                        Seconds = 0;
                        break;
                }
                if (split1.Length == 2)
                {
                    Milliseconds = Convert.ToInt32(split1[1]);
                }
                else
                {
                    Milliseconds = 0;
                }
            }
        }

        [JsonPropertyName("person_id")]
        public string PersonId { get; set; }
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
        [JsonPropertyName("seconds")]
        public int Seconds { get; set; }
        [JsonPropertyName("milliseconds")]
        public int Milliseconds { get; set; }
        [JsonPropertyName("chip_seconds")]
        public int ChipSeconds { get; set; }
        [JsonPropertyName("chip_milliseconds")]
        public int ChipMilliseconds { get; set; }
        [JsonPropertyName("segment")]
        public string Segment { get; set; }
        [JsonPropertyName("location")]
        public string Location { get; set; }
        [JsonPropertyName("occurence")]
        public int Occurence { get; set; }
        [JsonPropertyName("ranking")]
        public int Ranking { get; set; }
        [JsonPropertyName("age_ranking")]
        public int AgeRanking { get; set; }
        [JsonPropertyName("gender_ranking")]
        public int GenderRanking { get; set; }
        [JsonPropertyName("finish")]
        public bool Finish { get; set; }
        [JsonPropertyName("type")]
        public int Type { get; set; }
        [JsonPropertyName("anonymous")]
        public bool Anonymous { get; set; }
        [JsonPropertyName("local_time")]
        public string LocalTime { get; set; }
        [JsonPropertyName("division")]
        public string Division { get; set; }
        [JsonPropertyName("division_ranking")]
        public int DivisionRanking { get; set; }
    }
}
