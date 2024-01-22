using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Chronokeep.Objects.ChronokeepRemote
{
    public class RemoteReader
    {
        public RemoteReader(string name, int api_id)
        {
            this.Name = name;
            this.APIIDentifier = api_id;
        }

        [JsonPropertyName("Name")]
        public string Name { get; set; }
        public int APIIDentifier { get; set; }
    }
}
