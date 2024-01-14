using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
