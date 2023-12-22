using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Chronokeep.Objects.ChronokeepPortal
{
    public class PortalSighting
    {
        [JsonPropertyName("participant")]
        public PortalParticipant Participant { get; set; }
        [JsonPropertyName("read")]
        public PortalRead Read { get; set; }
    }
}
