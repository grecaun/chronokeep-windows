using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronokeepPortal.Responses
{
    public class Response
    {
        public const string READERS               = "readers";
        public const string READER_ANTENNAS       = "reader_antennas";
        public const string ERROR                 = "error";
        public const string SETTINGS              = "settings";
        public const string SETTINGS_ALL          = "settings_all";
        public const string API_LIST              = "api_list";
        public const string READS                 = "reads";
        public const string SUCCESS               = "success";
        public const string TIME                  = "time";
        public const string PARTICIPANTS          = "participants";
        public const string SIGHTINGS             = "sightings";
        public const string EVENTS                = "events";
        public const string EVENT_YEARS           = "event_years";
        public const string READ_AUTO_UPLOAD      = "read_auto_upload";
        public const string CONNECTION_SUCCESSFUL = "connection_successful";
        public const string KEEPALIVE             = "keepalive";
        public const string DISCONNECT            = "disconnect";

        [JsonPropertyName("command")]
        public string Command { get; set; }
    }

    public class ReadersResponse : Response
    {
        [JsonPropertyName("readers")]
        public List<PortalReader> List { get; set; }
    }

    public class ReaderAntennasResponse: Response
    {
        [JsonPropertyName("reader_name")]
        public string reader_name { get; set; }
        [JsonPropertyName("antennas")]
        public int[] Antennas { get; set; }
    }

    public class ErrorResponse : Response
    {
        [JsonPropertyName("error")]
        public PortalError Value { get; set; }
    }

    public class SettingsResponse : Response
    {
        [JsonPropertyName("settings")]
        public List<PortalSetting> List { get; set; }
    }

    public class SettingsAllResponse : Response
    {
        [JsonPropertyName("settings")]
        public List<PortalSetting> Settings { get; set; }
        [JsonPropertyName("readers")]
        public List<PortalReader> Readers { get; set; }
        [JsonPropertyName("apis")]
        public List<PortalAPI> APIs { get; set; }
        [JsonPropertyName("auto_upload")]
        public PortalStatus AutoUpload { get; set; }
    }

    public class ApiListResponse : Response
    {
        [JsonPropertyName("apis")]
        public List<PortalAPI> List { get; set; }
    }

    public class ReadsResponse : Response
    {
        [JsonPropertyName("list")]
        public List<PortalRead> List { get; set; }
    }

    public class SuccessResponse : Response
    {
        [JsonPropertyName("count")]
        public ulong Count { get; set; }
    }

    public class TimeResponse : Response
    {
        [JsonPropertyName("local")]
        public string Local { get; set; }
        [JsonPropertyName("utc")]
        public string UTC { get; set; }
    }

    public class ParticipantsResponse : Response
    {
        [JsonPropertyName("participants")]
        public List<PortalParticipant> List { get; set; }
    }

    public class SightingsResponse : Response
    {
        [JsonPropertyName("list")]
        public List<PortalSighting> List { get; set; }
    }

    public class EventsResponse : Response
    {
        [JsonPropertyName("events")]
        public List<PortalEvent> List { get; set; }
    }

    public class EventYearsResponse : Response
    {
        [JsonPropertyName("years")]
        public List<string> Years { get; set; }
    }

    public class ReadAutoUploadResponse : Response
    {
        [JsonPropertyName("status")]
        public PortalStatus Status { get; set; }
    }

    public class ConnectionSuccessfulResponse : Response
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
        [JsonPropertyName("updatable")]
        public bool Updateable { get; set; }
    }
}
