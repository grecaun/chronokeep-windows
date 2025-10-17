using Chronokeep.Constants;
using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.IO;
using Chronokeep.IO.HtmlTemplates;
using Chronokeep.Network.API;
using Chronokeep.Objects;
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
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using static Chronokeep.Helpers.Globals;
using TextBox = System.Windows.Controls.TextBox;
using UiButton = Wpf.Ui.Controls.Button;

namespace Chronokeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for TimingPage.xaml
    /// </summary>
    public partial class TimingPage : IMainPage, ITimingPage
    {
        private readonly IMainWindow mWindow;
        private readonly IDBInterface database;
        private ISubPage subPage;

        private CancellationTokenSource cts;

        private readonly Event theEvent;
        private readonly List<TimingLocation> locations;

        private DateTime startTime;
        private readonly DispatcherTimer Timer = new();
        private bool TimerStarted = false;
        private SetTimeWindow timeWindow = null;
        private RewindWindow rewindWindow = null;

        private static bool alreadyRecalculating = false;
        private static readonly int uploadTimer = 1000;

        private readonly ObservableCollection<DistanceStat> stats = [];

        private int total = 0, known = 0;

        private const string ipformat = "{0:D}.{1:D}.{2:D}.{3:D}";
        private readonly int[] baseIP = { 0, 0, 0, 0 };

        private readonly bool remote_api = false;

        private readonly Dictionary<int, (long seconds, int milliseconds)> waveTimes = [];
        private readonly HashSet<int> waves = [];
        private int selectedWave = -1;
        private readonly List<TimeRelativeWave> relativeToWaveList = [];

        [GeneratedRegex(@"[^a-z0-9\-]")]
        private static partial Regex FileSaveRegex();

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
            Timer.Tick += new EventHandler(Timer_Tick);
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
            EllapsedRelativeToBox.ItemsSource = relativeToWaveList;
            relativeToWaveList.Clear();
            relativeToWaveList.Add(new()
            {
                Name = "Start Time",
                Wave = -1
            });
            foreach (Distance div in database.GetDistances(theEvent.Identifier))
            {
                relativeToWaveList.Add(new()
                {
                    Name = div.Name + " (Wave " + div.Wave + ")",
                    Wave = div.Wave
                });
                waveTimes[div.Wave] = (div.StartOffsetSeconds, div.StartOffsetMilliseconds);
                waves.Add(div.Wave);
            }
            EllapsedRelativeToBox.SelectedIndex = 0;

            // Populate the list of readers with connected readers (or at least 4 readers)
            ReadersBox.Items.Clear();
            locations = database.GetTimingLocations(theEvent.Identifier);
            int locCount = locations.Count;
            if (!theEvent.CommonStartFinish)
            {
                locations.Insert(0, new(Constants.Timing.LOCATION_ANNOUNCER, theEvent.Identifier, "Announcer", 0, 0));
                locations.Insert(0, new(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                locations.Insert(0, new(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow));
            }
            else
            {
                locations.Insert(0, new(Constants.Timing.LOCATION_ANNOUNCER, theEvent.Identifier, "Announcer", 0, 0));
                locations.Insert(0, new(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
            }

            locationBox.Items.Clear();
            if (locCount > 0)
            {
                locationBox.Items.Add(new ComboBoxItem()
                {
                    Content = "All Locations"
                });
                foreach (TimingLocation loc in locations)
                {
                    if (!loc.Name.Equals("Announcer", StringComparison.OrdinalIgnoreCase))
                    {
                        locationBox.Items.Add(new ComboBoxItem()
                        {
                            Content = loc.Name,
                        });
                    }
                }
                locationBox.SelectedIndex = 0;
                locationBox.Visibility = Visibility.Visible;
            }
            else
            {
                locationBox.Visibility = Visibility.Collapsed;
            }

            List<TimingSystem> systems = mWindow.GetConnectedSystems();
            int numSystems = systems.Count;
            string system = Readers.DEFAULT_TIMING_SYSTEM;
            try
            {
                system = database.GetAppSetting(Settings.DEFAULT_TIMING_SYSTEM).Value;
            }
            catch
            {
                Log.D("UI.MainPages.TimingPage", "Error fetching default timing system information.");
                system = Readers.DEFAULT_TIMING_SYSTEM;
            }
            if (numSystems < 3)
            {
                Log.D("UI.MainPages.TimingPage", systems.Count + " systems found.");
                for (int i = 0; i < 3 - numSystems; i++)
                {
                    systems.Add(new(string.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]), system));
                }
            }
            systems.Sort((x, y) =>
            {
                return x.Status == y.Status ? x.IPAddress.CompareTo(y.IPAddress) : x.Status.CompareTo(y.Status);
            });
            systems.Add(new(string.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]), system));
            known = 0;
            foreach (TimingSystem sys in systems)
            {
                ReadersBox.Items.Add(new AReaderBox(this, sys, locations));
                if (sys.IPAddress != string.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]))
                {
                    known++;
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
                if (api.Type == APIConstants.CHRONOKEEP_REMOTE_SELF || api.Type == APIConstants.CHRONOKEEP_REMOTE)
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

            List<ReaderMessage> readerMsgs = GetReaderMessages();
            if (readerMsgs.Count > 0)
            {
                ReaderMessageButton.Visibility = Visibility.Visible;
                ReaderMessageNumberBox.Value = readerMsgs.FindAll(x => !x.Notified).Count.ToString();
            }
            else
            {
                ReaderMessageButton.Visibility = Visibility.Hidden;
                ReaderMessageNumberBox.Value = 0.ToString();
            }

            if (alreadyRecalculating)
            {
                recalculateButton.Content = "Working...";
            }
            else
            {
                recalculateButton.Content = "Recalculate";
            }
        }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public void UpdateDatabase() { }

        public void Closing()
        {
            List<TimingSystem> removedSystems = database.GetTimingSystems();
            List<TimingSystem> ourSystems = [];
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
            if (theEvent == null || theEvent.Identifier == -1)
            {
                // Something went wrong and this shouldn't be visible.
                return;
            }
            if (TimerStarted)
            {
                UpdateStartTime();
            }

            // Update locations in the list of readers (and reader status)
            total = ReadersBox.Items.Count; known = 0;
            foreach (AReaderBox read in ReadersBox.Items)
            {
                read.UpdateStatus();
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
                if (read.reader.IPAddress != string.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]))
                {
                    known++;
                }
            }

            if (total < 4 || known >= total)
            {
                string system = Readers.DEFAULT_TIMING_SYSTEM;
                try
                {
                    system = database.GetAppSetting(Settings.DEFAULT_TIMING_SYSTEM).Value;
                }
                catch
                {
                    Log.D("UI.MainPages.TimingPage", "Error fetching default timing system information.");
                    system = Readers.DEFAULT_TIMING_SYSTEM;
                }
                for (int i = total; i < 3; i++)
                {
                    ReadersBox.Items.Add(new AReaderBox(
                        this,
                        new(string.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]),
                            system),
                            locations));
                }
                ReadersBox.Items.Add(new AReaderBox(
                    this,
                    new(string.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]),
                        system),
                        locations));
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
            HashSet<int> newWaves = [];
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
                relativeToWaveList.Clear();
                relativeToWaveList.Add(new()
                {
                    Name = "Start Time",
                    Wave = -1
                });
                foreach (Distance div in database.GetDistances(theEvent.Identifier))
                {
                    relativeToWaveList.Add(new()
                    {
                        Name = div.Name + " (Wave " + div.Wave + ")",
                        Wave = div.Wave
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
            List<ReaderMessage> readerMsgs = GetReaderMessages();
            if (readerMsgs.Count() > 0)
            {
                ReaderMessageButton.Visibility = Visibility.Visible;
                ReaderMessageNumberBox.Value = readerMsgs.FindAll(x => !x.Notified).Count().ToString();
            }
            else
            {
                ReaderMessageButton.Visibility = Visibility.Hidden;
                ReaderMessageNumberBox.Value = 0.ToString();
            }
            UpdateSubView();
            if (alreadyRecalculating)
            {
                recalculateButton.Content = "Working...";
            }
            else
            {
                recalculateButton.Content = "Recalculate";
            }
        }

        public void UpdateSubView()
        {
            Log.D("UI.MainPages.TimingPage", "Updating sub view.");
            if (cts != null)
            {
                cts.Cancel();
                cts = null;
            }
            cts = new();
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

        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeSpan ellapsed = DateTime.Now - startTime;
            if (waveTimes.TryGetValue(selectedWave, out (long seconds, int milliseconds) value))
            {
                ellapsed = ellapsed.Subtract(TimeSpan.FromSeconds(value.seconds));
                ellapsed = ellapsed.Subtract(TimeSpan.FromMilliseconds(value.milliseconds));
            }
            EllapsedTime.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", Math.Abs(ellapsed.Days) * 24 + Math.Abs(ellapsed.Hours), Math.Abs(ellapsed.Minutes), Math.Abs(ellapsed.Seconds));
        }

        private void StartRaceClick(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Starting race.");
            StartTime.Text = DateTime.Now.ToString("HH:mm:ss.fff");
            StartRace.IsEnabled = false;
            EllapsedRelativeToBox.IsEnabled = true;
            if (waves.Count > 1)
            {
                EllapsedRelativeToBox.Visibility = Visibility.Visible;
            }
            else
            {
                EllapsedRelativeToBox.Visibility = Visibility.Collapsed;
            }
            StartTimeChanged();
        }

        private void ChangeWaves(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Set Wave Times clicked.");
            WaveWindow waves = new(mWindow, database);
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
            OpenFileDialog csv_dialog = new() { Filter = "Log Files (*.csv,*.txt,*.log)|*.csv;*.txt;*.log|All Files|*" };
            if (csv_dialog.ShowDialog() == true)
            {
                try
                {
                    LogImporter importer = new(csv_dialog.FileName);
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
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    Log.D("UI.MainPages.TimingPage", "User wants to reset start time value.");
                    theEvent.StartSeconds = 0;
                    theEvent.StartMilliseconds = 0;
                    if (TimerStarted)
                    {
                        TimerStarted = false;
                        Timer.Stop();
                    }
                    database.UpdateEvent(theEvent);
                    StartTime.Text = "";
                    EllapsedTime.Text = "00:00:00";
                    StartRace.IsEnabled = true;
                    EllapsedRelativeToBox.IsEnabled = false;
                    EllapsedRelativeToBox.Visibility = Visibility.Collapsed;
                    return;
                }
                Log.D("UI.MainPages.TimingPage", "Start Time Box return key found.");
                UpdateStartTime();
            }
        }

        private void StartTimeLostFocus(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", $"Start Time Box has lost focus. {StartTime.Text}");
            if (StartTime.Text.Any(char.IsDigit))
            {
                StartTimeChanged();
            }
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
                database.UpdateStart(); // This is a MemStore specific database call that updates the Start value for ChipReads.
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
            EllapsedRelativeToBox.IsEnabled = true;
            if (waves.Count > 1)
            {
                EllapsedRelativeToBox.Visibility = Visibility.Visible;
            }
            else
            {
                EllapsedRelativeToBox.Visibility = Visibility.Collapsed;
            }
            StartTime.Text = startTimeValue;
            Log.D("UI.MainPages.TimingPage", "Start time is " + startTimeValue);
            startTime = DateTime.ParseExact(startTimeValue + DateTime.Parse(theEvent.Date).ToString("ddMMyyyy"), "HH:mm:ss.fffddMMyyyy", null);
            Log.D("UI.MainPages.TimingPage", "Start time is " + startTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }

        public void NewMessage()
        {
            timeWindow?.UpdateTime();
        }

        public void OpenTimeWindow(TimingSystem system)
        {
            Log.D("UI.MainPages.TimingPage", "Opening Set Time Window.");
            timeWindow = new(this, system);
            timeWindow.ShowDialog();
            timeWindow = null;
        }

        public void OpenRewindWindow(TimingSystem system)
        {
            Log.D("UI.MainPages.TimingPage", "Opening Rewind Window.");
            rewindWindow = new(system);
            rewindWindow.ShowDialog();
            rewindWindow = null;

        }

        public void SetAllTimingSystemsToTime(DateTime time, bool now)
        {
            List<TimingSystem> systems = mWindow.GetConnectedSystems();
            foreach (TimingSystem sys in systems)
            {
                try
                {
                    if (sys.Status == SYSTEM_STATUS.CONNECTED)
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
                catch (Exception e)
                {
                    Log.E("TimingPage", $"Error setting time on timing system via set all. {e.Message}");
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
            return sys.Status != SYSTEM_STATUS.DISCONNECTED;
        }

        public bool DisconnectSystem(TimingSystem sys)
        {
            mWindow.DisconnectTimingSystem(sys);
            return sys.Status == SYSTEM_STATUS.DISCONNECTED;
        }

        private void RawReads_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Raw Reads selected.");
            if (RawButton.Content.ToString().Equals("Raw Data", StringComparison.OrdinalIgnoreCase))
            {
                RawButton.Content = "Refresh Data";
                subPage = new TimingRawReadsPage(this, database);
                TimingFrame.NavigationService.RemoveBackEntry();
                TimingFrame.Content = subPage;
            }
            else if (subPage is TimingRawReadsPage rawReadsPage)
            {
                // Refresh data
                rawReadsPage.PrivateUpdateView();
            }
            else
            {
                SetRawReadsFinished();
            }
        }

        internal void SetRawReadsFinished()
        {
            RawButton.Content = "Raw Data";
        }

        public void LoadMainDisplay()
        {
            Log.D("UI.MainPages.TimingPage", "Going back to main display.");
            SetRawReadsFinished();
            subPage = new TimingResultsPage(this, database);
            TimingFrame.NavigationService.RemoveBackEntry();
            TimingFrame.Content = subPage;
        }

        private async void Recalculate_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Recalculate results clicked.");
            if ((string)recalculateButton.Content == "Working..." || alreadyRecalculating)
            {
                return;
            }
            recalculateButton.Content = "Working...";
            alreadyRecalculating = true;
            if (APIController.SetUploadableFalse(uploadTimer))
            {
                bool canRecalculate = await Task<bool>.Run(() =>
                {
                    int counter = 0;
                    while (true)
                    {
                        if (counter > 5)
                        {
                            return false;
                        }
                        if (!APIController.IsUploading())
                        {
                            return true;
                        }
                        counter++;
                        //Log.D("UI.MainPages.TimingPage", "APIController is uploading. Sleeping for 1 second. Counter is " + counter.ToString());
                        Thread.Sleep(1000);
                    };
                });
                if (!canRecalculate)
                {
                    await Task.Run(() => {
                        int counter = 0;
                        while (!APIController.SetUploadableTrue(uploadTimer))
                        {
                            counter++;
                            if (counter > 5)
                            {
                                break;
                            }
                        }
                    });
                    recalculateButton.Content = "Recalculate";
                    alreadyRecalculating = false;
                    DialogBox.Show("Unable to recalculate results.");
                    return;
                }
            }
            else
            {
                recalculateButton.Content = "Recalculate";
                alreadyRecalculating = false;
                return;
            }
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
            if (event_ids.Length == 2 && api != null)
            {
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
            await Task.Run(() => {
                int counter = 0;
                while (!APIController.SetUploadableTrue(uploadTimer))
                {
                    counter++;
                    if (counter > 5)
                    {
                        break;
                    }
                }
            });
            recalculateButton.Content = "Recalculate";
            alreadyRecalculating = false;
            UpdateSubView();
            mWindow.NetworkClearResults();
            mWindow.NotifyTimingWorker();
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
                case "Clock Time":
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

        public string GetLocation()
        {
            ComboBoxItem locItem = (ComboBoxItem)locationBox.SelectedItem;
            if (locItem == null)
            {
                return "";
            }
            return locItem.Content.ToString();
        }

        private void SearchBox_TextChanged(Wpf.Ui.Controls.AutoSuggestBox sender, Wpf.Ui.Controls.AutoSuggestBoxTextChangedEventArgs args)
        {
            Log.D("UI.MainPages.TimingPage", "Searchbox text has changed");
            if (cts != null)
            {
                cts.Cancel();
                cts = null;
            }
            cts = new();
            try
            {
                subPage.Search(cts.Token, searchBox.Text.Trim());
                cts = null;
            }
            catch
            {
                Log.D("UI.MainPages.TimingPage", "Update cancelled.");
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Export clicked.");
            ExportResults exportResults = new(mWindow, database);
            if (!exportResults.SetupError())
            {
                mWindow.AddWindow(exportResults);
                exportResults.ShowDialog();
            }
        }

        private void Export_Abbott_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Export Abbott Clicked.");
            ExportDistanceResults exportAbbott = new(mWindow, database, OutputType.Abbott);
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
            ExportDistanceResults exportBAA = new(mWindow, database, OutputType.Boston);
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
            ExportDistanceResults exportUS = new(mWindow, database, OutputType.UltraSignup);
            if (!exportUS.SetupError())
            {
                mWindow.AddWindow(exportUS);
                exportUS.ShowDialog();
            } 
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Print clicked.");
            SetRawReadsFinished();
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
            SetRawReadsFinished();
            subPage = new DistanceStatsPage(this, mWindow, database, selected.DistanceID, selected.DistanceName);
            TimingFrame.NavigationService.RemoveBackEntry();
            TimingFrame.Content = subPage;

        }

        private void Award_Click (object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Awards clicked.");
            SetRawReadsFinished();
            subPage = new AwardPage(this, database);
            TimingFrame.NavigationService.RemoveBackEntry();
            TimingFrame.Content = subPage;
        }

        private void CreateHTML_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Create HTML clicked.");
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "HTML file (*.htm,*.html)|*.htm;*.html",
                FileName = string.Format("{0} {1} Web.{2}", theEvent.YearCode, theEvent.Name, "html"),
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                List<TimeResult> finishResults = database.GetFinishTimes(theEvent.Identifier);
                Dictionary<int, Participant> partDict = database.GetParticipants(theEvent.Identifier).ToDictionary(v => v.EventSpecific.Identifier, v => v);
                HtmlResultsTemplate template = new(theEvent, finishResults);
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

        private async void ManualAPI_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Manual API clicked.");
            if (ManualAPIButton.Content.ToString() != "Uploading")
            {
                Log.D("UI.MainPages.TimingPage", "Uploading data.");
                ManualAPIButton.Content = "Uploading";
                await Task.Run(() =>
                {
                    UploadResults();
                });
                return;
            }
            Log.D("UI.MainPages.TimingPage", "Already uploading.");
        }

        private async void UploadResults()
        {
            // Get API to upload.
            if (theEvent.API_ID < 0 && theEvent.API_Event_ID.Length > 1)
            {
                return;
            }
            APIObject api = database.GetAPI(theEvent.API_ID);
            string[] event_ids = theEvent.API_Event_ID.Split(',');
            if (event_ids.Length != 2)
            {
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
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
                {
                    ManualAPIButton.Content = "Manual Upload";
                }));
                return;
            }
            // Upload results
            Log.D("UI.MainPages.TimingPage", "Results count: " + results.Count.ToString());
            if (APIController.GetUploadable(3000))
            {
                await APIController.UploadResults(results, api, event_ids, database, null, null, theEvent);
            }
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                ManualAPIButton.Content = "Manual Upload";
            }));
        }

        private void SaveLog(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Save Log clicked.");
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = string.Format("{0} {1} Log.{2}", theEvent.YearCode, theEvent.Name, "csv"),
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                Dictionary<string, List<ChipRead>> locationReadDict = [];
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
                    if (!locationReadDict.TryGetValue(read.LocationName, out List<ChipRead> locChipReads))
                    {
                        locChipReads = [];
                        locationReadDict[read.LocationName] = locChipReads;
                    }

                    locChipReads.Add(read);
                }
                StringBuilder format = new();
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
                    List<object[]> data = [];
                    foreach (ChipRead read in chipReads)
                    {
                        data.Add([
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
                        ]);
                    }
                    CSVExporter exporter = new(format.ToString());
                    exporter.SetData(headers, data);
                    exporter.ExportData(saveFileDialog.FileName);
                }
                // Multiple locations, save each individually.
                else
                {
                    foreach (string key in locationReadDict.Keys)
                    {
                        List<object[]> data = [];
                        foreach (ChipRead read in locationReadDict[key])
                        {
                            data.Add([
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
                        ]);
                        }
                        CSVExporter exporter = new(format.ToString());
                        exporter.SetData(headers, data);
                        string outFileName = string.Format("{0}\\{1}-{2}", Path.GetDirectoryName(saveFileDialog.FileName), FileSaveRegex().Replace(key.ToLower(), ""), Path.GetFileName(saveFileDialog.FileName));
                        Log.D("UI.MainPages.TimingPage", string.Format("Saving file to: {0}", outFileName));
                        exporter.ExportData(outFileName);
                    }
                }
                DialogBox.Show("File saved.");
            }
        }

        private void StatsListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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

        private void DnsMode_Click(object sender, RoutedEventArgs e)
        {
            bool worked;
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
            SetRawReadsFinished();
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

        private void RemoteControllerSwitch_Checked(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Remote toggle switch checked.");
            remoteControllerSwitch.IsEnabled = false;
            mWindow.StartRemote();

        }

        private void RemoteControllerSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Remote toggle switch unchecked.");
            remoteControllerSwitch.IsEnabled = false;
            mWindow.StopRemote();
        }

        private void EllapsedRelativeToBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "EllapsedRelativeToBox selection changed.");
            selectedWave = -1;
            if (EllapsedRelativeToBox.SelectedIndex >= 0 && EllapsedRelativeToBox.SelectedItem is TimeRelativeWave wave)
            {
                selectedWave = wave.Wave;
            }
        }

        private void ModifySMSButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Modify SMS button clicked.");
            SMSWaveEnabledWindow smsWindow = new(mWindow, database);
            smsWindow.Show();
        }

        private async void SendEmailsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Send Emails button clicked.");
            if ((string)sendEmailsButton.Content != "Send Emails")
            {
                return;
            }
            sendEmailsButton.Content = "Sending...";
            await Task.Run(() =>
            {
                HashSet<int> sentIDs = [];
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
                Dictionary<string, Participant> participantDictionary = [];
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
                GlobalVars.UpdateBannedEmails();
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
                    Participant part = participantDictionary.TryGetValue(result.ParticipantId, out Participant oPart) ? oPart : null;
                    if (part != null && result.EventSpecificId != Constants.Timing.EVENTSPECIFIC_UNKNOWN)
                    {
                        if (part.Email.Length > 0 && !GlobalVars.BannedEmails.Contains(part.Email) && !sentIDs.Contains(result.EventSpecificId))
                        {
                            MultipartFormDataContent postData = new()
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

        private void ReaderMessageButton_Click(object sender, RoutedEventArgs e)
        {
            ReaderNotificationWindow notificationWindow = ReaderNotificationWindow.NewWindow(mWindow);
            notificationWindow.Show();
        }

        private partial class AReaderBox : ListBoxItem
        {
            public ComboBox ReaderType { get; private set; }
            public TextBox ReaderIP { get; private set; }
            public TextBox ReaderPort { get; private set; }
            public ComboBox ReaderLocation { get; private set; }
            public UiButton ConnectButton { get; private set; }
            public UiButton ClockButton { get; private set; }
            public UiButton RewindButton { get; private set; }
            public UiButton SettingsButton { get; private set; }
            public UiButton RemoveButton { get; private set; }

            readonly TimingPage parent;
            private List<TimingLocation> locations;
            public TimingSystem reader;

            public RewindWindow rewind = null;

            [GeneratedRegex("^([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])$")]
            private static partial Regex IPPattern();
            [GeneratedRegex("[^0-9.]")]
            private static partial Regex AllowedChars();
            [GeneratedRegex("[^0-9]")]
            private static partial Regex AllowedNums();

            public AReaderBox(TimingPage window, TimingSystem sys, List<TimingLocation> locations)
            {
                parent = window;
                this.locations = locations;
                reader = sys;
                Grid thePanel = new()
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
                Content = thePanel;
                ReaderType = new ComboBox()
                {
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(5, 5, 5, 5),
                    Height = 40
                };
                ComboBoxItem current = null, selected = null;
                foreach (string SYSTEM_IDVAL in Readers.SYSTEM_NAMES.Keys)
                {
                    current = new ComboBoxItem()
                    {
                        Content = Readers.SYSTEM_NAMES[SYSTEM_IDVAL],
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
                ClockButton = new()
                {
                    Icon = new SymbolIcon() { Symbol = SymbolRegular.Clock24 },
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
                RewindButton = new()
                {
                    Icon = new SymbolIcon() { Symbol = SymbolRegular.Rewind24 },
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
                SettingsButton = new()
                {
                    Icon = new SymbolIcon() { Symbol = SymbolRegular.Settings24 },
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 40
                };
                SettingsButton.Click += new RoutedEventHandler(this.Settings);
                thePanel.Children.Add(SettingsButton);
                Grid.SetColumn(SettingsButton, 6);
                ConnectButton = new()
                {
                    Icon = new SymbolIcon() { Symbol = SymbolRegular.Play24 },
                    Uid = "connect",
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 40
                };
                ConnectButton.Click += new RoutedEventHandler(this.Connect);
                thePanel.Children.Add(ConnectButton);
                Grid.SetColumn(ConnectButton, 7);
                RemoveButton = new()
                {
                    Icon = new SymbolIcon() { Symbol = SymbolRegular.Delete24 },
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
                ComboBoxItem current, selected = null;
                foreach (TimingLocation loc in this.locations)
                {
                    current = new()
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
                ChangeReadingStatus(reader.SystemStatus);
            }

            private void SelectAll(object sender, RoutedEventArgs e)
            {
                TextBox src = (TextBox)e.OriginalSource;
                src.SelectAll();
            }

            private void IPValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = AllowedChars().IsMatch(e.Text);
            }

            private void NumberValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = AllowedNums().IsMatch(e.Text);
            }

            public void UpdateReader()
            {
                // Check if IP is a valid IP address
                if (!IPPattern().IsMatch(ReaderIP.Text.Trim()))
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
                Log.D("UI.MainPages.TimingPage", "Updating to type: " + Readers.SYSTEM_NAMES[type]);
                reader.UpdateSystemType(type);
                ReaderPort.Text = reader.Port.ToString();
                ReaderPort.IsEnabled = Readers.SYSTEM_CHRONOKEEP_PORTAL != type;
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
                if (reader == null || reader.SystemInterface == null)
                {
                    return;
                }
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control
                    || (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                {
                    if (reader.SystemStatus == TimingSystem.READING_STATUS_READING
                        || reader.SystemStatus == TimingSystem.READING_STATUS_PARTIAL)
                    {
                        reader.SystemInterface.StopReading();
                    }
                    else if (reader.SystemStatus == TimingSystem.READING_STATUS_STOPPED)
                    {
                        reader.SystemInterface.StartReading();
                    }
                    return;
                }
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
                if (!IPPattern().IsMatch(ReaderIP.Text.Trim()))
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
                if (reader.Type.Equals(Readers.SYSTEM_IPICO_LITE, StringComparison.OrdinalIgnoreCase))
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
                ConnectButton.Icon = new SymbolIcon() { Symbol = SymbolRegular.Stop24 };
                ConnectButton.Uid = "disconnect";
            }

            private void SetDisconnected()
            {
                ReaderType.IsEnabled = true;
                ReaderIP.IsEnabled = true;
                ReaderPort.IsEnabled = Readers.SYSTEM_CHRONOKEEP_PORTAL != reader.Type;
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
                ConnectButton.Icon = new SymbolIcon() { Symbol = SymbolRegular.Play24 };
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
                ConnectButton.Icon = new SymbolIcon() { Symbol = SymbolRegular.CatchUp24 };
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

            private void ChangeReadingStatus(string status)
            {
                if (status == TimingSystem.READING_STATUS_STOPPED)
                {
                    SettingsButton.Foreground = new SolidColorBrush(Colors.Red);
                }
                else if (status == TimingSystem.READING_STATUS_READING)
                {
                    SettingsButton.Foreground = new SolidColorBrush(Colors.LimeGreen);
                }
                else if (status == TimingSystem.READING_STATUS_PARTIAL)
                {
                    SettingsButton.Foreground = new SolidColorBrush(Colors.Violet);
                }
                else
                {
                    SettingsButton.SetResourceReference(ForegroundProperty, "TextFillColorPrimaryBrush");
                }
            }

            internal void UpdateSystemType(string type, IDBInterface database)
            {
                reader.UpdateSystemType(type);
                this.ReaderPort.Text = reader.Port.ToString();
            }
        }

        private void Export_Runsignup_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Export Runsignup Clicked.");
            if (theEvent.EventType == Constants.Timing.EVENT_TYPE_TIME)
            {
                DialogBox.Show("Exporting time based events not supported.");
                return;
            }
            ExportDistanceResults exportRunsignup = new(mWindow, database, OutputType.Runsignup);
            if (!exportRunsignup.SetupError())
            {
                mWindow.AddWindow(exportRunsignup);
                exportRunsignup.ShowDialog();
            }
        }

        private void LocationBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (subPage == null)
            {
                return;
            }
            ComboBoxItem locItem = (ComboBoxItem)locationBox.SelectedItem;
            if (locItem == null)
            {
                subPage.Location("");
            }
            else
            {
                subPage.Location(locItem.Content.ToString());
            }
        }

        private void ReaderSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (subPage == null) return;
            string readerItem = (string) readerSelectionBox.SelectedItem;
            if (readerItem == null)
            {
                subPage.Reader("");
            }
            else
            {
                subPage.Reader(readerItem);
            }
        }

        public void SetReaders(string[] readers, bool visible)
        {
            readerSelectionBox.Items.Clear();
            foreach (string reader in readers)
            {
                readerSelectionBox.Items.Add(reader);
            }
            readerSelectionBox.SelectedIndex = 0;
            readerSelectionBox.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public string GetReader()
        {
            return readerSelectionBox.SelectedItem != null ? readerSelectionBox.SelectedItem.ToString() : "";
        }

        private void OpenClock_Click(object sender, RoutedEventArgs e)
        {
            ClockControl clockWindow = ClockControl.CreateWindow(mWindow, database);
            clockWindow.Show();
        }

        public class TimeRelativeWave
        {
            public string Name { get; set; }
            public int Wave { get; set; }
        }
    }
}
