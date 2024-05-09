using Chronokeep.Database;
using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.Timing;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Chronokeep.UI.UIObjects;
using Chronokeep.Objects.ChronokeepRemote;
using Chronokeep.Objects.ChronokeepPortal;
using Chronokeep.UI.Timing;
using Chronokeep.UI.MainPages;

namespace Chronokeep.UI
{
    /// <summary>
    /// Interaction logic for MinWindow.xaml
    /// </summary>
    public partial class MinWindow : IMainWindow
    {
        IDBInterface database;
        IMainPage page;
        string dbName = "Chronokeep.sqlite";

        // Timing objects.
        Thread TimingControllerThread = null;
        TimingController TimingController = null;

        List<Window> openWindows = new List<Window>();

        // Set up a mutex that will be unique for this program to ensure we only ever have a single instance of it running.
        static Mutex OneWindow = new Mutex(true, "{48ED48DE-6E1B-4F3B-8C5C-D0BAB5295366}-chronokeep");

        public MinWindow()
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
            database = new SQLiteInterface(path);
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

            TimingController = new TimingController(this, database);

            page = new MinTimingPage(this, database);
            TheFrame.Content = page;

            DataContext = this;
        }

        private void NewEvent_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.DashboardPage", "New event clicked.");
            if (CancelEventChangeAsync(EventClickType.NewEvent))
            {
                return;
            }
            NewEventWindow newEventWindow = NewEventWindow.NewWindow(this, database);
            if (newEventWindow != null)
            {
                this.AddWindow(newEventWindow);
                newEventWindow.ShowDialog();
            }
        }

        private void ChangeEvent_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.DashboardPage", "Change event clicked.");
            if (CancelEventChangeAsync(EventClickType.ChangeEvent))
            {
                return;
            }
            ChangeEventWindow changeEventWindow = ChangeEventWindow.NewWindow(this, database);
            if (changeEventWindow != null)
            {
                this.AddWindow(changeEventWindow);
                changeEventWindow.ShowDialog();
            }
        }

        private bool CancelEventChangeAsync(EventClickType clickType)
        {
            Log.D("UI.DashboardPage", "Checking if we need to cancel the change.");
            if (BackgroundProcessesRunning())
            {
                DialogBox.Show(
                    "There are processes running in the background. Do you wish to stop these and continue?",
                    "Yes",
                    "No",
                    () =>
                    {
                        StopBackgroundProcesses();
                        switch (clickType)
                        {
                            case EventClickType.NewEvent:
                                NewEventWindow newEventWindow = NewEventWindow.NewWindow(this, database);
                                if (newEventWindow != null)
                                {
                                    this.AddWindow(newEventWindow);
                                    newEventWindow.ShowDialog();
                                }
                                break;
                            case EventClickType.ChangeEvent:
                                ChangeEventWindow changeEventWindow = ChangeEventWindow.NewWindow(this, database);
                                if (changeEventWindow != null)
                                {
                                    this.AddWindow(changeEventWindow);
                                    changeEventWindow.ShowDialog();
                                }
                                break;
                        }
                    }
                    );
                return true;
            }
            return false;
        }

        private enum EventClickType
        {
            NewEvent,
            ImportEvent,
            ChangeEvent,
            DeleteEvent,
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

        public void WindowFinalize(Window w)
        {
            page.UpdateView();
        }

        public void AddWindow(Window w)
        {
            openWindows.Add(w);
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
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (!system.SystemInterface.WasShutdown())
                {
                    DialogBox.Show(string.Format("Reader at {0} has unexpectedly disconnected. IP Address was {1}.", system.LocationName, system.IPAddress));
                }
                system.Status = SYSTEM_STATUS.DISCONNECTED;
                UpdateTiming();
            }));
        }

        public void NotifyTimingWorker() { }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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

        public void Exit()
        {
            Close();
        }

        public bool BackgroundProcessesRunning()
        {
            return TimingController.IsRunning();
        }

        public void StopBackgroundProcesses()
        {
            try
            {
                StopTimingController();
            }
            catch { }
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
                    DialogBox.Show(PortalNotification.GetRemoteNotificationMessage(ReaderName, notification.Type));
                }
            }));
        }

        public void SwitchPage(IMainPage iPage) { }

        public void NetworkUpdateResults() { }

        public void NetworkClearResults() { }

        public void StartHttpServer() { }

        public void StopHttpServer() { }

        public bool HttpServerActive() { return false; }

        public void UpdateStatus() { }

        public void UpdateTimingFromController()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (page is MinTimingPage)
                {
                    page.UpdateView();
                    ((MinTimingPage)page).NewMessage();
                }
            }));
        }

        public void UpdateTiming()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (page is MinTimingPage)
                {
                    page.UpdateView();
                    ((MinTimingPage)page).NewMessage();
                }
            }));
        }

        public void UpdateAnnouncerWindow() { }

        public void UpdateRegistrationDistances() { }

        public void UpdateParticipantsFromRegistration() { }

        public bool InDidNotStartMode() { return false; }

        public bool StartDidNotStartMode() { return false; }

        public bool StopDidNotStartMode() { return false; }

        public void NotifyAlarm(string Bib, string Chip) { }

        public bool AnnouncerConnected() { return false; }

        public void AnnouncerClosing() { }

        public bool AnnouncerOpen() { return false; }

        public void StopAnnouncer() { }

        public void StartAPIController() { }

        public bool StopAPIController() { return false; }

        public bool IsAPIControllerRunning() { return false; }

        public int APIErrors() { return 0; }

        public void StartRemote() { }

        public bool StopRemote() { return false; }

        public bool IsRemoteRunning() { return false; }

        public int RemoteErrors() { return 0; }

        public bool StartRegistration() { return false; }

        public bool StopRegistration() { return false; }

        public bool IsRegistrationRunning() { return false; }
    }
}
