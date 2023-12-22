using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2010.CustomUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Chronokeep.Objects.ChronokeepPortal.Requests
{
    public class Request
    {
        public static readonly string UNKNOWN = "unknown";

        // API related commands
        public static readonly string API_ADD                       = "api_add";
        public static readonly string API_LIST                      = "api_list";
        public static readonly string API_REMOTE_AUTO_UPLOAD        = "api_remote_auto_upload";
        public static readonly string API_REMOTE_MANUAL_UPLOAD      = "api_remote_manual_upload";
        public static readonly string API_REMOVE                    = "api_remove";
        public static readonly string API_RESULTS_EVENTS_GET        = "api_results_events_get";
        public static readonly string API_RESULTS_EVENT_YEARS_GET   = "api_results_event_years_get";
        public static readonly string API_RESULTS_PARTICIPANTS_GET  = "api_results_participants_get";

        // Connection or program related requests
        public static readonly string CONNECT       = "connect"; 
        public static readonly string DISCONNECT    = "disconnect";
        public static readonly string KEEPALIVE_ACK = "keepalive_ack";
        public static readonly string QUIT          = "quit";
        public static readonly string SHUTDOWN      = "shutdown";

        // Participants related requests
        public static readonly string PARTICIPANTS_GET      = "participants_get";
        public static readonly string PARTICIPANTS_REMOVE   = "participants_remove";

        // Reader related requests
        public static readonly string READER_ADD        = "reader_add";
        public static readonly string READER_CONNECT    = "reader_connect";
        public static readonly string READER_DISCONNECT = "reader_disconnect";
        public static readonly string READER_LIST       = "reader_list";
        public static readonly string READER_REMOVE     = "reader_remove";
        public static readonly string READER_START      = "reader_start";
        public static readonly string READER_STOP       = "reader_stop";

        // Reads related requests
        public static readonly string READS_ADD         = "reads_add";
        public static readonly string READS_DELETE_ALL  = "reads_delete_all";
        public static readonly string READS_DELETE      = "reads_delete";
        public static readonly string READS_GET_ALL     = "reads_get_all";
        public static readonly string READS_GET         = "reads_get";
        public static readonly string SIGHTINGS_GET_ALL = "sightings_get_all";
        public static readonly string SIGHTINGS_GET     = "sightings_get";
        public static readonly string SIGHTINGS_DELETE  = "sightings_delete";

        // Settings related requests
        public static readonly string SETTINGS_SET = "settings_set";
        public static readonly string SETTINGS_GET = "settings_get"; //30

        // Subscription request to subscribe to new reads/sightings
        public static readonly string SUBSCRIBE = "subscribe";

        // Time related requests
        public static readonly string TIME_GET = "time_get";
        public static readonly string TIME_SET = "time_set";

        [JsonPropertyName("command")]
        public string Command { get; set; }
    }

    public class ApiAdd : Request
    {
        public ApiAdd()
        {
            Command = API_ADD;
        }

        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("kind")]
        public string Type { get; set; }
        [JsonPropertyName("uri")]
        public string URI { get; set; }
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }

    public class ApiList : Request
    {
        public ApiList()
        {
            Command = API_LIST;
        }
    }

    public class ApiRemoteAutoUpload : Request
    {
        public ApiRemoteAutoUpload()
        {
            Command = API_REMOTE_AUTO_UPLOAD;
        }

        // AutoUploadQuery
        [JsonPropertyName("query")]
        public string Query { get; set; }
    }

    public class ApiRemoteManualUpload : Request
    {
        public ApiRemoteManualUpload()
        {
            Command = API_REMOTE_MANUAL_UPLOAD;
        }
    }

    public class ApiRemove : Request
    {
        public ApiRemove()
        {
            Command = API_REMOVE;
        }

        [JsonPropertyName("name")]
        public string APIName { get; set; }
    }

    public class ApiResultsEventGet : Request
    {
        public ApiResultsEventGet()
        {
            Command = API_RESULTS_EVENTS_GET;
        }

        [JsonPropertyName("api_name")]
        public string APIName { get; set; }
    }

    public class ApiResultsEventYearGet : Request
    {
        public ApiResultsEventYearGet()
        {
            Command = API_RESULTS_EVENT_YEARS_GET;
        }

        [JsonPropertyName("api_name")]
        public string APIName { get; set; }
        [JsonPropertyName("event_slug")]
        public string EventSlug { get; set; }
    }

    public class ApiResultsParticipantsGet : Request
    {
        public ApiResultsParticipantsGet()
        {
            Command = API_RESULTS_PARTICIPANTS_GET;
        }

        [JsonPropertyName("api_name")]
        public string APIName { get; set; }
        [JsonPropertyName("event_slug")]
        public string EventSlug { get; set; }
        [JsonPropertyName("event_year")]
        public string EventYear { get; set; }
    }

    public class Connect : Request
    {
        public Connect()
        {
            Command = CONNECT;
        }

        [JsonPropertyName("reads")]
        public bool Reads { get; set; }
        [JsonPropertyName("sightings")]
        public bool Sightings { get; set; }
    }

    public class Disconnect : Request
    {
        public Disconnect()
        {
            Command = DISCONNECT;
        }
    }

    public class KeepaliveAck : Request
    {
        public KeepaliveAck()
        {
            Command = KEEPALIVE_ACK;
        }
    }

    public class Quit : Request
    {
        public Quit()
        {
            Command = QUIT;
        }
    }

    public class Shutdown : Request
    {
        public Shutdown()
        {
            Command = SHUTDOWN;
        }
    }

    public class ParticipantsGet : Request
    {
        public ParticipantsGet()
        {
            Command = PARTICIPANTS_GET;
        }
    }

    public class ParticipantsRemove : Request
    {
        public ParticipantsRemove()
        {
            Command = PARTICIPANTS_REMOVE;
        }
    }

    public class ReaderAdd : Request
    {
        public ReaderAdd()
        {
            Command = READER_ADD;
        }

        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("kind")]
        public string Type { get; set; }
        [JsonPropertyName("ip_address")]
        public string IPAddress { get; set; }
        [JsonPropertyName("port")]
        public uint Port { get; set; }
        [JsonPropertyName("auto_connect")]
        public bool AutoConnect { get; set; }
    }

    public class ReaderConnect : Request
    {
        public ReaderConnect()
        {
            Command = READER_CONNECT;
        }

        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class ReaderDisconnect : Request
    {
        public ReaderDisconnect()
        {
            Command= READER_DISCONNECT;
        }

        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class ReaderList : Request
    {
        public ReaderList()
        {
            Command = READER_LIST;
        }
    }

    public class ReaderRemove : Request
    {
        public ReaderRemove()
        {
            Command = READER_REMOVE;
        }

        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class ReaderStart : Request
    {
        public ReaderStart()
        {
            Command = READER_START;
        }

        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class ReaderStop : Request
    {
        public ReaderStop()
        {
            Command = READER_STOP;
        }

        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class ReadsAdd : Request
    {
        public ReadsAdd()
        {
            Command = READS_ADD;
        }

        [JsonPropertyName("read")]
        public PortalRead Read { get; set; }
    }

    public class ReadsDeleteAll : Request
    {
        public ReadsDeleteAll()
        {
            Command = READS_DELETE_ALL;
        }
    }

    public class ReadsDelete : Request
    {
        public ReadsDelete()
        {
            Command= READS_DELETE;
        }

        [JsonPropertyName("start_seconds")]
        public long StartSeconds { get; set; }
        [JsonPropertyName("end_seconds")]
        public long EndSeconds { get; set; }
    }

    public class ReadsGetAll : Request
    {
        public ReadsGetAll()
        {
            Command = READS_GET_ALL;
        }
    }

    public class ReadsGet : Request
    {
        public ReadsGet()
        {
            Command = READS_GET;
        }

        [JsonPropertyName("start_seconds")]
        public long StartSeconds { get; set; }
        [JsonPropertyName("end_seconds")]
        public long EndSeconds { get; set; }
    }

    public class SightingsGetAll : Request
    {
        public SightingsGetAll()
        {
            Command = SIGHTINGS_GET_ALL;
        }
    }

    public class SightingsGet : Request
    {
        public SightingsGet()
        {
            Command = SIGHTINGS_GET;
        }

        [JsonPropertyName("start_seconds")]
        public long StartSeconds { get; set; }
        [JsonPropertyName("end_seconds")]
        public long EndSeconds { get; set; }
    }

    public class SightingsDelete : Request
    {
        public SightingsDelete()
        {
            Command = SIGHTINGS_DELETE;
        }
    }

    public class SettingsSet : Request
    {
        public SettingsSet()
        {
            Command = SETTINGS_SET;
        }

        [JsonPropertyName("settings")]
        public List<PortalSetting> Settings { get; set; }
    }

    public class SettingsGet : Request 
    {
        public SettingsGet()
        {
            Command = SETTINGS_GET;
        }
    }

    public class Subscribe : Request
    {
        public Subscribe()
        {
            Command = SUBSCRIBE;
        }

        [JsonPropertyName("reads")]
        public bool Reads { get; set; }
        [JsonPropertyName("sightings")]
        public bool Sightings { get; set; }
    }

    public class TimeGet : Request
    {
        public TimeGet()
        {
            Command = TIME_GET;
        }
    }

    public class TimeSet : Request
    {
        public TimeSet()
        {
            Command = TIME_SET;
        }

        [JsonPropertyName("time")]
        public string Time { get; set; }
    }
}
