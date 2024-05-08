using Chronokeep.Constants;
using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.UI.Timing;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chronokeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for TimingPage.xaml
    /// </summary>
    public partial class MinTimingPage : IMainPage, ITimingPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private TimingRawReadsPage subPage;

        private Event theEvent;
        List<TimingLocation> locations;

        private SetTimeWindow timeWindow = null;
        private RewindWindow rewindWindow = null;

        int total = 4, connected = 0;

        private const string ipformat = "{0:D}.{1:D}.{2:D}.{3:D}";
        private int[] baseIP = { 0, 0, 0, 0 };

        public MinTimingPage(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.database = database;
            this.mWindow = window;
            theEvent = database.GetCurrentEvent();

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

            if (theEvent == null || theEvent.Identifier == -1)
            {
                return;
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
                    systems.Add(new TimingSystem(string.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]), system));
                }
            }
            systems.Add(new TimingSystem(string.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]), system));
            connected = 0;
            foreach (TimingSystem sys in systems)
            {
                ReadersBox.Items.Add(new MinReaderBox(this, sys, locations));
                if (sys.Status == SYSTEM_STATUS.CONNECTED || sys.Status == SYSTEM_STATUS.WORKING)
                {
                    connected++;
                }
            }
            total = ReadersBox.Items.Count;
            subPage = new TimingRawReadsPage(this, database);
            TimingFrame.Content = subPage;
        }



        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public void UpdateDatabase() { }

        public void Closing()
        {
            List<TimingSystem> removedSystems = database.GetTimingSystems();
            List<TimingSystem> ourSystems = new List<TimingSystem>();
            foreach (MinReaderBox box in ReadersBox.Items)
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
        }

        public void UpdateView()
        {
            Log.D("UI.MainPages.TimingPage", "Updating timing information.");
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier == -1)
            {
                subPage = null;
                TimingFrame.Content = subPage;
                TimingFrame.Visibility = Visibility.Hidden;
                // Something went wrong and this shouldn't be visible.
                return;
            }
            if (subPage == null)
            {
                subPage = new TimingRawReadsPage(this, database);
                TimingFrame.Content = subPage;
                TimingFrame.Visibility = Visibility.Visible;
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
            foreach (MinReaderBox read in ReadersBox.Items)
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
                for (int i = total; i < 4; i++)
                {
                    ReadersBox.Items.Add(new MinReaderBox(
                        this,
                        new TimingSystem(
                            string.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]),
                            system),
                            locations));
                }
            }

            subPage.SafemodeUpdateView();
        }

        public void DatasetChanged()
        {
            mWindow.NotifyTimingWorker();
        }

        public void NotifyTimingWorker()
        {
            mWindow.NotifyTimingWorker();
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
            MinReaderBox removed = null;
            foreach (MinReaderBox box in ReadersBox.Items)
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
                ReadersBox.Items.Add(new MinReaderBox(this, new TimingSystem(string.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]), system), locations));
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
            return sys.Status == SYSTEM_STATUS.DISCONNECTED;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.TimingPage", "Starting TimingPage Update Timer.");
        }

        public string GetSearchValue()
        {
            return "";
        }

        public SortType GetSortType()
        {
            return SortType.SYSTIME;
        }

        public void LoadMainDisplay(){}

        private class MinReaderBox : ListBoxItem
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

            readonly ITimingPage parent;
            private List<TimingLocation> locations;
            public TimingSystem reader;

            public RewindWindow rewind = null;

            private const string IPPattern = "^([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])$";
            private const string allowedChars = "[^0-9.]";
            private const string allowedNums = "[^0-9]";

            public MinReaderBox(ITimingPage window, TimingSystem sys, List<TimingLocation> locations)
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
                Log.D("UI.MainPages.TimingPage", "Updating to type: " + Readers.SYSTEM_NAMES[type]);
                reader.UpdateSystemType(type);
                ReaderPort.Text = reader.Port.ToString();
                ReaderPort.IsEnabled = Readers.SYSTEM_CHRONOKEEP_PORTAL == type ? false : true;
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
                ConnectButton.Icon = new Wpf.Ui.Controls.SymbolIcon() { Symbol = Wpf.Ui.Controls.SymbolRegular.Stop24 };
                ConnectButton.Uid = "disconnect";
            }

            private void SetDisconnected()
            {
                ReaderType.IsEnabled = true;
                ReaderIP.IsEnabled = true;
                ReaderPort.IsEnabled = Readers.SYSTEM_CHRONOKEEP_PORTAL == reader.Type ? false : true;
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
