﻿using Chronokeep.Objects.API;
using Chronokeep.Objects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using static Chronokeep.Network.Util.Helpers;

namespace Chronokeep.Network.API
{
    public class APIHandlers
    {

        public static async Task<bool> IsHealthy(APIObject api)
        {
            string content;
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(api.URL + "health"),
                    };
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        Log.D("Network.API.APIHandlers", "Status code ok.");
                        return true;
                    }
                    content = "Unable to contact API.";
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.API.APIHandlers", "Exception thrown.");
                throw new APIException("Exception thrown checking health: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<GetEventsResponse> GetEvents(APIObject api)
        {
            string content;
            Log.D("Network.API.APIHandlers", "Getting events.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(api.URL + "event/my")
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api.AuthToken);
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.API.APIHandlers", "Status code ok.");
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<GetEventsResponse>(json);
                        return result;
                    }
                    Log.D("Network.API.APIHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    content = errresult.Message;
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.API.APIHandlers", "Exception thrown.");
                throw new APIException("Exception thrown getting events: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<GetEventYearsResponse> GetEventYears(APIObject api, string slug)
        {
            string content;
            Log.D("Network.API.APIHandlers", "Getting event years.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(api.URL + "event-year/event"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new GetEventRequest
                            {
                                Slug = slug
                            }),
                            Encoding.UTF8,
                            "application/json"
                            )
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api.AuthToken);
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.API.APIHandlers", "Status code ok.");
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<GetEventYearsResponse>(json);
                        return result;
                    }
                    Log.D("Network.API.APIHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    content = errresult.Message;
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.API.APIHandlers", "Exception thrown.");
                throw new APIException("Exception thrown getting event years: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<ModifyEventResponse> AddEvent(APIObject api, APIEvent ev)
        {
            string content;
            Log.D("Network.API.APIHandlers", "Adding event.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(api.URL + "event/add"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new ModifyEventRequest
                            {
                                Event = ev
                            }),
                            Encoding.UTF8,
                            "application/json"
                            )
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api.AuthToken);
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.API.APIHandlers", "Status code ok.");
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<ModifyEventResponse>(json);
                        return result;
                    }
                    Log.D("Network.API.APIHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    content = errresult.Message;
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.API.APIHandlers", "Exception thrown.");
                throw new APIException("Exception thrown adding event: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<EventYearResponse> AddEventYear(APIObject api, string slug, APIEventYear year)
        {
            string content;
            Log.D("Network.API.APIHandlers", "Adding event year.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(api.URL + "event-year/add"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new ModifyEventYearRequest
                            {
                                Slug = slug,
                                Year = year
                            }),
                            Encoding.UTF8,
                            "application/json"
                            )
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api.AuthToken);
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.API.APIHandlers", "Status code ok.");
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<EventYearResponse>(json);
                        return result;
                    }
                    Log.D("Network.API.APIHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    content = errresult.Message;
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.API.APIHandlers", "Exception thrown.");
                throw new APIException("Exception thrown adding event year: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<AddResultsResponse> UploadResults(APIObject api, string slug, string year, List<APIResult> results)
        {
            string content;
            Log.D("Network.API.APIHandlers", "Uploading results.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(api.URL + "results/add"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new AddResultsRequest
                            {
                                Slug = slug,
                                Year = year,
                                Results = results
                            }),
                            Encoding.UTF8,
                            "application/json"
                            )
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api.AuthToken);
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.API.APIHandlers", "Status code ok.");
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<AddResultsResponse>(json);
                        return result;
                    }
                    Log.D("Network.API.APIHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    content = errresult.Message;
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.API.APIHandlers", "Exception thrown.");
                throw new APIException("Exception thrown uploading results: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<AddResultsResponse> DeleteResults(APIObject api, string slug, string year)
        {
            string content;
            Log.D("Network.API.APIHandlers", "Deleting results.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri(api.URL + "results/delete"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new GetResultsRequest
                            {
                                Slug = slug,
                                Year = year
                            }),
                            Encoding.UTF8,
                            "application/json"
                            )
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api.AuthToken);
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.API.APIHandlers", "Status code ok.");
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<AddResultsResponse>(json);
                        return result;
                    }
                    Log.D("Network.API.APIHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    content = errresult.Message;
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.API.APIHandlers", "Exception thrown.");
                throw new APIException("Exception thrown deleting results: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<AddResultsResponse> UploadParticipants(APIObject api, string slug, string year, List<APIPerson> people)
        {
            string content;
            Log.D("Network.API.APIHandlers", "Uploading participants.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(api.URL + "participants/add"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new AddParticipantsRequest
                            {
                                Slug = slug,
                                Year = year,
                                Participants = people,
                            }),
                            Encoding.UTF8,
                            "application/json"
                            )
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api.AuthToken);
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.API.APIHandlers", "Status code ok.");
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<AddResultsResponse>(json);
                        return result;
                    }
                    Log.D("Network.API.APIHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    content = errresult.Message;
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.API.APIHandlers", "Exception thrown.");
                throw new APIException("Exception thrown deleting results: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<AddResultsResponse> DeleteParticipants(APIObject api, string slug, string year)
        {
            string content;
            Log.D("Network.API.APIHandlers", "Deleting participants.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri(api.URL + "participants/delete"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new GetParticipantsRequest
                            {
                                Slug = slug,
                                Year = year
                            }),
                            Encoding.UTF8,
                            "application/json"
                            )
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api.AuthToken);
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.API.APIHandlers", "Status code ok.");
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<AddResultsResponse>(json);
                        return result;
                    }
                    Log.D("Network.API.APIHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    content = errresult.Message;
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.API.APIHandlers", "Exception thrown.");
                throw new APIException("Exception thrown deleting results: " + ex.Message);
            }
            throw new APIException(content);
        }
    }
}
