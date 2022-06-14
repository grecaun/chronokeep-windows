using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Objects.API
{
    public class APIEventYear
    {
        [JsonProperty("year")]
        public string Year { get; set; }
        [JsonProperty("date_time")]
        public string DateTime { get; set; }
        [JsonProperty("live")]
        public bool Live { get; set; }
    }
}
