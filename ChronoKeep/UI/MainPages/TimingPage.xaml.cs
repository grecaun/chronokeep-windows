using ChronoKeep.API;
using ChronoKeep.Interfaces;
using ChronoKeep.IO;
using ChronoKeep.IO.HtmlTemplates;
using ChronoKeep.Network.API;
using ChronoKeep.Objects;
using ChronoKeep.Objects.API;
using ChronoKeep.UI.Export;
using ChronoKeep.UI.IO;
using ChronoKeep.UI.Timing;
using ChronoKeep.UI.Timing.Import;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ChronoKeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for TimingPage.xaml
    /// </summary>
    public partial class TimingPage : IMainPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private ISubPage subPage;

        private CancellationTokenSource cts;

        private Event theEvent;
        List<TimingLocation> locations;
        List<TimeResult> results = new List<TimeResult>();

        private DateTime startTime;
        DispatcherTimer Timer = new DispatcherTimer();
        DispatcherTimer ViewUpdateTimer = new DispatcherTimer();
        private Boolean TimerStarted = false;
        private SetTimeWindow timeWindow = null;

        ObservableCollection<DistanceStat> stats = new ObservableCollection<DistanceStat>();

        int total = 4, connected = 0;

        private const string ipformat = "{0:D}.{1:D}.{2:D}.{3:D}";
        private int[] baseIP = { 0, 0, 0, 0 };

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

            // Setup a timer for updating the view
            ViewUpdateTimer.Tick += new EventHandler(ViewUpdateTimer_Click);
            ViewUpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);

            IPAdd.Content = "localhost";
            Port.Content = "6933";
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
                                IPAdd.Content = ipinfo.Address;
                                Log.D("IP Address :" + ipinfo.Address);
                                Log.D("IPv4 Mask  :" + ipinfo.IPv4Mask);
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
                StartTime.Text = String.Format("{0:D2}:{1:D2}:{2:D2}.{3:D3}",
                    theEvent.StartSeconds / 3600, (theEvent.StartSeconds % 3600) / 60, theEvent.StartSeconds % 60, theEvent.StartMilliseconds);
                UpdateStartTime();
            }

            // Populate the list of readers with connected readers (or at least 4 readers)
            ReadersBox.Items.Clear();
            locations = database.GetTimingLocations(theEvent.Identifier);
            if (!theEvent.CommonStartFinish)
            {
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow));
            }
            else
            {
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
            }
            List<TimingSystem> systems = mWindow.GetConnectedSystems();
            int numSystems = systems.Count;
            if (numSystems < 3)
            {
                Log.D(systems.Count + " systems found.");
                for (int i = 0; i < 3 - numSystems; i++)
                {
                    systems.Add(new TimingSystem(String.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]), Constants.Settings.TIMING_RFID));
                }
            }
            systems.Add(new TimingSystem(String.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]), Constants.Settings.TIMING_RFID));
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
            }
            else
            {
                AutoAPIButton.Content = "Auto Upload";
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
                    box.reader.IPAddress != String.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]))
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
            ViewUpdateTimer.Stop();
        }

        public void UpdateView()
        {
            Log.D("Updating timing information.");
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
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow));
            }
            else
            {
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
            }

            // Update locations in the list of readers
            connected = 0; total = ReadersBox.Items.Count;
            foreach (AReaderBox read in ReadersBox.Items)
            {
                read.UpdateLocations(locations);
                read.UpdateStatus();
                connected += read.reader.Status == SYSTEM_STATUS.DISCONNECTED ? 0 : 1;
            }
            if (total < 4)
            {
                for (int i = total; i < 4; i++)
                {
                    ReadersBox.Items.Add(new AReaderBox(
                        this,
                        new TimingSystem(
                            String.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]),
                            Constants.Settings.TIMING_RFID),
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
                AutoAPIButton.Content = "Stop Uploads";
            }
            else
            {
                AutoAPIButton.Content = "Auto Upload";
            }
            subPage.UpdateView();
        }

        private async void ViewUpdateTimer_Click(object sender, EventArgs e)
        {
            bool updates = false;
            await Task.Run(() =>
            {
                updates = mWindow.NewTimingInfo();
            });
            if (updates)
            {
                Log.D("Updates available.");
                List<DistanceStat> inStats = database.GetDistanceStats(theEvent.Identifier);
                stats.Clear();
                foreach (DistanceStat s in inStats)
                {
                    stats.Add(s);
                }
                subPage.UpdateView();
            }
        }

        public void DatasetChanged()
        {
            mWindow.NotifyTimingWorker();
        }

        private void Timer_Click(object sender, EventArgs e)
        {
            TimeSpan ellapsed = DateTime.Now - startTime;
            EllapsedTime.Content = String.Format("{0:D2}:{1:D2}:{2:D2}", Math.Abs(ellapsed.Days) * 24 + Math.Abs(ellapsed.Hours), Math.Abs(ellapsed.Minutes), Math.Abs(ellapsed.Seconds));
        }

        private void StartRaceClick(object sender, RoutedEventArgs e)
        {
            Log.D("Starting race.");
            StartTime.Text = DateTime.Now.ToString("HH:mm:ss.fff");
            StartRace.IsEnabled = false;
            StartTimeChanged();
        }

        private void ChangeWaves(object sender, RoutedEventArgs e)
        {
            Log.D("Set Wave Times clicked.");
            WaveWindow waves = new WaveWindow(mWindow, database);
            mWindow.AddWindow(waves);
            waves.ShowDialog();
        }

        private void ManualEntry(object sender, RoutedEventArgs e)
        {
            Log.D("Manual Entry selected.");
            ManualEntryWindow manualEntryWindow = ManualEntryWindow.NewWindow(mWindow, database, locations);
            if (manualEntryWindow != null)
            {
                mWindow.AddWindow(manualEntryWindow);
                manualEntryWindow.ShowDialog();
            }
        }

        private async void LoadLog(object sender, RoutedEventArgs e)
        {
            Log.D("Loading from log.");
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
                    Log.E("Something went wrong when trying to read the CSV file.");
                    Log.E(ex.StackTrace);
                }
            }
        }

        public void NotifyTimingWorker()
        {
            mWindow.NotifyTimingWorker();
        }

        private void Search()
        {
            Log.D("Searching");
            if (cts != null)
            {
                cts.Cancel();
                cts = null;
            }
            cts = new CancellationTokenSource();
            try
            {
                subPage.Search(searchBox.Text.Trim(), cts.Token);
            }
            catch
            {
                Log.D("Search cancelled.");
            }
            finally
            {
                cts = null;
            }
        }

        private void StartTimeKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Log.D("Start Time Box return key found.");
                UpdateStartTime();
            }
        }

        private void StartTimeLostFocus(object sender, RoutedEventArgs e)
        {
            Log.D("Start Time Box has lost focus.");
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
                mWindow.NetworkClearResults(theEvent.Identifier);
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
            String startTimeValue = StartTime.Text.Replace('_', '0');
            StartRace.IsEnabled = false;
            StartTime.Text = startTimeValue;
            Log.D("Start time is " + startTimeValue);
            startTime = DateTime.ParseExact(startTimeValue + DateTime.Parse(theEvent.Date).ToString("ddMMyyyy"), "HH:mm:ss.fffddMMyyyy", null);
            Log.D("Start time is " + startTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
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
            Log.D("Opening Set Time Window.");
            timeWindow = new SetTimeWindow(this, system);
            timeWindow.ShowDialog();
            timeWindow = null;
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

        internal void RemoveSystem(TimingSystem sys)
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

        internal bool ConnectSystem(TimingSystem sys)
        {
            mWindow.ConnectTimingSystem(sys);
            if (sys.Status == SYSTEM_STATUS.CONNECTED || sys.Status == SYSTEM_STATUS.WORKING)
            {
                connected++;
            }
            Log.D(connected + " systems connected or trying to connect.");
            if (connected >= total)
            {
                ReadersBox.Items.Add(new AReaderBox(this, new TimingSystem(String.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]), Constants.Settings.TIMING_RFID), locations));
                total = ReadersBox.Items.Count;
            }
            return sys.Status != SYSTEM_STATUS.DISCONNECTED;
        }

        internal bool DisconnectSystem(TimingSystem sys)
        {
            mWindow.DisconnectTimingSystem(sys);
            if (sys.Status == SYSTEM_STATUS.DISCONNECTED)
            {
                connected--;
            }
            Log.D(connected + " systems connected or trying to connect/disconnect.");
            if (total > 4 && connected < total - 1)
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
            }
            return sys.Status == SYSTEM_STATUS.DISCONNECTED;
        }

        private void RawReads_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Raw Reads selected.");
            subPage = new TimingRawReadsPage(this, database);
            TimingFrame.NavigationService.RemoveBackEntry();
            TimingFrame.Content = subPage;
        }

        public void LoadMainDisplay()
        {
            Log.D("Going back to main display.");
            subPage = new TimingResultsPage(this, database);
            TimingFrame.NavigationService.RemoveBackEntry();
            TimingFrame.Content = subPage;
        }

        private async void Recalculate_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Recalculate results clicked.");
            if ((string)recalculateButton.Content == "Working...")
            {
                return;
            }
            recalculateButton.Content = "Working...";
            ResultsAPI api = null;
            try
            {
                api = database.GetResultsAPI(theEvent.API_ID);
                Log.D("API found.");
            }
            catch {}
            // Get the event id values. Exit if not valid.
            string[] event_ids = theEvent.API_Event_ID.Split(',');
            Log.D("Event Id's found: " + event_ids.Length + " API is null? " + (api == null).ToString());
            // Create a bool for checking if we've grabbed the APIController's mutex so we release it later
            bool mutexGrabbed = false;
            if (event_ids.Length == 2 && api != null && APIController.GrabMutex(15000))
            {
                mutexGrabbed = true;
                try
                {
                    Log.D("Deleting results from API.");
                    await APIController.DeleteResults(api, event_ids[0], event_ids[1]);
                }
                catch (APIException ex)
                {
                    MessageBox.Show(ex.Message);
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
            mWindow.NetworkClearResults(theEvent.Identifier);
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
                case "Show All":
                    subPage.Show(PeopleType.ALL);
                    break;
                case "Show Only Starts":
                    subPage.Show(PeopleType.ONLYSTART);
                    break;
                case "Show Only Finishes":
                    subPage.Show(PeopleType.ONLYFINISH);
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
                    return PeopleType.ONLYSTART;
                case "Show Only Finishes":
                    return PeopleType.ONLYFINISH;
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

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Search();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Export clicked.");
            ExportResults exportResults = new ExportResults(mWindow, database);
            mWindow.AddWindow(exportResults);
            exportResults.ShowDialog();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Print clicked.");
            subPage = new PrintPage(this, database);
            TimingFrame.NavigationService.RemoveBackEntry();
            TimingFrame.Content = subPage;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Log.D("Starting TimingPage Update Timer.");
            ViewUpdateTimer.Start();
        }

        private void AddDNF_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Add DNF Entry clicked.");
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
            Log.D("Stats double cliked. Distance is " + selected.DistanceName);
            subPage = new DistanceStatsPage(this, database, selected.DistanceID, selected.DistanceName);
            TimingFrame.NavigationService.RemoveBackEntry();
            TimingFrame.Content = subPage;

        }

        private void Award_Click (object sender, RoutedEventArgs e)
        {
            Log.D("Awards clicked.");
            subPage = new AwardPage(this, database);
            TimingFrame.NavigationService.RemoveBackEntry();
            TimingFrame.Content = subPage;
        }

        private void CreateHTML_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Create HTML clicked.");
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "HTML file (*.htm,*.html)|*.htm;*.html",
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                String content = "";
                List<TimeResult> finishResults = database.GetFinishTimes(theEvent.Identifier);
                Dictionary<int, Participant> partDict = database.GetParticipants(theEvent.Identifier).ToDictionary(v => v.EventSpecific.Identifier, v => v);
                // if event is TIME BASED
                if (theEvent.EventType == Constants.Timing.EVENT_TYPE_TIME)
                {
                    Dictionary<string, int> maxLoops = new Dictionary<string, int>();
                    Dictionary<(int, int), TimeResult> LoopResults = new Dictionary<(int, int), TimeResult>();
                    Dictionary<int, int> RunnerLoopsCompleted = new Dictionary<int, int>();
                    Dictionary<string, double> DivisionDistancePerLoop = new Dictionary<string, double>();
                    Dictionary<string, string> DivisionDistanceType = new Dictionary<string, string>();
                    foreach (TimeResult result in finishResults)
                    {
                        if (!maxLoops.ContainsKey(result.DistanceName))
                        {
                            maxLoops[result.DistanceName] = result.Occurrence;
                        }
                        maxLoops[result.DistanceName] = result.Occurrence > maxLoops[result.DistanceName] ? result.Occurrence : maxLoops[result.DistanceName];
                        LoopResults[(result.EventSpecificId, result.Occurrence)] = result;
                        if (!RunnerLoopsCompleted.ContainsKey(result.EventSpecificId))
                        {
                            RunnerLoopsCompleted[result.EventSpecificId] = result.Occurrence;
                        }
                        RunnerLoopsCompleted[result.EventSpecificId] =
                            RunnerLoopsCompleted[result.EventSpecificId] > result.Occurrence ?
                                RunnerLoopsCompleted[result.EventSpecificId] :
                                result.Occurrence;
                    }
                    List<Distance> divs = database.GetDistances(theEvent.Identifier);
                    foreach (Distance d in divs)
                    {
                        DivisionDistancePerLoop[d.Name] = d.DistanceValue;
                        DivisionDistanceType[d.Name] = d.DistanceUnit == Constants.Distances.MILES ? "Miles" :
                            d.DistanceUnit == Constants.Distances.FEET ? "Feet" :
                            d.DistanceUnit == Constants.Distances.KILOMETERS ? "Kilometers" :
                            d.DistanceUnit == Constants.Distances.METERS ? "Meters" :
                            d.DistanceUnit == Constants.Distances.YARDS ? "Yards" :
                            "Unknown";
                    }
                    HtmlResultsTemplateTime template = new HtmlResultsTemplateTime(theEvent, finishResults, partDict,
                        maxLoops, LoopResults, RunnerLoopsCompleted, DivisionDistancePerLoop, DivisionDistanceType);
                    content = template.TransformText();
                }
                // if event is DISTANCE BASED
                else
                {
                    HtmlResultsTemplate template = new HtmlResultsTemplate(theEvent, finishResults, partDict);
                    content = template.TransformText();
                }
                System.IO.File.WriteAllText(saveFileDialog.FileName, content);
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
                    MessageBox.Show("Unable to start the web server. Please type this command in an elevated command prompt: 'netsh http add urlacl url=http://*:6933/ user=everyone'");
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
            Log.D("Auto API clicked.");
            if ((string)AutoAPIButton.Content == "Auto Upload")
            {
                mWindow.StartAPIController();
            }
            else
            {
                mWindow.StopAPIController();
            }
        }

        private void ManualAPI_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Manual API clicked.");
            if (ManualAPIButton.Content.ToString() != "Uploading")
            {
                Log.D("Uploading data.");
                ManualAPIButton.Content = "Uploading";
                UploadResults();
                return;
            }
            Log.D("Already uploading.");
        }

        private async void UploadResults()
        {
            // Get API to upload.
            if (theEvent.API_ID < 0 && theEvent.API_Event_ID.Length > 1)
            {
                ManualAPIButton.Content = "Manual Upload";
                return;
            }
            ResultsAPI api = database.GetResultsAPI(theEvent.API_ID);
            string[] event_ids = theEvent.API_Event_ID.Split(',');
            if (event_ids.Length != 2)
            {
                ManualAPIButton.Content = "Manual Upload";
                return;
            }
            // Get results to upload.
            List<TimeResult> results = database.GetNonUploadedResults(theEvent.Identifier);
            if (results.Count < 1)
            {
                Log.D("Nothing to upload.");
                ManualAPIButton.Content = "Manual Upload";
                return;
            }
            // Change TimeResults to APIResults
            List<APIResult> upRes = new List<APIResult>();
            Log.D("Results count: " + results.Count.ToString());
            foreach (TimeResult tr in results)
            {
                tr.Uploaded = Constants.Timing.TIMERESULT_UPLOADED_TRUE;
                upRes.Add(new APIResult(theEvent, tr));
            }
            Log.D("Attempting to upload " + upRes.Count.ToString() + " results.");
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
                    MessageBox.Show(ex.Message);
                    ManualAPIButton.Content = "Manual Upload";
                    return;
                }
                if (response != null)
                {
                    total += response.Count;
                    Log.D("Total: " + total + " Count: " + response.Count);
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
                    MessageBox.Show(ex.Message);
                    ManualAPIButton.Content = "Manual Upload";
                    return;
                }
                if (response != null)
                {
                    total += response.Count;
                    Log.D("Total: " + total + " Count: " + response.Count);
                }
                Log.D("Upload finished. Count total: " + total);
            }
            if (results.Count == total)
            {
                Log.D("Count matches, updating records.");
                database.AddTimingResults(results);
            }
            ManualAPIButton.Content = "Manual Upload";
        }

        private void SaveLog(object sender, RoutedEventArgs e)
        {
            Log.D("Save Log clicked.");
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value
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
                Log.D(String.Format("The format is '{0}'", format.ToString()));
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
                        Log.D("Saving file to: " + Path.GetDirectoryName(saveFileDialog.FileName) + "\\" + Regex.Replace(key.ToLower(), @"[^a-z0-9\-]", "") + "-" + Path.GetFileName(saveFileDialog.FileName));
                        exporter.ExportData(Path.GetDirectoryName(saveFileDialog.FileName) + "\\" + Regex.Replace(key.ToLower(), @"[^a-z0-9\-]", "") + "-" + Path.GetFileName(saveFileDialog.FileName));
                    }
                }
            }
        }

        private class AReaderBox : ListBoxItem
        {
            public ComboBox ReaderType { get; private set; }
            public TextBox ReaderIP { get; private set; }
            public TextBox ReaderPort { get; private set; }
            public ComboBox ReaderLocation { get; private set; }
            public Button ConnectButton { get; private set; }
            public Button ClockButton { get; private set; }
            public Button RewindButton { get; private set; }
            public Button RemoveButton { get; private set; }

            readonly TimingPage parent;
            private List<TimingLocation> locations;
            public TimingSystem reader;

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
                    MaxWidth = 740,
                    Width = 790
                };
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(140) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(140) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(70) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(120) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(65) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(65) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(90) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) });
                this.Content = thePanel;
                ReaderType = new ComboBox()
                {
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 5, 5, 5),
                    Height = 25
                };
                ComboBoxItem current = null, selected = null;
                foreach (string SYSTEM_IDVAL in Constants.Timing.SYSTEM_NAMES.Keys)
                {
                    current = new ComboBoxItem()
                    {
                        Content = Constants.Timing.SYSTEM_NAMES[SYSTEM_IDVAL],
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
                    Height = 25
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
                ClockButton = new Button()
                {
                    Content = "Clock",
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    IsEnabled = false
                };
                ClockButton.Click += new RoutedEventHandler(this.Clock);
                thePanel.Children.Add(ClockButton);
                Grid.SetColumn(ClockButton, 4);
                RewindButton = new Button()
                {
                    Content = "Rewind",
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    IsEnabled = false
                };
                RewindButton.Click += new RoutedEventHandler(this.Rewind);
                thePanel.Children.Add(RewindButton);
                Grid.SetColumn(RewindButton, 5);
                ConnectButton = new Button()
                {
                    Content = "Connect",
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                ConnectButton.Click += new RoutedEventHandler(this.Connect);
                thePanel.Children.Add(ConnectButton);
                Grid.SetColumn(ConnectButton, 6);
                RemoveButton = new Button()
                {
                    Content = "X",
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                if (reader.Saved())
                {
                    RemoveButton.Click += new RoutedEventHandler(this.Remove);
                    thePanel.Children.Add(RemoveButton);
                    Grid.SetColumn(RemoveButton, 7);
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
                Log.D("Reader type has changed.");
                string type = ((ComboBoxItem)ReaderType.SelectedItem).Uid;
                Log.D("Updating to type: " + Constants.Timing.SYSTEM_NAMES[type]);
                reader.UpdateSystemType(type);
                ReaderPort.Text = reader.Port.ToString();
            }

            private void Remove(object sender, RoutedEventArgs e)
            {
                Log.D("Remove button for a timing system has been clicked.");
                if (reader.Saved())
                {
                    parent.RemoveSystem(reader);
                };
            }

            private void Connect(object sender, RoutedEventArgs e)
            {
                if ("Connect" != (String)ConnectButton.Content)
                {
                    Log.D("Disconnect pressed.");
                    reader.Status = SYSTEM_STATUS.WORKING;
                    parent.DisconnectSystem(reader);
                    UpdateStatus();
                    return;
                }
                Log.D("Connect button pressed. IP is " + ReaderIP.Text);
                // Check if IP is a valid IP address
                if (!Regex.IsMatch(ReaderIP.Text.Trim(), IPPattern))
                {
                    MessageBox.Show("IP address given not valid.");
                    return;
                }
                reader.IPAddress = ReaderIP.Text.Trim();
                // Check if Port is valid.
                int portNo = -1;
                int.TryParse(ReaderPort.Text.Trim(), out portNo);
                if (portNo < 0 || portNo > 65535)
                {
                    MessageBox.Show("Port given not valid.");
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
                ReaderIP.IsEnabled = false;
                ReaderPort.IsEnabled = false;
                ReaderLocation.IsEnabled = false;
                RemoveButton.IsEnabled = false;
                if (reader.Type.Equals(Constants.Settings.TIMING_IPICO_LITE, StringComparison.OrdinalIgnoreCase))
                {
                    RewindButton.IsEnabled = false;
                    ClockButton.IsEnabled = false;
                }
                else
                {
                    RewindButton.IsEnabled = true;
                    ClockButton.IsEnabled = true;
                }
                ConnectButton.IsEnabled = true;
                ConnectButton.Content = "Disconnect";
            }

            private void SetDisconnected()
            {
                ReaderIP.IsEnabled = true;
                ReaderPort.IsEnabled = true;
                RemoveButton.IsEnabled = true;
                ReaderLocation.IsEnabled = true;
                ClockButton.IsEnabled = false;
                RewindButton.IsEnabled = false;
                ConnectButton.IsEnabled = true;
                ConnectButton.Content = "Connect";
            }

            private void SetWorking()
            {
                ReaderIP.IsEnabled = false;
                ReaderPort.IsEnabled = false;
                ReaderLocation.IsEnabled = false;
                ClockButton.IsEnabled = false;
                RewindButton.IsEnabled = false;
                ConnectButton.IsEnabled = false;
                RemoveButton.IsEnabled = false;
                ConnectButton.Content = "Working...";
            }

            private void Rewind(object sender, RoutedEventArgs e)
            {
                Log.D("Settings button pressed. IP is " + ReaderIP.Text);
                RewindWindow rewind = new RewindWindow(reader);
                rewind.ShowDialog();
            }

            private void Clock(object sender, RoutedEventArgs e)
            {
                Log.D("Clock button pressed. IP is " + ReaderIP.Text);
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
