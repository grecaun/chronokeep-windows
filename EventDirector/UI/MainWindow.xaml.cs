using EventDirector.Interfaces;
using EventDirector.UI.MainPages;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
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

namespace EventDirector.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INewMainWindow, IChangeUpdater
    {
        IDBInterface database;
        String dbName = "EventDirector.sqlite";
        String programDir = "EventDirector";
        bool closing = false;

        Thread tcpServerThread = null;
        TCPServer tcpServer = null;
        Thread zeroConfThread = null;
        ZeroConf zeroConf = null;

        public MainWindow()
        {
            InitializeComponent();
            String dirPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), programDir);
            String path = System.IO.Path.Combine(dirPath, dbName);
            Log.D("Looking for database file.");
            if (!Directory.Exists(dirPath))
            {
                Log.D("Creating directory.");
                Directory.CreateDirectory(dirPath);
            }
            if (!File.Exists(path))
            {
                Log.D("Creating database file.");
                SQLiteConnection.CreateFile(path);
            }
            database = new SQLiteInterface(path);
            database.Initialize();

            SetupSettings();

            TheFrame.Content = new DashboardPage(this, database);
        }

        private void SetupSettings()
        {
            if (database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR) == null)
            {
                String dirPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), programDir, "Exports");
                database.SetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR, dirPath);
            }
            if (database.GetAppSetting(Constants.Settings.DEFAULT_TIMING_SYSTEM) == null)
            {
                database.SetAppSetting(Constants.Settings.DEFAULT_TIMING_SYSTEM, Constants.Settings.TIMING_LAST_USED);
            }
            if (database.GetAppSetting(Constants.Settings.DEFAULT_WAIVER) == null)
            {
                database.SetAppSetting(Constants.Settings.DEFAULT_WAIVER, Constants.Settings.EXAMPLE_WAIVER);
            }
            if (database.GetAppSetting(Constants.Settings.CURRENT_EVENT) == null)
            {
                List<Event> events = database.GetEvents();
                int highest = -1;
                foreach (Event e in events)
                {
                    highest = e.Identifier > highest ? e.Identifier : highest;
                }
                database.SetAppSetting(Constants.Settings.CURRENT_EVENT, highest.ToString());
            }
            if (database.GetAppSetting(Constants.Settings.COMPANY_NAME) == null)
            {
                database.SetAppSetting(Constants.Settings.COMPANY_NAME, Constants.Settings.WAIVER_COMPANY);
            }
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ParticipantsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DivisionsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LocationsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SegmentsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AgegroupsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            await StopNetworkServices();
        }

        public void WindowClosed(Window window) { }

        public void UpdateEvent(int identifier, string nameString, long dateVal, int nextYear, int shirtOptionalVal, int shirtPrice) { }

        public void AddEvent(string nameString, long dateVal, int shirtOptionalVal, int shirtPrice) { }

        public async Task<bool> StartNetworkServices()
        {
            return await Task.Run( () =>
            {
                try
                {
                    Log.D("Starting TCP server thread.");
                    tcpServer = new TCPServer(database, this);
                    tcpServerThread = new Thread(new ThreadStart(tcpServer.Run));
                    tcpServerThread.Start();
                    Log.D("Starting zero configuration thread.");
                    zeroConf = new ZeroConf(database.GetServerName());
                    zeroConfThread = new Thread(new ThreadStart(zeroConf.Run));
                    zeroConfThread.Start();
                }
                catch
                {
                    return false;
                }
                return true;
            });
        }

        public async Task<bool> StopNetworkServices()
        {
            return await Task.Run(() =>
            {
                try
                {
                    Log.D("Stopping TCP server thread.");
                    tcpServer.Stop();
                    tcpServerThread.Abort();
                    tcpServerThread.Join();
                    Log.D("Stopping zero configuration thread.");
                    zeroConf.Stop();
                    zeroConfThread.Abort();
                    zeroConfThread.Join();
                }
                catch
                {
                    return false;
                }
                return true;
            });
        }

        public void UpdateChangesBox()
        {
        }
    }
}
