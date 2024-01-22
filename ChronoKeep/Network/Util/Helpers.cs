using System;
using System.Net.Http.Headers;
using System.Net.Http;

namespace Chronokeep.Network.Util
{
    internal class Helpers
    {
        internal static HttpClient GetHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}
