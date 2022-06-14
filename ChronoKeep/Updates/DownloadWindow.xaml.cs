using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Chronokeep.Updates
{
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow : Window
    {
        private string uri;
        private string download_uri;
        private string version;

        private CancellationTokenSource cancellationToken = null;

        public DownloadWindow(Release release, Version version)
        {
            InitializeComponent();
            DownloadProgress.Visibility = Visibility.Collapsed;
            this.version = version.ToString();
            if (Is64Bit())
            {
                if (release.x64 != null)
                {
                    Log.D("Updates.Check", string.Format("Download URL (64 bit) - {0}", release.x64.Assets[0].BrowserDownloadURL));
                    uri = release.x64.Assets[0].BrowserDownloadURL;
                }
            }
            else
            {
                if (release.x86 != null)
                {
                    Log.D("Updates.Check", string.Format("Download URL (32 bit) - {0}", release.x86.Assets[0].BrowserDownloadURL));
                    uri = release.x86.Assets[0].BrowserDownloadURL;
                }
            }
            download_uri = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\Chronokeep-{version}-{(Is64Bit() ? "x64" : "x86")}.zip";
            this.Activate();
            this.Topmost = true;
        }

        private static bool Is64Bit()
        {
            return IntPtr.Size == 8;
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
