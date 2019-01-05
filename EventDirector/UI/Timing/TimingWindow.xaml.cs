using EventDirector.Interfaces;
using EventDirector.UI.EventWindows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for TimingWindow.xaml
    /// </summary>
    public partial class TimingWindow : Window
    {
        private IDBInterface database;
        private IWindowCallback window = null;
        private IMainWindow mainWindow = null;

        Event theEvent;
        List<TimingLocation> locations;

        private DateTime startTime;
        DispatcherTimer Timer = new DispatcherTimer();
        private Boolean TimerStarted = false;

        private int[] baseIP = { 0, 0, 0, 0 };

        public TimingWindow(IDBInterface database, IMainWindow mainWindow)
        {
            InitializeComponent();
            this.database = database;
            this.window = null;
            this.mainWindow = mainWindow;
            Timer.Tick += new EventHandler(Timer_Click);
            Timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
        }

        public TimingWindow(IWindowCallback window, IDBInterface database)
        {
            InitializeComponent();
            this.database = database;
            this.window = window;
            this.mainWindow = null;
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
                locations.Insert(0, new TimingLocation(Constants.DefaultTiming.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                locations.Insert(0, new TimingLocation(Constants.DefaultTiming.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow));
            }
            else
            {
                locations.Insert(0, new TimingLocation(Constants.DefaultTiming.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
            }
            for (int i=0; i<4; i++)
            {
                ReadersBox.Items.Add(new AReaderBox(this, baseIP, locations));
            }
        }

        public void UpdateAll()
        {
            Log.D("Updating timing information.");
            theEvent = database.GetCurrentEvent();
            if (TimerStarted)
            {
                UpdateStartTime();
            }

            // Get updated list of locations
            locations = database.GetTimingLocations(theEvent.Identifier);
            if (theEvent.CommonStartFinish != 1)
            {
                locations.Insert(0, new TimingLocation(Constants.DefaultTiming.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                locations.Insert(0, new TimingLocation(Constants.DefaultTiming.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow));
            }
            else
            {
                locations.Insert(0, new TimingLocation(Constants.DefaultTiming.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
            }
            // Update locations in the list of readers
            foreach (AReaderBox read in ReadersBox.Items)
            {
                read.UpdateLocations(locations);
            }
        }

        public static TimingWindow NewWindow(IWindowCallback window, IDBInterface database)
        {
            if (StaticEvent.changeMainEventWindow != null || StaticEvent.timingWindow != null)
            {
                return null;
            }
            TimingWindow output = new TimingWindow(window, database);
            StaticEvent.timingWindow = output;
            return output;
        }

        private void Timer_Click(object sender, EventArgs e)
        {
            TimeSpan ellapsed = DateTime.Now - startTime;
            EllapsedTime.Content = String.Format("{0:D2}:{1:D2}:{2:D2}", ellapsed.Days * 24 + ellapsed.Hours, ellapsed.Minutes, ellapsed.Seconds);
        }

        private void StartRaceClick(object sender, RoutedEventArgs e)
        {

            Log.D("Starting race.");
            StartTime.Text = DateTime.Now.ToString("HH:mm:ss.fff");
            StartRace.IsEnabled = false;
            StartTimeChanged();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mainWindow != null) mainWindow.WindowClosed(this);
            if (window != null) window.WindowFinalize(this);
            StaticEvent.timingWindow = null;
        }

        private void EditSelected(object sender, RoutedEventArgs e)
        {
            Log.D("Edit Selected");
        }

        private void Overrides(object sender, RoutedEventArgs e)
        {
            Log.D("Overrides selected.");
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

        private class AReaderBox : ListBoxItem
        {
            public TextBox ReaderIP { get; private set; }
            public ComboBox ReaderLocation { get; private set; }
            public Button ConnectBtn { get; private set; }
            public Button ClockBtn { get; private set; }
            public Button SettingsBtn { get; private set; }

            private const string ipformat = "{0:D}.{1:D}.{2:D}.{3:D}";
            readonly TimingWindow window;
            private List<TimingLocation> locations;
            // public Reader reader;

            private readonly Regex allowedChars = new Regex("[^0-9.]");

            public AReaderBox(TimingWindow window, int[] baseIP, List<TimingLocation> locations)
            {
                this.window = window;
                this.locations = locations;
                Grid thePanel = new Grid();
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Star) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                this.Content = thePanel;
                ReaderIP = new TextBox()
                {
                    Text = String.Format(ipformat, baseIP[0], baseIP[1], baseIP[2], baseIP[3]),
                    FontSize = 14,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5,5,5,5)
                };
                ReaderIP.GotFocus += new RoutedEventHandler(this.SelectAll);
                ReaderIP.PreviewTextInput += new TextCompositionEventHandler(this.NumberValidation);
                thePanel.Children.Add(ReaderIP);
                Grid.SetColumn(ReaderIP, 0);
                ReaderLocation = new ComboBox()
                {
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 5, 5, 5),
                    Height = 25,
                    Width = 100
                };
                ComboBoxItem current = null, selected = null;
                foreach (TimingLocation loc in this.locations)
                {
                    current = new ComboBoxItem()
                    {
                        Content = loc.Name,
                        Uid = loc.Identifier.ToString()
                    };
                    // check if location ID is the same as the reader location ID and set selected
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
                Grid.SetColumn(ReaderLocation, 1);
                ClockBtn = new Button()
                {
                    Content = "Clock",
                    FontSize = 14,
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Height = 25,
                    IsEnabled = false
                };
                ClockBtn.Click += new RoutedEventHandler(this.Clock);
                thePanel.Children.Add(ClockBtn);
                Grid.SetColumn(ClockBtn, 2);
                SettingsBtn = new Button()
                {
                    Content = "Settings",
                    FontSize = 14,
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Height = 25,
                    IsEnabled = false
                };
                SettingsBtn.Click += new RoutedEventHandler(this.Settings);
                thePanel.Children.Add(SettingsBtn);
                Grid.SetColumn(SettingsBtn, 3);
                ConnectBtn = new Button()
                {
                    Content = "Connect",
                    FontSize = 14,
                    Margin = new Thickness(5, 5, 5, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Height = 25
                };
                ConnectBtn.Click += new RoutedEventHandler(this.Connect);
                thePanel.Children.Add(ConnectBtn);
                Grid.SetColumn(ConnectBtn, 4);
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

            private void SelectAll(object sender, RoutedEventArgs e)
            {
                TextBox src = (TextBox)e.OriginalSource;
                src.SelectAll();
            }

            private void NumberValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = allowedChars.IsMatch(e.Text);
            }

            private void Connect(object sender, RoutedEventArgs e)
            {
                Log.D("Connect button pressed. IP is " + ReaderIP.Text);
            }

            private void Settings(object sender, RoutedEventArgs e)
            {
                Log.D("Settings button pressed. IP is " + ReaderIP.Text);
            }

            private void Clock(object sender, RoutedEventArgs e)
            {
                Log.D("Clock button pressed. IP is " + ReaderIP.Text);
            }
        }
    }
}
