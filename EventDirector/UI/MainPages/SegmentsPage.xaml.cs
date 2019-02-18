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
        private IMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;
        private List<TimingLocation> locations;
        private List<Division> divisions;
        
        private static HashSet<int> DivisionsToReset = new HashSet<int>();

        public SegmentsPage(IMainWindow mWindow, IDBInterface database)
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
                    locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                }
                else
                {
                    locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                    locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 1, theEvent.StartWindow));
                }
                divisions = database.GetDivisions(theEvent.Identifier);
                divisions.Sort((x1, x2) => x1.Name.CompareTo(x2.Name));
            }
        }

        public void UpdateView()
        {
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            SegmentsBox.Items.Clear();
            List<Segment> segments = database.GetSegments(theEvent.Identifier);
            if (theEvent.DivisionSpecificSegments == 1)
            {
                foreach (Division div in divisions)
                {
                    List<Segment> divSegments = new List<Segment>(segments);
                    divSegments.RemoveAll(x => x.DivisionId != div.Identifier);
                    SegmentsBox.Items.Add(new ADivisionSegmentHolder(this, div, divisions, divSegments, locations));
                }
            }
            else
            {
                segments.RemoveAll(x => x.DivisionId != Constants.Timing.COMMON_SEGMENTS_DIVISIONID);
                SegmentsBox.Items.Add(new ADivisionSegmentHolder(this, null, divisions, segments, locations));
            }
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
            DivisionsToReset.Add(mySegment.DivisionId);
            UpdateView();
        }

        public void UpdateDatabase()
        {
            List<Segment> segments = new List<Segment>();
            foreach (ADivisionSegmentHolder divSegHolder in SegmentsBox.Items)
            {
                foreach (ASegment aSegment in divSegHolder.segmentHolder.Items)
                {
                    aSegment.UpdateSegment();
                    segments.Add(aSegment.mySegment);
                }
            }
            database.UpdateSegments(segments);
        }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S()
        {
            UpdateDatabase();
            UpdateView();
        }

        public void Keyboard_Ctrl_Z()
        {
            UpdateView();
        }

        public void Closing()
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            if (DivisionsToReset.Count > 0)
            {
                foreach (int identifier in DivisionsToReset)
                {
                    if (identifier == Constants.Timing.COMMON_SEGMENTS_DIVISIONID)
                    {
                        database.ResetTimingResultsEvent(theEvent.Identifier);
                    }
                    else
                    {
                        database.ResetTimingResultsDivision(theEvent.Identifier, identifier);
                    }
                }
                mWindow.NotifyTimingWorker();
            }
        }

        public void AddSegment(int divisionId, int finish_occurrences)
        {
            Log.D("Adding segment.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            database.AddSegment(new Segment(theEvent.Identifier, divisionId, Constants.Timing.LOCATION_FINISH, finish_occurrences, 0.0, 0.0, Constants.Distances.MILES, "Finish " + finish_occurrences));
            DivisionsToReset.Add(divisionId);
            UpdateView();
        }

        public void CopyFromDivision(int intoDivision, int fromDivision)
        {
            Log.D("Copying segments.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            List<Segment> oldSegments = database.GetSegments(theEvent.Identifier);
            List<Segment> newSegments = new List<Segment>(oldSegments);
            oldSegments.RemoveAll(x => x.DivisionId != intoDivision);
            database.RemoveSegments(oldSegments);
            newSegments.RemoveAll(x => x.DivisionId != fromDivision);
            foreach (Segment seg in newSegments)
            {
                seg.DivisionId = intoDivision;
            }
            database.AddSegments(newSegments);
            DivisionsToReset.Add(intoDivision);
            UpdateView();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateView();
        }

        private class ADivisionSegmentHolder : ListBoxItem
        {
            private SegmentsPage page;
            private ComboBox copyFromDivision = null;
            private int finish_occurrences;
            private List<Division> otherDivisions;

            public ListBox segmentHolder;
            public Division division;

            public ADivisionSegmentHolder(SegmentsPage page, Division division,
                List<Division> divisions, List<Segment> segments, List<TimingLocation> locations)
            {
                this.division = division;
                this.page = page;
                otherDivisions = new List<Division>(divisions);
                otherDivisions.RemoveAll(x => x.Identifier == (division == null ? -1 : division.Identifier));
                StackPanel thePanel = new StackPanel();
                this.Content = thePanel;
                Grid namePanel = new Grid();
                namePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                namePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(85) });
                if (division != null)
                {
                    namePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(250) });
                }
                Label divName = new Label()
                {
                    Content = division == null ? "All Divisions" : division.Name,
                    FontSize = 20,
                    Margin = new Thickness(10,5,0,5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                namePanel.Children.Add(divName);
                Grid.SetColumn(divName, 0);
                Button addButton = new Button()
                {
                    Content = "Add",
                    FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 75,
                    Height = 25
                };
                addButton.Click += new RoutedEventHandler(this.AddClick);
                namePanel.Children.Add(addButton);
                Grid.SetColumn(addButton, 1);
                if (division != null)
                {
                    DockPanel copyPanel = new DockPanel();
                    copyPanel.Children.Add(new Label()
                    {
                        Content = "Copy from",
                        FontSize = 14,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(10, 0, 2, 0)
                    });
                    copyFromDivision = new ComboBox()
                    {
                        FontSize = 14,
                        Height = 25,
                        VerticalAlignment = VerticalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0,0,10,0)
                    };
                    copyFromDivision.Items.Add(new ComboBoxItem()
                    {
                        Content = "",
                        Uid = "-1"
                    });
                    foreach (Division div in otherDivisions)
                    {
                        copyFromDivision.Items.Add(new ComboBoxItem()
                        {
                            Content = div.Name,
                            Uid = div.Identifier.ToString()
                        });
                    }
                    copyFromDivision.SelectedIndex = 0;
                    copyFromDivision.SelectionChanged += new SelectionChangedEventHandler(this.CopyFromDivisionSelected);
                    copyPanel.Children.Add(copyFromDivision);
                    namePanel.Children.Add(copyPanel);
                    Grid.SetColumn(copyPanel, 2);
                }
                thePanel.Children.Add(namePanel);
                thePanel.Children.Add(new Rectangle()
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5,0,5,0),
                    Height = 1,
                    Fill = new SolidColorBrush(Colors.Gray)
                });
                segmentHolder = new ListBox()
                {
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    BorderThickness = new Thickness(0)
                };
                thePanel.Children.Add(segmentHolder);
                finish_occurrences = 0;
                foreach (Segment s in segments)
                {
                    segmentHolder.Items.Add(new ASegment(page, s, locations));
                    if (s.LocationId == Constants.Timing.LOCATION_FINISH || s.LocationId == Constants.Timing.LOCATION_START)
                    {
                        finish_occurrences = s.Occurrence > finish_occurrences ? s.Occurrence : finish_occurrences;
                    }
                }
                finish_occurrences++;
                thePanel.Children.Add(new Rectangle()
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 0, 5, 0),
                    Height = 1,
                    Fill = new SolidColorBrush(Colors.Gray)
                });
            }

            private void AddClick(Object sender, RoutedEventArgs e)
            {
                Log.D("Add segment clicked.");
                int selectedDiv = Constants.Timing.COMMON_SEGMENTS_DIVISIONID;
                if (division != null)
                {
                    selectedDiv = division.Identifier;
                }
                page.AddSegment(selectedDiv, finish_occurrences);
            }

            private void CopyFromDivisionSelected(Object sender, SelectionChangedEventArgs e)
            {
                Log.D("Copy from division changed.");
                if (division == null || copyFromDivision.SelectedIndex < 1)
                {
                    return;
                }
                page.CopyFromDivision(division.Identifier, Convert.ToInt32(((ComboBoxItem)copyFromDivision.SelectedItem).Uid));
            }
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

        private void SegmentsBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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
