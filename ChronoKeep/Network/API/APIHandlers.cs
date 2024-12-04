using Chronokeep.Objects.API;
using Chronokeep.Objects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using static Chronokeep.Network.Util.Helpers;
using Chronokeep.Objects.ChronokeepPortal;
using Chronokeep.Objects.ChronoKeepAPI;

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

        public static async Task<AddResultsResponse> DeleteDistanceResults(APIObject api, string slug, string year, string distance)
        {
            string content;
            Log.D("Network.API.APIHandlers", "Deleting distance results.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri(api.URL + "results/delete"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new GetResultsDistanceRequest
                            {
                                Slug = slug,
                                Year = year,
                                Distance = distance,
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

        public static async Task<AddResultsResponse> UploadBibChips(APIObject api, string slug, string year, List<BibChip> bibChips)
        {
            string content;
            Log.D("Network.API.APIHandlers", "Uploading bibchips.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(api.URL + "bibchips/add"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new AddBibChipsRequest
                            {
                                Slug = slug,
                                Year = year,
                                BibChips = bibChips,
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
                throw new APIException("Exception thrown adding bibchips: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<AddResultsResponse> DeleteBibChips(APIObject api, string slug, string year)
        {
            string content;
            Log.D("Network.API.APIHandlers", "Deleting bibchips.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri(api.URL + "bibchips/delete"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new GetBibChipsRequest
                            {
                                Slug = slug,
                                Year = year,
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
                throw new APIException("Exception thrown deleting bibchips: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<GetBibChipsResponse> GetBibChips(APIObject api, string slug, string year)
        {
            string content;
            Log.D("Network.API.APIHandlers", "Getting bibchips.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(api.URL + "bibchips"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new GetBibChipsRequest
                            {
                                Slug = slug,
                                Year = year,
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
                        var result = JsonSerializer.Deserialize<GetBibChipsResponse>(json);
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
                throw new APIException("Exception thrown getting bibchips: " + ex.Message);
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
                throw new APIException("Exception thrown uploading participants: " + ex.Message);
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
                            JsonSerializer.Serialize(new DeleteParticipantsRequest
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
                throw new APIException("Exception thrown deleting participants: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<GetParticipantsResponse> GetParticipants(APIObject api, string slug, string year, int limit, int page)
        {
            string content;
            Log.D("Network.API.APIHandlers", "Getting participants.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(api.URL + "participants"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new GetParticipantsRequest
                            {
                                Slug = slug,
                                Year = year,
                                Limit = limit,
                                Page = page,
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
                        var result = JsonSerializer.Deserialize<GetParticipantsResponse>(json);
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
                throw new APIException("Exception thrown getting participants: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<GetBannedPhonesResponse> GetBannedPhones()
        {
            string content;
            Log.D("Network.API.APIHandlers", "Getting banned phone numbers.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(Constants.APIConstants.API_URL[Constants.APIConstants.CHRONOKEEP_RESULTS] + "blocked/phones/get"),
                    };
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.API.APIHandlers", "Status code ok.");
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<GetBannedPhonesResponse>(json);
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
                throw new APIException("Exception thrown getting banned phone numbers: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<int> AddBannedPhone(string phone)
        {
            string validPhone = Constants.Globals.GetValidPhone(phone);
            if (validPhone == null || validPhone.Length == 0)
            {
                throw new APIException("Invalid phone number.");
            }
            string content;
            Log.D("Network.API.APIHandlers", "Blocking phone number.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(Constants.APIConstants.API_URL[Constants.APIConstants.CHRONOKEEP_RESULTS] + "blocked/phones/add"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new ModifyBannedPhoneRequest
                            {
                                Phone = validPhone,
                            }),
                            Encoding.UTF8,
                            "application/json"
                            )
                    };
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.API.APIHandlers", "Status code ok.");
                        return 200;
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
                throw new APIException("Exception thrown blocking phone number: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async void UnblockBannedPhone(string phone)
        {
            string validPhone = Constants.Globals.GetValidPhone(phone);
            if (validPhone == null || validPhone.Length == 0)
            {
                throw new APIException("Invalid phone number.");
            }
            string content;
            Log.D("Network.API.APIHandlers", "Unblocking phone number.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(Constants.APIConstants.API_URL[Constants.APIConstants.CHRONOKEEP_RESULTS] + "blocked/phones/unblock"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new ModifyBannedPhoneRequest
                            {
                                Phone = validPhone,
                            }),
                            Encoding.UTF8,
                            "application/json"
                            )
                    };
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.API.APIHandlers", "Status code ok.");
                        return;
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
                throw new APIException("Exception thrown unblocking phone number: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<GetBannedEmailsResponse> GetBannedEmails()
        {
            string content;
            Log.D("Network.API.APIHandlers", "Getting banned emails.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(Constants.APIConstants.API_URL[Constants.APIConstants.CHRONOKEEP_RESULTS] + "blocked/emails/get"),
                    };
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.API.APIHandlers", "Status code ok.");
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<GetBannedEmailsResponse>(json);
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
                throw new APIException("Exception thrown getting banned emails: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async void AddBannedEmail(string email)
        {
            string content;
            Log.D("Network.API.APIHandlers", "Blocking email.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(Constants.APIConstants.API_URL[Constants.APIConstants.CHRONOKEEP_RESULTS] + "blocked/emails/add"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new ModifyBannedEmailRequest
                            {
                                Email = email,
                            }),
                            Encoding.UTF8,
                            "application/json"
                            )
                    };
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.API.APIHandlers", "Status code ok.");
                        return;
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
                throw new APIException("Exception thrown blocking email: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async void UnblockBannedEmail(string email)
        {
            string content;
            Log.D("Network.API.APIHandlers", "Unblocking email.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(Constants.APIConstants.API_URL[Constants.APIConstants.CHRONOKEEP_RESULTS] + "blocked/emails/unblock"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new ModifyBannedEmailRequest
                            {
                                Email = email,
                            }),
                            Encoding.UTF8,
                            "application/json"
                            )
                    };
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.API.APIHandlers", "Status code ok.");
                        return;
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
                throw new APIException("Exception thrown unblocking email: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<AddSegmentsResponse> AddSegments(APIObject api, string slug, string year, List<APISegment> segments)
        {
            Log.D("Network.API.APIHandlers", "Adding Segments.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(api.URL + "segments/add"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new AddSegmentsRequest
                            {
                                Slug = slug,
                                Year = year,
                                Segments = segments,
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
                        var result = JsonSerializer.Deserialize<AddSegmentsResponse>(json);
                        return result;
                    }
                    Log.D("Network.API.APIHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    throw new APIException(errresult.Message);
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.API.APIHandlers", "Exception thrown.");
                throw new APIException("Exception thrown adding segments: " + ex.Message);
            }
        }

        public static async Task<DeleteSegmentsResponse> DeleteSegments(APIObject api, string slug, string year)
        {
            Log.D("Network.API.APIHandlers", "Deleting Segments.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri(api.URL + "segments/delete"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new DeleteSegmentsRequest
                            {
                                Slug = slug,
                                Year = year,
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
                        var result = JsonSerializer.Deserialize<DeleteSegmentsResponse>(json);
                        return result;
                    }
                    Log.D("Network.API.APIHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    throw new APIException(errresult.Message);
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.API.APIHandlers", "Exception thrown.");
                throw new APIException("Exception thrown deleting segments: " + ex.Message);
            }
        }

        public static async Task<GetDistancesResponse> AddDistances(APIObject api, string slug, string year, List<APIDistance> distances)
        {
            Log.D("Network.API.APIHandlers", "Adding Distances.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(api.URL + "distances/add"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new AddDistancesRequest
                            {
                                Slug = slug,
                                Year = year,
                                Distances = distances,
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
                        var result = JsonSerializer.Deserialize<GetDistancesResponse>(json);
                        return result;
                    }
                    Log.D("Network.API.APIHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    throw new APIException(errresult.Message);
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.API.APIHandlers", "Exception thrown.");
                throw new APIException("Exception thrown adding distances: " + ex.Message);
            }
        }

        public static async Task<DeleteDistancesResponse> DeleteDistances(APIObject api, string slug, string year)
        {
            Log.D("Network.API.APIHandlers", "Deleting Distances.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri(api.URL + "distances/delete"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new DeleteDistancesRequest
                            {
                                Slug = slug,
                                Year = year,
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
                        var result = JsonSerializer.Deserialize<DeleteDistancesResponse>(json);
                        return result;
                    }
                    Log.D("Network.API.APIHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    throw new APIException(errresult.Message);
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.API.APIHandlers", "Exception thrown.");
                throw new APIException("Exception thrown deleting distances: " + ex.Message);
            }
        }

        public static async Task<GetSmsSubscriptionsResponse> GetSmsSubscriptions(APIObject api, string slug, string year)
        {
            Log.D("Network.API.APIHandlers", "Adding Segments.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(api.URL + "sms"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new GetSmsSubscriptionsRequest
                            {
                                Slug = slug,
                                Year = year,
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
                        var result = JsonSerializer.Deserialize<GetSmsSubscriptionsResponse>(json);
                        return result;
                    }
                    Log.D("Network.API.APIHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    throw new APIException(errresult.Message);
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.API.APIHandlers", "Exception thrown.");
                throw new APIException("Exception thrown getting sms subscriptions: " + ex.Message);
            }
        }
    }
}
