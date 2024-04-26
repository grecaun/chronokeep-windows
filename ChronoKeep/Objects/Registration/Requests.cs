using System.Text.Json.Serialization;

namespace Chronokeep.Objects.Registration
{
    public class Request
    {
        public const string GET_PARTICIPANTS    = "participant_get";
        public const string UPDATE_PARTICIPANT  = "participant_update";
        public const string ADD_PARTICIPANT     = "participant_add";
        public const string DISCONNECT          = "disconnect";

        [JsonPropertyName("command")]
        public string Command { get; set; }
    }

    public class ModifyParticipant : Request
    {
        [JsonPropertyName("participant")]
        public Participant Participant { get; set; }
        [JsonPropertyName("old_bib")]
        public string OldBib { get; set; }
    }
}
