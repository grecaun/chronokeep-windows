using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Objects.API
{
    public class APIEvent
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("slug")]
        public string Slug { get; set; }
        [JsonProperty("website")]
        public string Website { get; set; }
        [JsonProperty("image")]
        public string Image { get; set; }
        [JsonProperty("contact_email")]
        public string ContactEmail { get; set; }
        [JsonProperty("access_restricted")]
        public bool AccessRestricted { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
