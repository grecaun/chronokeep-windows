using System;
using System.Text.Json.Serialization;

namespace Chronokeep.Objects.API
{
    public class APIResult
    {
        public APIResult(Event theEvent, TimeResult result, DateTime start, string unique_pad)
        {
            this.PersonId = string.Format("{0}-{1}", result.ParticipantId, unique_pad);
            this.Bib = result.Bib.ToString();
            this.First = result.Anonymous ? "" : result.First;
            this.Last = result.Anonymous ? "" : result.Last;
            this.Age = result.Age(theEvent.Date);
            this.Gender = result.Gender;
            this.AgeGroup = result.PrettyAgeGroupName();
            this.Distance = result.DistanceName;
            this.ChipSeconds = (int)result.ChipSeconds;
            this.ChipMilliseconds = result.ChipMilliseconds;
            this.Segment = result.SegmentName;
            this.Location = result.LocationName;
            this.Occurence = result.Occurrence;
            this.Ranking = result.Place;
            this.AgeRanking = result.AgePlace;
            this.GenderRanking = result.GenderPlace;
            this.Finish = result.SegmentId == Constants.Timing.SEGMENT_FINISH;
            this.Type = result.Type;
            this.Chip = result.Chip;
            this.Anonymous = result.Anonymous;
            this.LocalTime = start.AddSeconds(result.Seconds).AddMilliseconds(result.Milliseconds).ToLocalTime().ToString("o");
            this.Division = result.Division;
            this.DivisionRanking = result.DivisionPlace;
            Log.D("Objects.API.APIResult", string.Format("Chip is {0}, Anonymous is {1}.", this.Chip, this.Anonymous));
            if (result.Status == Constants.Timing.TIMERESULT_STATUS_DNF)
            {
                this.Type = Constants.Timing.API_TYPE_DNF;
                this.Seconds = 0;
                this.Milliseconds = 0;
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
                        this.Seconds = Convert.ToInt32(split2[2]) + (Convert.ToInt32(split2[1]) * 60) + (Convert.ToInt32(split2[0]) * 60 * 60);
                        break;
                    case 2:
                        // MINUTES : SECONDS -- Seconds * 1 + Minutes * 60
                        this.Seconds = Convert.ToInt32(split2[1]) + (Convert.ToInt32(split2[0]) * 60);
                        break;
                    case 1:
                        // SECONDS
                        this.Seconds = Convert.ToInt32(split2[0]);
                        break;
                    default:
                        this.Seconds = 0;
                        break;
                }
                if (split1.Length == 2)
                {
                    this.Milliseconds = Convert.ToInt32(split1[1]);
                }
                else
                {
                    this.Milliseconds = 0;
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
        [JsonPropertyName("chip")]
        public string Chip { get; set; }
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
