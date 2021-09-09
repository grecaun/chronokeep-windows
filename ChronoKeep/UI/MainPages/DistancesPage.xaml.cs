using ChronoKeep.Interfaces;
using ChronoKeep.Objects;
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
using Xceed.Wpf.Toolkit;

namespace ChronoKeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for DistancesPage.xaml
    /// </summary>
    public partial class DistancesPage : Page, IMainPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;
        private List<Distance> distances;
        private Dictionary<int, Distance> distanceDictionary = new Dictionary<int, Distance>();
        private Dictionary<int, List<Distance>> subDistanceDictionary = new Dictionary<int, List<Distance>>();
        private HashSet<int> distancesChanged = new HashSet<int>();
        private bool UpdateTimingWorker = false;
        private int DistanceCount = 1;

        public DistancesPage(IMainWindow mWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mWindow;
            this.database = database;
            this.theEvent = database.GetCurrentEvent();
            UpdateView();
        }

        public void UpdateView()
        {
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            DistancesBox.Items.Clear();
            distances = database.GetDistances(theEvent.Identifier);
            DistanceCount = 1;
            distances.Sort();
            distanceDictionary.Clear();
            subDistanceDictionary.Clear();
            List<Distance> superDivs = new List<Distance>();
            foreach (Distance div in distances)
            {
                // Check if we're a linked distance
                if (div.LinkedDistance > 0)
                {
                    if (!subDistanceDictionary.ContainsKey(div.LinkedDistance))
                    {
                        subDistanceDictionary[div.LinkedDistance] = new List<Distance>();
                    }
                    subDistanceDictionary[div.LinkedDistance].Add(div);
                }
                else
                {
                    superDivs.Add(div);
                }
            }
            foreach (Distance div in superDivs)
            {
                distanceDictionary[div.Identifier] = div;
                ADistance parent = new ADistance(this, div, theEvent.FinishMaxOccurrences, distances, distanceDictionary, theEvent);
                DistancesBox.Items.Add(parent);
                DistanceCount = div.Identifier > DistanceCount - 1 ? div.Identifier + 1 : DistanceCount;
                // Add linked distances
                if (subDistanceDictionary.ContainsKey(div.Identifier))
                {
                    foreach (Distance sub in subDistanceDictionary[div.Identifier])
                    {
                        DistancesBox.Items.Add(new ASubDistance(this, sub, parent));
                        DistanceCount = sub.Identifier > DistanceCount - 1 ? sub.Identifier + 1 : DistanceCount;
                    }
                }
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.DistancesPage", "Add distance clicked.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            database.AddDistance(new Distance("New Distance " + DistanceCount, theEvent.Identifier));
            UpdateTimingWorker = true;
            UpdateView();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.DistancesPage", "Update clicked.");
            UpdateDatabase();
            UpdateView();
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.DistancesPage", "Revert clicked.");
            UpdateView();
        }

        internal void RemoveDistance(Distance distance)
        {
            Log.D("UI.MainPages.DistancesPage", "Remove distance clicked.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            database.RemoveDistance(distance);
            UpdateTimingWorker = true;
            UpdateView();
        }

        public void UpdateDatabase()
        {
            Dictionary<int, Distance> oldDistances = new Dictionary<int, Distance>();
            foreach (Distance distance in database.GetDistances(theEvent.Identifier))
            {
                oldDistances[distance.Identifier] = distance;
            }
            foreach (ADistanceInterface listDiv in DistancesBox.Items)
            {
                listDiv.UpdateDistance();
                int divId = listDiv.GetDistance().Identifier;
                if (oldDistances.ContainsKey(divId) &&
                    (oldDistances[divId].StartOffsetSeconds != listDiv.GetDistance().StartOffsetSeconds
                    || oldDistances[divId].StartOffsetMilliseconds != listDiv.GetDistance().StartOffsetMilliseconds
                    || oldDistances[divId].FinishOccurrence != listDiv.GetDistance().FinishOccurrence) )
                {
                    distancesChanged.Add(divId);
                    UpdateTimingWorker = true;
                }
                database.UpdateDistance(listDiv.GetDistance());
            }
        }

        public void Keyboard_Ctrl_A()
        {
            Log.D("UI.MainPages.DistancesPage", "Ctrl + A Passed to this page.");
            Add_Click(null, null);
        }

        public void Keyboard_Ctrl_S()
        {
            Log.D("UI.MainPages.DistancesPage", "Ctrl + S Passed to this page.");
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
            if (UpdateTimingWorker || distancesChanged.Count > 0)
            {
                database.ResetTimingResultsEvent(theEvent.Identifier);
                mWindow.NotifyTimingWorker();
            }
        }

        public void UpdateDistance(Distance distance)
        {
            int divId = distance.Identifier;
            Distance oldDiv = database.GetDistance(divId);
            if (oldDiv.StartOffsetSeconds != distance.StartOffsetSeconds ||
                oldDiv.StartOffsetMilliseconds != distance.StartOffsetMilliseconds
                || oldDiv.FinishOccurrence != distance.FinishOccurrence)
            {
                distancesChanged.Add(divId);
            }
            database.UpdateDistance(distance);
            UpdateView();
        }

        public void AddSubDistance(Distance theDistance)
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            database.AddDistance(new Distance(theDistance.Name + " Linked " + DistanceCount, theDistance.EventIdentifier, theDistance.Identifier, Constants.Timing.DISTANCE_TYPE_EARLY, 1, theDistance.Wave, theDistance.StartOffsetSeconds, theDistance.StartOffsetMilliseconds));
            UpdateTimingWorker = true;
            UpdateView();
        }

        private interface ADistanceInterface
        {
            Distance GetDistance();
            void UpdateDistance();
        }

        private class ASubDistance : ListBoxItem, ADistanceInterface
        {
            public TextBox DistanceName { get; private set; }
            public TextBox Wave { get; private set; }
            public Image WaveTypeImg { get; private set; }
            public TextBox Ranking { get; private set; }
            public MaskedTextBox StartOffset { get; private set; }
            public ComboBox TypeBox { get; private set; }
            public Button Remove { get; private set; }

            private const string TimeFormat = "{0:D2}:{1:D2}:{2:D2}.{3:D3}";

            readonly DistancesPage page;
            public Distance theDistance;

            private ADistance parent;
            private int waveType = 1;

            private readonly Regex allowedWithDot = new Regex("[^0-9.]");
            private readonly Regex allowedChars = new Regex("[^0-9]");

            public ASubDistance(DistancesPage page, Distance distance, ADistance parent)
            {
                this.page = page;
                this.parent = parent;
                this.theDistance = distance;
                StackPanel thePanel = new StackPanel()
                {
                    Margin = new Thickness(50, 0, 0, 0),
                    MaxWidth = 600
                };
                this.Content = thePanel;
                this.IsTabStop = false;

                // Name Grid (Name NameBox) - Rank Order - Remove Button
                Grid nameGrid = new Grid();
                nameGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Star) });
                nameGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Star) });
                nameGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                thePanel.Children.Add(nameGrid);
                // Name information.
                DockPanel namePanel = new DockPanel();
                namePanel.Children.Add(new Label()
                {
                    Content = "Name",
                    Width = 55,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                DistanceName = new TextBox()
                {
                    Text = theDistance.Name,
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                DistanceName.GotFocus += new RoutedEventHandler(this.SelectAll);
                namePanel.Children.Add(DistanceName);
                nameGrid.Children.Add(namePanel);
                Grid.SetColumn(namePanel, 0);
                DockPanel rankPanel = new DockPanel();
                rankPanel.Children.Add(new Label()
                {
                    Content = "Rank Priority",
                    Width = 135,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                Ranking = new TextBox
                {
                    Text = theDistance.Ranking.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                Ranking.PreviewTextInput += new TextCompositionEventHandler(this.NumberValidation);
                rankPanel.Children.Add(Ranking);
                nameGrid.Children.Add(rankPanel);
                Grid.SetColumn(rankPanel, 1);
                Remove = new Button()
                {
                    Content = "Remove",
                    FontSize = 14,
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(0, 5, 0, 5),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Remove.Click += new RoutedEventHandler(this.Remove_Click);
                nameGrid.Children.Add(Remove);
                Grid.SetColumn(Remove, 2);
                // Wave # - Start Offset - Type - Ranking Order
                DockPanel wavePanel = new DockPanel();
                wavePanel.Children.Add(new Label()
                {
                    Content = "Wave",
                    Width = 55,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                Uri imgUri = new Uri("pack://application:,,,/img/plus.png");
                waveType = 1;
                if (theDistance.StartOffsetSeconds < 0)
                {
                    Log.D("UI.MainPages.DistancesPage", "Setting type to negative and making seconds/milliseconds positive for offset textbox.");
                    waveType = -1;
                    imgUri = new Uri("pack://application:,,,/img/dash.png");
                    theDistance.StartOffsetSeconds *= -1;
                    theDistance.StartOffsetMilliseconds *= -1;
                }
                WaveTypeImg = new Image()
                {
                    Width = 25,
                    Margin = new Thickness(0, 0, 3, 0),
                    Source = new BitmapImage(imgUri),
                };
                WaveTypeImg.MouseLeftButtonDown += new MouseButtonEventHandler(this.SwapWaveType_Click);
                wavePanel.Children.Add(WaveTypeImg);
                Wave = new TextBox()
                {
                    Text = theDistance.Wave.ToString(),
                    FontSize = 16,
                    Width = 50,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                Wave.GotFocus += new RoutedEventHandler(this.SelectAll);
                Wave.PreviewTextInput += new TextCompositionEventHandler(this.NumberValidation);
                wavePanel.Children.Add(Wave);
                wavePanel.Children.Add(new Label()
                {
                    Content = "Start",
                    Width = 55,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                string sOffset = string.Format(TimeFormat, theDistance.StartOffsetSeconds / 3600,
                    (theDistance.StartOffsetSeconds % 3600) / 60, theDistance.StartOffsetSeconds % 60,
                    theDistance.StartOffsetMilliseconds);
                StartOffset = new MaskedTextBox()
                {
                    Text = sOffset,
                    Mask = "00:00:00.000",
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                StartOffset.GotFocus += new RoutedEventHandler(this.SelectAll);
                wavePanel.Children.Add(StartOffset);
                wavePanel.Children.Add(new Label()
                {
                    Content = "Type",
                    Width = 55,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                TypeBox = new ComboBox()
                {
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                TypeBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = "Early Start",
                        Uid = Constants.Timing.DISTANCE_TYPE_EARLY.ToString()
                    });
                TypeBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = "Unranked",
                        Uid = Constants.Timing.DISTANCE_TYPE_UNOFFICIAL.ToString()
                    });
                if (theDistance.Type == Constants.Timing.DISTANCE_TYPE_EARLY)
                {
                    TypeBox.SelectedIndex = 0;
                }
                else if (theDistance.Type == Constants.Timing.DISTANCE_TYPE_UNOFFICIAL)
                {
                    TypeBox.SelectedIndex = 1;
                }
                wavePanel.Children.Add(TypeBox);
                thePanel.Children.Add(wavePanel);
            }

            private void Remove_Click(object sender, RoutedEventArgs e)
            {
                Log.D("UI.MainPages.DistancesPage", "Removing distance.");
                this.page.RemoveDistance(theDistance);
            }

            private void DotValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = allowedWithDot.IsMatch(e.Text);
            }

            private void NumberValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = allowedChars.IsMatch(e.Text);
            }

            private void SelectAll(object sender, RoutedEventArgs e)
            {
                TextBox src = (TextBox)e.OriginalSource;
                src.SelectAll();
            }

            private void SwapWaveType_Click(object sender, RoutedEventArgs e)
            {
                Log.D("UI.MainPages.DistancesPage", "Plus/Minus sign clicked. WaveType is: " + waveType);
                if (waveType < 0)
                {
                    WaveTypeImg.Source = new BitmapImage(new Uri("pack://application:,,,/img/plus.png"));
                }
                else if (waveType > 0)
                {
                    WaveTypeImg.Source = new BitmapImage(new Uri("pack://application:,,,/img/dash.png"));
                }
                else
                {
                    Log.E("UI.MainPages.DistancesPage", "Something went wrong and the wave type was set to 0.");
                }
                waveType *= -1;
            }

            public Distance GetDistance()
            {
                return theDistance;
            }

            public void UpdateDistance()
            {
                Log.D("UI.MainPages.DistancesPage", "Updating sub distance.");
                parent.UpdateDistance();
                Distance parentDiv = parent.GetDistance();
                theDistance.Name = DistanceName.Text;
                theDistance.DistanceValue = parentDiv.DistanceValue;
                theDistance.EndSeconds = parentDiv.EndSeconds;
                theDistance.FinishOccurrence = parent.GetDistance().FinishOccurrence;
                theDistance.Type = TypeBox.SelectedIndex == 0 ? Constants.Timing.DISTANCE_TYPE_EARLY : Constants.Timing.DISTANCE_TYPE_UNOFFICIAL;
                int ranking;
                int.TryParse(Ranking.Text, out ranking);
                if (ranking >= 0)
                {
                    theDistance.Ranking = ranking;
                }
                int wave;
                int.TryParse(Wave.Text, out wave);
                if (wave >= 0)
                {
                    theDistance.Wave = wave;
                }
                string[] firstparts = StartOffset.Text.Replace('_', '0').Split(':');
                string[] secondparts = firstparts[2].Split('.');
                try
                {
                    theDistance.StartOffsetSeconds = (Convert.ToInt32(firstparts[0]) * 3600)
                        + (Convert.ToInt32(firstparts[1]) * 60)
                        + Convert.ToInt32(secondparts[0]);
                    theDistance.StartOffsetMilliseconds = Convert.ToInt32(secondparts[1]);
                }
                catch
                {
                    System.Windows.MessageBox.Show("Error with values given.");
                }
            }

        }

        private class ADistance : ListBoxItem, ADistanceInterface
        {
            public TextBox DistanceName { get; private set; }
            public ComboBox CopyFromBox { get; private set; }
            public TextBox Distance { get; private set; }
            public ComboBox DistanceUnit { get; private set; }
            public ComboBox FinishOccurrence { get; private set; } = null;
            public TextBox Wave { get; private set; }
            public Image WaveTypeImg { get; private set; }
            public MaskedTextBox StartOffset { get; private set; }
            public MaskedTextBox TimeLimit { get; private set; } = null;
            public Button AddSubDistance { get; private set; }
            public Button Remove { get; private set; }

            private const string TimeFormat = "{0:D2}:{1:D2}:{2:D2}.{3:D3}";
            private const string LimitFormat = "{0:D2}:{1:D2}:{2:D2}";
            readonly DistancesPage page;
            public Distance theDistance;
            private Dictionary<int, Distance> distanceDictionary;
            private int waveType = 1;

            private readonly Regex allowedWithDot = new Regex("[^0-9.]");
            private readonly Regex allowedChars = new Regex("[^0-9]");

            public ADistance(DistancesPage page, Distance distance, int maxOccurrences,
                List<Distance> distances, Dictionary<int, Distance> distanceDictionary, Event theEvent)
            {
                List<Distance> otherDistances = new List<Distance>(distances);
                this.distanceDictionary = distanceDictionary;
                otherDistances.Remove(distance);
                this.page = page;
                this.theDistance = distance;
                StackPanel thePanel = new StackPanel()
                {
                    MaxWidth = 600
                };
                this.Content = thePanel;
                this.IsTabStop = false;

                // Name Grid (Name NameBox -- Copy From DistancesBox)
                Grid nameGrid = new Grid();
                nameGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                nameGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                // Name information.
                DockPanel namePanel = new DockPanel();
                namePanel.Children.Add(new Label()
                {
                    Content = "Name",
                    Width = 55,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                DistanceName = new TextBox()
                {
                    Text = theDistance.Name,
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                DistanceName.GotFocus += new RoutedEventHandler(this.SelectAll);
                namePanel.Children.Add(DistanceName);
                nameGrid.Children.Add(namePanel);
                Grid.SetColumn(namePanel, 0);
                DockPanel copyPanel = new DockPanel();
                copyPanel.Children.Add(new Label()
                {
                    Content = "Copy From",
                    Width = 90,
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                CopyFromBox = new ComboBox()
                {
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                CopyFromBox.Items.Add(new ComboBoxItem()
                {
                    Content = "",
                    Uid = "-1"
                });
                foreach (Distance div in otherDistances)
                {
                    CopyFromBox.Items.Add(new ComboBoxItem()
                    {
                        Content = div.Name,
                        Uid = div.Identifier.ToString()
                    });
                }
                CopyFromBox.SelectedIndex = 0;
                CopyFromBox.SelectionChanged += new SelectionChangedEventHandler(this.CopyFromBox_SelectionChanged);
                copyPanel.Children.Add(CopyFromBox);
                nameGrid.Children.Add(copyPanel);
                Grid.SetColumn(copyPanel, 1);
                thePanel.Children.Add(nameGrid);

                // Distance - DistanceUnit - Occurrence
                Grid settingsGrid = new Grid();
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                DockPanel distPanel = new DockPanel();
                distPanel.Children.Add(new Label()
                {
                    Content = "Distance",
                    Width = 75,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                Distance = new TextBox()
                {
                    Text = theDistance.DistanceValue.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                Distance.GotFocus += new RoutedEventHandler(this.SelectAll);
                Distance.PreviewTextInput += new TextCompositionEventHandler(this.DotValidation);
                distPanel.Children.Add(Distance);
                settingsGrid.Children.Add(distPanel);
                Grid.SetColumn(distPanel, 1);
                // Distance Unit
                DistanceUnit = new ComboBox()
                {
                    FontSize = 16,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center
                };
                DistanceUnit.Items.Add(new ComboBoxItem()
                {
                    Content = "",
                    Uid = Constants.Distances.UNKNOWN.ToString()
                });
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
                if (theDistance.DistanceUnit == Constants.Distances.MILES)
                {
                    DistanceUnit.SelectedIndex = 1;
                }
                else if (theDistance.DistanceUnit == Constants.Distances.KILOMETERS)
                {
                    DistanceUnit.SelectedIndex = 2;
                }
                else if (theDistance.DistanceUnit == Constants.Distances.METERS)
                {
                    DistanceUnit.SelectedIndex = 3;
                }
                else if (theDistance.DistanceUnit == Constants.Distances.YARDS)
                {
                    DistanceUnit.SelectedIndex = 4;
                }
                else if (theDistance.DistanceUnit == Constants.Distances.FEET)
                {
                    DistanceUnit.SelectedIndex = 5;
                }
                else
                {
                    DistanceUnit.SelectedIndex = 0;
                }
                settingsGrid.Children.Add(DistanceUnit);
                Grid.SetColumn(DistanceUnit, 2);
                if (Constants.Timing.EVENT_TYPE_TIME != theEvent.EventType)
                {
                    // Occurence
                    DockPanel occPanel = new DockPanel();
                    occPanel.Children.Add(new Label()
                    {
                        Content = "Occurrence",
                        Width = 100,
                        FontSize = 16,
                        Margin = new Thickness(0, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Right
                    });
                    FinishOccurrence = new ComboBox()
                    {
                        FontSize = 16,
                        Margin = new Thickness(0, 5, 0, 5),
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                    ComboBoxItem selected = null, current;
                    for (int i = 1; i <= maxOccurrences; i++)
                    {
                        current = new ComboBoxItem()
                        {
                            Content = i.ToString(),
                            Uid = i.ToString()
                        };
                        if (i == theDistance.FinishOccurrence)
                        {
                            selected = current;
                        }
                        FinishOccurrence.Items.Add(current);
                    }
                    if (selected != null)
                    {
                        FinishOccurrence.SelectedItem = selected;
                    }
                    else
                    {
                        FinishOccurrence.SelectedIndex = 0;
                    }
                    occPanel.Children.Add(FinishOccurrence);
                    settingsGrid.Children.Add(occPanel);
                    Grid.SetColumn(occPanel, 3);
                }
                else
                {
                    DockPanel limitPanel = new DockPanel();
                    limitPanel.Children.Add(new Label()
                    {
                        Content = "Max Time",
                        Width = 80,
                        FontSize = 14,
                        Margin = new Thickness(0, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Right
                    });
                    string limit = string.Format(LimitFormat, theDistance.EndSeconds / 3600,
                        theDistance.EndSeconds % 3600 / 60, theDistance.EndSeconds % 60);
                    TimeLimit = new MaskedTextBox()
                    {
                        Text = limit,
                        Mask = "00:00:00",
                        FontSize = 16,
                        Margin = new Thickness(0, 5, 0, 5),
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                    TimeLimit.GotFocus += new RoutedEventHandler(this.SelectAll);
                    limitPanel.Children.Add(TimeLimit);
                    settingsGrid.Children.Add(limitPanel);
                    Grid.SetColumn(limitPanel, 3);
                }
                thePanel.Children.Add(settingsGrid);

                // Wave #, Start Offset, Bib Group #, Remove Button
                Grid numGrid = new Grid();
                numGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                numGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                DockPanel wavePanel = new DockPanel();
                wavePanel.Children.Add(new Label()
                {
                    Content = "Wave",
                    Width = 55,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                Wave = new TextBox()
                {
                    Text = theDistance.Wave.ToString(),
                    FontSize = 16,
                    Width = 50,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                Wave.GotFocus += new RoutedEventHandler(this.SelectAll);
                Wave.PreviewTextInput += new TextCompositionEventHandler(this.NumberValidation);
                wavePanel.Children.Add(Wave);
                wavePanel.Children.Add(new Label()
                {
                    Content = "Start",
                    Width = 55,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                Uri imgUri = new Uri("pack://application:,,,/img/plus.png");
                waveType = 1;
                if (theDistance.StartOffsetSeconds < 0)
                {
                    Log.D("UI.MainPages.DistancesPage", "Setting type to negative and making seconds/milliseconds positive for offset textbox.");
                    waveType = -1;
                    imgUri = new Uri("pack://application:,,,/img/dash.png");
                    theDistance.StartOffsetSeconds *= -1;
                    theDistance.StartOffsetMilliseconds *= -1;
                }
                WaveTypeImg = new Image()
                {
                    Width = 25,
                    Margin = new Thickness(0,0,3,0),
                    Source = new BitmapImage(imgUri),
                };
                WaveTypeImg.MouseLeftButtonDown += new MouseButtonEventHandler(this.SwapWaveType_Click);
                wavePanel.Children.Add(WaveTypeImg);
                string sOffset = string.Format(TimeFormat, theDistance.StartOffsetSeconds / 3600,
                    theDistance.StartOffsetSeconds % 3600 / 60, theDistance.StartOffsetSeconds % 60,
                    theDistance.StartOffsetMilliseconds);
                StartOffset = new MaskedTextBox()
                {
                    Text = sOffset,
                    Mask = "00:00:00.000",
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                StartOffset.GotFocus += new RoutedEventHandler(this.SelectAll);
                wavePanel.Children.Add(StartOffset);
                numGrid.Children.Add(wavePanel);
                Grid.SetColumn(wavePanel, 0);
                Grid secondGrid = new Grid();
                secondGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                secondGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                AddSubDistance = new Button()
                {
                    Content = "Add Linked",
                    FontSize = 14,
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(0, 5, 0, 5),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                AddSubDistance.Click += new RoutedEventHandler(this.AddSub_Click);
                secondGrid.Children.Add(AddSubDistance);
                Grid.SetColumn(AddSubDistance, 0);
                Remove = new Button()
                {
                    Content = "Remove",
                    FontSize = 14,
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(0, 5, 0, 5),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Remove.Click += new RoutedEventHandler(this.Remove_Click);
                secondGrid.Children.Add(Remove);
                Grid.SetColumn(Remove, 1);
                numGrid.Children.Add(secondGrid);
                Grid.SetColumn(secondGrid, 1);
                thePanel.Children.Add(numGrid);
            }

            private void AddSub_Click(object sender, RoutedEventArgs e)
            {
                Log.D("UI.MainPages.DistancesPage", "Adding sub distance.");
                this.page.AddSubDistance(theDistance);
            }

            private void Remove_Click(object sender, RoutedEventArgs e)
            {
                Log.D("UI.MainPages.DistancesPage", "Removing distance.");
                this.page.RemoveDistance(theDistance);
            }

            private void SwapWaveType_Click(object sender, RoutedEventArgs e)
            {
                Log.D("UI.MainPages.DistancesPage", "Plus/Minus sign clicked. WaveType is: " + waveType);
                if (waveType < 0)
                {
                    WaveTypeImg.Source = new BitmapImage(new Uri("pack://application:,,,/img/plus.png"));
                } else if (waveType > 0)
                {
                    WaveTypeImg.Source = new BitmapImage(new Uri("pack://application:,,,/img/dash.png"));
                } else
                {
                    Log.E("UI.MainPages.DistancesPage", "Something went wrong and the wave type was set to 0.");
                }
                waveType *= -1;
            }

            public Distance GetDistance()
            {
                return theDistance;
            }

            public void UpdateDistance()
            {
                Log.D("UI.MainPages.DistancesPage", "Updating distance.");
                theDistance.Name = DistanceName.Text;
                double dist = 0.0;
                try
                {
                    dist = Convert.ToDouble(Distance.Text);
                }
                catch
                {
                    dist = 0.0;
                }
                if (dist != 0.0)
                {
                    theDistance.DistanceValue = dist;
                }
                theDistance.DistanceUnit = Convert.ToInt32(((ComboBoxItem)DistanceUnit.SelectedItem).Uid);
                if (FinishOccurrence != null && FinishOccurrence.SelectedItem != null)
                {
                    theDistance.FinishOccurrence = Convert.ToInt32(((ComboBoxItem)FinishOccurrence.SelectedItem).Uid);
                }
                theDistance.EndSeconds = 0;
                if (TimeLimit != null)
                {
                    string[] limitParts = TimeLimit.Text.Replace('_', '0').Split(':');
                    theDistance.EndSeconds = (Convert.ToInt32(limitParts[0]) * 3600)
                        + (Convert.ToInt32(limitParts[1]) * 60)
                        + Convert.ToInt32(limitParts[2]);
                }
                int wave = -1;
                int.TryParse(Wave.Text, out wave);
                if (wave >= 0)
                {
                    theDistance.Wave = wave;
                }
                string[] firstparts = StartOffset.Text.Replace('_', '0').Split(':');
                string[] secondparts = firstparts[2].Split('.');
                try
                {
                    theDistance.StartOffsetSeconds = (Convert.ToInt32(firstparts[0]) * 3600)
                        + (Convert.ToInt32(firstparts[1]) * 60)
                        + Convert.ToInt32(secondparts[0]);
                    theDistance.StartOffsetMilliseconds = Convert.ToInt32(secondparts[1]);
                }
                catch
                {
                    System.Windows.MessageBox.Show("Error with values given.");
                }
                if (waveType < 0)
                {
                    Log.D("UI.MainPages.DistancesPage", "Recording negative values.");
                    theDistance.StartOffsetSeconds *= -1;
                    theDistance.StartOffsetMilliseconds *= -1;
                }
            }

            private void SelectAll(object sender, RoutedEventArgs e)
            {
                TextBox src = (TextBox)e.OriginalSource;
                src.SelectAll();
            }

            private void CopyFromBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                Log.D("UI.MainPages.DistancesPage", "Attempting to copy from a different distance! Here we go!");
                // Ensure we've got something selected, it has a parseable UID,
                // and there's a distance related to it
                if (CopyFromBox.SelectedItem != null
                    && int.TryParse(((ComboBoxItem)CopyFromBox.SelectedItem).Uid, out int newDivId)
                    && distanceDictionary.ContainsKey(newDivId))
                {
                    Distance newDiv = distanceDictionary[newDivId];
                    theDistance.Name = DistanceName.Text;
                    theDistance.DistanceValue = newDiv.DistanceValue;
                    theDistance.DistanceUnit = newDiv.DistanceUnit;
                    theDistance.FinishOccurrence = newDiv.FinishOccurrence;
                    theDistance.Wave = newDiv.Wave;
                    theDistance.StartOffsetSeconds = newDiv.StartOffsetSeconds;
                    theDistance.StartOffsetMilliseconds = newDiv.StartOffsetMilliseconds;
                    page.UpdateDistance(theDistance);
                }
            }

            private void DotValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = allowedWithDot.IsMatch(e.Text);
            }

            private void NumberValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = allowedChars.IsMatch(e.Text);
            }
        }

        private void DistanceBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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
