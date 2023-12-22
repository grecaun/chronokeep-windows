using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Chronokeep.Objects.ChronokeepPortal.Responses
{
    public class Response
    {
        public static readonly string READERS               = "readers";
        public static readonly string ERROR                 = "error";
        public static readonly string SETTINGS              = "settings";
        public static readonly string API_LIST              = "api_list";
        public static readonly string READS                 = "reads";
        public static readonly string SUCCESS               = "success";
        public static readonly string TIME                  = "time";
        public static readonly string PARTICIPANTS          = "participants";
        public static readonly string SIGHTINGS             = "sightings";
        public static readonly string EVENTS                = "events";
        public static readonly string EVENT_YEARS           = "event_years";
        public static readonly string READ_AUTO_UPLOAD      = "read_auto_upload";
        public static readonly string CONNECTION_SUCCESSFUL = "connection_successful";
        public static readonly string KEEPALIVE             = "keepalive";
        public static readonly string DISCONNECT            = "disconnect";

        [JsonPropertyName("command")]
        public string Command { get; set; }
    }

    public class Readers : Response
    {
        [JsonPropertyName("readers")]
        public List<PortalReader> List { get; set; }
    }

    public class Error : Response
    {
        [JsonPropertyName("error")]
        public PortalError Value { get; set; }
    }

    public class Settings : Response
    {
        [JsonPropertyName("settings")]
        public List<PortalSetting> List { get; set; }
    }

    public class ApiList : Response
    {
        [JsonPropertyName("apis")]
        public List<PortalAPI> List { get; set; }
    }

    public class Reads : Response
    {
        [JsonPropertyName("list")]
        public List<PortalRead> List { get; set; }
    }

    public class Success : Response
    {
        [JsonPropertyName("count")]
        public ulong Count { get; set; }
    }

    public class Time : Response
    {
        [JsonPropertyName("local")]
        public string Local { get; set; }
        [JsonPropertyName("utc")]
        public string UTC { get; set; }
    }

    public class Participants : Response
    {
        [JsonPropertyName("participants")]
        public List<PortalParticipant> List { get; set; }
    }

    public class Sightings : Response
    {
        [JsonPropertyName("list")]
        public List<PortalSighting> List { get; set; }
    }

    public class Events : Response
    {
        [JsonPropertyName("events")]
        public List<PortalEvent> List { get; set; }
    }

    public class EventYears : Response
    {
        [JsonPropertyName("years")]
        public List<string> Years { get; set; }
    }

    public class ReadAutoUpload : Response
    {
        [JsonPropertyName("status")]
        public PortalStatus Status { get; set; }
    }

    public class ConnectionSuccessful : Response
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("kind")]
        public string Type { get; set; }
        [JsonPropertyName("version")]
        public ulong Version { get; set; }
        [JsonPropertyName("reads_subscribed")]
        public bool ReadsSubscribed { get; set; }
        [JsonPropertyName("sightings_subscribed")]
        public bool SightingsSubscrubed { get; set; }
        [JsonPropertyName("readers")]
        public List<PortalReader> Readers { get; set; }
    }
}
