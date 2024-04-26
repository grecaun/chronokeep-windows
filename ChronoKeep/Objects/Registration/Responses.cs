﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chronokeep.Objects.Registration
{
    public class Response
    {
        public const string ERROR = "error";
        public const string DISCONNECT = "disconnect";
        public const string PARTICIPANTS = "participants";
        public const string CONNECTION_SUCCESSFUL = "connection_successful";

        [JsonPropertyName("command")]
        public string Command { get; set; }
    }

    public class ConnectionSuccessfulResponse : Response
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("kind")]
        public string Type { get; set; }
        [JsonPropertyName("version")]
        public ulong Version { get; set; }
    }

    public class ParticipantsResponse : Response
    {
        [JsonPropertyName("participants")]
        public List<Participant> Participants { get; set; }
        [JsonPropertyName("distances")]
        public List<string> Distances { get; set; }
    }

    public class ErrorResponse : Response
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }
    }
}