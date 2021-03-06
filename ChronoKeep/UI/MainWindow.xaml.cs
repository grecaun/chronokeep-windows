using Chronokeep.API;
using Chronokeep.Database;
using Chronokeep.Interfaces;
using Chronokeep.Network;
using Chronokeep.Objects;
using Chronokeep.Timing;
using Chronokeep.UI.MainPages;
using Chronokeep.UI.Announcer;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Chronokeep.Timing.Announcer;

namespace Chronokeep.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainWindow
    {
        IDBInterface database;
        IMainPage page;
        string dbName = "Chronokeep.sqlite";

        // Network objects
        HttpServer httpServer = null;
        int httpServerPort = 6933;

        // Timing objects.
        Thread TimingControllerThread = null;
        TimingController TimingController = null;
        Thread TimingWorkerThread = null;
        TimingWorker TimingWorker = null;

        // API objects.
        Thread APIControllerThread = null;
        APIController APIController = null;

        // Announcer objects
        AnnouncerWindow announcerWindow = null;

        List<Window> openWindows = new List<Window>();

        // Set up a mutex that will be unique for this program to ensure we only ever have a single instance of it running.
        static Mutex OneWindow = new Mutex(true,
            "{48ED48DE-6E1B-4F3B-8C5C-D0BAB5295366}-chronokeep");
        bool release = false;

        public MainWindow()
        {
            InitializeComponent();
            // Check that no other instance of this program are running.
            if (!OneWindow.WaitOne(TimeSpan.Zero, true))
            {
                MessageBox.Show("Chronokeep is already running.");
                this.Close();
            }
            release = true;

            string dirPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), Constants.Settings.PROGRAM_DIR);
            string path = System.IO.Path.Combine(dirPath, dbName);
            Log.D("UI.MainWindow", "Looking for database file.");
            if (!Directory.Exists(dirPath))
            {
                Log.D("UI.MainWindow", "Creating directory.");
                Directory.CreateDirectory(dirPath);
            }
            if (!File.Exists(path))
            {
                Log.D("UI.MainWindow", "Creating database file.");
                SQLiteConnection.CreateFile(path);
            }
            database = new SQLiteInterface(path);
            try
            {
                database.Initialize();
            }
            catch (InvalidDatabaseVersion db)
            {
                MessageBox.Show("Database version greater than the max known by this client. Please update the client.", "fv"+db.FoundVersion+"mv"+db.MaxVersion);
                this.Close();
            }
            Constants.Settings.SetupSettings(database);
            UpdateStatus();

            // Setup AgeGroup static variables
            Event theEvent = database.GetCurrentEvent();
            if (theEvent != null && theEvent.Identifier != -1)
            {
                AgeGroup.SetAgeGroups(database.GetAgeGroups(theEvent.Identifier));
            }

            page = new DashboardPage(this, database);
            TheFrame.Content = page;

            // Check for updates.
            if (database.GetAppSetting(Constants.Settings.CHECK_UPDATES).value == Constants.Settings.SETTING_TRUE)
            {
                Updates.Check.Do(this);
            }
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Dashboard button clicked.");
            if (page is DashboardPage)
            {
                Log.D("UI.MainWindow", "Dashboard page already displayed.");
                return;
            }
            SwitchPage(new DashboardPage(this, database), true);
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Reports button clicked.");
            if (page is ReportsPage)
            {
                Log.D("UI.MainWindow", "Reports page already displayed");
                return;
            }
            //SwitchPage(new ReportsPage(this, database), true);
        }

        private void ParticipantsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Participants button clicked.");
            if (page is ParticipantsPage)
            {
                Log.D("UI.MainWindow", "Participants page already displayed.");
                return;
            }
            SwitchPage(new ParticipantsPage(this, database), true);
        }

        private void ChipsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Chips button clicked.");
            if (page is ChipAssigmentPage)
            {
                Log.D("UI.MainWindow", "Chips page already displayed.");
                return;
            }
            SwitchPage(new ChipAssigmentPage(this, database), true);
        }

        private void DistancesButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Distances button clicked.");
            if (page is DistancesPage)
            {
                Log.D("UI.MainWindow", "Distances page already displayed.");
                return;
            }
            SwitchPage(new DistancesPage(this, database), true);
        }

        private void LocationsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Locations button clicked.");
            if (page is LocationsPage)
            {
                Log.D("UI.MainWindow", "Locations page already displayed.");
                return;
            }
            SwitchPage(new LocationsPage(this, database), true);
        }

        private void SegmentsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Segments button clicked.");
            if (page is SegmentsPage)
            {
                Log.D("UI.MainWindow", "Segments page already displayed.");
                return;
            }
            SwitchPage(new SegmentsPage(this, database), true);
        }

        private void AgegroupsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Age Groups button clicked.");
            if (page is AgeGroupsPage)
            {
                Log.D("UI.MainWindow", "Age groups page already displayed.");
                return;
            }
            SwitchPage(new AgeGroupsPage(this, database), true);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Settings button clicked.");
            if (page is SettingsPage)
            {
                Log.D("UI.MainWindow", "Settings page already displayed.");
                return;
            }
            SwitchPage(new SettingsPage(this, database), true);
        }

        private void TimingButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Timing button clicked.");
            if (page is TimingPage)
            {
                Log.D("UI.MainWindow", "Timing page already displayed.");
                ((TimingPage)page).LoadMainDisplay();
                return;
            }
            SwitchPage(new TimingPage(this, database), true);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (database == null)
            {
                return;
            }
            if (database.GetAppSetting(Constants.Settings.EXIT_NO_PROMPT).value == Constants.Settings.SETTING_FALSE &&
                (TimingController.IsRunning() || AnnouncerOpen()))
            {
                MessageBoxResult result = MessageBox.Show("Are you sure you wish to exit?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            try
            {
                StopTimingController();
            }
            catch { }
            try
            {
                StopTimingWorker();
            }
            catch { }
            try
            {
                StopAPIController();
            }
            catch { }
            try
            {
                StopAnnouncer();
            }
            catch { }
            if (httpServer != null)
            {
                httpServer.Stop();
            }
            foreach (Window w in openWindows)
            {
                try
                {
                    w.Close();
                }
                catch
                {
                    Log.D("UI.MainWindow", "Oh well!");
                }
            }
            if (page != null) page.Closing();
            if (release)
            {
                OneWindow.ReleaseMutex();
            }
        }

        private bool StopTimingWorker()
        {
            try
            {
                Log.D("UI.MainWindow", "Stopping Timing Worker.");
                TimingWorker.Shutdown();
                TimingWorker.Notify();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool StopTimingController()
        {
            try
            {
                Log.D("UI.MainWindow", "Stopping Timing Controller.");
                if (TimingController != null) TimingController.Shutdown();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool StopAPIController()
        {
            try
            {
                Log.D("UI.MainWindow", "Stopping API Controller");
                if (APIController != null)
                {
                    APIController.Shutdown();
                }
                APIController = null;
            }
            catch
            {
                return false;
            }
            page.UpdateView();
            return true;
        }

        public async void StartAPIController()
        {
            await Task.Run(() =>
            {
                if (!APIController.IsRunning())
                {
                    APIController = new APIController(this, database);
                    APIControllerThread = new Thread(new ThreadStart(APIController.Run));
                    APIControllerThread.Start();
                }
            });
        }

        public bool IsAPIControllerRunning()
        {
            return APIController == null ? false : APIController.IsRunning();
        }

        public int APIErrors()
        {
            return APIController.Errors;
        }

        public void WindowFinalize(Window w)
        {
            page.UpdateView();
            UpdateStatus();
        }

        public void UpdateTimingFromController()
        {
            TimingWorker.Notify();
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                if (page is TimingPage)
                {
                    page.UpdateView();
                    ((TimingPage)page).NewMessage();
                }
            }));
        }

        public void UpdateAnnouncerWindow()
        {
            // Let the announcer window know that it has new information.
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                if (announcerWindow != null) announcerWindow.UpdateView();
            }));
        }

        public void UpdateTiming()
        {
            if (page is TimingPage)
            {
                page.UpdateView();
            }
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
                chipsButton.IsEnabled = false;
                reportsButton.IsEnabled = false;
                distancesButton.IsEnabled = false;
                locationsButton.IsEnabled = false;
                segmentsButton.IsEnabled = false;
                agegroupsButton.IsEnabled = false;
                timingButton.IsEnabled = false;
                announcer.IsEnabled = false;
            }
            else
            {
                participantsButton.IsEnabled = true;
                chipsButton.IsEnabled = true;
                reportsButton.IsEnabled = false;  // REPORTS DISABLED FOR NOW
                distancesButton.IsEnabled = true;
                locationsButton.IsEnabled = true;
                segmentsButton.IsEnabled = true;
                agegroupsButton.IsEnabled = true;
                timingButton.IsEnabled = true;
                announcer.IsEnabled = true;
            }
        }

        public bool NewTimingInfo()
        {
            bool output = (TimingWorker.NewResultsExist() || TimingController.NewReadsExist());
            return output;
        }

        public async void ConnectTimingSystem(TimingSystem system)
        {
            await Task.Run(() =>
            {
                TimingController.ConnectTimingSystem(system);
            });
            UpdateTiming();
            if (announcerWindow != null) announcerWindow.UpdateView();
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
            UpdateTiming();
            if (announcerWindow != null) announcerWindow.UpdateView();
        }

        public void ShutdownTimingController()
        {
            TimingController.Shutdown();
        }

        public List<TimingSystem> GetConnectedSystems()
        {
            List<TimingSystem> connected = TimingController.GetConnectedSystems();
            List<TimingSystem> saved = database.GetTimingSystems();
            saved.RemoveAll(x => connected.Contains(x));
            saved.InsertRange(0, connected);
            return saved;
        }

        public void TimingSystemDisconnected(TimingSystem system)
        {
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                MessageBox.Show("Reader at " + system.LocationName + " has unexpectedly disconnected. IP Address was " + system.IPAddress + ".");
                system.Status = SYSTEM_STATUS.DISCONNECTED;
                UpdateTiming();
                if (announcerWindow != null) announcerWindow.UpdateView();
            }));
        }

        public void NotifyTimingWorker()
        {
            Log.D("UI.MainWindow", "MainWindow notifying timer.");
            TimingWorker.ResetDictionaries();
            TimingWorker.Notify();
            // Let the AnnouncerWorker know there are new reads (potentially).
            AnnouncerWorker.Notify();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TimingController = new TimingController(this, database);
            TimingWorker = TimingWorker.NewWorker(this, database);
            TimingWorkerThread = new Thread(new ThreadStart(TimingWorker.Run));
            TimingWorkerThread.Start();
            TimingWorker.Notify();
        }

        public void SwitchPage(IMainPage iPage, bool IsMainPage)
        {
            page.Closing();
            page = iPage;
            TheFrame.NavigationService.RemoveBackEntry();
            TheFrame.Content = iPage;
        }

        private void Announcer_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Announer window button clicked.");
            if (announcerWindow != null)
            {
                return;
            }
            announcerWindow = new AnnouncerWindow(this, database);
            announcerWindow.Show();
        }

        public void NetworkUpdateResults(int eventid, List<TimeResult> results)
        {
            if (httpServer != null)
            {
                httpServer.UpdateInformation();
            }
        }

        public void NetworkAddResults(int eventid, List<TimeResult> results)
        {
        }

        public void NetworkClearResults(int eventid)
        {
            if (httpServer != null)
            {
                httpServer.UpdateInformation();
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "About button clicked.");
            if (page is AboutPage)
            {
                Log.D("UI.MainWindow", "About page already displayed.");
                return;
            }
            SwitchPage(new AboutPage(this), true);
        }

        public void StartHttpServer()
        {
            if (httpServer != null)
            {
                httpServer.Stop();
                httpServer = null;
            }
            httpServer = new HttpServer(database, httpServerPort);
        }

        public void StopHttpServer()
        {
            if (httpServer != null)
            {
                httpServer.Stop();
            }
            httpServer = null;
        }

        public bool HttpServerActive()
        {
            return httpServer != null;
        }

        public bool AnnouncerConnected()
        {
            foreach (TimingSystem system in TimingController.GetConnectedSystems())
            {
                if (system.LocationID == Constants.Timing.LOCATION_ANNOUNCER)
                {
                    return true;
                }
            }
            return false;
        }

        public void AnnouncerClosing()
        {
            if (announcerWindow != null)
            {
                announcerWindow = null;
                Log.D("UI.MainWindow", "Announcer Window has closed.");
            }
            else
            {
                Log.D("UI.MainWindow", "Announcer Window was supposed to close but did not.");
            }
        }

        public bool AnnouncerOpen()
        {
            return announcerWindow != null;
        }

        public void StopAnnouncer()
        {
            if (announcerWindow != null) announcerWindow.Close();
        }

        public void Exit()
        {
            Close();
        }
    }
}
