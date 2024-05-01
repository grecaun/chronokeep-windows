using Chronokeep.Interfaces;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xceed.Wpf.Toolkit;

namespace Chronokeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for LocationsPage.xaml
    /// </summary>
    public partial class LocationsPage : IMainPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;
        private int LocationCount = 1;
        private bool UpdateTimingWorker = false;

        public LocationsPage(IMainWindow mWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mWindow;
            this.database = database;
            UpdateView();
        }

        public void UpdateView()
        {
            this.theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            LocationsBox.Items.Clear();
            LocationsBox.Items.Add(new ALocation(this, new TimingLocation(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow), theEvent));
            LocationsBox.Items.Add(new ALocation(this, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin), theEvent));
            List<TimingLocation> locations = database.GetTimingLocations(theEvent.Identifier);
            LocationCount = 1;
            locations.Sort();
            foreach (TimingLocation loc in locations)
            {
                LocationsBox.Items.Add(new ALocation(this, loc, theEvent));
                LocationCount = loc.Identifier > LocationCount - 1 ? loc.Identifier + 1 : LocationCount;
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.LocationsPage", "Add Location clicked.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            database.AddTimingLocation(new TimingLocation(theEvent.Identifier, "Location " + LocationCount));
            UpdateTimingWorker = true;
            UpdateView();
        }

        internal void RemoveLocation(TimingLocation location)
        {
            Log.D("UI.MainPages.LocationsPage", "Removing a location.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            if (location.Identifier == Constants.Timing.LOCATION_FINISH || location.Identifier == Constants.Timing.LOCATION_START)
            {
                Log.E("UI.MainPages.LocationsPage", "Somehow they told us to delete the start/finish location.");
            }
            else
            {
                database.RemoveTimingLocation(location);
            }
            UpdateTimingWorker = true;
            UpdateView();
        }

        public void Update_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.LocationsPage", "Update all clicked.");
            UpdateDatabase();
            UpdateView();
        }

        public void UpdateDatabase()
        {
            foreach (ALocation locItem in LocationsBox.Items)
            {
                locItem.UpdateLocation();
                if (locItem.myLocation.Identifier == Constants.Timing.LOCATION_FINISH)
                {
                    if (theEvent.FinishMaxOccurrences != locItem.myLocation.MaxOccurrences
                        || theEvent.FinishIgnoreWithin != locItem.myLocation.IgnoreWithin)
                    {
                        theEvent.FinishMaxOccurrences = locItem.myLocation.MaxOccurrences;
                        theEvent.FinishIgnoreWithin = locItem.myLocation.IgnoreWithin;
                        database.SetFinishOptions(theEvent);
                        UpdateTimingWorker = true;
                    }
                }
                else if (locItem.myLocation.Identifier == Constants.Timing.LOCATION_START)
                {
                    if (theEvent.StartWindow != locItem.myLocation.IgnoreWithin)
                    {
                        theEvent.StartWindow = locItem.myLocation.IgnoreWithin;
                        database.SetStartWindow(theEvent);
                    }
                }
                else
                {
                    if (locItem.IsUpdated())
                    {
                        database.UpdateTimingLocation(locItem.myLocation);
                        UpdateTimingWorker = true;
                    }
                }
            }
        }

        public void Keyboard_Ctrl_A()
        {
            Add_Click(null, null);
        }

        public void Keyboard_Ctrl_S()
        {
            UpdateDatabase();
            UpdateView();
        }

        public void Keyboard_Ctrl_Z()
        {
            UpdateView();
        }

        private class ALocation : ListBoxItem
        {
            public TextBox LocationName { get; private set; }
            public TextBox MaxOccurrences { get; private set; }
            public MaskedTextBox IgnoreWithin { get; private set; }
            public Button Remove { get; private set; }

            private const string TimeFormat = "{0:D2}:{1:D2}:{2:D2}";
            readonly LocationsPage page;
            public TimingLocation myLocation;

            private readonly Regex allowedChars = new Regex("[^0-9]+");

            public ALocation(LocationsPage page, TimingLocation location, Event theEvent)
            {
                this.page = page;
                this.myLocation = location;
                StackPanel thePanel = new StackPanel()
                {
                    MaxWidth = 450
                };
                this.Content = thePanel;
                this.IsTabStop = false;
                // Name information.
                DockPanel namePanel = new DockPanel();
                namePanel.Children.Add(new TextBlock()
                {
                    Text = "Name",
                    Width = 140,
                    FontSize = 16,
                    Margin = new Thickness(10,0,0,0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                });
                LocationName = new TextBox()
                {
                    Text = myLocation.Name,
                    FontSize = 16,
                    Margin = new Thickness(0,10,0,10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                LocationName.GotFocus += new RoutedEventHandler(this.SelectAll);
                namePanel.Children.Add(LocationName);
                thePanel.Children.Add(namePanel);

                // Max Occurrences - Ignore Within
                Grid settingsGrid = new Grid();
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                DockPanel occPanel = new DockPanel();
                occPanel.Children.Add(new TextBlock()
                {
                    Text = "Max Occurrences",
                    Width = 140,
                    FontSize = 16,
                    Margin = new Thickness(10, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                });
                MaxOccurrences = new TextBox()
                {
                    Text = myLocation.MaxOccurrences.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(0, 10, 0, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                MaxOccurrences.GotFocus += new RoutedEventHandler(this.SelectAll);
                MaxOccurrences.PreviewTextInput += new TextCompositionEventHandler(this.NumberValidation);
                occPanel.Children.Add(MaxOccurrences);
                settingsGrid.Children.Add(occPanel);
                Grid.SetColumn(occPanel, 0);
                DockPanel ignPanel = new DockPanel();
                string labelLabel = myLocation.Identifier == Constants.Timing.LOCATION_START ? "Start Window" : "Ignore Within";
                int labelWidth = 120;
                if (myLocation.Identifier == Constants.Timing.LOCATION_START || Constants.Timing.EVENT_TYPE_TIME == theEvent.EventType)
                {
                    occPanel.Visibility = Visibility.Collapsed;
                    labelWidth = 140;
                }
                ignPanel.Children.Add(new TextBlock()
                {
                    Text = labelLabel,
                    Width = labelWidth,
                    FontSize = 16,
                    Margin = new Thickness(10, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                });
                string ignorewithin = string.Format(TimeFormat, myLocation.IgnoreWithin / 3600, (myLocation.IgnoreWithin % 3600) / 60, myLocation.IgnoreWithin % 60);
                IgnoreWithin = new MaskedTextBox()
                {
                    Text = ignorewithin,
                    Mask = "00:00:00",
                    FontSize = 16,
                    Height = 35,
                    Margin = new Thickness(0, 10, 0, 10),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    TextAlignment = TextAlignment.Center,
                };
                IgnoreWithin.GotFocus += new RoutedEventHandler(this.SelectAll);
                ignPanel.Children.Add(IgnoreWithin);
                settingsGrid.Children.Add(ignPanel);
                if (myLocation.Identifier != Constants.Timing.LOCATION_START && Constants.Timing.EVENT_TYPE_TIME != theEvent.EventType)
                {
                    Grid.SetColumn(ignPanel, 1);
                }
                else
                {
                    Grid.SetColumnSpan(ignPanel, 2);
                }
                thePanel.Children.Add(settingsGrid);
                Remove = new Button()
                {
                    Content = "Remove",
                    FontSize = 16,
                    Height = 35,
                    Width = 150,
                    Margin = new Thickness(10,10,10,10)
                };
                if (myLocation.Identifier == Constants.Timing.LOCATION_FINISH 
                    || myLocation.Identifier == Constants.Timing.LOCATION_START)
                {
                    LocationName.IsEnabled = false;
                    Remove.IsEnabled = false;
                    Remove.Visibility = Visibility.Collapsed;
                }
                Remove.Click += new RoutedEventHandler(this.Remove_Click);
                thePanel.Children.Add(Remove);
            }

            private void Remove_Click(object sender, EventArgs e)
            {
                Log.D("UI.MainPages.LocationsPage", "Removing an item.");
                this.page.RemoveLocation(myLocation);
            }

            public bool IsUpdated()
            {
                try
                {
                    string[] parts = IgnoreWithin.Text.Replace('_', '0').Split(':');
                    int hours = Convert.ToInt32(parts[0]), minutes = Convert.ToInt32(parts[1]), seconds = Convert.ToInt32(parts[2]);
                    return myLocation.MaxOccurrences != Convert.ToInt32(MaxOccurrences) && myLocation.IgnoreWithin != (hours * 3600) + (minutes * 60) + seconds;
                }
                catch
                {
                    return true;
                }
            }

            public void UpdateLocation()
            {
                Log.D("UI.MainPages.LocationsPage", "Updating location.");
                try
                {
                    myLocation.Name = LocationName.Text;
                    myLocation.MaxOccurrences = Convert.ToInt32(MaxOccurrences.Text);
                    string[] parts = IgnoreWithin.Text.Replace('_', '0').Split(':');
                    int hours = Convert.ToInt32(parts[0]), minutes = Convert.ToInt32(parts[1]), seconds = Convert.ToInt32(parts[2]);
                    myLocation.IgnoreWithin = (hours * 3600) + (minutes * 60) + seconds;
                }
                catch
                {
                    DialogBox.Show("Error with values given.");
                    return;
                }
            }

            private void SelectAll(object sender, RoutedEventArgs e)
            {
                TextBox src = (TextBox)e.OriginalSource;
                src.SelectAll();
            }

            private void KeyPressHandler(object sender, KeyEventArgs e)
            {
                if (e.Key >= Key.D0 && e.Key <= Key.D9) { }
                else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) { }
                else if (e.Key == Key.Tab) { }
                else
                {
                    e.Handled = true;
                }
            }

            private void NumberValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = allowedChars.IsMatch(e.Text);
            }
        }

        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        public void Closing()
        {
            Log.D("UI.MainPages.LocationsPage", "Location page closing.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            if (UpdateTimingWorker)
            {
                Log.D("UI.MainPages.LocationsPage", "Resetting results.");
                database.ResetTimingResultsEvent(theEvent.Identifier);
                mWindow.NetworkClearResults();
                mWindow.NotifyTimingWorker();
            }
        }

        private void LocationsBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Utils.GetScrollViewer(sender as DependencyObject) is ScrollViewer scrollViewer)
            {
                if (e.Delta < 0)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 35);
                }
                else if (e.Delta > 0)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 35);
                }
            }
        }
    }
}
