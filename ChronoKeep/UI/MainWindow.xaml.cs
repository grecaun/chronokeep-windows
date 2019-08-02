using ChronoKeep.Database;
using ChronoKeep.Interfaces;
using ChronoKeep.Network;
using ChronoKeep.Objects;
using ChronoKeep.Timing;
using ChronoKeep.UI.MainPages;
using ChronoKeep.UI.Timing;
using ChronoKeep.UI.Timing.Import;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ChronoKeep.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainWindow, IChangeUpdater
    {
        IDBInterface database;
        IMainPage page;
        String dbName = "ChronoKeep.sqlite";
        bool closing = false;
        bool excelEnabled = false;

        bool NetworkRunning = false;
        Mutex NetworkRunningBoolMutex = new Mutex();

        // Network objects
        Thread tcpServerThread = null;
        TCPServer tcpServer = null;
        Thread zeroConfThread = null;
        ZeroConf zeroConf = null;
        HttpServer httpServer = null;
        int httpServerPort = 6933;

        // Timing objects.
        Thread TimingControllerThread = null;
        TimingController TimingController = null;
        Thread TimingWorkerThread = null;
        TimingWorker TimingWorker = null;

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
            try
            {
                database.Initialize();
            }
            catch (InvalidDatabaseVersion db)
            {
                MessageBox.Show("Database version greater than the max known by this client. Please update the client.", "fv"+db.FoundVersion+"mv"+db.MaxVersion);
                this.Close();
            }

            UpdateImportOptions();
            Constants.Settings.SetupSettings(database);
            UpdateStatus();

            page = new DashboardPage(this, database);
            TheFrame.Content = page;
        }

        private async void UpdateImportOptions()
        {
            await Task.Run(() =>
            {
                excelEnabled = Utils.ExcelEnabled();
                Utils.QuitExcel();
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
            SwitchPage(new DashboardPage(this, database), true);
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Reports button clicked.");
            if (page is ReportsPage)
            {
                Log.D("Reports page already displayed");
                return;
            }
            //SwitchPage(new ReportsPage(this, database), true);
        }

        private void ParticipantsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Participants button clicked.");
            if (page is ParticipantsPage)
            {
                Log.D("Participants page already displayed.");
                return;
            }
            SwitchPage(new ParticipantsPage(this, database), true);
        }

        private void BibsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Bibs button clicked.");
            if (page is BibAssignmentPage)
            {
                Log.D("Bib page already displayed.");
                return;
            }
            SwitchPage(new BibAssignmentPage(this, database), true);
        }

        private void ChipsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Chips button clicked.");
            if (page is ChipAssigmentPage)
            {
                Log.D("Chips page already displayed.");
                return;
            }
            SwitchPage(new ChipAssigmentPage(this, database), true);
        }

        private void DivisionsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Divisions button clicked.");
            if (page is DivisionsPage)
            {
                Log.D("Divisions page already displayed.");
                return;
            }
            SwitchPage(new DivisionsPage(this, database), true);
        }

        private void LocationsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Locations button clicked.");
            if (page is LocationsPage)
            {
                Log.D("Locations page already displayed.");
                return;
            }
            SwitchPage(new LocationsPage(this, database), true);
        }

        private void SegmentsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Segments button clicked.");
            if (page is SegmentsPage)
            {
                Log.D("Segments page already displayed.");
                return;
            }
            SwitchPage(new SegmentsPage(this, database), true);
        }

        private void AgegroupsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Age Groups button clicked.");
            if (page is AgeGroupsPage)
            {
                Log.D("Age groups page already displayed.");
                return;
            }
            SwitchPage(new AgeGroupsPage(this, database), true);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Settings button clicked.");
            if (page is SettingsPage)
            {
                Log.D("Settings page already displayed.");
                return;
            }
            SwitchPage(new SettingsPage(this, database), true);
        }

        private void TimingButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Timing button clicked.");
            if (page is TimingPage)
            {
                Log.D("Timing page already displayed.");
                ((TimingPage)page).LoadMainDisplay();
                return;
            }
            SwitchPage(new TimingPage(this, database), true);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (database.GetAppSetting(Constants.Settings.EXIT_NO_PROMPT).value == Constants.Settings.SETTING_FALSE &&
                TimingController.IsRunning())
            {
                MessageBoxResult result = MessageBox.Show("Are you sure you wish to exit?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            closing = true;
            StopNetworkServices();
            StopTimingController();
            StopTimingWorker();
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
                    Log.D("Oh well!");
                }
            }
            if (page != null) page.Closing();
        }

        private bool StopTimingWorker()
        {
            try
            {
                Log.D("Stopping Timing Worker.");
                TimingWorker.Shutdown();
                TimingWorker.Notify();
                if (TimingWorkerThread != null) TimingWorkerThread.Join();
            }
            catch
            {
                return false;
            }
            return true;
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
            // *TODO* Update Timing Controller
        }

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
                if (NetworkRunningBoolMutex.WaitOne(3000))
                {
                    NetworkRunning = false;
                    NetworkRunningBoolMutex.ReleaseMutex();
                }
                return false;
            }
            if (NetworkRunningBoolMutex.WaitOne(3000))
            {
                NetworkRunning = true;
                NetworkRunningBoolMutex.ReleaseMutex();
            }
            return true;
        }

        public bool StopNetworkServices()
        {
            try
            {
                Log.D("Stopping TCP server thread.");
                if (tcpServer != null) tcpServer.Stop();
                if (tcpServerThread != null) tcpServerThread.Abort();
                if (tcpServerThread != null) tcpServerThread.Join();
                Log.D("Stopping zero configuration thread.");
                if (zeroConf != null) zeroConf.Stop();
                if (zeroConfThread != null) zeroConfThread.Abort();
                if (zeroConfThread != null) zeroConfThread.Join();
            }
            catch
            {
                if (NetworkRunningBoolMutex.WaitOne(3000))
                {
                    NetworkRunning = false;
                    NetworkRunningBoolMutex.ReleaseMutex();
                }
                return false;
            }
            if (NetworkRunningBoolMutex.WaitOne(3000))
            {
                NetworkRunning = false;
                NetworkRunningBoolMutex.ReleaseMutex();
            }
            return true;
        }

        public bool NetworkServicesRunning()
        {
            bool output = false;
            if (NetworkRunningBoolMutex.WaitOne(3000))
            {
                output = NetworkRunning;
                NetworkRunningBoolMutex.ReleaseMutex();
            }
            return output;
        }

        public void NetworkServicesStopped()
        {
            try
            {
                Log.D("Stopping TCP server thread.");
                if (tcpServer != null) tcpServer.Stop();
                if (tcpServerThread != null) tcpServerThread.Abort();
                if (tcpServerThread != null) tcpServerThread.Join();
                Log.D("Stopping zero configuration thread.");
                if (zeroConf != null) zeroConf.Stop();
                if (zeroConfThread != null) zeroConfThread.Abort();
                if (zeroConfThread != null) zeroConfThread.Join();
            }
            catch
            {
                if (NetworkRunningBoolMutex.WaitOne(3000))
                {
                    NetworkRunning = false;
                    NetworkRunningBoolMutex.ReleaseMutex();
                }
            }
            if (NetworkRunningBoolMutex.WaitOne(3000))
            {
                NetworkRunning = false;
                NetworkRunningBoolMutex.ReleaseMutex();
            }
        }

        public bool StopTimingController()
        {
            try
            {
                Log.D("Stopping Timing Controller.");
                if (TimingController != null) TimingController.Shutdown();
                if (TimingControllerThread != null) TimingControllerThread.Abort();
                if (TimingControllerThread != null) TimingControllerThread.Join();
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
                bibsButton.IsEnabled = false;
                chipsButton.IsEnabled = false;
                reportsButton.IsEnabled = false;
                divisionsButton.IsEnabled = false;
                locationsButton.IsEnabled = false;
                segmentsButton.IsEnabled = false;
                agegroupsButton.IsEnabled = false;
                timingButton.IsEnabled = false;
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
                timingButton.IsEnabled = true;
            }
        }

        public bool ExcelEnabled()
        {
            return excelEnabled;
        }

        public bool NewTimingInfo()
        {
            bool output = (TimingWorker.NewResultsExist() || TimingController.NewReadsExist());
            TimingWorker.ResetNewResults();
            TimingController.ResetNewReads();
            return output;
        }

        public async void ConnectTimingSystem(TimingSystem system)
        {
            await Task.Run(() =>
            {
                TimingController.ConnectTimingSystem(system);
            });
            UpdateTiming();
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
            }));
        }

        public void NotifyTimingWorker()
        {
            Log.D("MainWindow notifying timer.");
            TimingWorker.Notify();
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
            if (iPage is TimingPage)
            {
                timingChildren.Visibility = Visibility.Visible;
            }
            else if (IsMainPage)
            {
                timingChildren.Visibility = Visibility.Collapsed;
            }
            page.Closing();
            page = iPage;
            TheFrame.NavigationService.RemoveBackEntry();
            TheFrame.Content = iPage;
        }

        private void Announcer_Click(object sender, RoutedEventArgs e)
        {

        }

        public void NotifyRecalculateAgeGroups()
        {
            TimingWorker.RecalculateAgeGroups();
            TimingWorker.ResetDictionaries();
        }

        public void DatasetChanged()
        {
            TimingWorker.ResetDictionaries();
        }

        public void NetworkUpdateResults(int eventid, List<TimeResult> results)
        {
            if (tcpServer != null)
            {
                tcpServer.UpdateResults(eventid, results);
            }
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
            if (tcpServer != null)
            {
                tcpServer.ClearResults(eventid);
            }
            if (httpServer != null)
            {
                httpServer.UpdateInformation();
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("About button clicked.");
            if (page is AboutPage)
            {
                Log.D("About page already displayed.");
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
    }
}
