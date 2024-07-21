using Chronokeep.Objects.ChronoKeepAPI;
using Chronokeep.Objects.ChronokeepPortal;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chronokeep.Objects.API
{
    /*
     * 
     * Classes for dealing with ChronoKeep API responses.
     * 
     */

    // Event specific responses.
    public class GetEventsResponse
    {
        [JsonPropertyName("events")]
        public List<APIEvent> Events { get; set; }
    }

    public class ModifyEventResponse
    {
        [JsonPropertyName("event")]
        public APIEvent Event { get; set; }
    }

    // Event Year specific responses.
    public class GetEventYearsResponse
    {
        [JsonPropertyName("years")]
        public List<APIEventYear> EventYears { get; set; }
    }

    public class EventYearResponse
    {
        [JsonPropertyName("event")]
        public APIEvent Event { get; set; }
        [JsonPropertyName("event_year")]
        public APIEventYear EventYear { get; set; }
    }

    // Results specific responses.
    public class AddResultsResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    // Error response.
    public class ErrorResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    // Banned emails/phone number responses
    public class GetBannedPhonesResponse
    {
        [JsonPropertyName("phones")]
        public List<string> Phones { get; set; }
    }

    public class GetBannedEmailsResponse
    {
        [JsonPropertyName("emails")]
        public List<string> Emails { get; set; }
    }

    // Participants responses
    public class GetParticipantsResponse
    {
        [JsonPropertyName("event")]
        public APIEvent Event { get; set; }
        [JsonPropertyName("year")]
        public APIEventYear Year { get; set; }
        [JsonPropertyName("participants")]
        public List<APIPerson> Participants { get; set; }
    }

    // BibChips responses
    public class GetBibChipsResponse
    {
        [JsonPropertyName("bib_chips")]
        public List<BibChip> BibChips { get; set; }
    }

    // SMS Subscription responses
    public class GetSmsSubscriptionsResponse
    {
        [JsonPropertyName("subscriptions")]
        public List<APISmsSubscription> Subscriptions { get; set; }
    }

    // Segment responses
    public class GetSegmentsResponse
    {
        [JsonPropertyName("subscriptions")]
        public List<APISegment> Segments { get; set; }
    }
    public class AddSegmentsResponse
    {
        [JsonPropertyName("subscriptions")]
        public List<APISegment> Segments { get; set; }
    }
    public class DeleteSegmentsResponse
    {
        [JsonPropertyName("count")]
        public long Count { get; set; }
    }
}
