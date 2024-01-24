using Chronokeep.Interfaces;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Chronokeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for SegmentsPage.xaml
    /// </summary>
    public partial class SegmentsPage : IMainPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;
        private List<TimingLocation> locations;
        private List<Distance> distances;

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
                distances = database.GetDistances(theEvent.Identifier);
                distances.Sort((x1, x2) => x1.Name.CompareTo(x2.Name));
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
            if (theEvent.DistanceSpecificSegments)
            {
                foreach (Distance d in distances)
                {
                    ADistanceSegmentHolder newHolder = new ADistanceSegmentHolder(theEvent, this, d, distances, allSegments[d.Identifier], locations);
                    items.Add(newHolder);
                    foreach (ListBoxItem item in newHolder.SegmentItems)
                    {
                        items.Add(item);
                    }
                }
            }
            else
            {
                ADistanceSegmentHolder newHolder = new ADistanceSegmentHolder(theEvent, this, null, distances, allSegments[Constants.Timing.COMMON_SEGMENTS_DISTANCEID], locations);
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
            if (theEvent.DistanceSpecificSegments)
            {
                foreach (Segment seg in segments)
                {
                    if (!allSegments.ContainsKey(seg.DistanceId))
                    {
                        allSegments[seg.DistanceId] = new List<Segment>();
                    }
                    allSegments[seg.DistanceId].Add(seg);
                }
                foreach (Distance d in distances)
                {
                    if (!allSegments.ContainsKey(d.Identifier))
                    {
                        allSegments[d.Identifier] = new List<Segment>();
                    }
                }
            }
            else
            {
                allSegments[Constants.Timing.COMMON_SEGMENTS_DISTANCEID] = segments;
                allSegments[Constants.Timing.COMMON_SEGMENTS_DISTANCEID].RemoveAll(x => x.DistanceId != Constants.Timing.COMMON_SEGMENTS_DISTANCEID);
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
                    Log.D("UI.MainPages.SegmentsPage", "Distance ID " + ((ASegment)seg).mySegment.DistanceId + " Segment Name " + ((ASegment)seg).mySegment.Name + " segment ID " + ((ASegment)seg).mySegment.Identifier);
                }
            }
            if (occurrence_error)
            {
                DialogBox.Show("Your finish lines has one or more segments beyond the maximum number it supports (" + (theEvent.FinishMaxOccurrences - 1) + ").  This could cause errors.");
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
            Log.D("UI.MainPages.SegmentsPage", "Removing segment.");
            SegmentsToRemove.Add(mySegment);
            allSegments[mySegment.DistanceId].Remove(mySegment);
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
                    Log.D("UI.MainPages.SegmentsPage", "Distance ID " + ((ASegment)seg).mySegment.DistanceId + " Segment Name " + ((ASegment)seg).mySegment.Name + " segment ID " + ((ASegment)seg).mySegment.Identifier);
                }
            }
            SegmentsToAdd.RemoveAll(x => x.Occurrence >= theEvent.FinishMaxOccurrences);
            database.AddSegments(SegmentsToAdd);
            database.RemoveSegments(SegmentsToRemove);
            Log.D("UI.MainPages.SegmentsPage", "Segments to remove count is " + SegmentsToRemove.Count);
            UpdateTimingWorker = true;
            segments.RemoveAll(x => (SegmentsToAdd.Contains(x) || SegmentsToRemove.Contains(x)));
            segments.RemoveAll(x => x.Occurrence >= theEvent.FinishMaxOccurrences);
            database.UpdateSegments(segments);
            Log.D("UI.MainPages.SegmentsPage", "Segments to update count is " + segments.Count);
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
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
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
                        Log.D("UI.MainPages.SegmentsPage", "Distance ID " + ((ASegment)seg).mySegment.DistanceId + " Segment Name " + ((ASegment)seg).mySegment.Name + " segment ID " + ((ASegment)seg).mySegment.Identifier);
                    }
                }
                if (occurrence_error)
                {
                    DialogBox.Show("Your finish lines has one or more segments beyond the maximum number it supports (" + (theEvent.FinishMaxOccurrences - 1) + ").  These will not be added. Update locations and max occurrences to fix this.");
                }
            }
            if (UpdateTimingWorker)
            {
                database.ResetTimingResultsEvent(theEvent.Identifier);
                mWindow.NetworkClearResults();
                mWindow.NotifyTimingWorker();
            }
        }

        public void AddSegment(int distanceId, int occurrence)
        {
            Log.D("UI.MainPages.SegmentsPage", "Adding segment.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            Segment newSeg = new Segment(theEvent.Identifier, distanceId, Constants.Timing.LOCATION_FINISH, occurrence, 0.0, 0.0, Constants.Distances.MILES, "Finish " + occurrence);
            SegmentsToAdd.Add(newSeg);
            allSegments[distanceId].Add(newSeg);
            UpdateView();
        }

        public void CopyFromDistance(int intoDistance, int fromDistance)
        {
            Log.D("UI.MainPages.SegmentsPage", "Copying segments.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            SegmentsToRemove.AddRange(allSegments[intoDistance]);
            allSegments[intoDistance].Clear();
            foreach (Segment seg in allSegments[fromDistance])
            {
                Segment newSeg = new Segment(seg);
                newSeg.DistanceId = intoDistance;
                allSegments[intoDistance].Add(newSeg);
            }
            SegmentsToAdd.AddRange(allSegments[intoDistance]);
            UpdateView();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateView();
        }

        private class ADistanceSegmentHolder : ListBoxItem
        {
            private SegmentsPage page;
            private ComboBox copyFromDistance = null;
            private int finish_occurrences;
            private List<Distance> otherDistances;
            public List<ListBoxItem> SegmentItems = new List<ListBoxItem>();
            private TextBox numAdd;

            //public ListBox segmentHolder;
            public Distance distance;

            public ADistanceSegmentHolder(Event theEvent, SegmentsPage page, Distance distance,
                List<Distance> distances, List<Segment> segments, List<TimingLocation> locations)
            {
                this.distance = distance;
                this.page = page;
                otherDistances = new List<Distance>(distances);
                otherDistances.RemoveAll(x => x.Identifier == (distance == null ? -1 : distance.Identifier));
                StackPanel thePanel = new StackPanel();
                this.Content = thePanel;
                this.IsTabStop = false;
                Grid namePanel = new Grid()
                {
                    Margin = new Thickness(5)
                };
                namePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                namePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) });
                namePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(85) });
                if (distance != null)
                {
                    namePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(250) });
                }
                TextBlock distanceName = new TextBlock()
                {
                    Text = distance == null ? "All Distances" : distance.Name,
                    FontSize = 20,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center
                };
                namePanel.Children.Add(distanceName);
                Grid.SetColumn(distanceName, 0);
                numAdd = new TextBox
                {
                    Text = "1",
                    FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 40,
                    Height = 35
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
                    Height = 35
                };
                addButton.Click += new RoutedEventHandler(this.AddClick);
                namePanel.Children.Add(addButton);
                Grid.SetColumn(addButton, 2);
                if (distance != null)
                {
                    DockPanel copyPanel = new DockPanel();
                    copyPanel.Children.Add(new TextBlock()
                    {
                        Text = "Copy from",
                        FontSize = 14,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(10, 0, 2, 0)
                    });
                    copyFromDistance = new ComboBox()
                    {
                        FontSize = 14,
                        Height = 35,
                        VerticalAlignment = VerticalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 10, 0)
                    };
                    copyFromDistance.Items.Add(new ComboBoxItem()
                    {
                        Content = "",
                        Uid = "-1"
                    });
                    foreach (Distance d in otherDistances)
                    {
                        copyFromDistance.Items.Add(new ComboBoxItem()
                        {
                            Content = d.Name,
                            Uid = d.Identifier.ToString()
                        });
                    }
                    copyFromDistance.SelectedIndex = 0;
                    copyFromDistance.SelectionChanged += new SelectionChangedEventHandler(this.CopyFromDistanceSelected);
                    copyPanel.Children.Add(copyFromDistance);
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
                Log.D("UI.MainPages.SegmentsPage", "Add segment clicked.");
                int selectedDistance = Constants.Timing.COMMON_SEGMENTS_DISTANCEID;
                if (distance != null)
                {
                    selectedDistance = distance.Identifier;
                }
                int count;
                int.TryParse(numAdd.Text, out count);
                for (int i = 0; i < count; i++)
                {
                    page.AddSegment(selectedDistance, finish_occurrences + i);
                }
            }

            private void CopyFromDistanceSelected(Object sender, SelectionChangedEventArgs e)
            {
                Log.D("UI.MainPages.SegmentsPage", "Copy from distance changed.");
                if (distance == null || copyFromDistance.SelectedIndex < 1)
                {
                    return;
                }
                page.CopyFromDistance(distance.Identifier, Convert.ToInt32(((ComboBoxItem)copyFromDistance.SelectedItem).Uid));
            }
        }

        private class ASegmentHeader : ListBoxItem
        {
            public TextBlock Where = new TextBlock()
            {
                Text = "Where",
                FontSize = 14,
                Width = 140,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public TextBlock NameLabel = new TextBlock()
            {
                Text = "Name",
                FontSize = 14,
                Width = 190,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public TextBlock Occurrence = new TextBlock()
            {
                Text = "Occ",
                FontSize = 14,
                Width = 70,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public TextBlock SegDistance = new TextBlock()
            {
                Text = "Dist",
                FontSize = 14,
                Width = 70,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public TextBlock TotalDistance = new TextBlock()
            {
                Text = "Total",
                FontSize = 14,
                Width = 70,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public TextBlock Unit = new TextBlock()
            {
                Text = "Unit",
                FontSize = 14,
                Width = 90,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public TextBlock Remove = new TextBlock()
            {
                Text = "",
                FontSize = 14,
                Width = 50,
                HorizontalAlignment = HorizontalAlignment.Center,
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
                Log.D("UI.MainPages.SegmentsPage", "Removing an item.");
                this.page.RemoveSegment(mySegment);
            }

            public void UpdateSegment()
            {
                Log.D("UI.MainPages.SegmentsPage", "Segments - Save clicked.");
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
