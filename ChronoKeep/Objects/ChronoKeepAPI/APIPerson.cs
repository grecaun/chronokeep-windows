﻿using System.Text.Json.Serialization;

namespace Chronokeep.Objects.API
{
    public class APIPerson
    {
        public APIPerson() { }

        public APIPerson(Participant person)
        {
            this.Identifier = person.EventIdentifier.ToString();
            this.Bib = person.Bib.ToString();
            this.First = person.Anonymous ? "" : person.FirstName;
            this.Last = person.Anonymous ? "" : person.LastName;
            this.Birthdate = person.Birthdate;
            this.Gender = person.Gender;
            this.AgeGroup = person.EventSpecific.AgeGroupName;
            this.Distance = person.EventSpecific.DistanceName;
            this.Anonymous = person.Anonymous;
            this.SMSEnabled = person.EventSpecific.SMSEnabled;
            this.Mobile = person.Mobile;
            this.Apparel = person.EventSpecific.Apparel;
        }

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
