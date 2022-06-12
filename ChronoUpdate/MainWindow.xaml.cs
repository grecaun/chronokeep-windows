using ChronoUpdate.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChronoUpdate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string uri;
        private string download_uri;
        private readonly string repo_uri = "https://api.github.com/repos/grecaun/chronokeep-windows/releases";

        private string version;

        private CancellationTokenSource cancellationToken = null;
        private Dictionary<string, string> arguments = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();
            DownloadProgress.Visibility = Visibility.Collapsed;
            InstallButton.IsEnabled = false;

            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i += 2)
            {
                arguments.Add(args[i], args[i + 1]);
            }
            if (arguments.ContainsKey("--version"))
            {
                version = arguments["--version"];
            }
            else
            {
                //this.Close();
                version = "v0.3.9-x64";
            }
            download_uri = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\Chronokeep-{version}.zip";
            uri = "";
            GetReleases();
        }

        private static HttpClient GetHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("Chronokeep Desktop Application");
            return client;
        }

        private async void GetReleases()
        {
            Log.D("Updates.Check", "Getting releases.");
            Vers current = new Vers();
            string[] vers = version.Split('.');
            if (vers.Length >= 3)
            {
                current.major = int.Parse(vers[0].Replace("v", "", StringComparison.OrdinalIgnoreCase));
                current.minor = int.Parse(vers[1]);
                current.patch = int.Parse(vers[2].Split('-')[0]);
                current.arch = vers[2].Split('-').Last();
            }
            string content = "";
            try
            {
                using (var client = GetHttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, repo_uri);
                    HttpResponseMessage response = await client.SendAsync(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Log.D("Updates.Check", "Status Code OK");
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<List<GithubRelease>>(json);
                        if (result != null)
                        {
                            foreach (GithubRelease release in result)
                            {
                                Vers releaseVersion = new Vers();
                                if (release.name != null)
                                {
                                    vers = release.name.Split('.');
                                    if (vers.Length == 4)
                                    {
                                        releaseVersion.major = int.Parse(vers[0].Replace("v", "", StringComparison.OrdinalIgnoreCase));
                                        releaseVersion.minor = int.Parse(vers[1]);
                                        releaseVersion.patch = int.Parse(vers[2].Split('-')[0]);
                                        releaseVersion.arch = vers[3];
                                    }
                                    if (vers.Length == 3)
                                    {
                                        releaseVersion.major = int.Parse(vers[0].Replace("v", "", StringComparison.OrdinalIgnoreCase));
                                        releaseVersion.minor = int.Parse(vers[1]);
                                        releaseVersion.patch = int.Parse(vers[2].Split('-')[0]);
                                        releaseVersion.arch = vers[2].Split('-').Last();
                                    }
                                    Log.D("MainWindow", $"Release version {releaseVersion}");
                                    if (releaseVersion.Equal(current))
                                    {
                                        Log.D("MainWindow", "Found our release version.");
                                        if (release.assets != null && release.assets[0].browser_download_url != null)
                                        {
                                            uri = release.assets[0].browser_download_url;
                                            InstallButton.IsEnabled = true;
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        return;
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

        private static async Task DownloadFileAsync(HttpClient client, Stream destination, string uri, IProgress<double> progress, CancellationToken token)
        {
            HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"The request returned with HTTP status code{response.StatusCode}");
            }

            long total = response.Content.Headers.ContentLength.HasValue ? response.Content.Headers.ContentLength.Value : -1L;
            bool canReportProgress = total != -1 && progress != null;

            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                long totalRead = 0L;
                byte[] buffer = new byte[8192];
                bool isMoreToRead = true;
                double lastReport = 0L;
                do
                {
                    token.ThrowIfCancellationRequested();
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (read == 0)
                    {
                        isMoreToRead = false;
                    }
                    else
                    {
                        await destination.WriteAsync(buffer, 0, read, token);
                        totalRead += read;
                        double report = Math.Truncate((totalRead * 1d) / (total * 1d) * 1000) / 10;
                        if (canReportProgress && ((report > lastReport + 0.5) || report == 100))
                        {
                            progress.Report(report);
                            lastReport = report;
                        }
                    }
                } while (isMoreToRead);
            }

        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (((string)InstallButton.Content).Equals("Download", StringComparison.OrdinalIgnoreCase))
            {
                Log.D("Updates.DownloadWindow", $"Download clicked. Downloading to {download_uri}");
                DownloadProgress.Visibility = Visibility.Visible;
                DownloadLabel.Content = $"Downloading {version}";
                InstallButton.Content = "Install";
                InstallButton.IsEnabled = false;
                using (var client = GetHttpClient())
                {
                    using (var file = new FileStream(download_uri, FileMode.Create))
                    {
                        var progress = new Progress<double>();
                        progress.ProgressChanged += (s, value) =>
                        {
                            Log.D("Updates.Check", $"Download at {value}%");
                            DownloadProgress.Value = value;
                        };
                        cancellationToken = new CancellationTokenSource();
                        try
                        {
                            await DownloadFileAsync(client, file, uri, progress, cancellationToken.Token);
                        }
                        catch (Exception ex)
                        {
                            Log.E("Updates.Check", $"Error downloading update. {ex.Message}");
                            MessageBox.Show("Unable to download update.", "Error");
                        }
                    }
                }
                InstallButton.IsEnabled = true;
            }
            else if (((string)InstallButton.Content).Equals("Install", StringComparison.OrdinalIgnoreCase))
            {
                Log.D("Updates.DownloadWindow", "Install clicked.");
            }
            else
            {
                Log.D("Updates.DownloadWindow", "Something went wrong and button text was not valid.");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Updates.DownloadWindow", "Cancel clicked.");
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (cancellationToken != null) cancellationToken.Cancel();
        }
    }
}
