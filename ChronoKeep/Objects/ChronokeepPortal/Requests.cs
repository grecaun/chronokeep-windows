﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronokeepPortal.Requests
{
    public class Request
    {
        public const string UNKNOWN = "unknown";

        // API related commands
        public const string API_SAVE                      = "api_save";
        public const string API_SAVE_ALL                  = "api_save_all";
        public const string API_LIST                      = "api_list";
        public const string API_REMOTE_AUTO_UPLOAD        = "api_remote_auto_upload";
        public const string API_REMOTE_MANUAL_UPLOAD      = "api_remote_manual_upload";
        public const string API_REMOVE                    = "api_remove";
        public const string API_RESULTS_EVENTS_GET        = "api_results_events_get";
        public const string API_RESULTS_EVENT_YEARS_GET   = "api_results_event_years_get";
        public const string API_RESULTS_PARTICIPANTS_GET  = "api_results_participants_get";

        // Connection or program related requests
        public const string CONNECT       = "connect"; 
        public const string DISCONNECT    = "disconnect";
        public const string KEEPALIVE_ACK = "keepalive_ack";
        public const string QUIT          = "quit";
        public const string SHUTDOWN      = "shutdown";
        public const string RESTART       = "restart";

        // Participants related requests
        public const string PARTICIPANTS_GET      = "participants_get";
        public const string PARTICIPANTS_REMOVE   = "participants_remove";
        public const string PARTICIPANTS_ADD      = "participants_add";

        // BibChips related requests
        public const string BIBCHIPS_GET          = "bib_chips_get";
        public const string BIBCHIPS_REMOVE       = "bib_chips_remove";
        public const string BIBCHIPS_ADD          = "bib_chips_add";

        // Reader related requests
        public const string READER_ADD        = "reader_add";
        public const string READER_CONNECT    = "reader_connect";
        public const string READER_DISCONNECT = "reader_disconnect";
        public const string READER_LIST       = "reader_list";
        public const string READER_REMOVE     = "reader_remove";
        public const string READER_START      = "reader_start";
        public const string READER_STOP       = "reader_stop";
        public const string READER_START_ALL  = "reader_start_all";
        public const string READER_STOP_ALL   = "reader_stop_all";

        // Reads related requests
        public const string READS_ADD         = "reads_add";
        public const string READS_DELETE_ALL  = "reads_delete_all";
        public const string READS_DELETE      = "reads_delete";
        public const string READS_GET_ALL     = "reads_get_all";
        public const string READS_GET         = "reads_get";
        public const string SIGHTINGS_GET_ALL = "sightings_get_all";
        public const string SIGHTINGS_GET     = "sightings_get";
        public const string SIGHTINGS_DELETE  = "sightings_delete";

        // Settings related requests
        public const string SETTINGS_SET      = "settings_set";
        public const string SETTINGS_GET      = "settings_get";
        public const string SETTINGS_GET_ALL  = "settings_get_all";

        // Subscription request to subscribe to new reads/sightings
        public const string SUBSCRIBE = "subscribe";

        // Time related requests
        public const string TIME_GET = "time_get";
        public const string TIME_SET = "time_set";

        public const string AUTO_UPLOAD_QUERY_STOP = "stop";
        public const string AUTO_UPLOAD_QUERY_START = "start";
        public const string AUTO_UPLOAD_QUERY_STATUS = "status";

        [JsonPropertyName("command")]
        public string Command { get; set; }
    }

    public enum AutoUploadQuery
    {
        STOP,
        START,
        STATUS
    }

    public class ApiSaveRequest : Request
    {
        public ApiSaveRequest()
        {
            Command = API_SAVE;
        }

        [JsonPropertyName("id")]
        public long ID { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("kind")]
        public string Type { get; set; }
        [JsonPropertyName("uri")]
        public string URI { get; set; }
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }

    public class ApiSaveAllRequest : Request
    {
        public ApiSaveAllRequest()
        {
            Command = API_SAVE_ALL;
        }

        [JsonPropertyName("list")]
        public List<PortalAPI> List { get; set; }
    }

    public class ApiListRequest : Request
    {
        public ApiListRequest()
        {
            Command = API_LIST;
        }
    }

    public class ApiRemoteAutoUploadRequest : Request
    {
        public ApiRemoteAutoUploadRequest()
        {
            Command = API_REMOTE_AUTO_UPLOAD;
        }

        // AutoUploadQuery
        [JsonPropertyName("query")]
        public string Query { get; set; }
    }

    public class ApiRemoteManualUploadRequest : Request
    {
        public ApiRemoteManualUploadRequest()
        {
            Command = API_REMOTE_MANUAL_UPLOAD;
        }
    }

    public class ApiRemoveRequest : Request
    {
        public ApiRemoveRequest()
        {
            Command = API_REMOVE;
        }

        [JsonPropertyName("id")]
        public long ID { get; set; }
    }

    public class ApiResultsEventGetRequest : Request
    {
        public ApiResultsEventGetRequest()
        {
            Command = API_RESULTS_EVENTS_GET;
        }

        [JsonPropertyName("api_id")]
        public long APIID { get; set; }
    }

    public class ApiResultsEventYearGetRequest : Request
    {
        public ApiResultsEventYearGetRequest()
        {
            Command = API_RESULTS_EVENT_YEARS_GET;
        }

        [JsonPropertyName("api_id")]
        public long APIID { get; set; }
        [JsonPropertyName("event_slug")]
        public string EventSlug { get; set; }
    }

    public class ApiResultsParticipantsGetRequest : Request
    {
        public ApiResultsParticipantsGetRequest()
        {
            Command = API_RESULTS_PARTICIPANTS_GET;
        }

        [JsonPropertyName("api_id")]
        public long APIID { get; set; }
        [JsonPropertyName("event_slug")]
        public string EventSlug { get; set; }
        [JsonPropertyName("event_year")]
        public string EventYear { get; set; }
    }

    public class ConnectRequest : Request
    {
        public ConnectRequest()
        {
            Command = CONNECT;
        }

        [JsonPropertyName("reads")]
        public bool Reads { get; set; }
        [JsonPropertyName("sightings")]
        public bool Sightings { get; set; }
    }

    public class DisconnectRequest : Request
    {
        public DisconnectRequest()
        {
            Command = DISCONNECT;
        }
    }

    public class KeepaliveAckRequest : Request
    {
        public KeepaliveAckRequest()
        {
            Command = KEEPALIVE_ACK;
        }
    }

    public class QuitRequest : Request
    {
        public QuitRequest()
        {
            Command = QUIT;
        }
    }

    public class ShutdownRequest : Request
    {
        public ShutdownRequest()
        {
            Command = SHUTDOWN;
        }
    }

    public class RestartRequest : Request
    {
        public RestartRequest()
        {
            Command = RESTART;
        }
    }

    public class ParticipantsGetRequest : Request
    {
        public ParticipantsGetRequest()
        {
            Command = PARTICIPANTS_GET;
        }
    }

    public class ParticipantsRemoveRequest : Request
    {
        public ParticipantsRemoveRequest()
        {
            Command = PARTICIPANTS_REMOVE;
        }
    }

    public class ParticipantsAddRequest : Request
    {
        public ParticipantsAddRequest()
        {
            Command = PARTICIPANTS_ADD;
        }

        [JsonPropertyName("participants")]
        public List<PortalParticipant> Participants { get; set; }
    }

    public class BibChipsGetRequest : Request
    {
        public BibChipsGetRequest()
        {
            Command = BIBCHIPS_GET;
        }
    }

    public class BibChipsRemoveRequest : Request
    {
        public BibChipsRemoveRequest()
        {
            Command = BIBCHIPS_REMOVE;
        }
    }

    public class BibChipsAddRequest : Request
    {
        public BibChipsAddRequest()
        {
            Command = BIBCHIPS_ADD;
        }

        [JsonPropertyName("bib_chips")]
        public List<BibChip> BibChips { get; set; }
    }

    public class ReaderAddRequest : Request
    {
        public ReaderAddRequest()
        {
            Command = READER_ADD;
        }

        [JsonPropertyName("id")]
        public long Id { get; set; }
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

    public class ReaderConnectRequest : Request
    {
        public ReaderConnectRequest()
        {
            Command = READER_CONNECT;
        }

        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class ReaderDisconnectRequest : Request
    {
        public ReaderDisconnectRequest()
        {
            Command= READER_DISCONNECT;
        }

        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class ReaderListRequest : Request
    {
        public ReaderListRequest()
        {
            Command = READER_LIST;
        }
    }

    public class ReaderRemoveRequest : Request
    {
        public ReaderRemoveRequest()
        {
            Command = READER_REMOVE;
        }

        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class ReaderStartRequest : Request
    {
        public ReaderStartRequest()
        {
            Command = READER_START;
        }

        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class ReaderStartAllRequest : Request
    {
        public ReaderStartAllRequest()
        {
            Command = READER_START_ALL;
        }
    }

    public class ReaderStopRequest : Request
    {
        public ReaderStopRequest()
        {
            Command = READER_STOP;
        }

        [JsonPropertyName("id")]
        public long Id { get; set; }
    }

    public class ReaderStopAllRequest : Request
    {
        public ReaderStopAllRequest()
        {
            Command = READER_STOP_ALL;
        }
    }

    public class ReadsAddRequest : Request
    {
        public ReadsAddRequest()
        {
            Command = READS_ADD;
        }

        [JsonPropertyName("read")]
        public PortalRead Read { get; set; }
    }

    public class ReadsDeleteAllRequest : Request
    {
        public ReadsDeleteAllRequest()
        {
            Command = READS_DELETE_ALL;
        }
    }

    public class ReadsDeleteRequest : Request
    {
        public ReadsDeleteRequest()
        {
            Command= READS_DELETE;
        }

        [JsonPropertyName("start_seconds")]
        public long StartSeconds { get; set; }
        [JsonPropertyName("end_seconds")]
        public long EndSeconds { get; set; }
    }

    public class ReadsGetAllRequest : Request
    {
        public ReadsGetAllRequest()
        {
            Command = READS_GET_ALL;
        }
    }

    public class ReadsGetRequest : Request
    {
        public ReadsGetRequest()
        {
            Command = READS_GET;
        }

        [JsonPropertyName("start_seconds")]
        public long StartSeconds { get; set; }
        [JsonPropertyName("end_seconds")]
        public long EndSeconds { get; set; }
    }

    public class SightingsGetAllRequest : Request
    {
        public SightingsGetAllRequest()
        {
            Command = SIGHTINGS_GET_ALL;
        }
    }

    public class SightingsGetRequest : Request
    {
        public SightingsGetRequest()
        {
            Command = SIGHTINGS_GET;
        }

        [JsonPropertyName("start_seconds")]
        public long StartSeconds { get; set; }
        [JsonPropertyName("end_seconds")]
        public long EndSeconds { get; set; }
    }

    public class SightingsDeleteRequest : Request
    {
        public SightingsDeleteRequest()
        {
            Command = SIGHTINGS_DELETE;
        }
    }

    public class SettingsSetRequest : Request
    {
        public SettingsSetRequest()
        {
            Command = SETTINGS_SET;
        }

        [JsonPropertyName("settings")]
        public List<PortalSetting> Settings { get; set; }
    }

    public class SettingsGetRequest : Request 
    {
        public SettingsGetRequest()
        {
            Command = SETTINGS_GET;
        }
    }

    public class SettingsGetAllRequest : Request
    {
        public SettingsGetAllRequest()
        {
            Command = SETTINGS_GET_ALL;
        }
    }

    public class SubscribeRequest : Request
    {
        public SubscribeRequest()
        {
            Command = SUBSCRIBE;
        }

        [JsonPropertyName("reads")]
        public bool Reads { get; set; }
        [JsonPropertyName("sightings")]
        public bool Sightings { get; set; }
    }

    public class TimeGetRequest : Request
    {
        public TimeGetRequest()
        {
            Command = TIME_GET;
        }
    }

    public class TimeSetRequest : Request
    {
        public TimeSetRequest()
        {
            Command = TIME_SET;
        }

        [JsonPropertyName("time")]
        public string Time { get; set; }
    }
}
