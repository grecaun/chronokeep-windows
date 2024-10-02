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
using System.Windows.Threading;
using Chronokeep.UI.UIObjects;
using Chronokeep.Helpers;
using System.Media;
using Chronokeep.Timing.API;
using Chronokeep.Timing.Remote;
using Chronokeep.Objects.ChronokeepRemote;
using Chronokeep.Objects.ChronokeepPortal;
using Chronokeep.Network.Registration;

namespace Chronokeep.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IMainWindow
    {
        IDBInterface database;
        IMainPage page;
        string dbName = "Chronokeep.sqlite";

        // Network objects
        HttpServer httpServer = null;
        int httpServerPort = 6933;

        // Zero Conf/Registration objects.
        Thread ZConfThread = null;
        ZeroConf ZConfServer = null;
        Thread RegistrationThread = null;
        RegistrationWorker RegistrationWorker = null;

        // Timing objects.
        Thread TimingControllerThread = null;
        TimingController TimingController = null;
        Thread TimingWorkerThread = null;
        TimingWorker TimingWorker = null;

        // API objects.
        Thread APIControllerThread = null;
        APIController APIController = null;

        // Remote Reads objects
        Thread RemoteThread = null;
        RemoteReadsController RemoteController = null;

        // Announcer objects
        AnnouncerWindow announcerWindow = null;

        List<Window> openWindows = new List<Window>();

        // Setting to allow the user to enter a mode where we can record DNS chips.
        private bool didNotStartMode = false;
        private Mutex dnsMutex = new Mutex();

        // Setup a timer for updating the view
        DispatcherTimer TimingUpdater = new DispatcherTimer();

        // Set up a mutex that will be unique for this program to ensure we only ever have a single instance of it running.
        static Mutex OneWindow = new Mutex(true, "{48ED48DE-6E1B-4F3B-8C5C-D0BAB5295366}-chronokeep");

        public MainWindow()
        {
            InitializeComponent();

            // Check that no other instance of this program are running.
            if (!OneWindow.WaitOne(TimeSpan.Zero, true))
            {
                DialogBox.Show("Chronokeep is already running.");
                this.Close();
            }
            OneWindow.ReleaseMutex();

            string dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), Constants.Settings.PROGRAM_DIR);
            string path = Path.Combine(dirPath, dbName);
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
            database = MemStore.MemStore.GetMemStore(new SQLiteInterface(path));
            try
            {
                database.Initialize();
            }
            catch (InvalidDatabaseVersion db)
            {
                DialogBox.Show(string.Format("Database version greater than the max known by this client. Please update the client. Database version {0}. Max version for this client {1}", db.FoundVersion, db.MaxVersion));
                this.Close();
                return;
            }
            Constants.Settings.SetupSettings(database);

            // Ensure Global values are set up.
            Globals.SetupValues(database);

            // Setup AgeGroup static variables
            Event theEvent = database.GetCurrentEvent();

            page = new DashboardPage(this, database);
            TheFrame.Content = page;
            UpdateStatus();

            // Check for updates.
            if (database.GetAppSetting(Constants.Settings.CHECK_UPDATES).Value == Constants.Settings.SETTING_TRUE)
            {
                Updates.Check.Do(this);
            }

            DataContext = this;

            // Set timing update to every half second.
            TimingUpdater.Tick += new EventHandler(UpdateTimingTick);
            TimingUpdater.Interval = new TimeSpan(0, 0, 0, 0, 500);
            TimingUpdater.Start();

            // Set the global upload interval.
            if (!int.TryParse(database.GetAppSetting(Constants.Settings.UPLOAD_INTERVAL).Value, out Globals.UploadInterval))
            {
                DialogBox.Show("Something went wrong trying to update the upload interval.");
            }

            // Set the global download interval.
            if (!int.TryParse(database.GetAppSetting(Constants.Settings.DOWNLOAD_INTERVAL).Value, out Globals.DownloadInterval))
            {
                DialogBox.Show("Something went wrong trying to update the download interval.");
            }

            // Pull alarms from the database.
            if (theEvent != null && theEvent.Identifier != -1)
            {
                Alarm.AddAlarms(database.GetAlarms(theEvent.Identifier));
            }

            // Setup global twilio account credentials.
            Constants.Globals.SetTwilioCredentials(database);
        }


        public void UpdateTheme(Wpf.Ui.Appearance.ApplicationTheme theme, bool system)
        {
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(theme);
            if (system)
            {
                Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this);
            }
            else
            {
                Wpf.Ui.Appearance.SystemThemeWatcher.UnWatch(this);
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
            SwitchPage(new DashboardPage(this, database));
        }

        private void ParticipantsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Participants button clicked.");
            if (page is ParticipantsPage)
            {
                Log.D("UI.MainWindow", "Participants page already displayed.");
                return;
            }
            SwitchPage(new ParticipantsPage(this, database));
        }

        private void ChipsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Chips button clicked.");
            if (page is ChipAssigmentPage)
            {
                Log.D("UI.MainWindow", "Chips page already displayed.");
                return;
            }
            SwitchPage(new ChipAssigmentPage(this, database));
        }

        private void DistancesButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Distances button clicked.");
            if (page is DistancesPage)
            {
                Log.D("UI.MainWindow", "Distances page already displayed.");
                return;
            }
            SwitchPage(new DistancesPage(this, database));
        }

        private void LocationsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Locations button clicked.");
            if (page is LocationsPage)
            {
                Log.D("UI.MainWindow", "Locations page already displayed.");
                return;
            }
            SwitchPage(new LocationsPage(this, database));
        }

        private void SegmentsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Segments button clicked.");
            if (page is SegmentsPage)
            {
                Log.D("UI.MainWindow", "Segments page already displayed.");
                return;
            }
            SwitchPage(new SegmentsPage(this, database));
        }

        private void AgegroupsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Age Groups button clicked.");
            if (page is AgeGroupsPage)
            {
                Log.D("UI.MainWindow", "Age groups page already displayed.");
                return;
            }
            SwitchPage(new AgeGroupsPage(this, database));
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Settings button clicked.");
            if (page is SettingsPage)
            {
                Log.D("UI.MainWindow", "Settings page already displayed.");
                return;
            }
            SwitchPage(new SettingsPage(this, database));
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
            SwitchPage(new TimingPage(this, database));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (database == null)
            {
                return;
            }
            if (database.GetAppSetting(Constants.Settings.EXIT_NO_PROMPT).Value == Constants.Settings.SETTING_FALSE &&
                (BackgroundProcessesRunning()))
            {
                bool AllowClose = false;
                DialogBox.Show(
                    "Are you sure you wish to exit?",
                    "Yes",
                    "No",
                    () =>
                    {
                        AllowClose = true;
                    }
                    );
                if (!AllowClose)
                {
                    e.Cancel = true;
                    return;
                }
            }
            Log.D("UI.MainWindow", "Window is closing!");
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
            try
            {
                StopRegistration();
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
            TimingUpdater.Stop();
        }

        public bool IsRegistrationRunning()
        {
            return (RegistrationWorker != null && RegistrationWorker.IsRunning()) && (ZConfServer != null && ZConfServer.IsRunning());
        }

        public bool StopRegistration()
        {
            bool output = true;
            try
            {
                Log.D("UI.MainWindow", "Stopping zero conf.");
                if (ZConfServer != null)
                {
                    ZConfServer.Stop();
                }
            }
            catch
            {
                output = false;
            }
            try
            {
                Log.D("UI.MainWindow", "Stopping registration.");
                if (RegistrationWorker != null)
                {
                    RegistrationWorker.Stop();
                }
            }
            catch
            {
                output = false;
            }
            return output;
        }

        public bool StartRegistration()
        {
            bool output = true;
            try
            {
                Log.D("UI.MainWindow", "Starting zero conf.");
                AppSetting zconfName = database.GetAppSetting(Constants.Settings.SERVER_NAME);
                ZConfServer = new ZeroConf(zconfName != null && zconfName.Value != null ? zconfName.Value : null);
                ZConfThread = new Thread(new ThreadStart(ZConfServer.Run));
                ZConfThread.Start();
            }
            catch
            {
                output = false;
            }
            try
            {
                Log.D("UI.MainWindow", "Starting registration.");
                RegistrationWorker = new RegistrationWorker(database, this);
                RegistrationThread = new Thread(new ThreadStart(RegistrationWorker.Run));
                RegistrationThread.Start();
            }
            catch
            {
                output = false;
            }
            return output;
        }
        public void UpdateRegistrationDistances()
        {
            if (RegistrationWorker != null)
            {
                RegistrationWorker.UpdateDistances();
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
            return APIController != null ? APIController.Errors : 0;
        }

        public void WindowFinalize(Window w)
        {
            page.UpdateView();
            UpdateStatus();
        }

        public void UpdateParticipantsFromRegistration()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (page is ParticipantsPage)
                {
                    page.UpdateView();
                }
            }));
        }

        public void UpdateTimingFromController()
        {
            TimingWorker.Notify();
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (page is TimingPage)
                {
                    page.UpdateView();
                    ((TimingPage)page).NewMessage();
                }
            }));
        }

        public async void StartRemote()
        {
            await Task.Run(() =>
            {
                Log.D("UI.MainWindow", "Checking Remote Thread");
                if (!RemoteReadsController.IsRunning())
                {
                    Log.D("UI.MainWindow", "Starting Remote Thread");
                    RemoteController = new RemoteReadsController(this, database);
                    RemoteThread = new Thread(new ThreadStart(RemoteController.Run));
                    RemoteThread.Start();
                }
            });
        }

        public bool StopRemote()
        {
            try
            {
                Log.D("UI.MainWindow", "Stopping Remote Controller");
                if (RemoteController != null)
                {
                    RemoteController.Shutdown();
                }
                RemoteController = null;
            }
            catch
            {
                return false;
            }
            page.UpdateView();
            return true;
        }

        public bool IsRemoteRunning()
        {
            return RemoteReadsController.IsRunning();
        }

        public int RemoteErrors()
        {
            return RemoteController != null ? RemoteController.Errors : 0;
        }

        public void UpdateAnnouncerWindow()
        {
            // Let the announcer window know that it has new information.
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                announcerWindow?.UpdateView();
            }));
        }

        public void UpdateTiming()
        {
            // Let the announcer window know that it has new information.
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (page is TimingPage)
                {
                    page.UpdateView();
                }
            }));
        }

        public void UpdateTimingTick(object sender, EventArgs e)
        {
            if (TimingWorker.NewResultsExist())
            {
                if (page is TimingPage)
                {
                    page.UpdateView();
                }
                announcerWindow?.UpdateTiming();
            }
        }

        public void AddWindow(Window w)
        {
            openWindows.Add(w);
        }

        public void UpdateStatus()
        {
            Event theEvent = database.GetCurrentEvent();
            Alarm.ClearAlarms();
            if (theEvent == null || theEvent.Identifier == -1)
            {
                participantsButton.IsEnabled = false;
                chipsButton.IsEnabled = false;
                distancesButton.IsEnabled = false;
                locationsButton.IsEnabled = false;
                segmentsButton.IsEnabled = false;
                agegroupsButton.IsEnabled = false;
                timingButton.IsEnabled = false;
                announcerButton.IsEnabled = false;

                participantsButton.Opacity = 0.2;
                chipsButton.Opacity = 0.2;
                distancesButton.Opacity = 0.2;
                locationsButton.Opacity = 0.2;
                segmentsButton.Opacity = 0.2;
                agegroupsButton.Opacity = 0.2;
                timingButton.Opacity = 0.2;
                announcerButton.Opacity = 0.2;
            }
            else
            {
                participantsButton.IsEnabled = true;
                chipsButton.IsEnabled = true;
                distancesButton.IsEnabled = true;
                locationsButton.IsEnabled = true;
                segmentsButton.IsEnabled = true;
                agegroupsButton.IsEnabled = true;
                timingButton.IsEnabled = true;
                announcerButton.IsEnabled = true;

                participantsButton.Opacity = 1.0;
                chipsButton.Opacity = 1.0;
                distancesButton.Opacity = 1.0;
                locationsButton.Opacity = 1.0;
                segmentsButton.Opacity = 1.0;
                agegroupsButton.Opacity = 1.0;
                timingButton.Opacity = 1.0;
                announcerButton.Opacity = 1.0;

                // Pull alarms from the database.
                Alarm.AddAlarms(database.GetAlarms(theEvent.Identifier));
            }
            if (OperatingSystem.IsWindowsVersionAtLeast(8))
            {
                dashboardButton.IsActive = page.GetType() == typeof(DashboardPage);
                timingButton.IsActive = page.GetType() == typeof(TimingPage);
                announcerButton.IsActive = announcerWindow != null;
                participantsButton.IsActive = page.GetType() == typeof(ParticipantsPage);
                chipsButton.IsActive = page.GetType() == typeof(ChipAssigmentPage);
                locationsButton.IsActive = page.GetType() == typeof(LocationsPage);
                distancesButton.IsActive = page.GetType() == typeof(DistancesPage);
                segmentsButton.IsActive = page.GetType() == typeof(SegmentsPage);
                agegroupsButton.IsActive = page.GetType() == typeof(AgeGroupsPage);
                settingsButton.IsActive = page.GetType() == typeof(SettingsPage);
                aboutButton.IsActive = page.GetType() == typeof(AboutPage);
            }
        }

        public async void ConnectTimingSystem(TimingSystem system)
        {
            await Task.Run(() =>
            {
                TimingController.ConnectTimingSystem(system);
            });
            UpdateTiming();
            announcerWindow?.UpdateView();
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
            announcerWindow?.UpdateView();
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
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (!system.SystemInterface.WasShutdown())
                {
                    DialogBox.Show(string.Format("Reader at {0} has unexpectedly disconnected. IP Address was {1}.", system.LocationName, system.IPAddress));
                }
                system.Status = SYSTEM_STATUS.DISCONNECTED;
                UpdateTiming();
                announcerWindow?.UpdateView();
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
            // Check for current theme color and apply it.
            AppSetting themeColor = database.GetAppSetting(Constants.Settings.CURRENT_THEME);
            if (OperatingSystem.IsWindowsVersionAtLeast(7))
            {
                Wpf.Ui.Appearance.ApplicationTheme theme = Wpf.Ui.Appearance.ApplicationTheme.Light;
                bool system = themeColor.Value == Constants.Settings.THEME_SYSTEM;
                if ((themeColor.Value == Constants.Settings.THEME_SYSTEM && Utils.GetSystemTheme() == 0) || themeColor.Value == Constants.Settings.THEME_DARK)
                {
                    theme = Wpf.Ui.Appearance.ApplicationTheme.Dark;
                }
                UpdateTheme(theme, system);
            }
        }

        public void SwitchPage(IMainPage iPage)
        {
            page.Closing();
            page = iPage;
            TheFrame.NavigationService.RemoveBackEntry();
            TheFrame.Content = iPage;
            UpdateStatus();
        }

        private void Announcer_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainWindow", "Announer window button clicked.");
            if (announcerWindow != null)
            {
                announcerWindow.Focus();
                return;
            }
            announcerWindow = new AnnouncerWindow(this, database);
            announcerWindow.Show();
            UpdateStatus();
        }

        public void NetworkUpdateResults()
        {
            if (httpServer != null)
            {
                httpServer.UpdateInformation();
            }
        }

        public void NetworkAddResults()
        {
        }

        public void NetworkClearResults()
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
            SwitchPage(new AboutPage(this));
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
                UpdateStatus();
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

        public bool InDidNotStartMode()
        {
            bool output = false;
            if (dnsMutex.WaitOne(3000))
            {
                output = didNotStartMode;
                dnsMutex.ReleaseMutex();
            }
            else
            {
                Log.D("UI.MainWindow", "Error getting DNSMutex.");
            }
            return output;
        }

        public bool BackgroundProcessesRunning()
        {
            return TimingController.IsRunning() || AnnouncerOpen() || IsRegistrationRunning() || IsAPIControllerRunning() || IsRemoteRunning();
        }

        public void StopBackgroundProcesses()
        {
            try
            {
                StopTimingController();
            }
            catch { }
            try
            {
                StopAnnouncer();
            }
            catch { }
            try
            {
                StopRegistration();
            }
            catch { }
            try
            {
                StopAPIController();
            }
            catch { }
            try
            {
                StopRemote();
            }
            catch { }
        }

        public bool StartDidNotStartMode()
        {
            if (dnsMutex.WaitOne(3000))
            {
                didNotStartMode = true;
                dnsMutex.ReleaseMutex();
                return true;
            }
            return false;
        }

        public bool StopDidNotStartMode()
        {
            if (dnsMutex.WaitOne(3000))
            {
                didNotStartMode = false;
                dnsMutex.ReleaseMutex();
                return true;
            }
            return false;
        }

        public void NotifyAlarm(string Bib, string Chip)
        {
            Event theEvent = database.GetCurrentEvent();
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                Alarm alarm = null;
                if (Bib.Length > 0)
                {
                    alarm = Alarm.GetAlarmByBib(Bib);
                }
                else if (Chip.Length > 0)
                {
                    alarm = Alarm.GetAlarmByChip(Chip);
                }
                if (alarm != null && alarm.Enabled)
                {
                    alarm.Enabled = false;
                    Alarm.SaveAlarm(theEvent.Identifier, database, alarm);
                    string soundFile = Environment.CurrentDirectory;
                    int sound = alarm.AlarmSound;
                    // Any value not between 1-5 (inclusive both) is defined to be the default sound.
                    if (sound < 1 || sound > 5)
                    {
                        // If for some reason we can't parse the value into integer, set it to 1.
                        if (!int.TryParse(database.GetAppSetting(Constants.Settings.ALARM_SOUND).Value, out sound))
                        {
                            sound = 1;
                        }
                    }
                    switch (sound)
                    {
                        case 2:
                            soundFile += "\\Sounds\\alert-2.wav";
                            break;
                        case 3:
                            soundFile += "\\Sounds\\alert-3.wav";
                            break;
                        case 4:
                            soundFile += "\\Sounds\\alert-4.wav";
                            break;
                        case 5:
                            soundFile += "\\Sounds\\alert-5.wav";
                            break;
                        case 6:
                            soundFile += "\\Sounds\\emily-runner-here.wav";
                            break;
                        case 7:
                            soundFile += "\\Sounds\\emily-runner-arrived.wav";
                            break;
                        case 8:
                            soundFile += "\\Sounds\\emily-alert-runner-here.wav";
                            break;
                        case 9:
                            soundFile += "\\Sounds\\michael-runner-here.wav";
                            break;
                        case 10:
                            soundFile += "\\Sounds\\michael-runner-arrived.wav";
                            break;
                        case 11:
                            soundFile += "\\Sounds\\michael-alert-runner-here.wav";
                            break;
                        default:
                            soundFile += "\\Sounds\\alert-1.wav";
                            break;
                    }
                    new SoundPlayer(soundFile).Play();
                }
                if (page is TimingPage)
                {
                    ((TimingPage)page).UpdateAlarms();
                }
            }));
        }

        public void ShowNotificationDialog(string ReaderName, RemoteNotification notification)
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (notification.Type == PortalError.NOT_ALLOWED)
                {
                    DialogBox.Show("Unable to set time with a reader connected.");
                }
                else
                {
                    Globals.AddReaderMessage(PortalNotification.GetRemoteNotificationMessage(ReaderName, notification.Type));
                }
            }));
        }
    }
}
