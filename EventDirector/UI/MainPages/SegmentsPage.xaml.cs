using EventDirector.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace EventDirector.UI.MainPages
{
    /// <summary>
    /// Interaction logic for SegmentsPage.xaml
    /// </summary>
    public partial class SegmentsPage : Page, IMainPage
    {
        private INewMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;
        private List<TimingLocation> locations;
        private int finish_occurrences;

        private static int selectedDiv = -1;

        public SegmentsPage(INewMainWindow mWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mWindow;
            this.database = database;
            this.theEvent = database.GetCurrentEvent();
            if (theEvent != null)
            {
                locations = database.GetTimingLocations(theEvent.Identifier);
                if (theEvent.CommonStartFinish == 1)
                {
                    locations.Insert(0, new TimingLocation(Constants.DefaultTiming.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                }
                else
                {
                    locations.Insert(0, new TimingLocation(Constants.DefaultTiming.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                    locations.Insert(0, new TimingLocation(Constants.DefaultTiming.LOCATION_START, theEvent.Identifier, "Start", 1, theEvent.StartWindow));
                }
            }
            UpdateDivisionsBox();
        }

        public void UpdateView()
        {
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            SegmentsBox.Items.Clear();
            List<Segment> segments = database.GetSegments(theEvent.Identifier);
            finish_occurrences = 0;
            segments.Sort();
            if (theEvent.DivisionSpecificSegments == 1)
            {
                selectedDiv = Convert.ToInt32(((ComboBoxItem)Divisions.SelectedItem).Uid);
                segments.RemoveAll(NotInDiv);
            }
            else
            {
                DivisionRow.Height = new GridLength(0);
                Divisions.IsEnabled = false;
            }
            foreach (Segment s in segments)
            {
                SegmentsBox.Items.Add(new ASegment(this, s, locations));
                if (s.LocationId == Constants.DefaultTiming.LOCATION_FINISH || s.LocationId == Constants.DefaultTiming.LOCATION_START)
                {
                    finish_occurrences = s.Occurrence > finish_occurrences ? s.Occurrence : finish_occurrences;
                }
            }
            finish_occurrences++;
        }

        private static bool NotInDiv(Segment s)
        {
            return s.DivisionId != selectedDiv;
        }

        public void UpdateDivisionsBox()
        {
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            Divisions.Items.Clear();
            List<Division> divisions = database.GetDivisions(theEvent.Identifier);
            foreach (Division d in divisions)
            {
                Divisions.Items.Add(new ComboBoxItem()
                {
                    Content = d.Name,
                    Uid = d.Identifier.ToString()
                });
            }
            Divisions.SelectedIndex = 0;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Add segment clicked.");
            int divId = Convert.ToInt32(((ComboBoxItem)Divisions.SelectedItem).Uid);
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            database.AddSegment(new Segment(theEvent.Identifier, divId, Constants.DefaultTiming.LOCATION_FINISH, finish_occurrences, 0.0, 0.0, Constants.Distances.MILES, "Finish " + finish_occurrences));
            UpdateView();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            UpdateDatabase();
            UpdateView();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            UpdateView();
        }

        private void RemoveSegment(Segment mySegment)
        {
            Log.D("Removing segment.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            database.RemoveSegment(mySegment);
            UpdateView();
        }

        private void Divisions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.D("Division changed.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            UpdateView();
        }

        public void UpdateDatabase()
        {
            foreach (ASegment segItem in SegmentsBox.Items)
            {
                segItem.UpdateSegment();
                database.UpdateSegment(segItem.mySegment);
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

        private class ASegment : ListBoxItem
        {
            public TextBox SegName { get; private set; }
            public ComboBox Location { get; private set; }
            public ComboBox Occurrence { get; private set; }
            public TextBox SegDistance { get; private set; }
            public TextBox CumDistance { get; private set; }
            public ComboBox DistanceUnit { get; private set; }
            public Button Remove { get; private set; }

            readonly SegmentsPage page;
            public Segment mySegment;
            private Dictionary<string, int> locationDictionary;

            private readonly Regex allowedChars = new Regex("[^0-9.]+");
            private readonly Regex allowedNums = new Regex("[^0-9]+");

            public ASegment(SegmentsPage page, Segment segment, List<TimingLocation> locations)
            {
                this.page = page;
                this.mySegment = segment;
                this.locationDictionary = new Dictionary<string, int>();
                StackPanel thePanel = new StackPanel()
                {
                    MaxWidth = 600
                };
                this.Content = thePanel;
                this.IsTabStop = false;

                // Name - Location
                DockPanel topDock = new DockPanel();
                topDock.Children.Add(new Label()
                {
                    Content = "Name",
                    Width = 100,
                    FontSize = 16,
                    Margin = new Thickness(10, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                SegName = new TextBox()
                {
                    Text = mySegment.Name,
                    FontSize = 16,
                    Width = 180,
                    Margin = new Thickness(0, 10, 0, 10)
                };
                SegName.GotFocus += new RoutedEventHandler(this.SelectAll);
                topDock.Children.Add(SegName);
                Location = new ComboBox()
                {
                    FontSize = 16,
                    Width = 150,
                    Margin = new Thickness(10,10,10,10)
                };
                ComboBoxItem selected = null, current;
                foreach (TimingLocation loc in locations)
                {
                    current = new ComboBoxItem()
                    {
                        Content = loc.Name,
                        Uid = loc.Identifier.ToString()
                    };
                    Location.Items.Add(current);
                    if (mySegment.LocationId == loc.Identifier)
                    {
                        selected = current;
                    }
                    locationDictionary[loc.Identifier.ToString()] = loc.MaxOccurrences;
                }
                if (selected != null)
                {
                    Location.SelectedItem = selected;
                }
                Location.SelectionChanged += new SelectionChangedEventHandler(this.Location_Changed);
                topDock.Children.Add(Location);
                topDock.Children.Add(new Label()
                {
                    Content = "Occurrence",
                    Width = 100,
                    FontSize = 15,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                Occurrence = new ComboBox()
                {
                    FontSize = 16,
                    Margin = new Thickness(0, 10, 0, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                if (Location.SelectedItem == null || !locationDictionary.TryGetValue(((ComboBoxItem)Location.SelectedItem).Uid, out int maxOccurrences))
                {
                    maxOccurrences = 1;
                }
                selected = null;
                for (int i=1; i<=maxOccurrences; i++)
                {
                    current = new ComboBoxItem()
                    {
                        Content = i.ToString(),
                        Uid = i.ToString()
                    };
                    if (i == mySegment.Occurrence)
                    {
                        selected = current;
                    }
                    Occurrence.Items.Add(current);
                }
                if (selected != null)
                {
                    Occurrence.SelectedItem = selected;
                }
                else
                {
                    Occurrence.SelectedIndex = 0;
                }
                topDock.Children.Add(Occurrence);
                thePanel.Children.Add(topDock);

                // Distance information
                DockPanel bottomDock = new DockPanel();
                bottomDock.Children.Add(new Label()
                {
                    Content = "Distance",
                    Width = 100,
                    FontSize = 16,
                    Margin = new Thickness(10, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                SegDistance = new TextBox()
                {
                    Text = mySegment.SegmentDistance.ToString(),
                    Width = 80,
                    FontSize = 16,
                    Margin = new Thickness(0, 10, 0, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                SegDistance.GotFocus += new RoutedEventHandler(this.SelectAll);
                SegDistance.PreviewTextInput += new TextCompositionEventHandler(this.DoubleValidation);
                bottomDock.Children.Add(SegDistance);
                bottomDock.Children.Add(new Label()
                {
                    Content = "Total Distance",
                    Width = 130,
                    FontSize = 16,
                    Margin = new Thickness(10, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                CumDistance = new TextBox()
                {
                    Text = mySegment.CumulativeDistance.ToString(),
                    Width = 80,
                    FontSize = 16,
                    Margin = new Thickness(0, 10, 0, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                CumDistance.GotFocus += new RoutedEventHandler(this.SelectAll);
                CumDistance.PreviewTextInput += new TextCompositionEventHandler(this.DoubleValidation);
                bottomDock.Children.Add(CumDistance);
                DistanceUnit = new ComboBox()
                {
                    FontSize = 16,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 10, 10, 10)
                };
                DistanceUnit.Items.Add(new ComboBoxItem()
                {
                    Content = "Miles",
                    Uid = Constants.Distances.MILES.ToString()
                });
                DistanceUnit.Items.Add(new ComboBoxItem()
                {
                    Content = "Kilometers",
                    Uid = Constants.Distances.KILOMETERS.ToString()
                });
                DistanceUnit.Items.Add(new ComboBoxItem()
                {
                    Content = "Meters",
                    Uid = Constants.Distances.METERS.ToString()
                });
                DistanceUnit.Items.Add(new ComboBoxItem()
                {
                    Content = "Yards",
                    Uid = Constants.Distances.YARDS.ToString()
                });
                DistanceUnit.Items.Add(new ComboBoxItem()
                {
                    Content = "Feet",
                    Uid = Constants.Distances.FEET.ToString()
                });
                DistanceUnit.SelectedIndex = 0;
                if (mySegment.DistanceUnit == Constants.Distances.KILOMETERS)
                {
                    DistanceUnit.SelectedIndex = 1;
                }
                else if (mySegment.DistanceUnit == Constants.Distances.METERS)
                {
                    DistanceUnit.SelectedIndex = 2;
                }
                else if (mySegment.DistanceUnit == Constants.Distances.YARDS)
                {
                    DistanceUnit.SelectedIndex = 3;
                }
                else if (mySegment.DistanceUnit == Constants.Distances.FEET)
                {
                    DistanceUnit.SelectedIndex = 4;
                }
                bottomDock.Children.Add(DistanceUnit);
                thePanel.Children.Add(bottomDock);

                Remove = new Button()
                {
                    Content = "Remove",
                    FontSize = 16,
                    Height = 35,
                    Width = 150,
                    Margin = new Thickness(10, 10, 10, 10)
                };
                Remove.Click += new RoutedEventHandler(this.Remove_Click);
                thePanel.Children.Add(Remove);
            }

            private void Location_Changed(object sender, SelectionChangedEventArgs e)
            {
                Occurrence.Items.Clear();
                if (Location.SelectedItem == null || !locationDictionary.TryGetValue(((ComboBoxItem)Location.SelectedItem).Uid, out int maxOccurrences))
                {
                    maxOccurrences = 1;
                }
                for (int i = 1; i <= maxOccurrences; i++)
                {

                    Occurrence.Items.Add(new ComboBoxItem()
                    {
                        Content = i.ToString(),
                        Uid = i.ToString()
                    });
                }
                Occurrence.SelectedIndex = 0;
            }

            private void Remove_Click(object sender, EventArgs e)
            {
                Log.D("Removing an item.");
                this.page.RemoveSegment(mySegment);
            }

            public void UpdateSegment()
            {
                Log.D("Save clicked.");
                try
                {
                    mySegment.Name = SegName.Text;
                    mySegment.LocationId = Convert.ToInt32(((ComboBoxItem)Location.SelectedItem).Uid);
                    mySegment.SegmentDistance = Convert.ToDouble(SegDistance.Text);
                    mySegment.CumulativeDistance = Convert.ToDouble(CumDistance.Text);
                    mySegment.DistanceUnit = Convert.ToInt32(((ComboBoxItem)DistanceUnit.SelectedItem).Uid);
                    mySegment.Occurrence = Convert.ToInt32(((ComboBoxItem)Occurrence.SelectedItem).Uid);
                }
                catch
                {
                    MessageBox.Show("Error with values given.");
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

            private void DoubleValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = allowedChars.IsMatch(e.Text);
            }

            private void NumberValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = allowedNums.IsMatch(e.Text);
            }
        }
    }
}
