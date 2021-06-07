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
        public APIResult(Event theEvent, TimeResult result)
        {
            this.Bib = result.Bib.ToString();
            this.First = result.First;
            this.Last = result.Last;
            this.Age = result.Age(theEvent.Date);
            this.Gender = result.Gender;
            this.AgeGroup = result.AgeGroupName;
            this.Distance = result.DivisionName;
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
                Log.D(string.Format("Time is {0} - Split1 {1} - Split2 length {2}", result.Time, split1[0], split2.Length));
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
        [JsonProperty("chip_seconds")]
        public int ChipSeconds { get; set; }
        [JsonProperty("chip_milliseconds")]
        public int ChipMilliseconds { get; set; }
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
        [JsonProperty("type")]
        public int Type { get; set; }
    }
}
