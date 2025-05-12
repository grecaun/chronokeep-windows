using Chronokeep.Objects.ChronokeepRemote;
using Chronokeep.Network.API;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using Chronokeep.Objects;
using System.Text.Json;
using Chronokeep.Objects.API;
using System.Threading.Tasks;
using System.Text;
using static Chronokeep.Network.Util.Helpers;

namespace Chronokeep.Network.Remote
{
    public class RemoteHandlers
    {
        public static async Task<GetReadersResponse> GetReaders(APIObject api)
        {
            string content;
            Log.D("Network.Remote.RemoteHandlers", "Getting remote readers.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(api.URL + "readers")
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api.AuthToken);
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.Remote.RemoteHandlers", "Status code ok.");
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<GetReadersResponse>(json);
                        return result;
                    }
                    Log.D("Network.Remote.RemoteHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    content = errresult.Message;
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.Remote.RemoteHandlers", "Exception thrown.");
                throw new APIException("Exception thrown getting events: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<GetReadsResponse> GetReads(APIObject api, string reader, long start, long end)
        {
            string content;
            Log.D("Network.Remote.RemoteHandlers", "Getting reads.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(api.URL + "reads"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new GetReadsRequest
                            {
                                ReaderName = reader,
                                Start = start,
                                End = end
                            }),
                            Encoding.UTF8,
                            "application/json"
                            )
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api.AuthToken);
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.Remote.RemoteHandlers", "Status code ok.");
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<GetReadsResponse>(json);
                        return result;
                    }
                    Log.D("Network.Remote.RemoteHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    content = errresult.Message;
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.Remote.RemoteHandlers", "Exception thrown.");
                throw new APIException("Exception thrown getting events: " + ex.Message);
            }
            throw new APIException(content);
        }

        public static async Task<DeleteReadsResponse> DeleteReads(APIObject api, string reader, long start, long end)
        {
            string content;
            Log.D("Network.Remote.RemoteHandlers", "Deleting reads.");
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri(api.URL + "reads/delete"),
                        Content = new StringContent(
                            JsonSerializer.Serialize(new DeleteReadsRequest
                            {
                                ReaderName = reader,
                                Start = start,
                                End = end
                            }),
                            Encoding.UTF8,
                            "application/json"
                            )
                    };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api.AuthToken);
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Network.Remote.RemoteHandlers", "Status code ok.");
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<DeleteReadsResponse>(json);
                        return result;
                    }
                    Log.D("Network.Remote.RemoteHandlers", "Status code not ok.");
                    var errjson = await response.Content.ReadAsStringAsync();
                    var errresult = JsonSerializer.Deserialize<ErrorResponse>(errjson);
                    content = errresult.Message;
                }
            }
            catch (Exception ex)
            {
                Log.D("Network.Remote.RemoteHandlers", "Exception thrown.");
                throw new APIException("Exception thrown getting events: " + ex.Message);
            }
            throw new APIException(content);
        }
    }
}
