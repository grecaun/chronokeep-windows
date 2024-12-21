using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronokeepPortal
{
    public class PortalError
    {
        public const string UNKNOWN_COMMAND       = "UNKNOWN_COMMAND";
        public const string TOO_MANY_CONNECTIONS  = "TOO_MANY_CONNECTIONS";
        public const string TOO_MANY_REMOTE_API   = "TOO_MANY_REMOTE_API";
        public const string SERVER_ERROR          = "SERVER_ERROR";
        public const string DATABASE_ERROR        = "DATABASE_ERROR";
        public const string INVALID_READER_TYPE   = "INVALID_READER_TYPE";
        public const string READER_CONNECTION     = "READER_CONNECTION";
        public const string NOT_FOUND             = "NOT_FOUND";
        public const string INVALID_SETTING       = "INVALID_SETTING";
        public const string INVALID_API_TYPE      = "INVALID_API_TYPE";
        public const string ALREADY_SUBSCRIBED    = "ALREADY_SUBSCRIBED";
        public const string ALREADY_RUNNING       = "ALREADY_RUNNING";
        public const string NOT_RUNNING           = "NOT_RUNNING";
        public const string NO_REMOTE_API         = "NO_REMOTE_API";
        public const string STARTING_UP           = "STARTING_UP";
        public const string INVALID_READ          = "INVALID_READ";
        public const string NOT_ALLOWED           = "NOT_ALLOWED";

        [JsonPropertyName("error_type")]
        public string Type { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
