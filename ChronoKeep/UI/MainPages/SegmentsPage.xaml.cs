using Chronokeep.Interfaces;
using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.API;
using Chronokeep.Objects.ChronoKeepAPI;
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
        private Dictionary<int, TimingLocation> LocationDict = new Dictionary<int, TimingLocation>();

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
                    locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences - 1, theEvent.FinishIgnoreWithin));
                }
                else
                {
                    locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences - 1, theEvent.FinishIgnoreWithin));
                }
                foreach (TimingLocation loc in locations)
                {
                    LocationDict.Add(loc.Identifier, loc);
                }
                distances = database.GetDistances(theEvent.Identifier);
                distances.Sort((x1, x2) => x1.Name.CompareTo(x2.Name));
                distances.RemoveAll(x => x.LinkedDistance != Constants.Timing.DISTANCE_NO_LINKED_ID);
                if (theEvent.API_ID > 0 && theEvent.API_Event_ID.Length > 1)
                {
                    apiPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    apiPanel.Visibility = Visibility.Collapsed;
                }
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
            UpdateDatabase();
            allSegments[mySegment.DistanceId].Remove(mySegment);
            database.RemoveSegment(mySegment);
            UpdateView();
        }

        public void UpdateDatabase()
        {
            List<Segment> upSegs = new List<Segment>();
            List<Segment> newSegs = new List<Segment>();
            HashSet<int> segSet = new HashSet<int>();
            foreach (Segment s in database.GetSegments(theEvent.Identifier))
            {
                segSet.Add(s.Identifier);
            }
            foreach (Object seg in SegmentsBox.Items)
            {
                if (seg is ASegment)
                {
                    ((ASegment)seg).UpdateSegment();
                    Segment thisSegment = ((ASegment)seg).mySegment;
                    if (thisSegment.Identifier < 0)
                    {
                        newSegs.Add(thisSegment);
                    }
                    else
                    {
                        upSegs.Add(thisSegment);
                    }
                }
            }
            newSegs.RemoveAll(x => !LocationDict.ContainsKey(x.LocationId) || x.Occurrence > LocationDict[x.LocationId].MaxOccurrences || x.Occurrence < 1);
            database.AddSegments(newSegs);
            UpdateTimingWorker = true;
            database.UpdateSegments(upSegs);
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

        public void AddSegment(int distanceId)
        {
            Log.D("UI.MainPages.SegmentsPage", "Adding segment.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            Segment newSeg = new Segment(theEvent.Identifier, distanceId, Constants.Timing.LOCATION_FINISH, 0, 0.0, 0.0, Constants.Distances.MILES, "", "", "");
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
            database.RemoveSegments(allSegments[intoDistance]);
            allSegments[intoDistance].Clear();
            foreach (Segment seg in allSegments[fromDistance])
            {
                Segment newSeg = new Segment(seg);
                newSeg.DistanceId = intoDistance;
                allSegments[intoDistance].Add(newSeg);
            }
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
                    page.AddSegment(selectedDistance);
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
                Margin = new Thickness(10, 0, 5, 0)
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
            public TextBlock Unit = new TextBlock()
            {
                Text = "Unit",
                FontSize = 14,
                Width = 90,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public TextBlock GPSLabel = new TextBlock()
            {
                Text = "GPS",
                FontSize = 14,
                Width = 190,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public TextBlock MapLinkLabel = new TextBlock()
            {
                Text = "Map Link",
                FontSize = 14,
                Width = 190,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public TextBlock Remove = new TextBlock()
            {
                Text = "",
                FontSize = 14,
                Width = 45,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0)
            };
            public ASegmentHeader(Event theEvent)
            {
                this.IsTabStop = false;
                DockPanel dockPanel = new DockPanel()
                {
                    MaxWidth = 1150
                };
                this.Content = dockPanel;
                dockPanel.Children.Add(Where);
                dockPanel.Children.Add(NameLabel);
                if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                {
                    dockPanel.Children.Add(Occurrence);
                }
                dockPanel.Children.Add(SegDistance);
                dockPanel.Children.Add(Unit);
                dockPanel.Children.Add(GPSLabel);
                dockPanel.Children.Add(MapLinkLabel);
                dockPanel.Children.Add(Remove);
            }
        }
        private class ASegment : ListBoxItem
        {
            public TextBox SegName { get; private set; }
            public ComboBox Location { get; private set; }
            public ComboBox Occurrence { get; private set; } = null;
            public TextBox CumDistance { get; private set; }
            public ComboBox DistanceUnit { get; private set; }
            public TextBox GPS { get; private set; }
            public TextBox MapLink { get; private set; }
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
                    MaxWidth = 1150
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
                // GPS
                GPS = new TextBox()
                {
                    Text = mySegment.GPS,
                    FontSize = 14,
                    Width = 190,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 5, 5, 0)
                };
                GPS.GotFocus += new RoutedEventHandler(this.SelectAll);
                thePanel.Children.Add(GPS);
                // MapLink
                MapLink = new TextBox()
                {
                    Text = mySegment.MapLink,
                    FontSize = 14,
                    Width = 190,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 5, 5, 0)
                };
                MapLink.GotFocus += new RoutedEventHandler(this.SelectAll);
                thePanel.Children.Add(MapLink);
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
                    try
                    {
                        mySegment.LocationId = Convert.ToInt32(((ComboBoxItem)Location.SelectedItem).Uid);
                    }
                    catch
                    {
                        mySegment.LocationId = Constants.Timing.LOCATION_DUMMY;
                    }
                    mySegment.CumulativeDistance = Convert.ToDouble(CumDistance.Text);
                    mySegment.DistanceUnit = Convert.ToInt32(((ComboBoxItem)DistanceUnit.SelectedItem).Uid);
                    if (Occurrence != null && Occurrence.SelectedItem != null) mySegment.Occurrence = Convert.ToInt32(((ComboBoxItem)Occurrence.SelectedItem).Uid);
                    else mySegment.Occurrence = 0;
                    mySegment.GPS = GPS.Text;
                    mySegment.MapLink = MapLink.Text;
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

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.SegmentsPage", "Uploading segments.");
            if (UploadButton.Content.ToString() == "Upload")
            {
                UploadButton.IsEnabled = false;
                UploadButton.Content = "Working...";
                if (theEvent.API_ID < 0 || theEvent.API_Event_ID.Length < 1)
                {
                    UploadButton.Content = "Error";
                    return;
                }
                APIObject api = database.GetAPI(theEvent.API_ID);
                string[] event_ids = theEvent.API_Event_ID.Split(',');
                if (event_ids.Length != 2)
                {
                    UploadButton.Content = "Error";
                    return;
                }
                // Save segments displayed
                UpdateDatabase();
                UpdateSegments();
                // Get Distances and Locations to get their names
                Dictionary<int, Distance> distances = new Dictionary<int, Distance>();
                foreach (Distance d in database.GetDistances(theEvent.Identifier))
                {
                    distances.Add(d.Identifier, d);
                }
                Dictionary<int, TimingLocation> locations = new Dictionary<int, TimingLocation>();
                foreach (TimingLocation l in database.GetTimingLocations(theEvent.Identifier))
                {
                    locations.Add(l.Identifier, l);
                }
                locations.Add(Constants.Timing.LOCATION_ANNOUNCER, new TimingLocation(Constants.Timing.LOCATION_ANNOUNCER, theEvent.Identifier, "Announcer", 0, 0));
                locations.Add(Constants.Timing.LOCATION_FINISH, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", 0, 0));
                locations.Add(Constants.Timing.LOCATION_START, new TimingLocation(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, 0));
                Dictionary<int, string> distanceUnits = new Dictionary<int, string>
                {
                    { Constants.Distances.FEET, "ft" },
                    { Constants.Distances.KILOMETERS, "km" },
                    { Constants.Distances.METERS, "m" },
                    { Constants.Distances.YARDS, "yd" },
                    { Constants.Distances.MILES, "mi" }
                };
                // Convert Segments to APISegments
                List<APISegment> segments = new List<APISegment>();
                foreach (Segment seg in database.GetSegments(theEvent.Identifier))
                {
                    if (locations.ContainsKey(seg.LocationId) && distances.ContainsKey(seg.DistanceId) && distanceUnits.ContainsKey(seg.DistanceUnit))
                    {
                        segments.Add(new APISegment
                        {
                            Location = locations[seg.LocationId].Name,
                            DistanceName = locations[seg.DistanceId].Name,
                            Name = seg.Name,
                            DistanceValue = seg.CumulativeDistance,
                            DistanceUnit = distanceUnits[seg.DistanceUnit],
                            GPS = seg.GPS,
                            MapLink = seg.MapLink,

                        });
                    }
                }
                // Delete old information from the API
                try
                {
                    await APIHandlers.DeleteSegments(api, event_ids[0], event_ids[1]);
                }
                catch (APIException ex)
                {
                    DialogBox.Show(ex.Message);
                    UploadButton.IsEnabled = true;
                    UploadButton.Content = "Upload";
                    return;
                }
                Log.D("UI.MainPages.SegmentsPage", "Attempting to upload " + segments.Count.ToString() + " segments.");
                try
                {
                    AddSegmentsResponse response = await APIHandlers.AddSegments(api, event_ids[0], event_ids[1], segments);
                    if (response.Segments.Count != segments.Count)
                    {
                        DialogBox.Show("Error uploading segments. Count uploaded not equal to count meant to be uploaded.");
                    }
                }
                catch (APIException ex)
                {
                    DialogBox.Show(ex.Message);
                }
                UploadButton.IsEnabled = true;
                UploadButton.Content = "Upload";
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.SegmentsPage", "Deleting uploaded segments.");
            if (DeleteButton.Content.ToString() == "Delete Uploaded")
            {
                DeleteButton.IsEnabled = false;
                DeleteButton.Content = "Working...";
                if (theEvent.API_ID < 0 || theEvent.API_Event_ID.Length < 1)
                {
                    DeleteButton.Content = "Error";
                    return;
                }
                APIObject api = database.GetAPI(theEvent.API_ID);
                string[] event_ids = theEvent.API_Event_ID.Split(',');
                if (event_ids.Length != 2)
                {
                    DeleteButton.Content = "Error";
                    return;
                }
                // Delete old information from the API
                try
                {
                    await APIHandlers.DeleteSegments(api, event_ids[0], event_ids[1]);
                }
                catch (APIException ex)
                {
                    DialogBox.Show(ex.Message);
                    DeleteButton.IsEnabled = true;
                    DeleteButton.Content = "Delete Uploaded";
                    return;
                }
            }
        }
    }
}
