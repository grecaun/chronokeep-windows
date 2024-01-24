using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronokeepPortal
{
    public class PortalError
    {
        public static readonly string UNKNOWN_COMMAND       = "UNKNOWN_COMMAND";
        public static readonly string TOO_MANY_CONNECTIONS  = "TOO_MANY_CONNECTIONS";
        public static readonly string TOO_MANY_REMOTE_API   = "TOO_MANY_REMOTE_API";
        public static readonly string SERVER_ERROR          = "SERVER_ERROR";
        public static readonly string DATABASE_ERROR        = "DATABASE_ERROR";
        public static readonly string INVALID_READER_TYPE   = "INVALID_READER_TYPE";
        public static readonly string READER_CONNECTION     = "READER_CONNECTION";
        public static readonly string NOT_FOUND             = "NOT_FOUND";
        public static readonly string INVALID_SETTING       = "INVALID_SETTING";
        public static readonly string INVALID_API_TYPE      = "INVALID_API_TYPE";
        public static readonly string ALREADY_SUBSCRIBED    = "ALREADY_SUBSCRIBED";
        public static readonly string ALREADY_RUNNING       = "ALREADY_RUNNING";
        public static readonly string NOT_RUNNING           = "NOT_RUNNING";
        public static readonly string NO_REMOTE_API         = "NO_REMOTE_API";
        public static readonly string STARTING_UP           = "STARTING_UP";
        public static readonly string INVALID_READ          = "INVALID_READ";

        [JsonPropertyName("error_type")]
        public string Type { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
