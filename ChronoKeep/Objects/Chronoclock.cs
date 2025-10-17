using Chronokeep.Helpers;
using Chronokeep.Network.API;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Chronokeep.Network.Util.Helpers;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Chronokeep.Objects
{
    public class Chronoclock
    {
        public int Identifier { get; set; }
        public string Name { get; set; }
        public string URL { get; set; }
        public bool Enabled { get; set; }

        public async Task<CountUpDownTimestampResponse> StartCountUp()
        {
            Log.D("Chronokeep.Objects.Chronoclock", "StartCountUp");
            if (this.URL == null || this.URL.Length == 0)
            {
                throw new APIException("url not set");
            }
            string content;
            try
            {
                using HttpClient client = GetHttpClient();
                HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new("http://" + this.URL + "/start"),
                };
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Log.D("Chronokeep.Objects.Chronoclock", "Status code = ok.");
                    string json = await response.Content.ReadAsStringAsync();
                    CountUpDownTimestampResponse result = JsonSerializer.Deserialize<CountUpDownTimestampResponse>(json);
                    return result;
                }
                Log.D("Chronokeep.Objects.Chronoclock", "Status code = conflict.");
                string eJson = await response.Content.ReadAsStringAsync();
                ChronoclockErrorResponse eResult = JsonSerializer.Deserialize<ChronoclockErrorResponse>(eJson);
                content = eResult.Error;
            }
            catch (Exception ex)
            {
                Log.D("Chronokeep.Objects.Chronoclock", "Exception thrown.");
                throw new APIException("Exception thrown starting countup: " + ex.Message);
            }
            throw new APIException(content);
        }

        public async Task<CountUpDownTimestampResponse> StopCountUp()
        {
            Log.D("Chronokeep.Objects.Chronoclock", "StopCountUp");
            if (this.URL == null || this.URL.Length == 0)
            {
                throw new APIException("url not set");
            }
            string content;
            try
            {
                using HttpClient client = GetHttpClient();
                HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new("http://" + this.URL + "/stop"),
                };
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Log.D("Chronokeep.Objects.Chronoclock", "Status code = ok.");
                    string json = await response.Content.ReadAsStringAsync();
                    CountUpDownTimestampResponse result = JsonSerializer.Deserialize<CountUpDownTimestampResponse>(json);
                    return result;
                }
                Log.D("Chronokeep.Objects.Chronoclock", "Status code = conflict.");
                string eJson = await response.Content.ReadAsStringAsync();
                ChronoclockErrorResponse eResult = JsonSerializer.Deserialize<ChronoclockErrorResponse>(eJson);
                content = eResult.Error;
            }
            catch (Exception ex)
            {
                Log.D("Chronokeep.Objects.Chronoclock", "Exception thrown.");
                throw new APIException("Exception thrown stopping countup: " + ex.Message);
            }
            throw new APIException(content);
        }

        public async Task<CountUpDownTimestampResponse> AdjustTime(int seconds)
        {
            Log.D("Chronokeep.Objects.Chronoclock", "AdjustTime");
            if (this.URL == null || this.URL.Length == 0)
            {
                throw new APIException("url not set");
            }
            string content;
            try
            {
                using HttpClient client = GetHttpClient();
                string which = "/add_seconds";
                if (seconds < 0)
                {
                    seconds = seconds * -1;
                    which = "/remove_seconds";
                }
                Dictionary<string, string> postContent = [];
                postContent["seconds"] = seconds.ToString();
                HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new("http://" + this.URL + which),
                    Content = new FormUrlEncodedContent(postContent)
                };
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Log.D("Chronokeep.Objects.Chronoclock", "Status code = ok.");
                    string json = await response.Content.ReadAsStringAsync();
                    CountUpDownTimestampResponse result = JsonSerializer.Deserialize<CountUpDownTimestampResponse>(json);
                    return result;
                }
                Log.D("Chronokeep.Objects.Chronoclock", "Status code = conflict.");
                string eJson = await response.Content.ReadAsStringAsync();
                ChronoclockErrorResponse eResult = JsonSerializer.Deserialize<ChronoclockErrorResponse>(eJson);
                content = eResult.Error;
            }
            catch (Exception ex)
            {
                Log.D("Chronokeep.Objects.Chronoclock", "Exception thrown.");
                throw new APIException("Exception thrown adjusting time: " + ex.Message);
            }
            throw new APIException(content);
        }

        public async Task<GetTimeResponse> GetTime()
        {
            Log.D("Chronokeep.Objects.Chronoclock", "GetTime");
            if (this.URL == null || this.URL.Length == 0)
            {
                throw new APIException("url not set");
            }
            string content;
            try
            {
                using HttpClient client = GetHttpClient();
                HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new("http://" + this.URL + "/get_time"),
                };
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Log.D("Chronokeep.Objects.Chronoclock", "Status code = ok.");
                    string json = await response.Content.ReadAsStringAsync();
                    GetTimeResponse result = JsonSerializer.Deserialize<GetTimeResponse>(json);
                    return result;
                }
                Log.D("Chronokeep.Objects.Chronoclock", "Status code = conflict.");
                string eJson = await response.Content.ReadAsStringAsync();
                ChronoclockErrorResponse eResult = JsonSerializer.Deserialize<ChronoclockErrorResponse>(eJson);
                content = eResult.Error;
            }
            catch (Exception ex)
            {
                Log.D("Chronokeep.Objects.Chronoclock", "Exception thrown.");
                throw new APIException("Exception thrown getting time: " + ex.Message);
            }
            throw new APIException(content);
        }

        public async Task<GetConfigResponse> GetConfig()
        {
            Log.D("Chronokeep.Objects.Chronoclock", "GetConfig");
            if (this.URL == null || this.URL.Length == 0)
            {
                throw new APIException("url not set");
            }
            string content;
            try
            {
                using HttpClient client = GetHttpClient();
                HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Get,
                    RequestUri = new("http://" + this.URL + "/config.json"),
                };
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Log.D("Chronokeep.Objects.Chronoclock", "Status code = ok.");
                    string json = await response.Content.ReadAsStringAsync();
                    GetConfigResponse result = JsonSerializer.Deserialize<GetConfigResponse>(json);
                    return result;
                }
                Log.D("Chronokeep.Objects.Chronoclock", "Status code = conflict.");
                string eJson = await response.Content.ReadAsStringAsync();
                ChronoclockErrorResponse eResult = JsonSerializer.Deserialize<ChronoclockErrorResponse>(eJson);
                content = eResult.Error;
            }
            catch (Exception ex)
            {
                Log.D("Chronokeep.Objects.Chronoclock", "Exception thrown.");
                throw new APIException("Exception thrown getting config: " + ex.Message);
            }
            throw new APIException(content);
        }

        public async Task<GetTimeResponse> SetTime(DateTime date)
        {
            Log.D("Chronokeep.Objects.Chronoclock", "SetTime");
            if (this.URL == null || this.URL.Length == 0)
            {
                throw new APIException("url not set");
            }
            string content;
            try
            {
                Dictionary<string, string> postContent = [];
                postContent["DateTime"] = date.ToString("yyyy-MM-dd HH:mm:ss").ToString();
                using HttpClient client = GetHttpClient();
                HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new("http://" + this.URL + "/set_time"),
                    Content = new FormUrlEncodedContent(postContent)
                };
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Log.D("Chronokeep.Objects.Chronoclock", "Status code = ok.");
                    string json = await response.Content.ReadAsStringAsync();
                    GetTimeResponse result = JsonSerializer.Deserialize<GetTimeResponse>(json);
                    return result;
                }
                Log.D("Chronokeep.Objects.Chronoclock", "Status code = conflict.");
                string eJson = await response.Content.ReadAsStringAsync();
                ChronoclockErrorResponse eResult = JsonSerializer.Deserialize<ChronoclockErrorResponse>(eJson);
                content = eResult.Error;
            }
            catch (Exception ex)
            {
                Log.D("Chronokeep.Objects.Chronoclock", "Exception thrown.");
                throw new APIException("Exception thrown setting time: " + ex.Message);
            }
            throw new APIException(content);
        }

        public async Task<CountUpDownTimestampResponse> SetCountUpDownTime(DateTime date)
        {
            Log.D("Chronokeep.Objects.Chronoclock", "SetCountUpDownTime");
            if (this.URL == null || this.URL.Length == 0)
            {
                throw new APIException("url not set");
            }
            string content;
            try
            {
                Dictionary<string, string> postContent = [];
                postContent["DateTime"] = date.ToString("yyyy-MM-dd HH:mm:ss.fff");
                using HttpClient client = GetHttpClient();
                HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new("http://" + this.URL + "/set_countupdown"),
                    Content = new FormUrlEncodedContent(postContent)
                };
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Log.D("Chronokeep.Objects.Chronoclock", "Status code = ok.");
                    string json = await response.Content.ReadAsStringAsync();
                    CountUpDownTimestampResponse result = JsonSerializer.Deserialize<CountUpDownTimestampResponse>(json);
                    return result;
                }
                Log.D("Chronokeep.Objects.Chronoclock", "Status code = conflict.");
                string eJson = await response.Content.ReadAsStringAsync();
                ChronoclockErrorResponse eResult = JsonSerializer.Deserialize<ChronoclockErrorResponse>(eJson);
                content = eResult.Error;
            }
            catch (Exception ex)
            {
                Log.D("Chronokeep.Objects.Chronoclock", "Exception thrown.");
                throw new APIException("Exception thrown setting time: " + ex.Message);
            }
            throw new APIException(content);
        }

        public async Task<CountUpDownTimestampResponse> SetFlipDisplay(bool flipDisplay)
        {
            Log.D("Chronokeep.Objects.Chronoclock", "SetFlipDisplay");
            if (this.URL == null || this.URL.Length == 0)
            {
                throw new APIException("url not set");
            }
            string content;
            try
            {
                Dictionary<string, string> postContent = [];
                postContent["value"] = flipDisplay ? "1" : "0";
                using HttpClient client = GetHttpClient();
                HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new("http://" + this.URL + "/set_flip"),
                    Content = new FormUrlEncodedContent(postContent)
                };
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Log.D("Chronokeep.Objects.Chronoclock", "Status code = ok.");
                    string json = await response.Content.ReadAsStringAsync();
                    CountUpDownTimestampResponse result = JsonSerializer.Deserialize<CountUpDownTimestampResponse>(json);
                    return result;
                }
                Log.D("Chronokeep.Objects.Chronoclock", "Status code = conflict.");
                string eJson = await response.Content.ReadAsStringAsync();
                ChronoclockErrorResponse eResult = JsonSerializer.Deserialize<ChronoclockErrorResponse>(eJson);
                content = eResult.Error;
            }
            catch (Exception ex)
            {
                Log.D("Chronokeep.Objects.Chronoclock", "Exception thrown.");
                throw new APIException("Exception thrown setting flipDisplay: " + ex.Message);
            }
            throw new APIException(content);
        }

        public async Task<CountUpDownTimestampResponse> SetTwelveHour(bool twelveHour)
        {
            Log.D("Chronokeep.Objects.Chronoclock", "SetTwelveHour");
            if (this.URL == null || this.URL.Length == 0)
            {
                throw new APIException("url not set");
            }
            string content;
            try
            {
                Dictionary<string, string> postContent = [];
                postContent["value"] = twelveHour ? "1" : "0";
                using HttpClient client = GetHttpClient();
                HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new("http://" + this.URL + "/set_twelvehour"),
                    Content = new FormUrlEncodedContent(postContent)
                };
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Log.D("Chronokeep.Objects.Chronoclock", "Status code = ok.");
                    string json = await response.Content.ReadAsStringAsync();
                    CountUpDownTimestampResponse result = JsonSerializer.Deserialize<CountUpDownTimestampResponse>(json);
                    return result;
                }
                Log.D("Chronokeep.Objects.Chronoclock", "Status code = conflict.");
                string eJson = await response.Content.ReadAsStringAsync();
                ChronoclockErrorResponse eResult = JsonSerializer.Deserialize<ChronoclockErrorResponse>(eJson);
                content = eResult.Error;
            }
            catch (Exception ex)
            {
                Log.D("Chronokeep.Objects.Chronoclock", "Exception thrown.");
                throw new APIException("Exception thrown setting twelveHour: " + ex.Message);
            }
            throw new APIException(content);
        }

        public async Task<CountUpDownTimestampResponse> SetLockCountUpDown(bool lockCountUpDown)
        {
            Log.D("Chronokeep.Objects.Chronoclock", "SetLockCountUpDown");
            if (this.URL == null || this.URL.Length == 0)
            {
                throw new APIException("url not set");
            }
            string content;
            try
            {
                Dictionary<string, string> postContent = [];
                postContent["value"] = lockCountUpDown ? "1" : "0";
                using HttpClient client = GetHttpClient();
                HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new("http://" + this.URL + "/set_lock"),
                    Content = new FormUrlEncodedContent(postContent)
                };
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Log.D("Chronokeep.Objects.Chronoclock", "Status code = ok.");
                    string json = await response.Content.ReadAsStringAsync();
                    CountUpDownTimestampResponse result = JsonSerializer.Deserialize<CountUpDownTimestampResponse>(json);
                    return result;
                }
                Log.D("Chronokeep.Objects.Chronoclock", "Status code = conflict.");
                string eJson = await response.Content.ReadAsStringAsync();
                ChronoclockErrorResponse eResult = JsonSerializer.Deserialize<ChronoclockErrorResponse>(eJson);
                content = eResult.Error;
            }
            catch (Exception ex)
            {
                Log.D("Chronokeep.Objects.Chronoclock", "Exception thrown.");
                throw new APIException("Exception thrown setting lockCountUpDown: " + ex.Message);
            }
            throw new APIException(content);
        }

        public async Task<CountUpDownTimestampResponse> SetBrightness(uint brightness)
        {
            Log.D("Chronokeep.Objects.Chronoclock", "SetBrightness");
            if (this.URL == null || this.URL.Length == 0)
            {
                throw new APIException("url not set");
            }
            string content;
            try
            {
                Dictionary<string, string> postContent = [];
                postContent["value"] = brightness.ToString();
                using HttpClient client = GetHttpClient();
                HttpRequestMessage request = new()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new("http://" + this.URL + "/set_brightness"),
                    Content = new FormUrlEncodedContent(postContent)
                };
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Log.D("Chronokeep.Objects.Chronoclock", "Status code = ok.");
                    string json = await response.Content.ReadAsStringAsync();
                    CountUpDownTimestampResponse result = JsonSerializer.Deserialize<CountUpDownTimestampResponse>(json);
                    return result;
                }
                Log.D("Chronokeep.Objects.Chronoclock", "Status code = conflict.");
                string eJson = await response.Content.ReadAsStringAsync();
                ChronoclockErrorResponse eResult = JsonSerializer.Deserialize<ChronoclockErrorResponse>(eJson);
                content = eResult.Error;
            }
            catch (Exception ex)
            {
                Log.D("Chronokeep.Objects.Chronoclock", "Exception thrown.");
                throw new APIException("Exception thrown setting brightness: " + ex.Message);
            }
            throw new APIException(content);
        }
    }

    public class GetConfigResponse
    {
        [JsonPropertyName("mdns")]
        public string MDNS { get; set; }
        [JsonPropertyName("apSsid")]
        public string ApSSID { get; set; }
        [JsonPropertyName("apPassword")]
        public string ApPassword { get; set; }
        [JsonPropertyName("ssids")]
        public List<string> SSIDs { get; set; }
        [JsonPropertyName("passwords")]
        public List<string> Passwords { get; set; }
        [JsonPropertyName("timeZone")]
        public string TimeZone { get; set; }
        [JsonPropertyName("brightness")]
        public uint Brightness {  get; set; }
        [JsonPropertyName("flipDisplay")]
        public bool FlipDisplay { get; set; }
        [JsonPropertyName("twelveHour")]
        public bool TwelveHour { get; set; }
        [JsonPropertyName("lockCountUpDown")]
        public bool LockCountUpDown { get; set; }
        [JsonPropertyName("ntpServer1")]
        public string NtpServer1 { get; set; }
        [JsonPropertyName("ntpServer2")]
        public string NtpServer2 { get; set; }
        [JsonPropertyName("countupdownTimestamp")]
        public long CountUpDownTimestamp { get; set; }
    }

    public class GetTimeResponse
    {
        [JsonPropertyName("time")]
        public string Time { get; set; }
    }

    public class CountUpDownTimestampResponse
    {
        [JsonPropertyName("brightness")]
        public uint Brightness { get; set; }
        [JsonPropertyName("flipDisplay")]
        public bool FlipDisplay { get; set; }
        [JsonPropertyName("twelveHour")]
        public bool TwelveHour { get; set; }
        [JsonPropertyName("lockCountUpDown")]
        public bool LockCountUpDown { get; set; }
        [JsonPropertyName("countupdownTimestamp")]
        public long CountUpDownTimestamp { get; set; }
    }

    public class ChronoclockErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }
    }
}
