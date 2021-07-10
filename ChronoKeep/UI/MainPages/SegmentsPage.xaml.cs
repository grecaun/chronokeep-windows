using ChronoKeep.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace ChronoKeep.UI.MainPages
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
        private List<Distance> divisions;

        private bool UpdateTimingWorker = false;

        private Dictionary<int, List<Segment>> allSegments = new Dictionary<int, List<Segment>>();
        private List<Segment> SegmentsToRemove = new List<Segment>();
        private List<Segment> SegmentsToAdd = new List<Segment>();

        public SegmentsPage(IMainWindow mWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mWindow;
            this.database = database;
            this.theEvent = database.GetCurrentEvent();
            if (theEvent != null)
            {
                locations = database.GetTimingLocations(theEvent.Identifier);
                if (theEvent.CommonStartFinish)
                {
                    locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                }
                else
                {
                    locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                    locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 1, theEvent.StartWindow));
                }
                divisions = database.GetDistances(theEvent.Identifier);
                divisions.Sort((x1, x2) => x1.Name.CompareTo(x2.Name));
            }
            UpdateSegments();
        }

        public void UpdateView()
        {
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            List<ListBoxItem> items = new List<ListBoxItem>();
            if (theEvent.DivisionSpecificSegments)
            {
                foreach (Distance div in divisions)
                {
                    ADivisionSegmentHolder newHolder = new ADivisionSegmentHolder(theEvent, this, div, divisions, allSegments[div.Identifier], locations);
                    items.Add(newHolder);
                    foreach (ListBoxItem item in newHolder.SegmentItems)
                    {
                        items.Add(item);
                    }
                }
            }
            else
            {
                ADivisionSegmentHolder newHolder = new ADivisionSegmentHolder(theEvent, this, null, divisions, allSegments[Constants.Timing.COMMON_SEGMENTS_DIVISIONID], locations);
                items.Add(newHolder);
                foreach (ListBoxItem item in newHolder.SegmentItems)
                {
                    items.Add(item);
                }
            }
            SegmentsBox.ItemsSource = items;
        }

        private void UpdateSegments()
        {
            allSegments.Clear();
            List<Segment> segments = database.GetSegments(theEvent.Identifier);
            if (theEvent.DivisionSpecificSegments)
            {
                foreach (Segment seg in segments)
                {
                    if (!allSegments.ContainsKey(seg.DivisionId))
                    {
                        allSegments[seg.DivisionId] = new List<Segment>();
                    }
                    allSegments[seg.DivisionId].Add(seg);
                }
                foreach (Distance div in divisions)
                {
                    if (!allSegments.ContainsKey(div.Identifier))
                    {
                        allSegments[div.Identifier] = new List<Segment>();
                    }
                }
            }
            else
            {
                allSegments[Constants.Timing.COMMON_SEGMENTS_DIVISIONID] = segments;
                allSegments[Constants.Timing.COMMON_SEGMENTS_DIVISIONID].RemoveAll(x => x.DivisionId != Constants.Timing.COMMON_SEGMENTS_DIVISIONID);
            }
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            UpdateDatabase();
            UpdateSegments();
            bool occurrence_error = false;
            foreach (Object seg in SegmentsBox.Items)
            {
                if (seg is ASegment)
                {
                    Segment thisSegment = ((ASegment)seg).mySegment;
                    if (thisSegment.LocationId == Constants.Timing.LOCATION_FINISH && thisSegment.Occurrence >= theEvent.FinishMaxOccurrences)
                    {
                        occurrence_error = true;
                    }
                    Log.D("Division ID " + ((ASegment)seg).mySegment.DivisionId + " Segment Name " + ((ASegment)seg).mySegment.Name + " segment ID " + ((ASegment)seg).mySegment.Identifier);
                }
            }
            if (occurrence_error)
            {
                MessageBox.Show("Your finish lines has one or more segments beyond the maximum number it supports (" + (theEvent.FinishMaxOccurrences - 1) + ").  This could cause errors.");
            }
            UpdateView();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            UpdateSegments();
            UpdateView();
        }

        private void RemoveSegment(Segment mySegment)
        {
            Log.D("Removing segment.");
            SegmentsToRemove.Add(mySegment);
            allSegments[mySegment.DivisionId].Remove(mySegment);
            UpdateView();
        }

        public void UpdateDatabase()
        {
            List<Segment> segments = new List<Segment>();
            foreach (Object seg in SegmentsBox.Items)
            {
                if (seg is ASegment)
                {
                    ((ASegment)seg).UpdateSegment();
                    Segment thisSegment = ((ASegment)seg).mySegment;
                    segments.Add(thisSegment);
                    Log.D("Division ID " + ((ASegment)seg).mySegment.DivisionId + " Segment Name " + ((ASegment)seg).mySegment.Name + " segment ID " + ((ASegment)seg).mySegment.Identifier);
                }
            }
            SegmentsToAdd.RemoveAll(x => x.Occurrence >= theEvent.FinishMaxOccurrences);
            database.AddSegments(SegmentsToAdd);
            database.RemoveSegments(SegmentsToRemove);
            Log.D("Segments to remove count is " + SegmentsToRemove.Count);
            UpdateTimingWorker = true;
            segments.RemoveAll(x => (SegmentsToAdd.Contains(x) || SegmentsToRemove.Contains(x)));
            segments.RemoveAll(x => x.Occurrence >= theEvent.FinishMaxOccurrences);
            database.UpdateSegments(segments);
            Log.D("Segments to update count is " + segments.Count);
            SegmentsToAdd.Clear();
            SegmentsToRemove.Clear();
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
                bool occurrence_error = false;
                foreach (Object seg in SegmentsBox.Items)
                {
                    if (seg is ASegment)
                    {
                        Segment thisSegment = ((ASegment)seg).mySegment;
                        if (thisSegment.LocationId == Constants.Timing.LOCATION_FINISH && thisSegment.Occurrence >= theEvent.FinishMaxOccurrences)
                        {
                            occurrence_error = true;
                        }
                        Log.D("Division ID " + ((ASegment)seg).mySegment.DivisionId + " Segment Name " + ((ASegment)seg).mySegment.Name + " segment ID " + ((ASegment)seg).mySegment.Identifier);
                    }
                }
                if (occurrence_error)
                {
                    MessageBox.Show("Your finish lines has one or more segments beyond the maximum number it supports (" + (theEvent.FinishMaxOccurrences - 1) + ").  These will not be added. Update locations and max occurrences to fix this.");
                }
            }
            if (UpdateTimingWorker)
            {
                database.ResetTimingResultsEvent(theEvent.Identifier);
                mWindow.NetworkClearResults(theEvent.Identifier);
                mWindow.NotifyTimingWorker();
            }
        }

        public void AddSegment(int divisionId, int occurrence)
        {
            Log.D("Adding segment.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            Segment newSeg = new Segment(theEvent.Identifier, divisionId, Constants.Timing.LOCATION_FINISH, occurrence, 0.0, 0.0, Constants.Distances.MILES, "Finish " + occurrence);
            SegmentsToAdd.Add(newSeg);
            allSegments[divisionId].Add(newSeg);
            UpdateView();
        }

        public void CopyFromDivision(int intoDivision, int fromDivision)
        {
            Log.D("Copying segments.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            SegmentsToRemove.AddRange(allSegments[intoDivision]);
            allSegments[intoDivision].Clear();
            foreach (Segment seg in allSegments[fromDivision])
            {
                Segment newSeg = new Segment(seg);
                newSeg.DivisionId = intoDivision;
                allSegments[intoDivision].Add(newSeg);
            }
            SegmentsToAdd.AddRange(allSegments[intoDivision]);
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
            private List<Distance> otherDivisions;
            public List<ListBoxItem> SegmentItems = new List<ListBoxItem>();
            private TextBox numAdd;

            //public ListBox segmentHolder;
            public Distance division;

            public ADivisionSegmentHolder(Event theEvent, SegmentsPage page, Distance division,
                List<Distance> divisions, List<Segment> segments, List<TimingLocation> locations)
            {
                this.division = division;
                this.page = page;
                otherDivisions = new List<Distance>(divisions);
                otherDivisions.RemoveAll(x => x.Identifier == (division == null ? -1 : division.Identifier));
                StackPanel thePanel = new StackPanel();
                this.Content = thePanel;
                this.IsTabStop = false;
                Grid namePanel = new Grid();
                namePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                namePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) });
                namePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(85) });
                if (division != null)
                {
                    namePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(250) });
                }
                Label divName = new Label()
                {
                    Content = division == null ? "All Divisions" : division.Name,
                    FontSize = 20,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                divName.IsTabStop = false;
                namePanel.Children.Add(divName);
                Grid.SetColumn(divName, 0);
                numAdd = new TextBox
                {
                    Text = "1",
                    FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 40,
                    Height = 25
                };
                numAdd.PreviewTextInput += (s, e) =>
                {
                    e.Handled = !e.Text.All(char.IsDigit);
                };
                namePanel.Children.Add(numAdd);
                Grid.SetColumn(numAdd, 1);
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
                Grid.SetColumn(addButton, 2);
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
                        Margin = new Thickness(10, 0, 2, 0),
                        IsTabStop = false
                    });
                    copyFromDivision = new ComboBox()
                    {
                        FontSize = 14,
                        Height = 25,
                        VerticalAlignment = VerticalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 10, 0)
                    };
                    copyFromDivision.Items.Add(new ComboBoxItem()
                    {
                        Content = "",
                        Uid = "-1"
                    });
                    foreach (Distance div in otherDivisions)
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
                    Grid.SetColumn(copyPanel, 3);
                }
                thePanel.Children.Add(namePanel);
                thePanel.Children.Add(new Rectangle()
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 0, 5, 0),
                    Height = 1,
                    Fill = new SolidColorBrush(Colors.Gray)
                });
                /*segmentHolder = new ListBox()
                {
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    BorderThickness = new Thickness(0)
                };
                thePanel.Children.Add(segmentHolder);//*/
                finish_occurrences = 0;
                SegmentItems.Add(new ASegmentHeader(theEvent));
                //segmentHolder.Items.Add(new ASegmentHeader(theEvent));
                foreach (Segment s in segments)
                {
                    ASegment newSeg = new ASegment(theEvent, page, s, locations);
                    SegmentItems.Add(newSeg);
                    //segmentHolder.Items.Add(newSeg);
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
                int count;
                int.TryParse(numAdd.Text, out count);
                for (int i = 0; i < count; i++)
                {
                    page.AddSegment(selectedDiv, finish_occurrences + i);
                }
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

        private class ASegmentHeader : ListBoxItem
        {
            public Label Where = new Label()
            {
                Content = "Where",
                FontSize = 14,
                Width = 140,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public Label NameLabel = new Label()
            {
                Content = "Name",
                FontSize = 14,
                Width = 190,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public Label Occurrence = new Label()
            {
                Content = "Occ",
                FontSize = 14,
                Width = 70,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public Label SegDistance = new Label()
            {
                Content = "Dist",
                FontSize = 14,
                Width = 70,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public Label TotalDistance = new Label()
            {
                Content = "Total",
                FontSize = 14,
                Width = 70,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public Label Unit = new Label()
            {
                Content = "Unit",
                FontSize = 14,
                Width = 90,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public Label Remove = new Label()
            {
                Content = "",
                FontSize = 14,
                Width = 50,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public ASegmentHeader(Event theEvent)
            {
                this.IsTabStop = false;
                DockPanel dockPanel = new DockPanel()
                {
                    MaxWidth = 750
                };
                this.Content = dockPanel;
                dockPanel.Children.Add(Where);
                dockPanel.Children.Add(NameLabel);
                if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                {
                    dockPanel.Children.Add(Occurrence);
                }
                dockPanel.Children.Add(SegDistance);
                dockPanel.Children.Add(TotalDistance);
                dockPanel.Children.Add(Unit);
                dockPanel.Children.Add(Remove);
            }
        }
        private class ASegment : ListBoxItem
        {
            public TextBox SegName { get; private set; }
            public ComboBox Location { get; private set; }
            public ComboBox Occurrence { get; private set; } = null;
            public TextBox SegDistance { get; private set; }
            public TextBox CumDistance { get; private set; }
            public ComboBox DistanceUnit { get; private set; }
            public Button Remove { get; private set; }

            readonly SegmentsPage page;
            public Segment mySegment;
            private Dictionary<string, int> locationDictionary;

            private readonly Regex allowedChars = new Regex("[^0-9.]+");
            private readonly Regex allowedNums = new Regex("[^0-9]+");

            public ASegment(Event theEvent, SegmentsPage page, Segment segment, List<TimingLocation> locations)
            {
                this.page = page;
                this.mySegment = segment;
                this.locationDictionary = new Dictionary<string, int>();
                DockPanel thePanel = new DockPanel()
                {
                    MaxWidth = 750
                };
                this.Content = thePanel;

                // Where
                Location = new ComboBox()
                {
                    FontSize = 16,
                    Width = 140,
                    Margin = new Thickness(5, 5, 5, 0)
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
                thePanel.Children.Add(Location);
                // Name
                SegName = new TextBox()
                {
                    Text = mySegment.Name,
                    FontSize = 14,
                    Width = 190,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 5, 5, 0)
                };
                SegName.GotFocus += new RoutedEventHandler(this.SelectAll);
                thePanel.Children.Add(SegName);
                // Occurrence
                if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                {
                    Occurrence = new ComboBox()
                    {
                        FontSize = 14,
                        Width = 70,
                        Margin = new Thickness(5, 5, 5, 0),
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                    if (Location.SelectedItem == null || !locationDictionary.TryGetValue(((ComboBoxItem)Location.SelectedItem).Uid, out int maxOccurrences))
                    {
                        maxOccurrences = 1;
                    }
                    selected = null;
                    for (int i = 1; i <= maxOccurrences; i++)
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
                    thePanel.Children.Add(Occurrence);
                }

                // Distance information
                SegDistance = new TextBox()
                {
                    Text = mySegment.SegmentDistance.ToString(),
                    Width = 70,
                    FontSize = 14,
                    Margin = new Thickness(5, 5, 5, 0),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                SegDistance.GotFocus += new RoutedEventHandler(this.SelectAll);
                SegDistance.PreviewTextInput += new TextCompositionEventHandler(this.DoubleValidation);
                thePanel.Children.Add(SegDistance);
                CumDistance = new TextBox()
                {
                    Text = mySegment.CumulativeDistance.ToString(),
                    Width = 70,
                    FontSize = 14,
                    Margin = new Thickness(5, 5, 5, 0),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                CumDistance.GotFocus += new RoutedEventHandler(this.SelectAll);
                CumDistance.PreviewTextInput += new TextCompositionEventHandler(this.DoubleValidation);
                thePanel.Children.Add(CumDistance);
                DistanceUnit = new ComboBox()
                {
                    FontSize = 14,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Width = 90,
                    Margin = new Thickness(5, 5, 5, 0)
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
                thePanel.Children.Add(DistanceUnit);

                Remove = new Button()
                {
                    Content = "X",
                    FontSize = 14,
                    Width = 50,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 5, 5, 0)
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
                Log.D("Segments - Save clicked.");
                try
                {
                    mySegment.Name = SegName.Text;
                    mySegment.LocationId = Convert.ToInt32(((ComboBoxItem)Location.SelectedItem).Uid);
                    mySegment.SegmentDistance = Convert.ToDouble(SegDistance.Text);
                    mySegment.CumulativeDistance = Convert.ToDouble(CumDistance.Text);
                    mySegment.DistanceUnit = Convert.ToInt32(((ComboBoxItem)DistanceUnit.SelectedItem).Uid);
                    if (Occurrence != null) mySegment.Occurrence = Convert.ToInt32(((ComboBoxItem)Occurrence.SelectedItem).Uid);
                    else mySegment.Occurrence = 1;
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
