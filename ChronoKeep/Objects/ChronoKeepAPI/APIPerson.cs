using System.Text.Json.Serialization;

namespace Chronokeep.Objects.API
{
    public class APIPerson
    {
        public APIPerson(Event theEvent, Participant person)
        {
            this.Bib = person.Bib.ToString();
            this.First = person.Anonymous ? "" : person.FirstName;
            this.Last = person.Anonymous ? "" : person.LastName;
            this.Age = person.GetAge(theEvent.Date);
            this.Gender = person.Gender;
            this.AgeGroup = person.EventSpecific.AgeGroupName;
            this.Distance = person.EventSpecific.DistanceName;
            this.Chip = person.Chip;
            this.Anonymous = person.Anonymous;
        }

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
