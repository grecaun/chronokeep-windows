using EventDirector.Interfaces;
using EventDirector.IO;
using EventDirector.Objects;
using EventDirector.UI.Timing;
using EventDirector.UI.Timing.Import;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace EventDirector.UI.MainPages
{
    /// <summary>
    /// Interaction logic for TimingPage.xaml
    /// </summary>
    public partial class TimingPage : IMainPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private ISubPage subPage;

        private Event theEvent;
        List<TimingLocation> locations;
        List<TimeResult> results = new List<TimeResult>();

        private DateTime startTime;
        DispatcherTimer Timer = new DispatcherTimer();
        private Boolean TimerStarted = false;

        int total = 4, connected = 0;

        private const string ipformat = "{0:D}.{1:D}.{2:D}.{3:D}";
        private int[] baseIP = { 0, 0, 0, 0 };

        public TimingPage(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.database = database;
            this.mWindow = window;
            theEvent = database.GetCurrentEvent();

            // Setup the running clock.
            Timer.Tick += new EventHandler(Timer_Click);
            Timer.Interval = new TimeSpan(0, 0, 0, 0, 100);

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

            // Setup timing systems.
            TimingType.Items.Clear();
            ComboBoxItem current, selected = null;
            foreach (string key in Constants.Timing.SYSTEM_NAMES.Keys)
            {
                current = new ComboBoxItem()
                {
                    Content = Constants.Timing.SYSTEM_NAMES[key],
                    Uid = key
                };
                TimingType.Items.Add(current);
                if (key == theEvent.TimingSystem)
                {
                    selected = current;
                }
            }
            if (selected != null)
            {
                TimingType.SelectedItem = selected;
            }
            else
            {
                TimingType.SelectedIndex = 0;
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
            if (theEvent.CommonStartFinish != 1)
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
                    systems.Add(new TimingSystem(String.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]), ((ComboBoxItem)TimingType.SelectedItem).Uid));
                }
            }
            systems.Add(new TimingSystem(String.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]), ((ComboBoxItem)TimingType.SelectedItem).Uid));
            connected = 0;
            foreach (TimingSystem sys in systems)
            {
                ReadersBox.Items.Add(new AReaderBox(this, sys, locations));
                if (sys.Status == SYSTEM_STATUS.CONNECTED || sys.Status == SYSTEM_STATUS.WORKING)
                {
                    connected++;
                }
            }
            if (connected > 0)
            {
                TimingTypeButton.IsEnabled = false;
            }
            else
            {
                TimingTypeButton.IsEnabled = true;
            }
            total = ReadersBox.Items.Count;
            subPage = new TimingResultsPage(this, database);
            TimingFrame.Content = subPage;
        }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public void UpdateDatabase() { }

        public void Closing() { }

        public void UpdateView()
        {
            Log.D("Updating timing information.");
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier == -1)
            {
                // Something went wrong and this shouldn't be visible.
                mWindow.UpdateStatus();
                return;
            }
            if (TimerStarted)
            {
                UpdateStartTime();
            }

            // Ensure we've still got the right timing system.
            foreach (ComboBoxItem item in TimingType.Items)
            {
                if (item.Uid == theEvent.TimingSystem)
                {
                    TimingType.SelectedItem = item;
                    break;
                }
            }

            // Get updated list of locations
            locations = database.GetTimingLocations(theEvent.Identifier);
            if (theEvent.CommonStartFinish != 1)
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
                read.UpdateSystemType(((ComboBoxItem)TimingType.SelectedItem).Uid, database);
                connected = read.reader.Status == SYSTEM_STATUS.DISCONNECTED ? connected : connected + 1;
            }

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

            // Ensure no editing allowed if we're connected.
            if (connected > 0)
            {
                TimingTypeButton.IsEnabled = false;
            }
            else
            {
                TimingTypeButton.IsEnabled = true;
            }
            subPage.UpdateView();
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

        private void EditSelected(object sender, RoutedEventArgs e)
        {
            Log.D("Edit Selected");
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
            OpenFileDialog csv_dialog = new OpenFileDialog() { Filter = "CSV Files (*.csv,*txt)|*.csv;*.txt|All Files|*" };
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

        private void Search(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void SearchKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Search();
            }
        }

        private void Search()
        {
            Log.D("Searching");
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
            theEvent.StartSeconds = (startTime.Hour * 3600) + (startTime.Minute * 60) + startTime.Second;
            theEvent.StartMilliseconds = startTime.Millisecond;
            database.UpdateEvent(theEvent);
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

        internal bool ConnectSystem(TimingSystem sys)
        {
            if (!TimingTypeButton.IsEnabled)
            {
                MessageBox.Show("Please select a timing method before attempting to connect.");
                sys.Status = SYSTEM_STATUS.DISCONNECTED;
                return false;
            }
            mWindow.ConnectTimingSystem(sys);
            if (sys.Status == SYSTEM_STATUS.CONNECTED || sys.Status == SYSTEM_STATUS.WORKING)
            {
                connected++;
            }
            Log.D(connected + " systems connected or trying to connect.");
            if (connected > 0)
            {
                TimingTypeButton.IsEnabled = false;
            }
            else
            {
                TimingTypeButton.IsEnabled = true;
            }
            if (connected >= total)
            {
                ReadersBox.Items.Add(new AReaderBox(this, new TimingSystem(String.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]), ((ComboBoxItem)TimingType.SelectedItem).Uid), locations));
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
            if (connected > 0)
            {
                TimingTypeButton.IsEnabled = false;
            }
            else
            {
                TimingTypeButton.IsEnabled = true;
            }
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

        private void TimingTypeButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("User wants to change the connected system type.");
            if (TimingTypeButton.Content.ToString() == "Edit")
            {
                if (connected == 0)
                {
                    TimingType.IsEnabled = true;
                    TimingTypeButton.Content = "Save";
                }
            }
            else if (TimingTypeButton.Content.ToString() == "Save")
            {
                TimingType.IsEnabled = false;
                TimingTypeButton.Content = "Edit";
                theEvent = database.GetCurrentEvent();
                theEvent.TimingSystem = ((ComboBoxItem)TimingType.SelectedItem).Uid;
                database.UpdateEvent(theEvent);
                UpdateView();
            }
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

        private void Recalculate_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Recalculate results clicked.");
            database.ResetTimingResults(theEvent.Identifier);
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
            switch (((ComboBoxItem)SortBy.SelectedItem).Content)
            {
                case "Gun Time":
                    subPage.SortBy(SortType.GUNTIME);
                    break;
                case "Bib":
                    subPage.SortBy(SortType.BIB);
                    break;
                case "Division":
                    subPage.SortBy(SortType.DIVISION);
                    break;
                default:
                    subPage.SortBy(SortType.SYSTIME);
                    break;
            }
        }

        public SortType GetSortType()
        {
            switch (((ComboBoxItem)SortBy.SelectedItem).Content)
            {
                case "Gun Time":
                    return SortType.GUNTIME;
                case "Bib":
                    return SortType.BIB;
                case "Division":
                    return SortType.DIVISION;
            }
            return SortType.SYSTIME;
        }

        private class AReaderBox : ListBoxItem
        {
            public TextBox ReaderIP { get; private set; }
            public TextBox ReaderPort { get; private set; }
            public ComboBox ReaderLocation { get; private set; }
            public Button ConnectButton { get; private set; }
            public Button ClockButton { get; private set; }
            public Button SettingsButton { get; private set; }

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
                    MaxWidth = 600,
                    Width = 600
                };
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(140) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(70) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(120) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(90) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(90) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(90) });
                this.Content = thePanel;
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
                Grid.SetColumn(ReaderIP, 0);
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
                Grid.SetColumn(ReaderPort, 1);
                ReaderLocation = new ComboBox()
                {
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 5, 5, 5),
                    Height = 25
                };
                ComboBoxItem current = null, selected = null;
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
                Grid.SetColumn(ReaderLocation, 2);
                ClockButton = new Button()
                {
                    Content = "Clock",
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    IsEnabled = false
                };
                ClockButton.Click += new RoutedEventHandler(this.Clock);
                thePanel.Children.Add(ClockButton);
                Grid.SetColumn(ClockButton, 3);
                SettingsButton = new Button()
                {
                    Content = "Settings",
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    IsEnabled = false
                };
                SettingsButton.Click += new RoutedEventHandler(this.Settings);
                thePanel.Children.Add(SettingsButton);
                Grid.SetColumn(SettingsButton, 4);
                ConnectButton = new Button()
                {
                    Content = "Connect",
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                ConnectButton.Click += new RoutedEventHandler(this.Connect);
                thePanel.Children.Add(ConnectButton);
                Grid.SetColumn(ConnectButton, 5);
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
                ClockButton.IsEnabled = true;
                SettingsButton.IsEnabled = true;
                ConnectButton.IsEnabled = true;
                ConnectButton.Content = "Disconnect";
            }

            private void SetDisconnected()
            {
                ReaderIP.IsEnabled = true;
                ReaderPort.IsEnabled = true;
                ReaderLocation.IsEnabled = true;
                ClockButton.IsEnabled = false;
                SettingsButton.IsEnabled = false;
                ConnectButton.IsEnabled = true;
                ConnectButton.Content = "Connect";
            }

            private void SetWorking()
            {
                ReaderIP.IsEnabled = false;
                ReaderPort.IsEnabled = false;
                ReaderLocation.IsEnabled = false;
                ClockButton.IsEnabled = false;
                SettingsButton.IsEnabled = false;
                ConnectButton.IsEnabled = false;
                ConnectButton.Content = "Working...";
            }

            private void Settings(object sender, RoutedEventArgs e)
            {
                Log.D("Settings button pressed. IP is " + ReaderIP.Text);
                reader.SystemInterface.Rewind(1, 50);
            }

            private void Clock(object sender, RoutedEventArgs e)
            {
                Log.D("Clock button pressed. IP is " + ReaderIP.Text);
                reader.SystemInterface.SetTime(DateTime.Now);
            }

            internal void UpdateSystemType(string type, IDBInterface database)
            {
                reader.UpdateSystemType(type);
                this.ReaderPort.Text = reader.Port.ToString();
            }
        }
    }
}
