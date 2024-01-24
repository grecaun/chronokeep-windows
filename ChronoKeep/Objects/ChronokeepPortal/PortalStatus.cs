using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronokeepPortal
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PortalStatus
    {
        NOTSET,
        RUNNING,
        STOPPING,
        STOPPED,
        UNKNOWN
    }
}
