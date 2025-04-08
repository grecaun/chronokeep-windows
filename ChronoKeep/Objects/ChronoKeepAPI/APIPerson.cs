using System.Text.Json.Serialization;

namespace Chronokeep.Objects.API
{
    public class APIPerson
    {
        public APIPerson() { }

        public APIPerson(Participant person, string uniqueID)
        {
            this.Identifier = string.Format("{0}{1}", uniqueID, person.EventSpecific.Identifier);
            this.Bib = person.Bib.ToString();
            this.First = person.Anonymous ? "" : person.FirstName;
            this.Last = person.Anonymous ? "" : person.LastName;
            this.Birthdate = person.Birthdate.Length < 1 ? "1901/01/01" : person.Birthdate;
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

        public void Trim()
        {
            Bib = Bib.Trim();
            First = First.Trim();
            Last = Last.Trim();
            Birthdate = Birthdate.Trim();
            Gender = Gender.Trim();
            AgeGroup = AgeGroup.Trim();
            Distance = Distance.Trim();
            Mobile = Mobile.Trim();
            Apparel = Apparel.Trim();
        }
    }
}
