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
        IMainPage page;
        String dbName = "EventDirector.sqlite";
        String programDir = "EventDirector";
        bool closing = false;

        Thread tcpServerThread = null;
        TCPServer tcpServer = null;
        Thread zeroConfThread = null;
        ZeroConf zeroConf = null;

        List<Window> openWindows = new List<Window>();

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

            page = new DashboardPage(this, database);
            TheFrame.Content = page;
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
                database.SetAppSetting(Constants.Settings.CURRENT_EVENT, Constants.Settings.NULL_EVENT_ID);
            }
            if (database.GetAppSetting(Constants.Settings.COMPANY_NAME) == null)
            {
                database.SetAppSetting(Constants.Settings.COMPANY_NAME, Constants.Settings.WAIVER_COMPANY);
            }
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Dashboard button clicked.");
            if (page is DashboardPage)
            {
                Log.D("Dashboard page already displayed.");
                return;
            }
            TheFrame.NavigationService.RemoveBackEntry();
            page = new DashboardPage(this, database);
            TheFrame.Content = page;
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ParticipantsButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BibsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Bibs button clicked.");
            if (page is BibAssignmentPage)
            {
                Log.D("Bib page already displayed.");
                return;
            }
            TheFrame.NavigationService.RemoveBackEntry();
            page = new BibAssignmentPage(this, database);
            TheFrame.Content = page;
        }

        private void ChipsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Chips button clicked.");
            if (page is ChipAssigmentPage)
            {
                Log.D("Chips page already displayed.");
                return;
            }
            TheFrame.NavigationService.RemoveBackEntry();
            page = new ChipAssigmentPage(this, database);
            TheFrame.Content = page;
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true;
            StopNetworkServices();
            foreach (Window w in openWindows)
            {
                try
                {
                    w.Close();
                }
                catch
                {
                    Log.D("Oh well!");
                }
            }
        }

        public void WindowClosed(Window window) { }

        public void UpdateEvent(int identifier, string nameString, long dateVal, int nextYear, int shirtOptionalVal, int shirtPrice)
        {
            Log.D("Updating event information via TCP Server.");
            tcpServer.UpdateEvent(identifier);
        }

        public void AddEvent(string nameString, long dateVal, int shirtOptionalVal, int shirtPrice) { }

        public bool StartNetworkServices()
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
        }

        public bool StopNetworkServices()
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
        }

        public void UpdateChangesBox()
        {
        }

        public void WindowFinalize(Window w)
        {
            page.Update();
            if (!closing) openWindows.Remove(w);
            try
            {
                if (tcpServer != null)
                {
                    tcpServer.UpdateEvent(Convert.ToInt32(database.GetAppSetting(Constants.Settings.CURRENT_EVENT).value));
                    tcpServer.UpdateEventKiosk(Convert.ToInt32(database.GetAppSetting(Constants.Settings.CURRENT_EVENT).value));
                }
            }
            catch { }
            UpdateStatus();
        }

        public void AddWindow(Window w)
        {
            openWindows.Add(w);
        }

        public void UpdateStatus()
        {
            Event theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier == -1)
            {
                participantsButton.IsEnabled = false;
                bibsButton.IsEnabled = false;
                chipsButton.IsEnabled = false;
                reportsButton.IsEnabled = false;
                divisionsButton.IsEnabled = false;
                locationsButton.IsEnabled = false;
                segmentsButton.IsEnabled = false;
                agegroupsButton.IsEnabled = false;
            }
            else
            {
                participantsButton.IsEnabled = true;
                bibsButton.IsEnabled = true;
                chipsButton.IsEnabled = true;
                reportsButton.IsEnabled = true;
                divisionsButton.IsEnabled = true;
                locationsButton.IsEnabled = true;
                segmentsButton.IsEnabled = true;
                agegroupsButton.IsEnabled = true;
            }
        }
    }
}
