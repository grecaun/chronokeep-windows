using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Chronokeep.Objects.ChronokeepPortal
{
    public class PortalReader
    {
        [JsonPropertyName("id")]
        public long ID { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("kind")]
        public string Kind { get; set; }
        [JsonPropertyName("ip_address")]
        public string IPAddress { get; set; }
        [JsonPropertyName("port")]
        public uint Port { get; set; }
        [JsonPropertyName("auto_connect")]
        public bool AutoConnect { get; set; }
        [JsonPropertyName("Reading")]
        public bool Reading { get; set; } = false;
        [JsonPropertyName("Connected")]
        public bool Connected { get; set; } = false;
    }
}
