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
        private int finish_occurances;

        public SegmentsPage(INewMainWindow mWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mWindow;
            this.database = database;
            Update();
        }

        public void Update()
        {
            theEvent = database.GetCurrentEvent();
            if (theEvent != null)
            {
                locations = database.GetTimingLocations(theEvent.Identifier);
            }
            UpdateDivisionsBox();
            UpdateSegmentsList();
        }

        public void UpdateSegmentsList()
        {
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            SegmentsBox.Items.Clear();
            finish_occurances = 0;
            int divId = Convert.ToInt32(((ComboBoxItem)Divisions.SelectedItem).Uid);
            List<Segment> segments = database.GetSegments(theEvent.Identifier);
            segments.Sort();
            if (theEvent.DivisionSpecificSegments == 1)
            {
                foreach (Segment s in segments)
                {
                    if (s.DivisionId == divId)
                    {
                        SegmentsBox.Items.Add(new ASegment(this, s, locations, theEvent.CommonStartFinish == 1));
                        if (s.LocationId == Constants.DefaultTiming.LOCATION_FINISH || s.LocationId == Constants.DefaultTiming.LOCATION_START)
                        {
                            finish_occurances = s.Occurance > finish_occurances ? s.Occurance : finish_occurances;
                        }
                    }
                }
            }
            else
            {
                DivisionRow.Height = new GridLength(0);
                Divisions.IsEnabled = false;
                foreach (Segment s in segments)
                {
                    SegmentsBox.Items.Add(new ASegment(this, s, locations, theEvent.CommonStartFinish == 1));
                    if (s.LocationId == Constants.DefaultTiming.LOCATION_FINISH || s.LocationId == Constants.DefaultTiming.LOCATION_START)
                    {
                        finish_occurances = s.Occurance > finish_occurances ? s.Occurance : finish_occurances;
                    }
                }
            }
            finish_occurances++;
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
            database.AddSegment(new Segment(theEvent.Identifier, divId, Constants.DefaultTiming.LOCATION_FINISH, finish_occurances, 0.0, 0.0, Constants.Distances.MILES, "Finish " + finish_occurances));
            UpdateSegmentsList();
        }

        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (ASegment segItem in SegmentsBox.Items)
            {
                segItem.UpdateSegment();
                database.UpdateSegment(segItem.mySegment);
            }
            UpdateSegmentsList();
        }

        private void RemoveSegment(Segment mySegment)
        {
            Log.D("Removing segment.");
            database.RemoveSegment(mySegment);
            UpdateSegmentsList();
        }

        private void Divisions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.D("Division changed.");
            UpdateSegmentsList();
        }

        private class ASegment : ListBoxItem
        {
            public TextBox SegName { get; private set; }
            public ComboBox Location { get; private set; }
            public TextBox Occurance { get; private set; }
            public TextBox SegDistance { get; private set; }
            public TextBox CumDistance { get; private set; }
            public ComboBox DistanceUnit { get; private set; }
            public Button Remove { get; private set; }

            readonly SegmentsPage page;
            public Segment mySegment;

            private readonly Regex allowedChars = new Regex("[^0-9.]+");
            private readonly Regex allowedNums = new Regex("[^0-9]+");

            public ASegment(SegmentsPage page, Segment segment, List<TimingLocation> locations, bool CommonStartFinish)
            {
                this.page = page;
                this.mySegment = segment;
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
                    Margin = new Thickness(0, 10, 0, 10),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                SegName.GotFocus += new RoutedEventHandler(this.SelectAll);
                topDock.Children.Add(SegName);
                Location = new ComboBox()
                {
                    FontSize = 16,
                    Width = 100,
                    Margin = new Thickness(10,10,10,10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                ComboBoxItem selected, current;
                if (CommonStartFinish)
                {
                    current = new ComboBoxItem()
                    {
                        Content = "Start/Finish",
                        Uid = Constants.DefaultTiming.LOCATION_FINISH.ToString()
                    };
                    Location.Items.Add(current);
                    selected = current;
                }
                else
                {
                    current = new ComboBoxItem()
                    {
                        Content = "Start",
                        Uid = Constants.DefaultTiming.LOCATION_START.ToString()
                    };
                    Location.Items.Add(current);
                    selected = current;
                    current = new ComboBoxItem()
                    {
                        Content = "Finish",
                        Uid = Constants.DefaultTiming.LOCATION_FINISH.ToString()
                    };
                    Location.Items.Add(current);
                    if (mySegment.LocationId == Constants.DefaultTiming.LOCATION_FINISH)
                    {
                        selected = current;
                    }
                }
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
                }
                Location.SelectedItem = selected;
                topDock.Children.Add(Location);
                topDock.Children.Add(new Label()
                {
                    Content = "Occurance",
                    Width = 100,
                    FontSize = 15,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                Occurance = new TextBox()
                {
                    Text = mySegment.Occurance.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(0, 10, 0, 10),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                Occurance.GotFocus += new RoutedEventHandler(this.SelectAll);
                Occurance.PreviewTextInput += new TextCompositionEventHandler(this.NumberValidation);
                topDock.Children.Add(Occurance);
                thePanel.Children.Add(topDock);
                // Distance for a division segment is set in the division options.
                if (mySegment.Identifier != Constants.DefaultTiming.SEGMENT_FINISH)
                {
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
                }
                Remove = new Button()
                {
                    Content = "Remove",
                    FontSize = 16,
                    Height = 35,
                    Width = 150,
                    Margin = new Thickness(10, 10, 10, 10)
                };
                Remove.Click += new RoutedEventHandler(this.Remove_Click);
                if (mySegment.Identifier == Constants.DefaultTiming.SEGMENT_FINISH)
                {
                    Remove.IsEnabled = false;
                }
                thePanel.Children.Add(Remove);
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
                    mySegment.Occurance = Convert.ToInt32(Occurance.Text);
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
