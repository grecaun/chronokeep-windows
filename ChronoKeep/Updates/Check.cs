using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChronoKeep.Updates
{
    internal class Version
    {
        public int major;
        public int minor;
        public int patch;

        public Version()
        {
            major = 0;
            minor = 0;
            patch = 0;
        }

        public bool Equal(Version other)
        {
            return this.major == other.major && this.minor == other.minor && this.patch == other.patch;
        }

        public bool Newer(Version other)
        {
            if ((this.major > other.major)
                || (this.major == other.major && this.minor > other.minor)
                || (this.major == other.major && this.minor == other.minor && this.patch > other.patch))
            {
                return true;
            }
            return false;
        }

        public void Set(Version other)
        {
            this.major = other.major;
            this.minor = other.minor;
            this.patch = other.patch;
        }

        public override string ToString()
        {
            return string.Format("v{0}.{1}.{2}", major, minor, patch);
        }
    }

    internal class Check
    {
        private static string RepoURL = "https://api.github.com/repos/grecaun/chronokeep-windows/releases";

        public static async void Do()
        {
            string curVersion;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ChronoKeep." + "version.txt"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    curVersion = reader.ReadToEnd();
                }
            }
            Version current = new Version();
            string[] version = curVersion.Split('.');
            if (version.Length >= 3)
            {
                version[0] = version[0].Replace('v', '0');
                version[2] = version[2].Split('-')[0];
                current.major = int.Parse(version[0]);
                current.minor = int.Parse(version[1]);
                current.patch = int.Parse(version[2]);
            }
            Log.D("Updates.Check", string.Format("Version found {0}", current.ToString()));
            List<GithubRelease> releases = null;
            try
            {
                releases = await GetReleases();
            }
            catch (Exception ex)
            {
                Log.D("Updates.Check", ex.Message);
                MessageBox.Show(ex.Message);
                return;
            }
            GithubRelease latest_x86 = null;
            GithubRelease latest_x64 = null;
            Version latest = new Version();
            foreach (GithubRelease release in releases)
            {
                Version rel = new Version();
                version = release.Name.Split('.');
                if (version.Length == 4)
                {
                    version[0] = version[0].Replace('v', '0');
                    version[2] = version[2].Split('-')[0];
                    rel.major = int.Parse(version[0]);
                    rel.minor = int.Parse(version[1]);
                    rel.patch = int.Parse(version[2]);
                }
                // Check for major version updates
                // Then minor version updates
                // patches
                // check for equal versions because we store both x86 and x64 versions
                if (rel.Newer(latest) || rel.Equal(latest))
                {
                    if (version[3].Equals("x86", StringComparison.OrdinalIgnoreCase))
                    {
                        latest_x86 = release;
                    }
                    else if (version[3].Equals("x64", StringComparison.OrdinalIgnoreCase))
                    {
                        latest_x64 = release;
                    }
                    latest.Set(rel);
                }
            }
            Log.D("Updates.Check", string.Format("Latest version is {0}", latest.ToString()));
            if (latest.Newer(current))
            {
                Log.D("Updates.Check", "Newer version found.");
                var selectedOption = MessageBox.Show("Download update?", "Update Found", MessageBoxButton.YesNo);
                if (selectedOption == MessageBoxResult.Yes)
                {
                    if (Is64Bit())
                    {
                        if (latest_x64 != null)
                        {
                            Log.D("Updates.Check", string.Format("Download URL (64 bit) - {0}", latest_x64.Assets[0].BrowserDownloadURL));
                            System.Diagnostics.Process.Start(latest_x64.Assets[0].BrowserDownloadURL);
                        }
                    }
                    else
                    {
                        if (latest_x86 != null)
                        {
                            Log.D("Updates.Check", string.Format("Download URL (32 bit) - {0}", latest_x86.Assets[0].BrowserDownloadURL));
                            System.Diagnostics.Process.Start(latest_x86.Assets[0].BrowserDownloadURL);
                        }
                    }
                }
            }
        }

        private static bool Is64Bit()
        {
            return IntPtr.Size == 8;
        }

        private static async Task<List<GithubRelease>> GetReleases()
        {
            Log.D("Updates.Check", "Getting releases.");
            string content = "";
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, RepoURL);
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Updates.Check", "Status Code OK");
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<List<GithubRelease>>(json);
                        return result;
                    }
                    Log.D("Updates.Check", "Status Code not OK");
                    content = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Log.D("Updates.Check", "Exception thrown.");
                throw new Exception("Exception thrown getting releases: " + ex.Message);
            }
            throw new Exception(string.Format("Unable to get releases. {0}", content));
        }

        private static HttpClient GetHttpClient()
        {
            var handler = new WinHttpHandler();
            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(2);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("Chronokeep Desktop Application");
            return client;
        }
    }
}