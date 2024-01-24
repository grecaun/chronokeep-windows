using System.Text.Json.Serialization;

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
