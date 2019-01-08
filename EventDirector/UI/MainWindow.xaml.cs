﻿using EventDirector.Interfaces;
using EventDirector.Objects;
using EventDirector.Timing;
using EventDirector.UI.EventWindows;
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
        bool closing = false;
        bool excelEnabled = false;

        Thread tcpServerThread = null;
        TCPServer tcpServer = null;
        Thread zeroConfThread = null;
        ZeroConf zeroConf = null;

        Thread TimingControllerThread = null;
        TimingController TimingController = null;

        List<Window> openWindows = new List<Window>();

        public MainWindow()
        {
            InitializeComponent();
            String dirPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), Constants.Settings.PROGRAM_DIR);
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

            UpdateImportOptions();
            Constants.Settings.SetupSettings(database);

            page = new DashboardPage(this, database);
            TheFrame.Content = page;

            TimingController = new TimingController(database, this);
        }

        private async void UpdateImportOptions()
        {
            await Task.Run(() =>
            {
                excelEnabled = Utils.ExcelEnabled();
            });
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Dashboard button clicked.");
            if (page is DashboardPage)
            {
                Log.D("Dashboard page already displayed.");
                return;
            }
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                page.UpdateDatabase();
            }
            TheFrame.NavigationService.RemoveBackEntry();
            page = new DashboardPage(this, database);
            TheFrame.Content = page;
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Reports button clicked.");
            if (page is ReportsPage)
            {
                Log.D("Reports page already displayed");
                return;
            }
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                page.UpdateDatabase();
            }
            //TheFrame.NavigationService.RemoveBackEntry();
            //page = new ReportsPage(this, database);
            //TheFrame.Content = page;
        }

        private void ParticipantsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Participants button clicked.");
            if (page is ParticipantsPage)
            {
                Log.D("Participants page already displayed.");
                return;
            }
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                page.UpdateDatabase();
            }
            TheFrame.NavigationService.RemoveBackEntry();
            page = new ParticipantsPage(this, database);
            TheFrame.Content = page;
        }

        private void BibsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Bibs button clicked.");
            if (page is BibAssignmentPage)
            {
                Log.D("Bib page already displayed.");
                return;
            }
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                page.UpdateDatabase();
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
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                page.UpdateDatabase();
            }
            TheFrame.NavigationService.RemoveBackEntry();
            page = new ChipAssigmentPage(this, database);
            TheFrame.Content = page;
        }

        private void DivisionsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Divisions button clicked.");
            if (page is DivisionsPage)
            {
                Log.D("Divisions page already displayed.");
            }
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                page.UpdateDatabase();
            }
            TheFrame.NavigationService.RemoveBackEntry();
            page = new DivisionsPage(this, database);
            TheFrame.Content = page;
        }

        private void LocationsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Locations button clicked.");
            if (page is LocationsPage)
            {
                Log.D("Locations page already displayed.");
                return;
            }
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                page.UpdateDatabase();
            }
            TheFrame.NavigationService.RemoveBackEntry();
            page = new LocationsPage(this, database);
            TheFrame.Content = page;
        }

        private void SegmentsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Segments button clicked.");
            if (page is SegmentsPage)
            {
                Log.D("Segments page already displayed.");
                return;
            }
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                page.UpdateDatabase();
            }
            TheFrame.NavigationService.RemoveBackEntry();
            page = new SegmentsPage(this, database);
            TheFrame.Content = page;
        }

        private void AgegroupsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Age Groups button clicked.");
            if (page is AgeGroupsPage)
            {
                Log.D("Age groups page already displayed.");
                return;
            }
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                page.UpdateDatabase();
            }
            TheFrame.NavigationService.RemoveBackEntry();
            page = new AgeGroupsPage(this, database);
            TheFrame.Content = page;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Settings button clicked.");
            if (page is SettingsPage)
            {
                Log.D("Settings page already displayed.");
                return;
            }
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                page.UpdateDatabase();
            }
            TheFrame.NavigationService.RemoveBackEntry();
            page = new SettingsPage(this, database);
            TheFrame.Content = page;
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
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                page.UpdateDatabase();
            }
        }

        public void WindowClosed(Window window)
        {
            Utils.QuitExcel();
        }

        public void UpdateEvent(int identifier, string nameString, long dateVal, int nextYear, int shirtOptionalVal, int shirtPrice)
        {
            Log.D("Updating event information via TCP Server.");
            if (tcpServer != null)
            {
                tcpServer.UpdateEvent(identifier);
            }
            if (StaticEvent.timingWindow != null)
            {
                ((TimingWindow)StaticEvent.timingWindow).UpdateAll();
            }
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
            page.UpdateView();
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

        public void Update()
        {
            page.UpdateView();
        }

        public void UpdateTimingWindow()
        {
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                if (StaticEvent.timingWindow != null)
                {
                    ((TimingWindow)StaticEvent.timingWindow).UpdateAll();
                }
            }));
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
                reportsButton.IsEnabled = false;  // REPORTS DISABLED FOR NOW
                divisionsButton.IsEnabled = true;
                locationsButton.IsEnabled = true;
                segmentsButton.IsEnabled = true;
                agegroupsButton.IsEnabled = true;
            }
        }

        public bool ExcelEnabled()
        {
            return excelEnabled;
        }

        public async void ConnectTimingSystem(TimingSystem system)
        {
            await Task.Run(() =>
            {
                TimingController.ConnectTimingSystem(system);
            });
            UpdateTimingWindow();
            await Task.Run(() =>
            {
                if (!TimingController.IsRunning())
                {
                    TimingControllerThread = new Thread(new ThreadStart(TimingController.Run));
                    TimingControllerThread.Start();
                }
            });
        }

        public async void DisconnectTimingSystem(TimingSystem system)
        {
            await Task.Run(() =>
            {
                TimingController.DisconnectTimingSystem(system);
            });
            UpdateTimingWindow();
        }

        public List<TimingSystem> GetConnectedSystems()
        {
            return TimingController.GetConnectedSystems();
        }

        public void TimingSystemDisconnected(TimingSystem system)
        {
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                MessageBox.Show("Reader at " + system.LocationName + " has unexpectedly disconnected. IP Address was " + system.IPAddress + ".");
                system.Status = SYSTEM_STATUS.DISCONNECTED;
                UpdateTimingWindow();
            }));
        }
    }
}
