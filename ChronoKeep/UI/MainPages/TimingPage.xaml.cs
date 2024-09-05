using Chronokeep.Constants;
using Chronokeep.Interfaces;
using Chronokeep.IO;
using Chronokeep.IO.HtmlTemplates;
using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.API;
using Chronokeep.Objects.Notifications;
using Chronokeep.Timing.API;
using Chronokeep.UI.API;
using Chronokeep.UI.Export;
using Chronokeep.UI.IO;
using Chronokeep.UI.Timing;
using Chronokeep.UI.Timing.Import;
using Chronokeep.UI.Timing.Notifications;
using Chronokeep.UI.UIObjects;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Chronokeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for TimingPage.xaml
    /// </summary>
    public partial class TimingPage : IMainPage, ITimingPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private ISubPage subPage;

        private CancellationTokenSource cts;

        private Event theEvent;
        List<TimingLocation> locations;

        private DateTime startTime;
        DispatcherTimer Timer = new DispatcherTimer();
        private bool TimerStarted = false;
        private SetTimeWindow timeWindow = null;
        private RewindWindow rewindWindow = null;

        ObservableCollection<DistanceStat> stats = new ObservableCollection<DistanceStat>();

        int total = 4, connected = 0;

        private const string ipformat = "{0:D}.{1:D}.{2:D}.{3:D}";
        private int[] baseIP = { 0, 0, 0, 0 };

        private bool remote_api = false;

        Dictionary<int, (long seconds, int milliseconds)> waveTimes = new Dictionary<int, (long, int)>();
        HashSet<int> waves = new HashSet<int>();
        int selectedWave = -1;

        public TimingPage(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.database = database;
            this.mWindow = window;
            theEvent = database.GetCurrentEvent();

            if (theEvent == null || theEvent.Identifier == -1)
            {
                return;
            }

            // Setup the running clock.
            Timer.Tick += new EventHandler(Timer_Click);
            Timer.Interval = new TimeSpan(0, 0, 0, 0, 100);

            IPAdd.Text = "localhost";
            Port.Text = "6933";
            // Check for default IP address to give to our reader boxes for connections
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet && adapter.OperationalStatus == OperationalStatus.Up)
                {
                    if (adapter.GetIPProperties().GatewayAddresses.FirstOrDefault() != null)
                    {
                        foreach (UnicastIPAddressInformation ipinfo in adapter.GetIPProperties().UnicastAddresses)
                        {
                            if (ipinfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                IPAdd.Text = ipinfo.Address.ToString();
                                Log.D("UI.MainPages.TimingPage", "IP Address :" + ipinfo.Address);
                                Log.D("UI.MainPages.TimingPage", "IPv4 Mask  :" + ipinfo.IPv4Mask);
                                string[] ipParts = ipinfo.Address.ToString().Split('.');
                                string[] maskParts = ipinfo.IPv4Mask.ToString().Split('.');
                                if (ipParts.Length == 4 && maskParts.Length == 4)
                                {
                                    for (int i = 0; i < 4; i++)
                                    {
                                        int ip, mask;
                                        try
                                        {
                                            ip = Convert.ToInt32(ipParts[i]);
                                            mask = Convert.ToInt32(maskParts[i]);
                                        }
                                        catch
                                        {
                                            ip = 0;
                                            mask = 0;
                                        }
                                        baseIP[i] = ip & mask;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Check if we've already started the event.  Show a clock if we have.
            if (theEvent != null && theEvent.StartSeconds >= 0)
            {
                StartTime.Text = Constants.Timing.ToTimeOfDay(theEvent.StartSeconds, theEvent.StartMilliseconds);
                UpdateStartTime();
            }

            // Check for multiple wave times, show an ellapsed relative to box if so
            waves.Clear();
            waveTimes.Clear();
            EllapsedRelativeToBox.Items.Clear();
            EllapsedRelativeToBox.Items.Add(new ComboBoxItem
            {
                Content = "Start Time",
                Uid = "-1"
            });
            EllapsedRelativeToBox.SelectedIndex = 0;
            foreach (Distance div in database.GetDistances(theEvent.Identifier))
            {
                EllapsedRelativeToBox.Items.Add(new ComboBoxItem
                {
                    Content = div.Name + " (Wave " + div.Wave + ")",
                    Uid = div.Wave.ToString()
                });
                waveTimes[div.Wave] = (div.StartOffsetSeconds, div.StartOffsetMilliseconds);
                waves.Add(div.Wave);
            }
            if (waves.Count > 1)
            {
                EllapsedRelativeToBox.Visibility = Visibility.Visible;
            }
            else
            {
                EllapsedRelativeToBox.Visibility = Visibility.Collapsed;
            }

            // Populate the list of readers with connected readers (or at least 4 readers)
            ReadersBox.Items.Clear();
            locations = database.GetTimingLocations(theEvent.Identifier);
            if (!theEvent.CommonStartFinish)
            {
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_ANNOUNCER, theEvent.Identifier, "Announcer", 0, 0));
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow));
            }
            else
            {
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_ANNOUNCER, theEvent.Identifier, "Announcer", 0, 0));
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
            }
            List<TimingSystem> systems = mWindow.GetConnectedSystems();
            int numSystems = systems.Count;
            string system = Constants.Readers.DEFAULT_TIMING_SYSTEM;
            try
            {
                system = database.GetAppSetting(Constants.Settings.DEFAULT_TIMING_SYSTEM).Value;
            }
            catch
            {
                Log.D("UI.MainPages.TimingPage", "Error fetching default timing system information.");
                system = Constants.Readers.DEFAULT_TIMING_SYSTEM;
            }
            if (numSystems < 3)
            {
                Log.D("UI.MainPages.TimingPage", systems.Count + " systems found.");
                for (int i = 0; i < 3 - numSystems; i++)
                {
                    systems.Add(new TimingSystem(string.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]), system));
                }
            }
            systems.Add(new TimingSystem(string.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]), system));
            connected = 0;
            foreach (TimingSystem sys in systems)
            {
                ReadersBox.Items.Add(new AReaderBox(this, sys, locations));
                if (sys.Status == SYSTEM_STATUS.CONNECTED || sys.Status == SYSTEM_STATUS.WORKING)
                {
                    connected++;
                }
            }
            total = ReadersBox.Items.Count;
            subPage = new TimingResultsPage(this, database);
            TimingFrame.Content = subPage;
            List<DistanceStat> inStats = database.GetDistanceStats(theEvent.Identifier);
            statsListView.ItemsSource = stats;
            stats.Clear();
            foreach (DistanceStat s in inStats)
            {
                stats.Add(s);
            }

            if (Constants.Timing.EVENT_TYPE_TIME == theEvent.EventType)
            {
                DNFButton.Content = "Add Finished";
            }

            // Check if our web server is active and update the button
            if (mWindow.HttpServerActive())
            {
                HttpServerButton.Content = "Stop Web";
                IPContainer.Visibility = Visibility.Visible;
                PortContainer.Visibility = Visibility.Visible;
            }
            else
            {
                HttpServerButton.Content = "Start Web";
                IPContainer.Visibility = Visibility.Collapsed;
                PortContainer.Visibility = Visibility.Collapsed;
            }
            if (theEvent.API_ID > 0 && theEvent.API_Event_ID.Length > 1)
            {
                apiPanel.Visibility = Visibility.Visible;
            }
            else
            {
                apiPanel.Visibility = Visibility.Collapsed;
            }
            if (mWindow.IsAPIControllerRunning())
            {
                AutoAPIButton.Content = "Stop Uploads";
                ManualAPIButton.IsEnabled = false;
            }
            else
            {
                AutoAPIButton.Content = "Auto Upload";
                ManualAPIButton.IsEnabled = true;
            }

            if (mWindow.IsRemoteRunning())
            {
                remoteControllerSwitch.IsChecked = true;
                remoteErrorsBlock.Text = mWindow.RemoteErrors() > 0 ? mWindow.RemoteErrors().ToString() : "";
            }
            else
            {
                remoteControllerSwitch.IsChecked = false;
                remoteErrorsBlock.Text = "";
            }
            remoteControllerSwitch.IsEnabled = true;

            UpdateDNSButton();

            // check if we have a remote api set up
            foreach (APIObject api in database.GetAllAPI())
            {
                if (api.Type == Constants.APIConstants.CHRONOKEEP_REMOTE_SELF || api.Type == Constants.APIConstants.CHRONOKEEP_REMOTE)
                {
                    if (remoteControllerSwitch != null)
                    {
                        remoteControllerSwitch.Visibility = Visibility.Visible;
                    }
                    if (remoteReadersButton != null)
                    {
                        remoteReadersButton.Visibility = readerExpander.IsExpanded ? Visibility.Visible : Visibility.Collapsed;
                    }
                    remote_api = true;
                    break;
                }
            }
        }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public void UpdateDatabase() { }

        public void Closing()
        {
            List<TimingSystem> removedSystems = database.GetTimingSystems();
            List<TimingSystem> ourSystems = new List<TimingSystem>();
            foreach (AReaderBox box in ReadersBox.Items)
            {
                box.UpdateReader();
                if (box.reader.IPAddress != "0.0.0.0" && box.reader.IPAddress.Length > 7 &&
                    box.reader.IPAddress != string.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]))
                {
                    ourSystems.Add(box.reader);
                }
            }
            removedSystems.RemoveAll(x => ourSystems.Contains(x));
            foreach (TimingSystem sys in removedSystems)
            {
                database.RemoveTimingSystem(sys);
            }
            foreach (TimingSystem sys in ourSystems)
            {
                database.AddTimingSystem(sys);
            }
            Timer.Stop();
        }

        public void UpdateAlarms()
        {
            if (subPage is AlarmsPage)
            {
                ((AlarmsPage)subPage).UpdateAlarms();
            }
        }

        public void UpdateView()
        {
            Log.D("UI.MainPages.TimingPage", "Updating timing information.");
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier == -1)
            {
                // Something went wrong and this shouldn't be visible.
                return;
            }
            if (TimerStarted)
            {
                UpdateStartTime();
            }

            // Get updated list of locations
            locations = database.GetTimingLocations(theEvent.Identifier);
            if (!theEvent.CommonStartFinish)
            {
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_ANNOUNCER, theEvent.Identifier, "Announcer", 0, 0));
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow));
            }
            else
            {
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_ANNOUNCER, theEvent.Identifier, "Announcer", 0, 0));
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
            }

            // Update locations in the list of readers
            connected = 0; total = ReadersBox.Items.Count;
            foreach (AReaderBox read in ReadersBox.Items)
            {
                read.UpdateLocations(locations);
                read.UpdateStatus();
                connected += read.reader.Status == SYSTEM_STATUS.DISCONNECTED ? 0 : 1;
                if (read.reader.Status == SYSTEM_STATUS.DISCONNECTED)
                {
                    if (timeWindow != null && timeWindow.IsTimingSystem(read.reader))
                    {
                        timeWindow.Close();
                        timeWindow = null;
                    }
                    if (rewindWindow != null && rewindWindow.IsTimingSystem(read.reader))
                    {
                        rewindWindow.Close();
                        rewindWindow = null;
                    }
                }
            }
            if (total < 4)
            {
                string system = Constants.Readers.DEFAULT_TIMING_SYSTEM;
                try
                {
                    system = database.GetAppSetting(Constants.Settings.DEFAULT_TIMING_SYSTEM).Value;
                }
                catch
                {
                    Log.D("UI.MainPages.TimingPage", "Error fetching default timing system information.");
                    system = Constants.Readers.DEFAULT_TIMING_SYSTEM;
                }
                for (int i = total; i < 4; i++)
                {
                    ReadersBox.Items.Add(new AReaderBox(
                        this,
                        new TimingSystem(
                            string.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]),
                            system),
                            locations));
                }
            }

            List<DistanceStat> inStats = database.GetDistanceStats(theEvent.Identifier);
            stats.Clear();
            foreach (DistanceStat s in inStats)
            {
                stats.Add(s);
            }
            if (mWindow.HttpServerActive())
            {
                HttpServerButton.Content = "Stop Web";
                IPContainer.Visibility = Visibility.Visible;
                PortContainer.Visibility = Visibility.Visible;
            }
            else
            {
                HttpServerButton.Content = "Start Web";
                IPContainer.Visibility = Visibility.Collapsed;
                PortContainer.Visibility = Visibility.Collapsed;
            }
            if (theEvent.API_ID > 0 && theEvent.API_Event_ID.Length > 1)
            {
                apiPanel.Visibility = Visibility.Visible;
            }
            else
            {
                apiPanel.Visibility = Visibility.Collapsed;
            }
            if (mWindow.IsAPIControllerRunning())
            {
                AutoAPIButton.Content = mWindow.APIErrors() > 0 ? string.Format("Stop Uploads ({0})", mWindow.APIErrors()) : "Stop Uploads";
                ManualAPIButton.IsEnabled = false;
            }
            else
            {
                AutoAPIButton.Content = "Auto Upload";
                ManualAPIButton.IsEnabled = true;
            }

            if (mWindow.IsRemoteRunning())
            {
                remoteControllerSwitch.IsChecked = true;
                remoteErrorsBlock.Text = mWindow.RemoteErrors() > 0 ? mWindow.RemoteErrors().ToString() : "";
            }
            else
            {
                remoteControllerSwitch.IsChecked = false;
                remoteErrorsBlock.Text = "";
            }
            remoteControllerSwitch.IsEnabled = true;

            UpdateDNSButton();

            // Check if there are waves we don't know about and only update the box if so.
            HashSet<int> newWaves = new HashSet<int>();
            foreach (Distance div in database.GetDistances(theEvent.Identifier))
            {
                newWaves.Add(div.Wave);
            }
            bool newWavesExist = false;
            foreach (int wave in newWaves)
            {
                if (!waves.Contains(wave))
                {
                    newWavesExist = true;
                    break;
                }
            }
            if (newWavesExist == true)
            {
                waves.Clear();
                waveTimes.Clear();
                EllapsedRelativeToBox.Items.Clear();
                EllapsedRelativeToBox.Items.Add(new ComboBoxItem
                {
                    Content = "Start Time",
                    Uid = "-1"
                });
                EllapsedRelativeToBox.SelectedIndex = 0;
                foreach (Distance div in database.GetDistances(theEvent.Identifier))
                {
                    EllapsedRelativeToBox.Items.Add(new ComboBoxItem
                    {
                        Content = div.Name + " (Wave " + div.Wave + ")",
                        Uid = div.Wave.ToString()
                    });
                    waveTimes[div.Wave] = (div.StartOffsetSeconds, div.StartOffsetMilliseconds);
                    waves.Add(div.Wave);
                }
                if (waves.Count > 1)
                {
                    EllapsedRelativeToBox.Visibility = Visibility.Visible;
                }
                else
                {
                    EllapsedRelativeToBox.Visibility = Visibility.Collapsed;
                }
            }

            subPage.UpdateView();
        }

        public void UpdateSubView()
        {
            Log.D("UI.MainPages.TimingPage", "Updating sub view.");
            if (cts != null)
            {
                cts.Cancel();
                cts = null;
            }
            cts = new CancellationTokenSource();
            try
            {
                subPage.CancelableUpdateView(cts.Token);
                cts = null;
            }
            catch
            {
                Log.D("UI.MainPages.TimingPage", "Update cancelled.");
            }
        }

        public void DatasetChanged()
        {
            mWindow.NotifyTimingWorker();
        }

        private void Timer_Click(object sender, EventArgs e)
        {
            TimeSpan ellapsed = DateTime.Now - startTime;
            if (selectedWave != -1 && waveTimes.ContainsKey(selectedWave))
            {
                ellapsed = ellapsed.Subtract(TimeSpan.FromSeconds(waveTimes[selectedWave].seconds));
                ellapsed = ellapsed.Subtract(TimeSpan.FromMilliseconds(waveTimes[selectedWave].milliseconds));
            }
            EllapsedTime.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", Math.Abs(ellapsed.Days) * 24 + Math.Abs(ellapsed.Hours), Math.Abs(ellapsed.Minutes), Math.Abs(ellapsed.Seconds));
        }

        private void StartRaceClick(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Starting race.");
            StartTime.Text = DateTime.Now.ToString("HH:mm:ss.fff");
            StartRace.IsEnabled = false;
            StartTimeChanged();
        }

        private void ChangeWaves(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Set Wave Times clicked.");
            WaveWindow waves = new WaveWindow(mWindow, database);
            mWindow.AddWindow(waves);
            waves.ShowDialog();
        }

        private void ManualEntry(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Manual Entry selected.");
            ManualEntryWindow manualEntryWindow = ManualEntryWindow.NewWindow(mWindow, database, locations);
            if (manualEntryWindow != null)
            {
                mWindow.AddWindow(manualEntryWindow);
                manualEntryWindow.ShowDialog();
            }
        }

        private async void LoadLog(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Loading from log.");
            OpenFileDialog csv_dialog = new OpenFileDialog() { Filter = "Log Files (*.csv,*.txt,*.log)|*.csv;*.txt;*.log|All Files|*" };
            if (csv_dialog.ShowDialog() == true)
            {
                try
                {
                    LogImporter importer = new LogImporter(csv_dialog.FileName);
                    await Task.Run(() =>
                    {
                        importer.FindType();
                    });
                    ImportLogWindow logWindow = ImportLogWindow.NewWindow(mWindow, importer, database);
                    if (logWindow != null)
                    {
                        mWindow.AddWindow(logWindow);
                        logWindow.ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    Log.E("UI.MainPages.TimingPage", "Something went wrong when trying to read the CSV file.");
                    Log.E("UI.MainPages.TimingPage", ex.StackTrace);
                }
            }
        }

        public void NotifyTimingWorker()
        {
            mWindow.NotifyTimingWorker();
        }

        private void StartTimeKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Log.D("UI.MainPages.TimingPage", "Start Time Box return key found.");
                UpdateStartTime();
            }
        }

        private void StartTimeLostFocus(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Start Time Box has lost focus.");
            StartTimeChanged();
        }

        private void StartTimeChanged()
        {
            UpdateStartTime();
            long oldStartSeconds = theEvent.StartSeconds;
            int oldStartMilliseconds = theEvent.StartMilliseconds;
            theEvent.StartSeconds = (startTime.Hour * 3600) + (startTime.Minute * 60) + startTime.Second;
            theEvent.StartMilliseconds = startTime.Millisecond;
            if (oldStartSeconds != theEvent.StartSeconds || oldStartMilliseconds != theEvent.StartMilliseconds)
            {
                database.UpdateEvent(theEvent);
                database.ResetTimingResultsEvent(theEvent.Identifier);
                UpdateView();
                mWindow.NetworkClearResults();
                mWindow.NotifyTimingWorker();
            }
        }

        private void UpdateStartTime()
        {
            if (!TimerStarted)
            {
                TimerStarted = true;
                Timer.Start();
            }
            string startTimeValue = StartTime.Text.Replace('_', '0');
            StartRace.IsEnabled = false;
            StartTime.Text = startTimeValue;
            Log.D("UI.MainPages.TimingPage", "Start time is " + startTimeValue);
            startTime = DateTime.ParseExact(startTimeValue + DateTime.Parse(theEvent.Date).ToString("ddMMyyyy"), "HH:mm:ss.fffddMMyyyy", null);
            Log.D("UI.MainPages.TimingPage", "Start time is " + startTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }

        public void NewMessage()
        {
            if (timeWindow != null)
            {
                timeWindow.UpdateTime();
            }
        }

        public void OpenTimeWindow(TimingSystem system)
        {
            Log.D("UI.MainPages.TimingPage", "Opening Set Time Window.");
            timeWindow = new SetTimeWindow(this, system);
            timeWindow.ShowDialog();
            timeWindow = null;
        }

        public void OpenRewindWindow(TimingSystem system)
        {
            Log.D("UI.MainPages.TimingPage", "Opening Rewind Window.");
            rewindWindow = new RewindWindow(system);
            rewindWindow.ShowDialog();
            rewindWindow = null;

        }

        public void SetAllTimingSystemsToTime(DateTime time, bool now)
        {
            List<TimingSystem> systems = mWindow.GetConnectedSystems();
            foreach (TimingSystem sys in systems)
            {
                if (now)
                {
                    sys.SystemInterface.SetTime(DateTime.Now);
                }
                else
                {
                    sys.SystemInterface.SetTime(time);
                }
            }
        }

        public void RemoveSystem(TimingSystem sys)
        {
            database.RemoveTimingSystem(sys.SystemIdentifier);
            AReaderBox removed = null;
            foreach (AReaderBox box in ReadersBox.Items)
            {
                if (box.reader.SystemIdentifier == sys.SystemIdentifier && sys.Saved())
                {
                    removed = box;
                    break;
                }
            }
            ReadersBox.Items.Remove(removed);
            UpdateView();
        }

        public bool ConnectSystem(TimingSystem sys)
        {
            mWindow.ConnectTimingSystem(sys);
            if (sys.Status == SYSTEM_STATUS.CONNECTED || sys.Status == SYSTEM_STATUS.WORKING)
            {
                connected++;
            }
            Log.D("UI.MainPages.TimingPage", connected + " systems connected or trying to connect.");
            if (connected >= total)
            {
                string system = Constants.Readers.DEFAULT_TIMING_SYSTEM;
                try
                {
                    system = database.GetAppSetting(Constants.Settings.DEFAULT_TIMING_SYSTEM).Value;
                }
                catch
                {
                    Log.D("UI.MainPages.TimingPage", "Error fetching default timing system information.");
                    system = Constants.Readers.DEFAULT_TIMING_SYSTEM;
                }
                ReadersBox.Items.Add(new AReaderBox(this, new TimingSystem(string.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]), system), locations));
                total = ReadersBox.Items.Count;
            }
            return sys.Status != SYSTEM_STATUS.DISCONNECTED;
        }

        public bool DisconnectSystem(TimingSystem sys)
        {
            mWindow.DisconnectTimingSystem(sys);
            if (sys.Status == SYSTEM_STATUS.DISCONNECTED)
            {
                connected--;
            }
            Log.D("UI.MainPages.TimingPage", connected + " systems connected or trying to connect/disconnect.");
            // This code appears to remove the connected reader if there are more than 4 readers displayed and at least 2 disconnected readers
            // this should not be necessary to do, just leave them all there and let the program deal with it the next time
            // the window opens
            /*if (total > 4 && connected < total - 1)
            {
                AReaderBox removeMe = null;
                foreach (AReaderBox aReader in ReadersBox.Items)
                {
                    if (aReader.reader.Status == SYSTEM_STATUS.DISCONNECTED)
                    {
                        removeMe = aReader;
                        break;
                    }
                }
                ReadersBox.Items.Remove(removeMe);
                total = ReadersBox.Items.Count;
            } //*/
            return sys.Status == SYSTEM_STATUS.DISCONNECTED;
        }

        private void RawReads_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Raw Reads selected.");
            subPage = new TimingRawReadsPage(this, database);
            TimingFrame.NavigationService.RemoveBackEntry();
            TimingFrame.Content = subPage;
        }

        public void LoadMainDisplay()
        {
            Log.D("UI.MainPages.TimingPage", "Going back to main display.");
            subPage = new TimingResultsPage(this, database);
            TimingFrame.NavigationService.RemoveBackEntry();
            TimingFrame.Content = subPage;
        }

        private async void Recalculate_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Recalculate results clicked.");
            if ((string)recalculateButton.Content == "Working...")
            {
                return;
            }
            recalculateButton.Content = "Working...";
            APIObject api = null;
            try
            {
                api = database.GetAPI(theEvent.API_ID);
                Log.D("UI.MainPages.TimingPage", "API found.");
            }
            catch {}
            // Get the event id values. Exit if not valid.
            string[] event_ids = theEvent.API_Event_ID.Split(',');
            Log.D("UI.MainPages.TimingPage", "Event Id's found: " + event_ids.Length + " API is null? " + (api == null).ToString());
            // Create a bool for checking if we've grabbed the APIController's mutex so we release it later
            bool mutexGrabbed = false;
            if (event_ids.Length == 2 && api != null && APIController.GrabMutex(15000))
            {
                mutexGrabbed = true;
                try
                {
                    Log.D("UI.MainPages.TimingPage", "Deleting results from API.");
                    if (theEvent.UploadSpecific == true)
                    {
                        foreach (Distance d in database.GetDistances(theEvent.Identifier))
                        {
                            if (d.Upload == true && d.LinkedDistance == Constants.Timing.DISTANCE_NO_LINKED_ID)
                            {
                                await APIController.DeleteResults(api, event_ids[0], event_ids[1], d.Name);
                            }
                        }
                    }
                    else
                    {
                        await APIController.DeleteResults(api, event_ids[0], event_ids[1], null);
                    }
                }
                catch (APIException ex)
                {

                    DialogBox.Show(ex.Message);
                }
            }
            // We do this because we want to ensure we've reset all the results before we allow
            // the auto uploader to start uploading any more results so we don't upload
            // old results over our brand new results.
            database.ResetTimingResultsEvent(theEvent.Identifier);
            if (mutexGrabbed)
            {
                APIController.ReleaseMutex();
            }
            UpdateView();
            mWindow.NetworkClearResults();
            mWindow.NotifyTimingWorker();
            recalculateButton.Content = "Recalculate";
        }

        private void ViewOnlyBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (subPage == null)
            {
                return;
            }
            switch (((ComboBoxItem)viewOnlyBox.SelectedItem).Content)
            {
                case "Show Only Unknown":
                    subPage.Show(PeopleType.UNKNOWN);
                    break;
                case "Show All":
                    subPage.Show(PeopleType.ALL);
                    break;
                case "Show Only Starts":
                    subPage.Show(PeopleType.STARTS);
                    break;
                case "Show Only Finishes":
                    subPage.Show(PeopleType.FINISHES);
                    break;
                case "Show Only Unknown Finishes":
                    subPage.Show(PeopleType.UNKNOWN_FINISHES);
                    break;
                case "Show Only Unknown Starts":
                    subPage.Show(PeopleType.UNKNOWN_STARTS);
                    break;
                default:
                    subPage.Show(PeopleType.KNOWN);
                    break;
            }
        }

        public PeopleType GetPeopleType()
        {
            switch (((ComboBoxItem)viewOnlyBox.SelectedItem).Content)
            {
                case "Show All":
                    return PeopleType.ALL;
                case "Show Only Starts":
                    return PeopleType.STARTS;
                case "Show Only Finishes":
                    return PeopleType.FINISHES;
                case "Show Only Unknown":
                    return PeopleType.UNKNOWN;
                case "Show Only Unknown Finishes":
                    return PeopleType.UNKNOWN_FINISHES;
                case "Show Only Unknown Starts":
                    return PeopleType.UNKNOWN_STARTS;
            }
            return PeopleType.KNOWN;
        }

        private void SortBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (subPage == null)
            {
                return;
            }
            subPage.SortBy(GetSortType());
        }

        public SortType GetSortType()
        {
            switch (((ComboBoxItem)SortBy.SelectedItem).Content)
            {
                case "Gun Time":
                    return SortType.GUNTIME;
                case "Bib":
                    return SortType.BIB;
                case "Distance":
                    return SortType.DISTANCE;
                case "Age Group":
                    return SortType.AGEGROUP;
                case "Gender":
                    return SortType.GENDER;
                case "Place":
                    return SortType.PLACE;
            }
            return SortType.SYSTIME;
        }

        public string GetSearchValue()
        {
            return searchBox.Text.Trim();
        }

        private void SearchBox_TextChanged(Wpf.Ui.Controls.AutoSuggestBox sender, Wpf.Ui.Controls.AutoSuggestBoxTextChangedEventArgs args)
        {
            UpdateSubView();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Export clicked.");
            ExportResults exportResults = new ExportResults(mWindow, database);
            if (!exportResults.SetupError())
            {
                mWindow.AddWindow(exportResults);
                exportResults.ShowDialog();
            }
        }

        private void Export_Abbott_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Export Abbott Clicked.");
            ExportDistanceResults exportAbbott = new ExportDistanceResults(mWindow, database, OutputType.Abbott);
            if (!exportAbbott.SetupError())
            {
                mWindow.AddWindow(exportAbbott);
                exportAbbott.ShowDialog();
            }
        }

        private void Export_BAA_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Export BAA Clicked.");
            if (theEvent.EventType == Constants.Timing.EVENT_TYPE_TIME)
            {
                DialogBox.Show("Exporting time based events not supported.");
                return;
            }
            ExportDistanceResults exportBAA = new ExportDistanceResults(mWindow, database, OutputType.Boston);
            if (!exportBAA.SetupError())
            {
                mWindow.AddWindow(exportBAA);
                exportBAA.ShowDialog();
            }
        }
        private void Export_US_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Export Ultrasignup Clicked.");
            if (theEvent.EventType == Constants.Timing.EVENT_TYPE_TIME)
            {
                DialogBox.Show("Exporting time based events not supported.");
                return;
            }
            ExportDistanceResults exportUS = new ExportDistanceResults(mWindow, database, OutputType.UltraSignup);
            if (!exportUS.SetupError())
            {
                mWindow.AddWindow(exportUS);
                exportUS.ShowDialog();
            } 
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Print clicked.");
            subPage = new PrintPage(this, database);
            TimingFrame.NavigationService.RemoveBackEntry();
            TimingFrame.Content = subPage;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Starting TimingPage Update Timer.");
        }

        private void AddDNF_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Add DNF Entry clicked.");
            ManualEntryWindow manualEntryWindow = ManualEntryWindow.NewWindow(mWindow, database);
            if (manualEntryWindow != null)
            {
                mWindow.AddWindow(manualEntryWindow);
                manualEntryWindow.ShowDialog();
            }
        }

        private void StatsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DistanceStat selected = (DistanceStat)statsListView.SelectedItem;
            if (selected == null)
            {
                return;
            }
            Log.D("UI.MainPages.TimingPage", "Stats double cliked. Distance is " + selected.DistanceName);
            subPage = new DistanceStatsPage(this, mWindow, database, selected.DistanceID, selected.DistanceName);
            TimingFrame.NavigationService.RemoveBackEntry();
            TimingFrame.Content = subPage;

        }

        private void Award_Click (object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Awards clicked.");
            subPage = new AwardPage(this, database);
            TimingFrame.NavigationService.RemoveBackEntry();
            TimingFrame.Content = subPage;
        }

        private void CreateHTML_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Create HTML clicked.");
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "HTML file (*.htm,*.html)|*.htm;*.html",
                FileName = string.Format("{0} {1} Web.{2}", theEvent.YearCode, theEvent.Name, "html"),
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                List<TimeResult> finishResults = database.GetFinishTimes(theEvent.Identifier);
                Dictionary<int, Participant> partDict = database.GetParticipants(theEvent.Identifier).ToDictionary(v => v.EventSpecific.Identifier, v => v);
                HtmlResultsTemplate template = new HtmlResultsTemplate(theEvent, finishResults);
                File.WriteAllText(saveFileDialog.FileName, template.TransformText());
                DialogBox.Show("File saved.");
            }
        }

        private void HTMLServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (HttpServerButton.Content.ToString().Equals("Start Web", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    mWindow.StartHttpServer();
                    HttpServerButton.Content = "Stop Web";
                    IPContainer.Visibility = Visibility.Visible;
                    PortContainer.Visibility = Visibility.Visible;
                }
                catch
                {
                    mWindow.StopHttpServer();
                    HttpServerButton.Content = "Start Web";
                    DialogBox.Show("Unable to start the web server. Please type this command in an elevated command prompt:", "netsh http add urlacl url=http://*:6933/ user=everyone");
                    IPContainer.Visibility = Visibility.Collapsed;
                    PortContainer.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                mWindow.StopHttpServer();
                HttpServerButton.Content = "Start Web";
                IPContainer.Visibility = Visibility.Collapsed;
                PortContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void AutoAPI_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Auto API clicked.");
            if ((string)AutoAPIButton.Content == "Auto Upload")
            {
                AutoAPIButton.Content = "Starting...";
                mWindow.StartAPIController();
            }
            else
            {
                AutoAPIButton.Content = "Stopping...";
                mWindow.StopAPIController();
            }
        }

        private void ManualAPI_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Manual API clicked.");
            if (ManualAPIButton.Content.ToString() != "Uploading")
            {
                Log.D("UI.MainPages.TimingPage", "Uploading data.");
                ManualAPIButton.Content = "Uploading";
                UploadResults();
                return;
            }
            Log.D("UI.MainPages.TimingPage", "Already uploading.");
        }

        private async void UploadResults()
        {
            // Get API to upload.
            if (theEvent.API_ID < 0 && theEvent.API_Event_ID.Length > 1)
            {
                ManualAPIButton.Content = "Manual Upload";
                return;
            }
            APIObject api = database.GetAPI(theEvent.API_ID);
            string[] event_ids = theEvent.API_Event_ID.Split(',');
            if (event_ids.Length != 2)
            {
                ManualAPIButton.Content = "Manual Upload";
                return;
            }
            // Get results to upload.
            List<TimeResult> results = database.GetNonUploadedResults(theEvent.Identifier);
            // Remove all results to upload that don't have a place set, are not DNF/DNS results, and are also not start times.
            results.RemoveAll(x => x.Place < 1
                && x.Status != Constants.Timing.TIMERESULT_STATUS_DNF
                && x.Status != Constants.Timing.TIMERESULT_STATUS_DNS
                && x.SegmentId != Constants.Timing.SEGMENT_START);
            if (results.Count < 1)
            {
                Log.D("UI.MainPages.TimingPage", "Nothing to upload.");
                ManualAPIButton.Content = "Manual Upload";
                return;
            }
            // Change TimeResults to APIResults
            List<APIResult> upRes = new List<APIResult>();
            Log.D("UI.MainPages.TimingPage", "Results count: " + results.Count.ToString());
            DateTime start = DateTime.SpecifyKind(DateTime.Parse(theEvent.Date), DateTimeKind.Local).AddSeconds(theEvent.StartSeconds).AddMilliseconds(theEvent.StartMilliseconds);
            Dictionary<string, DateTime> waveStartTimes = new Dictionary<string, DateTime>();
            HashSet<string> uploadDistances = new();
            foreach (Distance d in database.GetDistances(theEvent.Identifier))
            {
                waveStartTimes[d.Name] = start.AddSeconds(d.StartOffsetSeconds).AddMilliseconds(d.StartOffsetMilliseconds);
                if (d.Upload && d.LinkedDistance == Constants.Timing.DISTANCE_NO_LINKED_ID)
                {
                    uploadDistances.Add(d.Name);
                }
            }
            foreach (TimeResult tr in results)
            {
                tr.Uploaded = Constants.Timing.TIMERESULT_UPLOADED_TRUE;
                DateTime trStart = waveStartTimes.TryGetValue(tr.RealDistanceName, out DateTime value) ? value : start;
                // only add to upload list if we want to upload everything (NOT Specific)
                // or we only want to upload specific distances and the distance is in the
                // list of distances we want to upload
                if (!theEvent.UploadSpecific || uploadDistances.Contains(tr.DistanceName))
                {
                    upRes.Add(new APIResult(theEvent, tr, trStart));
                }
            }
            Log.D("UI.MainPages.TimingPage", "Attempting to upload " + upRes.Count.ToString() + " results.");
            int total = 0;
            int loops = upRes.Count / Constants.Timing.API_LOOP_COUNT;
            AddResultsResponse response;
            for (int i = 0; i < loops; i += 1)
            {
                try
                {
                    response = await APIHandlers.UploadResults(api, event_ids[0], event_ids[1], upRes.GetRange(i * Constants.Timing.API_LOOP_COUNT, Constants.Timing.API_LOOP_COUNT));
                }
                catch (APIException ex)
                {
                    DialogBox.Show(ex.Message);
                    ManualAPIButton.Content = "Manual Upload";
                    return;
                }
                if (response != null)
                {
                    total += response.Count;
                    Log.D("UI.MainPages.TimingPage", "Total: " + total + " Count: " + response.Count);
                }
            }
            int leftovers = upRes.Count - (loops * Constants.Timing.API_LOOP_COUNT);
            if (leftovers > 0)
            {
                try
                {
                    response = await APIHandlers.UploadResults(api, event_ids[0], event_ids[1], upRes.GetRange(loops * Constants.Timing.API_LOOP_COUNT, leftovers));
                }
                catch (APIException ex)
                {
                    DialogBox.Show(ex.Message);
                    ManualAPIButton.Content = "Manual Upload";
                    return;
                }
                if (response != null)
                {
                    total += response.Count;
                    Log.D("UI.MainPages.TimingPage", "Total: " + total + " Count: " + response.Count);
                }
                Log.D("UI.MainPages.TimingPage", "Upload finished. Count total: " + total);
            }
            if (results.Count == total)
            {
                Log.D("UI.MainPages.TimingPage", "Count matches, updating records.");
                database.AddTimingResults(results);
            }
            ManualAPIButton.Content = "Manual Upload";
        }

        private void SaveLog(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Save Log clicked.");
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = string.Format("{0} {1} Log.{2}", theEvent.YearCode, theEvent.Name, "csv"),
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                Dictionary<string, List<ChipRead>> locationReadDict = new Dictionary<string, List<ChipRead>>();
                string[] headers =
                {
                    "status",
                    "chip_number",
                    "seconds",
                    "milliseconds",
                    "time_seconds",
                    "time_milliseconds",
                    "antenna",
                    "reader",
                    "box",
                    "log_index",
                    "rssi",
                    "is_rewind",
                    "reader_time",
                    "start_time",
                    "read_bib",
                    "type"
                };
                List<ChipRead> chipReads = database.GetChipReads(theEvent.Identifier);
                foreach (ChipRead read in chipReads)
                {
                    if (!locationReadDict.ContainsKey(read.LocationName))
                    {
                        locationReadDict[read.LocationName] = new List<ChipRead>();
                    }
                    locationReadDict[read.LocationName].Add(read);
                }
                StringBuilder format = new StringBuilder();
                for (int i = 0; i < headers.Length; i++)
                {
                    format.Append("\"{");
                    format.Append(i);
                    format.Append("}\",");
                }
                format.Remove(format.Length - 1, 1);
                Log.D("UI.MainPages.TimingPage", string.Format("The format is '{0}'", format.ToString()));
                if (locationReadDict.Keys.Count == 1)
                {
                    List<object[]> data = new List<object[]>();
                    foreach (ChipRead read in chipReads)
                    {
                        data.Add(new object[] {
                            read.Status,
                            read.ChipNumber,
                            read.Seconds,
                            read.Milliseconds,
                            read.TimeSeconds,
                            read.TimeMilliseconds,
                            read.Antenna,
                            read.Reader,
                            read.Box,
                            read.LogId,
                            read.RSSI,
                            read.IsRewind,
                            read.ReaderTime,
                            read.StartTime,
                            read.ReadBib,
                            read.Type
                        });
                    }
                    IDataExporter exporter = new CSVExporter(format.ToString());
                    exporter.SetData(headers, data);
                    exporter.ExportData(saveFileDialog.FileName);
                }
                // Multiple locations, save each individually.
                else
                {
                    foreach (string key in locationReadDict.Keys)
                    {
                        List<object[]> data = new List<object[]>();
                        foreach (ChipRead read in locationReadDict[key])
                        {
                            data.Add(new object[] {
                            read.Status,
                            read.ChipNumber,
                            read.Seconds,
                            read.Milliseconds,
                            read.TimeSeconds,
                            read.TimeMilliseconds,
                            read.Antenna,
                            read.Reader,
                            read.Box,
                            read.LogId,
                            read.RSSI,
                            read.IsRewind,
                            read.ReaderTime,
                            read.StartTime,
                            read.ReadBib,
                            read.Type
                        });
                        }
                        IDataExporter exporter = new CSVExporter(format.ToString());
                        exporter.SetData(headers, data);
                        Log.D("UI.MainPages.TimingPage", "Saving file to: " + Path.GetDirectoryName(saveFileDialog.FileName) + "\\" + Regex.Replace(key.ToLower(), @"[^a-z0-9\-]", "") + "-" + Path.GetFileName(saveFileDialog.FileName));
                        exporter.ExportData(Path.GetDirectoryName(saveFileDialog.FileName) + "\\" + Regex.Replace(key.ToLower(), @"[^a-z0-9\-]", "") + "-" + Path.GetFileName(saveFileDialog.FileName));
                    }
                }
                DialogBox.Show("File saved.");
            }
        }

        private void statsListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = controlScroll;
            if (e.Delta < 0)
            {
                if (scv.VerticalOffset - e.Delta <= scv.ExtentHeight - scv.ViewportHeight)
                {
                    scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
                }
                else
                {
                    scv.ScrollToBottom();
                }
            }
            else
            {
                if (scv.VerticalOffset - e.Delta > 0)
                {
                    scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
                }
                else
                {
                    scv.ScrollToTop();
                }
            }
        }

        private void dnsMode_Click(object sender, RoutedEventArgs e)
        {
            bool worked = false;
            if (dnsMode.Content.Equals("Start DNS Mode"))
            {
                Log.D("UI.MainPages.TimingPage", "Starting DNS Mode.");
                worked = mWindow.StartDidNotStartMode();
            }
            else
            {
                Log.D("UI.MainPages.TimingPage", "Stopping DNS Mode.");
                worked = mWindow.StopDidNotStartMode();
            }
            if (!worked)
            {
                DialogBox.Show("An error occurred entering DNS mode.");
            }
            UpdateDNSButton();
        }

        private void UpdateDNSButton()
        {
            if (mWindow.InDidNotStartMode())
            {
                dnsMode.Content = "Stop DNS Mode";
            }
            else
            {
                dnsMode.Content = "Start DNS Mode";
            }
        }

        private void AlarmButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Alarms selected.");
            subPage = new AlarmsPage(this, database);
            TimingFrame.NavigationService.RemoveBackEntry();
            TimingFrame.Content = subPage;
        }

        private void remoteReadersButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Remote readers button clicked.");
            RemoteReadersWindow win = RemoteReadersWindow.CreateWindow(mWindow, database);
            win.Show();
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            if (remoteReadersButton != null)
            {
                if (readerExpander.IsExpanded == true && remote_api)
                {
                    remoteReadersButton.Visibility = Visibility.Visible;
                }
                else
                {
                    remoteReadersButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void remoteControllerSwitch_Checked(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Remote toggle switch checked.");
            remoteControllerSwitch.IsEnabled = false;
            mWindow.StartRemote();

        }

        private void remoteControllerSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Remote toggle switch unchecked.");
            remoteControllerSwitch.IsEnabled = false;
            mWindow.StopRemote();
        }

        private void EllapsedRelativeToBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "EllapsedRelativeToBox selection changed.");
            selectedWave = -1;
            if (EllapsedRelativeToBox.SelectedIndex > 0)
            {
                try
                {
                    selectedWave = Convert.ToInt32(((ComboBoxItem)EllapsedRelativeToBox.SelectedItem).Uid);
                }
                catch
                {
                    selectedWave = -1;
                }
            }
        }

        private void modifySMSButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Modify SMS button clicked.");
            SMSWaveEnabledWindow smsWindow = new SMSWaveEnabledWindow(mWindow, database);
            smsWindow.Show();
        }

        private async void sendEmailsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Send Emails button clicked.");
            if ((string)sendEmailsButton.Content != "Send Emails")
            {
                return;
            }
            sendEmailsButton.Content = "Sending...";
            await Task.Run(() =>
            {
                HashSet<int> sentIDs = new HashSet<int>();
                List<int> idents = database.GetEmailAlerts(theEvent.Identifier);
                if (idents == null)
                {
                    return;
                }
                foreach (int es_id in idents)
                {
                    sentIDs.Add(es_id);
                }
                List<TimeResult> finishTimes = database.GetFinishTimes(theEvent.Identifier);
                APIObject api = database.GetAPI(theEvent.API_ID);
                Dictionary<string, Participant> participantDictionary = new Dictionary<string, Participant>();
                int distances = 0;
                foreach (Participant p in database.GetParticipants(theEvent.Identifier))
                {
                    participantDictionary[p.Identifier.ToString()] = p;
                }
                foreach (Distance d in database.GetDistances(theEvent.Identifier))
                {
                    if (Constants.Timing.DISTANCE_NO_LINKED_ID == d.LinkedDistance)
                    {
                        distances++;
                    }
                }
                Globals.UpdateBannedEmails();
                HttpClient client = new();
                MailgunCredentials credentials = MailgunCredentials.GetCredentials(database);
                if (!credentials.Valid())
                {
                    return;
                }
                string base64String = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", credentials.Username, credentials.APIKey)));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(@"Basic", base64String);
                foreach (TimeResult result in finishTimes)
                {
                    Participant part = participantDictionary.ContainsKey(result.ParticipantId) ? participantDictionary[result.ParticipantId] : null;
                    if (part != null && result.EventSpecificId != Constants.Timing.EVENTSPECIFIC_UNKNOWN)
                    {
                        if (part.Email.Length > 0 && !Globals.BannedEmails.Contains(part.Email) && !sentIDs.Contains(result.EventSpecificId))
                        {
                            MultipartFormDataContent postData = new MultipartFormDataContent
                    {
                        { new StringContent(credentials.From()), "from" },
                        { new StringContent(part.Email), "to" },
                        { new StringContent(string.Format("{0} {1}", theEvent.Year, theEvent.Name)), "subject" },
                        { new StringContent(new HtmlCertificateEmailTemplate(
                            theEvent,
                            result,
                            part.Email,
                            distances == 1,
                            api
                            ).TransformText()), "html" }
                    };
                            try
                            {
                                client.PostAsync(string.Format("https://api.mailgun.net/v3/{0}/messages", credentials.Domain), postData);
                            }
                            catch
                            {
                                Log.E("UI.MainPages.TimingPage", "Error sending email.");
                            }
                            database.AddEmailAlert(theEvent.Identifier, result.EventSpecificId);
                        }
                    }
                }
                Log.D("UI.MainPages.TimingPage", "Async operation to send emails finished.");
            });
            Log.D("UI.MainPages.TimingPage", "Changing button back and sending dialog box.");
            DialogBox.Show("Emails sent.");
            sendEmailsButton.Content = "Send Emails";
        }

        private class AReaderBox : ListBoxItem
        {
            public ComboBox ReaderType { get; private set; }
            public TextBox ReaderIP { get; private set; }
            public TextBox ReaderPort { get; private set; }
            public ComboBox ReaderLocation { get; private set; }
            public Wpf.Ui.Controls.Button ConnectButton { get; private set; }
            public Wpf.Ui.Controls.Button ClockButton { get; private set; }
            public Wpf.Ui.Controls.Button RewindButton { get; private set; }
            public Wpf.Ui.Controls.Button SettingsButton { get; private set; }
            public Wpf.Ui.Controls.Button RemoveButton { get; private set; }

            readonly TimingPage parent;
            private List<TimingLocation> locations;
            public TimingSystem reader;

            public RewindWindow rewind = null;

            private const string IPPattern = "^([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])$";
            private const string allowedChars = "[^0-9.]";
            private const string allowedNums = "[^0-9]";

            public AReaderBox(TimingPage window, TimingSystem sys, List<TimingLocation> locations)
            {
                this.parent = window;
                this.locations = locations;
                this.reader = sys;
                Grid thePanel = new Grid()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(140) }); // Reader Type
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(140) }); // Reader IP
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) });  // Reader Port
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(140) }); // Location
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(60) });  // Clock
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(60) });  // Rewind
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(60) });  // Settings
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(60) });  // Connect
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(60) });  // Disconnect
                this.Content = thePanel;
                ReaderType = new ComboBox()
                {
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(5, 5, 5, 5),
                    Height = 40
                };
                ComboBoxItem current = null, selected = null;
                foreach (string SYSTEM_IDVAL in Constants.Readers.SYSTEM_NAMES.Keys)
                {
                    current = new ComboBoxItem()
                    {
                        Content = Constants.Readers.SYSTEM_NAMES[SYSTEM_IDVAL],
                        Uid = SYSTEM_IDVAL
                    };
                    if (SYSTEM_IDVAL == reader.Type)
                    {
                        selected = current;
                    }
                    ReaderType.Items.Add(current);
                }
                if (selected != null)
                {
                    ReaderType.SelectedItem = selected;
                }
                else
                {
                    ReaderType.SelectedIndex = 0;
                }
                ReaderType.SelectionChanged += new SelectionChangedEventHandler(ReaderTypeChanged);
                thePanel.Children.Add(ReaderType);
                Grid.SetColumn(ReaderType, 0);
                ReaderIP = new TextBox()
                {
                    Text = reader.IPAddress,
                    FontSize = 14,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(5, 5, 5, 5)
                };
                ReaderIP.GotFocus += new RoutedEventHandler(this.SelectAll);
                ReaderIP.PreviewTextInput += new TextCompositionEventHandler(this.IPValidation);
                thePanel.Children.Add(ReaderIP);
                Grid.SetColumn(ReaderIP, 1);
                ReaderPort = new TextBox()
                {
                    Text = reader.Port.ToString(),
                    FontSize = 14,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(5, 5, 5, 5)
                };
                ReaderPort.GotFocus += new RoutedEventHandler(this.SelectAll);
                ReaderPort.PreviewTextInput += new TextCompositionEventHandler(this.NumberValidation);
                thePanel.Children.Add(ReaderPort);
                Grid.SetColumn(ReaderPort, 2);
                ReaderLocation = new ComboBox()
                {
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 5, 5, 5),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 40
                };
                current = null; selected = null;
                foreach (TimingLocation loc in this.locations)
                {
                    current = new ComboBoxItem()
                    {
                        Content = loc.Name,
                        Uid = loc.Identifier.ToString()
                    };
                    if (reader.LocationID == loc.Identifier)
                    {
                        selected = current;
                    }
                    ReaderLocation.Items.Add(current);
                }
                if (selected != null)
                {
                    ReaderLocation.SelectedItem = selected;
                }
                else
                {
                    ReaderLocation.SelectedIndex = 0;
                }
                thePanel.Children.Add(ReaderLocation);
                Grid.SetColumn(ReaderLocation, 3);
                ClockButton = new Wpf.Ui.Controls.Button()
                {
                    Icon = new Wpf.Ui.Controls.SymbolIcon() { Symbol = Wpf.Ui.Controls.SymbolRegular.Clock24 },
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    IsEnabled = false,
                    Opacity = 0.2,
                    Height = 40
                };
                ClockButton.Click += new RoutedEventHandler(this.Clock);
                thePanel.Children.Add(ClockButton);
                Grid.SetColumn(ClockButton, 4);
                RewindButton = new Wpf.Ui.Controls.Button()
                {
                    Icon = new Wpf.Ui.Controls.SymbolIcon() { Symbol = Wpf.Ui.Controls.SymbolRegular.Rewind24 },
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    IsEnabled = false,
                    Opacity = 0.2,
                    Height = 40
                };
                RewindButton.Click += new RoutedEventHandler(this.Rewind);
                thePanel.Children.Add(RewindButton);
                Grid.SetColumn(RewindButton, 5);
                SettingsButton = new Wpf.Ui.Controls.Button()
                {
                    Icon = new Wpf.Ui.Controls.SymbolIcon() { Symbol = Wpf.Ui.Controls.SymbolRegular.Settings24 },
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 40
                };
                SettingsButton.Click += new RoutedEventHandler(this.Settings);
                thePanel.Children.Add(SettingsButton);
                Grid.SetColumn(SettingsButton, 6);
                ConnectButton = new Wpf.Ui.Controls.Button()
                {
                    Icon = new Wpf.Ui.Controls.SymbolIcon() { Symbol = Wpf.Ui.Controls.SymbolRegular.Play24 },
                    Uid = "connect",
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 40
                };
                ConnectButton.Click += new RoutedEventHandler(this.Connect);
                thePanel.Children.Add(ConnectButton);
                Grid.SetColumn(ConnectButton, 7);
                RemoveButton = new Wpf.Ui.Controls.Button()
                {
                    Icon = new Wpf.Ui.Controls.SymbolIcon() { Symbol = Wpf.Ui.Controls.SymbolRegular.Delete24 },
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 40
                };
                if (reader.Saved())
                {
                    RemoveButton.Click += new RoutedEventHandler(this.Remove);
                    thePanel.Children.Add(RemoveButton);
                    Grid.SetColumn(RemoveButton, 8);
                }
                UpdateStatus();
            }

            public void UpdateLocations(List<TimingLocation> locations)
            {
                this.locations = locations;
                int selectedLocation = Convert.ToInt32(((ComboBoxItem)ReaderLocation.SelectedItem).Uid);
                ReaderLocation.Items.Clear();
                ComboBoxItem current = null, selected = null;
                foreach (TimingLocation loc in this.locations)
                {
                    current = new ComboBoxItem()
                    {
                        Content = loc.Name,
                        Uid = loc.Identifier.ToString()
                    };
                    if (selectedLocation == loc.Identifier)
                    {
                        selected = current;
                    }
                    ReaderLocation.Items.Add(current);
                }
                if (selected != null)
                {
                    ReaderLocation.SelectedItem = selected;
                }
                else
                {
                    ReaderLocation.SelectedIndex = 0;
                }
            }

            public void UpdateStatus()
            {
                if (reader.Status == SYSTEM_STATUS.CONNECTED)
                {
                    SetConnected();
                }
                else if (reader.Status == SYSTEM_STATUS.DISCONNECTED)
                {
                    SetDisconnected();
                }
                else
                {
                    SetWorking();
                }
            }

            private void SelectAll(object sender, RoutedEventArgs e)
            {
                TextBox src = (TextBox)e.OriginalSource;
                src.SelectAll();
            }

            private void IPValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = Regex.IsMatch(e.Text, allowedChars);
            }

            private void NumberValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = Regex.IsMatch(e.Text, allowedNums);
            }

            public void UpdateReader()
            {
                // Check if IP is a valid IP address
                if (!Regex.IsMatch(ReaderIP.Text.Trim(), IPPattern))
                {
                    reader.IPAddress = "";
                }
                else
                {
                    reader.IPAddress = ReaderIP.Text.Trim();
                }
                // Check if Port is valid.
                int portNo = -1;
                int.TryParse(ReaderPort.Text.Trim(), out portNo);
                if (portNo > 65535)
                {
                    portNo = -1;
                }
                reader.Port = portNo;
                reader.LocationID = Convert.ToInt32(((ComboBoxItem)ReaderLocation.SelectedItem).Uid);
                reader.LocationName = ((ComboBoxItem)ReaderLocation.SelectedItem).Content.ToString();
            }

            private void ReaderTypeChanged(object sender, SelectionChangedEventArgs args)
            {
                Log.D("UI.MainPages.TimingPage", "Reader type has changed.");
                string type = ((ComboBoxItem)ReaderType.SelectedItem).Uid;
                Log.D("UI.MainPages.TimingPage", "Updating to type: " + Constants.Readers.SYSTEM_NAMES[type]);
                reader.UpdateSystemType(type);
                ReaderPort.Text = reader.Port.ToString();
                ReaderPort.IsEnabled = Constants.Readers.SYSTEM_CHRONOKEEP_PORTAL == type ? false : true;
            }

            private void Remove(object sender, RoutedEventArgs e)
            {
                Log.D("UI.MainPages.TimingPage", "Remove button for a timing system has been clicked.");
                if (reader.Saved())
                {
                    parent.RemoveSystem(reader);
                };
            }

            private void Settings(object sender, RoutedEventArgs e)
            {
                if (reader.SystemInterface.SettingsEditable())
                {
                    reader.SystemInterface.OpenSettings();
                }
                else
                {
                    DialogBox.Show("Settings not yet implemented.");
                }
            }

            private void Connect(object sender, RoutedEventArgs e)
            {
                if ("connect" != ConnectButton.Uid)
                {
                    Log.D("UI.MainPages.TimingPage", "Disconnect pressed.");
                    reader.Status = SYSTEM_STATUS.WORKING;
                    parent.DisconnectSystem(reader);
                    UpdateStatus();
                    reader.SystemInterface.CloseSettings();
                    return;
                }
                Log.D("UI.MainPages.TimingPage", "Connect button pressed. IP is " + ReaderIP.Text);
                // Check if IP is a valid IP address
                if (!Regex.IsMatch(ReaderIP.Text.Trim(), IPPattern))
                {
                    DialogBox.Show("IP address given not valid.");
                    return;
                }
                reader.IPAddress = ReaderIP.Text.Trim();
                // Check if Port is valid.
                int portNo = -1;
                int.TryParse(ReaderPort.Text.Trim(), out portNo);
                if (portNo < 0 || portNo > 65535)
                {
                    DialogBox.Show("Port given not valid.");
                    return;
                }
                reader.Port = portNo;
                reader.LocationID = Convert.ToInt32(((ComboBoxItem)ReaderLocation.SelectedItem).Uid);
                reader.LocationName = ((ComboBoxItem)ReaderLocation.SelectedItem).Content.ToString();
                reader.Status = SYSTEM_STATUS.WORKING;
                parent.ConnectSystem(reader);
                UpdateStatus();
            }

            private void SetConnected()
            {
                ReaderType.IsEnabled = false;
                ReaderIP.IsEnabled = false;
                ReaderPort.IsEnabled = false;
                ReaderLocation.IsEnabled = false;
                RemoveButton.IsEnabled = false;
                RemoveButton.Opacity = 0.2;
                if (reader.Type.Equals(Constants.Readers.SYSTEM_IPICO_LITE, StringComparison.OrdinalIgnoreCase))
                {
                    RewindButton.IsEnabled = false;
                    ClockButton.IsEnabled = false;
                    SettingsButton.IsEnabled = false;
                    RewindButton.Opacity = 0.2;
                    ClockButton.Opacity = 0.2;
                    SettingsButton.Opacity = 0.2;
                }
                else
                {
                    RewindButton.IsEnabled = true;
                    ClockButton.IsEnabled = true;
                    RewindButton.Opacity = 1.0;
                    ClockButton.Opacity = 1.0;
                    if (reader.SystemInterface.SettingsEditable())
                    {
                        SettingsButton.IsEnabled = true;
                        SettingsButton.Opacity = 1.0;
                    }
                    else
                    {
                        SettingsButton.IsEnabled = false;
                        SettingsButton.Opacity = 0.2;
                    }
                }
                ConnectButton.IsEnabled = true;
                ConnectButton.Opacity = 1.0;
                ConnectButton.Icon = new Wpf.Ui.Controls.SymbolIcon() { Symbol = Wpf.Ui.Controls.SymbolRegular.Stop24 };
                ConnectButton.Uid = "disconnect";
            }

            private void SetDisconnected()
            {
                ReaderType.IsEnabled = true;
                ReaderIP.IsEnabled = true;
                ReaderPort.IsEnabled = Constants.Readers.SYSTEM_CHRONOKEEP_PORTAL == reader.Type ? false : true;
                ReaderLocation.IsEnabled = true;
                // Set Remove and Connect buttons to enabled
                RemoveButton.IsEnabled = true;
                ConnectButton.IsEnabled = true;
                RemoveButton.Opacity = 1.0;
                ConnectButton.Opacity = 1.0;
                // Set Clock and Rewind Buttons to disabled
                ClockButton.IsEnabled = false;
                RewindButton.IsEnabled = false;
                SettingsButton.IsEnabled = false;
                ClockButton.Opacity = 0.2;
                RewindButton.Opacity = 0.2;
                SettingsButton.Opacity = 0.2;
                ConnectButton.Icon = new Wpf.Ui.Controls.SymbolIcon() { Symbol = Wpf.Ui.Controls.SymbolRegular.Play24 };
                ConnectButton.Uid = "connect";
            }

            private void SetWorking()
            {
                ReaderType.IsEnabled = false;
                ReaderIP.IsEnabled = false;
                ReaderPort.IsEnabled = false;
                ReaderLocation.IsEnabled = false;
                ClockButton.IsEnabled = false;
                RewindButton.IsEnabled = false;
                ConnectButton.IsEnabled = false;
                RemoveButton.IsEnabled = false;
                SettingsButton.IsEnabled = false;
                ClockButton.Opacity = 0.2;
                RewindButton.Opacity = 0.2;
                ConnectButton.Opacity = 0.2;
                RemoveButton.Opacity = 0.2;
                SettingsButton.Opacity = 0.2;
                ConnectButton.Icon = new Wpf.Ui.Controls.SymbolIcon() { Symbol = Wpf.Ui.Controls.SymbolRegular.CatchUp24 };
                ConnectButton.Uid = "working";
            }

            private void Rewind(object sender, RoutedEventArgs e)
            {
                Log.D("UI.MainPages.TimingPage", "Settings button pressed. IP is " + ReaderIP.Text);
                parent.OpenRewindWindow(reader);
            }

            private void Clock(object sender, RoutedEventArgs e)
            {
                Log.D("UI.MainPages.TimingPage", "Clock button pressed. IP is " + ReaderIP.Text);
                parent.OpenTimeWindow(reader);
            }

            internal void UpdateSystemType(string type, IDBInterface database)
            {
                reader.UpdateSystemType(type);
                this.ReaderPort.Text = reader.Port.ToString();
            }
        }
    }
}
