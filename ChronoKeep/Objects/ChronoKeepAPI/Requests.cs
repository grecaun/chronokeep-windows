﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronoKeepAPI
{
    /*
     * 
     * Classes for dealing with ChronoKeep API requests.
     * 
     */

    // Event specific requests
    public class GetEventRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
    }

    public class ModifyEventRequest
    {
        [JsonPropertyName("event")]
        public APIEvent Event { get; set; }
    }

    // Event Year specific requests.
    public class GetEventYearRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    public class ModifyEventYearRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("event_year")]
        public APIEventYear Year { get; set; }
    }

    // Result specific requests
    public class GetResultsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    public class GetResultsDistanceRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
        [JsonPropertyName("distance")]
        public string Distance { get; set; }
    }

    public class AddResultsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
        [JsonPropertyName("results")]
        public List<APIResult> Results { get; set; }
    }

    // Participant specific requests
    public class GetParticipantsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
        [JsonPropertyName("limit")]
        public int Limit { get; set; }
        [JsonPropertyName("page")]
        public int Page { get; set; }
    }

    public class DeleteParticipantsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    public class AddParticipantsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
        [JsonPropertyName("participants")]
        public List<APIPerson> Participants { get; set; }
    }

    // Bibchip specific requests
    public class GetBibChipsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    public class AddBibChipsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
        [JsonPropertyName("bib_chips")]
        public List<BibChip> BibChips { get; set; }
    }

    // Banned emails/phone numbers requests
    public class ModifyBannedPhoneRequest
    {
        [JsonPropertyName("phone")]
        public string Phone { get; set; }
    }

    public class ModifyBannedEmailRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }
    }

    // SMS Subscription requests
    public class GetSmsSubscriptionsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    public class AddSmsSubscriptionRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
        [JsonPropertyName("bib")]
        public string Bib { get; set; }
        [JsonPropertyName("first")]
        public string First { get; set; }
        [JsonPropertyName("last")]
        public string Last { get; set; }
        [JsonPropertyName("Phone")]
        public string Phone { get; set; }
    }

    public class RemoveSmsSubscriptionRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
        [JsonPropertyName("Phone")]
        public string Phone { get; set; }
    }

    // Segment requests
    public class GetSegmentsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    public class AddSegmentsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
        [JsonPropertyName("segments")]
        public List<APISegment> Segments { get; set; }
    }

    public class DeleteSegmentsRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    // Distance requests
    public class GetDistancesRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    public class AddDistancesRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
        [JsonPropertyName("distances")]
        public List<APIDistance> Distances { get; set; }
    }

    public class DeleteDistancesRequest
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; }
        [JsonPropertyName("year")]
        public string Year { get; set; }
    }
}
