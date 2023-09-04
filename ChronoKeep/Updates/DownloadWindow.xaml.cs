﻿using Chronokeep.Interfaces;
using Chronokeep.UI.UIObjects;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Controls;

namespace Chronokeep.Updates
{
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow : UiWindow
    {
        private string uri;
        private string download_uri;
        private string version;

        private string appName = "Chronokeep";
        private string dbName = "Chronokeep.sqlite";

        private CancellationTokenSource cancellationToken = null;

        private IMainWindow mWindow;

        public DownloadWindow(GithubRelease r, Version v, IMainWindow mWindow)
        {
            InitializeComponent();
            DownloadProgress.Visibility = Visibility.Collapsed;
            this.version = v.ToString();
            download_uri = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\chronokeep-setup-{version}.exe";
            Log.D("Updates.Check", string.Format("Download URL - {0}", r.Assets[0].BrowserDownloadURL));
            uri = r.Assets[0].BrowserDownloadURL;
            Activate();
            Topmost = true;
            this.mWindow = mWindow;
            this.MinHeight = 0;
            this.Height = 250;
            this.MinWidth = 0;
            this.Width = 400;
        }

        private static HttpClient GetHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
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
                DownloadLabel.Text = $"Downloading {version}";
                InstallButton.Content = "Install";
                InstallButton.IsEnabled = false;
                BackupDatabaseButton.IsEnabled = false;
                BackupDatabaseButton.Visibility = Visibility.Visible;
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
                            DialogBox.Show("Unable to download update.");
                            Close();
                        }
                    }
                }
                InstallButton.IsEnabled = true;
                BackupDatabaseButton.IsEnabled = true;
            }
            else if (((string)InstallButton.Content).Equals("Install", StringComparison.OrdinalIgnoreCase))
            {
                Log.D("Updates.DownloadWindow", "Install clicked.");
                using Process install = new Process();
                install.StartInfo.FileName = download_uri;
                install.Start();
                Close();
                mWindow.Exit();
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

        private void BackupDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Updates.DownloadWindow", "Backup Database clicked.");
            UpdatePanel.Visibility = Visibility.Collapsed;
            BackupDatabaseButton.Visibility = Visibility.Collapsed;
            BackupPanel.Visibility = Visibility.Visible;
            backupBlock.Text = $"{backupBlock.Text}\nChecking for old database files.";
            string dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), appName);
            string path = Path.Combine(dirPath, dbName);
            Log.D("Updates.DownloadWindow", "Looking for database file.");
            if (Directory.Exists(dirPath))
            {
                if (File.Exists(path))
                {
                    string backup = System.IO.Path.Combine(dirPath, $"Chronokeep-{DateTime.Now.ToString("yyyy-MM-dd")}-backup.sqlite");
                    try
                    {
                        backupBlock.Text = $"{backupBlock.Text}\nBacking up database.";
                        File.Copy(path, backup, false);
                        backupBlock.Text = $"{backupBlock.Text}\n{backup}";
                    }
                    catch
                    {
                        backupBlock.Text = $"{backupBlock.Text}\nError backing up database.";
                    }
                }
            }
        }
    }
}
