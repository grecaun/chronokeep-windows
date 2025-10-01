using Chronokeep.Database.SQLite;
using Chronokeep.Interfaces;
using Chronokeep.Network.API;
using Chronokeep.Objects.API;
using Chronokeep.Objects.ChronoKeepAPI;
using Chronokeep.Objects;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xceed.Wpf.Toolkit;
using Chronokeep.Helpers;

namespace Chronokeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for DistancesPage.xaml
    /// </summary>
    public partial class DistancesPage : IMainPage
    {
        private readonly IMainWindow mWindow;
        private readonly IDBInterface database;
        private readonly Event theEvent;
        private readonly Dictionary<int, Distance> distanceDictionary = [];
        private readonly Dictionary<int, List<Distance>> subDistanceDictionary = [];
        private readonly HashSet<int> distancesChanged = [];
        private List<Distance> distances;
        private bool UpdateTimingWorker = false;
        private int DistanceCount = 1;

        public DistancesPage(IMainWindow mWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mWindow;
            this.database = database;
            this.theEvent = database.GetCurrentEvent();
            if (theEvent.API_ID > 0 && theEvent.API_Event_ID.Length > 1)
            {
                apiPanel.Visibility = Visibility.Visible;
            }
            else
            {
                apiPanel.Visibility = Visibility.Collapsed;
            }
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
            List<Distance> superDivs = [];
            foreach (Distance div in distances)
            {
                // Check if we're a linked distance
                if (div.LinkedDistance > 0)
                {
                    if (!subDistanceDictionary.TryGetValue(div.LinkedDistance, out List<Distance> oSubDistList))
                    {
                        oSubDistList = [];
                        subDistanceDictionary[div.LinkedDistance] = oSubDistList;
                    }
                    oSubDistList.Add(div);
                }
                else
                {
                    superDivs.Add(div);
                }
            }
            foreach (Distance div in superDivs)
            {
                distanceDictionary[div.Identifier] = div;
                ADistance parent = new(this, div, theEvent.FinishMaxOccurrences, distances, distanceDictionary, theEvent);
                DistancesBox.Items.Add(parent);
                DistanceCount = div.Identifier > DistanceCount - 1 ? div.Identifier + 1 : DistanceCount;
                // Add linked distances
                if (subDistanceDictionary.TryGetValue(div.Identifier, out List<Distance> tSubDistList))
                {
                    foreach (Distance sub in tSubDistList)
                    {
                        DistancesBox.Items.Add(new ASubDistance(this, sub, parent));
                        DistanceCount = sub.Identifier > DistanceCount - 1 ? sub.Identifier + 1 : DistanceCount;
                    }
                }
            }
            if (theEvent.EventType == Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA && distances.Count > 0)
            {
                Add.IsEnabled = false;
            }
            else
            {
                Add.IsEnabled = true;
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.DistancesPage", "Add distance clicked.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            database.AddDistance(new("New Distance " + DistanceCount, theEvent.Identifier));
            UpdateTimingWorker = true;
            UpdateView();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.DistancesPage", "Update clicked.");
            UpdateDatabase();
            UpdateView();
            mWindow.NetworkUpdateResults();
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.DistancesPage", "Revert clicked.");
            UpdateView();
        }

        internal void RemoveDistance(Distance distance)
        {
            Log.D("UI.MainPages.DistancesPage", "Remove distance clicked.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            // Check for and delete linked distances
            List<Distance> allDistances = database.GetDistances(theEvent.Identifier);
            bool keepDeleting = true, ignoreParticipantCheck = false;
            foreach (Distance d in allDistances)
            {
                if (!keepDeleting)
                {
                    return;
                }
                if (d.LinkedDistance >= 0 && d.LinkedDistance == distance.Identifier)
                {
                    if (!ignoreParticipantCheck && database.GetParticipants(theEvent.Identifier, d.Identifier).Count > 0)
                    {
                        keepDeleting = false;
                        DialogBox.Show(
                            "Distance has participants, continue?",
                            "Yes", 
                            "No",
                            () => {
                                keepDeleting = true;
                                ignoreParticipantCheck = true;
                                database.RemoveDistance(d);
                            }
                        );
                    }
                    else
                    {
                        database.RemoveDistance(d);
                    }
                }
            }
            if (!keepDeleting)
            {
                return;
            }
            if (!ignoreParticipantCheck && database.GetParticipants(theEvent.Identifier, distance.Identifier).Count > 0)
            {
                keepDeleting = false;
                DialogBox.Show(
                    "Distance has participants, continue?",
                    "Yes",
                    "No",
                    () => {
                        keepDeleting = true;
                        ignoreParticipantCheck = true;
                        database.RemoveDistance(distance);
                    }
                );
            }
            else
            {
                database.RemoveDistance(distance);
            }
            UpdateTimingWorker = true;
            UpdateView();
        }

        public void UpdateDatabase()
        {
            Dictionary<int, Distance> oldDistances = [];
            foreach (Distance distance in database.GetDistances(theEvent.Identifier))
            {
                oldDistances[distance.Identifier] = distance;
            }
            foreach (ADistanceInterface listDiv in DistancesBox.Items)
            {
                listDiv.UpdateDistance();
                int divId = listDiv.GetDistance().Identifier;
                if (oldDistances.TryGetValue(divId, out Distance oDist) &&
                    (oDist.StartOffsetSeconds != listDiv.GetDistance().StartOffsetSeconds
                    || oDist.StartOffsetMilliseconds != listDiv.GetDistance().StartOffsetMilliseconds
                    || oDist.FinishOccurrence != listDiv.GetDistance().FinishOccurrence) )
                {
                    distancesChanged.Add(divId);
                    UpdateTimingWorker = true;
                }
                database.UpdateDistance(listDiv.GetDistance());
            }
            if (database is SQLiteInterface)
            {
                Results.GetStaticVariables(database);
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
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            if (UpdateTimingWorker || distancesChanged.Count > 0)
            {
                database.ResetTimingResultsEvent(theEvent.Identifier);
                mWindow.NotifyTimingWorker();
                mWindow.UpdateRegistrationDistances();
                mWindow.NetworkUpdateResults();
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
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            database.AddDistance(new(theDistance.Name + " Linked " + DistanceCount, theDistance.EventIdentifier, theDistance.Identifier, Constants.Timing.DISTANCE_TYPE_EARLY, 1, theDistance.Wave, theDistance.StartOffsetSeconds, theDistance.StartOffsetMilliseconds));
            UpdateTimingWorker = true;
            UpdateView();
        }

        private interface ADistanceInterface
        {
            Distance GetDistance();
            void UpdateDistance();
        }

        private partial class ASubDistance : ListBoxItem, ADistanceInterface
        {
            public TextBox DistanceName { get; private set; }
            public TextBox Wave { get; private set; }
            public TextBlock WaveType { get; private set; }
            public TextBox Ranking { get; private set; }
            public MaskedTextBox StartOffset { get; private set; }
            public ComboBox TypeBox { get; private set; }
            public Button Remove { get; private set; }

            private const string TimeFormat = "{0:D2}:{1:D2}:{2:D2}.{3:D3}";

            readonly DistancesPage page;
            public Distance theDistance;

            private ADistance parent;
            private int waveType = 1;

            [GeneratedRegex("[^0-9.]")]
            private static partial Regex AllowedWithDot();
            [GeneratedRegex("[^0-9]")]
            private static partial Regex AllowedChars();

            public ASubDistance(DistancesPage page, Distance distance, ADistance parent)
            {
                this.page = page;
                this.parent = parent;
                this.theDistance = distance;
                StackPanel thePanel = new()
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
                DockPanel namePanel = new();
                namePanel.Children.Add(new TextBlock()
                {
                    Text = "Name",
                    Width = 55,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
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
                DockPanel rankPanel = new();
                rankPanel.Children.Add(new TextBlock()
                {
                    Text = "Rank Priority",
                    Width = 135,
                    FontSize = 16,
                    Margin = new Thickness(10, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
                Ranking = new TextBox
                {
                    Text = theDistance.Ranking.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                Ranking.GotFocus += new RoutedEventHandler(this.SelectAll);
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
                DockPanel wavePanel = new();
                wavePanel.Children.Add(new TextBlock()
                {
                    Text = "Wave",
                    Width = 55,
                    FontSize = 16,
                    Margin = new Thickness(10, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
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
                wavePanel.Children.Add(new TextBlock()
                {
                    Text = "Start",
                    Width = 50,
                    FontSize = 16,
                    Margin = new Thickness(10, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
                string waveText = "+";
                waveType = 1;
                if (theDistance.StartOffsetSeconds < 0)
                {
                    Log.D("UI.MainPages.DistancesPage", "Setting type to negative and making seconds/milliseconds positive for offset textbox.");
                    waveType = -1;
                    waveText = "-";
                    theDistance.StartOffsetSeconds *= -1;
                    theDistance.StartOffsetMilliseconds *= -1;
                }
                WaveType = new TextBlock()
                {
                    Width = 25,
                    Margin = new Thickness(0, 0, 3, 0),
                    Text = waveText,
                    FontSize = 30,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };
                WaveType.MouseLeftButtonDown += new MouseButtonEventHandler(this.SwapWaveType_Click);
                wavePanel.Children.Add(WaveType);
                string sOffset = string.Format(TimeFormat, theDistance.StartOffsetSeconds / 3600,
                    (theDistance.StartOffsetSeconds % 3600) / 60, theDistance.StartOffsetSeconds % 60,
                    theDistance.StartOffsetMilliseconds);
                StartOffset = new MaskedTextBox()
                {
                    Text = sOffset,
                    Mask = "00:00:00.000",
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };
                StartOffset.GotFocus += new RoutedEventHandler(this.SelectAll);
                wavePanel.Children.Add(StartOffset);
                wavePanel.Children.Add(new TextBlock()
                {
                    Text = "Type",
                    Width = 55,
                    FontSize = 16,
                    Margin = new Thickness(10, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
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
                        Content = "Normal",
                        Uid = Constants.Timing.DISTANCE_TYPE_NORMAL.ToString()
                    });
                TypeBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = "Early Start",
                        Uid = Constants.Timing.DISTANCE_TYPE_EARLY.ToString()
                    });
                TypeBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = "Late Start",
                        Uid = Constants.Timing.DISTANCE_TYPE_LATE.ToString()
                    });
                TypeBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = "Drop",
                        Uid = Constants.Timing.DISTANCE_TYPE_DROP.ToString()
                    });
                TypeBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = "Unranked",
                        Uid = Constants.Timing.DISTANCE_TYPE_UNOFFICIAL.ToString()
                    });
                TypeBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = "Virtual",
                        Uid = Constants.Timing.DISTANCE_TYPE_VIRTUAL.ToString()
                    });
                if (theDistance.Type == Constants.Timing.DISTANCE_TYPE_EARLY)
                {
                    TypeBox.SelectedIndex = 1;
                }
                else if (theDistance.Type == Constants.Timing.DISTANCE_TYPE_LATE)
                {
                    Ranking.Text = "0";
                    Ranking.IsEnabled = false;
                    TypeBox.SelectedIndex = 2;
                }
                else if (theDistance.Type == Constants.Timing.DISTANCE_TYPE_DROP)
                {
                    TypeBox.SelectedIndex = 3;
                }
                else if (theDistance.Type == Constants.Timing.DISTANCE_TYPE_UNOFFICIAL)
                {
                    TypeBox.SelectedIndex = 4;
                }
                else if (theDistance.Type == Constants.Timing.DISTANCE_TYPE_VIRTUAL)
                {
                    TypeBox.SelectedIndex = 5;
                }
                else if (theDistance.Type == Constants.Timing.DISTANCE_TYPE_NORMAL)
                {
                    TypeBox.SelectedIndex = 0;
                }
                TypeBox.SelectionChanged += TypeBox_SelectionChanged;
                wavePanel.Children.Add(TypeBox);
                thePanel.Children.Add(wavePanel);
            }

            private void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                if (TypeBox.SelectedIndex == 2)
                {
                    Ranking.Text = "0";
                    Ranking.IsEnabled = false;
                }
                else
                {
                    Ranking.Text = theDistance.Ranking.ToString();
                    Ranking.IsEnabled = true;
                }
            }

            private void Remove_Click(object sender, RoutedEventArgs e)
            {
                Log.D("UI.MainPages.DistancesPage", "Removing distance.");
                this.page.RemoveDistance(theDistance);
            }

            private void DotValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = AllowedWithDot().IsMatch(e.Text);
            }

            private void NumberValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = AllowedChars().IsMatch(e.Text);
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
                    WaveType.Text = "+";
                }
                else if (waveType > 0)
                {
                    WaveType.Text = "-";
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
                int typeVal = -1;
                if (TypeBox.SelectedItem != null)
                {
                    int.TryParse(((ComboBoxItem)TypeBox.SelectedItem).Uid, out typeVal);
                }
                theDistance.Type = typeVal != -1 ? typeVal : Constants.Timing.DISTANCE_TYPE_EARLY;
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
                    DialogBox.Show("Error with values given.");
                }
                if (waveType < 0)
                {
                    Log.D("UI.MainPages.DistancesPage", "Recording negative values.");
                    theDistance.StartOffsetSeconds *= -1;
                    theDistance.StartOffsetMilliseconds *= -1;
                }
                theDistance.Upload = false;
            }

        }

        private partial class ADistance : ListBoxItem, ADistanceInterface
        {
            public TextBox DistanceName { get; private set; }
            public TextBox Certification { get; private set; }
            public ComboBox CopyFromBox { get; private set; }
            public TextBox Distance { get; private set; }
            public ComboBox DistanceUnit { get; private set; }
            public ComboBox FinishOccurrence { get; private set; } = null;
            public TextBox Wave { get; private set; }
            public TextBlock WaveType { get; private set; }
            public MaskedTextBox StartOffset { get; private set; }
            public MaskedTextBox TimeLimit { get; private set; } = null;
            public Wpf.Ui.Controls.ToggleSwitch Upload { get; private set; }
            public Button AddSubDistance { get; private set; }
            public Button Remove { get; private set; }

            private const string TimeFormat = "{0:D2}:{1:D2}:{2:D2}.{3:D3}";
            private const string LimitFormat = "{0:D2}:{1:D2}:{2:D2}";
            readonly DistancesPage page;
            public Distance theDistance;
            private Dictionary<int, Distance> distanceDictionary;
            private int waveType = 1;

            [GeneratedRegex("[^0-9.]")]
            private static partial Regex AllowedWithDot();
            [GeneratedRegex("[^0-9]")]
            private static partial Regex AllowedChars();

            public ADistance(DistancesPage page, Distance distance, int maxOccurrences,
                List<Distance> distances, Dictionary<int, Distance> distanceDictionary, Event theEvent)
            {
                List<Distance> otherDistances = [.. distances];
                this.distanceDictionary = distanceDictionary;
                otherDistances.Remove(distance);
                this.page = page;
                this.theDistance = distance;
                StackPanel thePanel = new()
                {
                    MaxWidth = theEvent.UploadSpecific == true ? 700 : 600
                };
                this.Content = thePanel;
                this.IsTabStop = false;

                // Name Grid (Name NameBox -- Copy From DistancesBox)
                Grid nameGrid = new();
                nameGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                nameGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                // Name information.
                DockPanel namePanel = new();
                namePanel.Children.Add(new TextBlock()
                {
                    Text = "Name",
                    Width = 55,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
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
                DockPanel copyPanel = new();
                copyPanel.Children.Add(new TextBlock()
                {
                    Text = "Copy From",
                    Width = 90,
                    FontSize = 16,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center
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
                Grid settingsGrid = new();
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                DockPanel distPanel = new();
                distPanel.Children.Add(new TextBlock()
                {
                    Text = "Distance",
                    Width = 75,
                    FontSize = 16,
                    Margin = new Thickness(10, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
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
                if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                {
                    // Occurence
                    DockPanel occPanel = new();
                    occPanel.Children.Add(new TextBlock()
                    {
                        Text = "Occurrence",
                        Width = 75,
                        FontSize = 12,
                        Margin = new Thickness(10, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
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
                    DockPanel limitPanel = new();
                    limitPanel.Children.Add(new TextBlock()
                    {
                        Text = "Max Time",
                        Width = 65,
                        FontSize = 12,
                        Margin = new Thickness(10, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
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

                // Wave #, Start Offset, Bib Group #, Remove Button (Upload Checkbox)
                Grid numGrid = new Grid();
                if (theEvent.UploadSpecific == true)
                {
                    numGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(150) });
                }
                numGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(4, GridUnitType.Star) });
                numGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Star) });
                int columnOffset = theEvent.UploadSpecific ? 1 : 0;
                DockPanel wavePanel = new()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                if (Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA != theEvent.EventType)
                {
                    wavePanel.Children.Add(new TextBlock()
                    {
                        Text = "Wave",
                        Width = 55,
                        FontSize = 16,
                        Margin = new Thickness(10, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
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
                    wavePanel.Children.Add(new TextBlock()
                    {
                        Text = "Start",
                        Width = 50,
                        FontSize = 16,
                        Margin = new Thickness(10, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    string waveText = "+";
                    waveType = 1;
                    if (theDistance.StartOffsetSeconds < 0)
                    {
                        Log.D("UI.MainPages.DistancesPage", "Setting type to negative and making seconds/milliseconds positive for offset textbox.");
                        waveType = -1;
                        waveText = "-";
                        theDistance.StartOffsetSeconds *= -1;
                        theDistance.StartOffsetMilliseconds *= -1;
                    }
                    WaveType = new TextBlock()
                    {
                        Width = 25,
                        Margin = new Thickness(0, 0, 3, 0),
                        Text = waveText,
                        FontSize = 30,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    };
                    WaveType.MouseLeftButtonDown += new MouseButtonEventHandler(this.SwapWaveType_Click);
                    wavePanel.Children.Add(WaveType);
                }
                else
                {
                    wavePanel.Children.Add(new TextBlock()
                    {
                        Text = "Interval",
                        Width = 75,
                        FontSize = 16,
                        Margin = new Thickness(10, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Right
                    });
                }
                string sOffset = string.Format(TimeFormat, theDistance.StartOffsetSeconds / 3600,
                    theDistance.StartOffsetSeconds % 3600 / 60, theDistance.StartOffsetSeconds % 60,
                    theDistance.StartOffsetMilliseconds);
                StartOffset = new MaskedTextBox()
                {
                    Text = sOffset,
                    Mask = "00:00:00.000",
                    FontSize = 16,
                    Height = 35,
                    Width = 125,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                StartOffset.GotFocus += new RoutedEventHandler(this.SelectAll);
                wavePanel.Children.Add(StartOffset);
                numGrid.Children.Add(wavePanel);
                Grid.SetColumn(wavePanel, columnOffset);
                columnOffset++;
                DockPanel certificationPanel = new()
                {
                    VerticalAlignment = VerticalAlignment.Center
                };
                certificationPanel.Children.Add(new TextBlock()
                {
                    Text = "Certification",
                    Width = 65,
                    FontSize = 12,
                    Margin = new Thickness(6, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
                Certification = new TextBox()
                {
                    Text = theDistance.Certification.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                certificationPanel.Children.Add(Certification);
                numGrid.Children.Add(certificationPanel);
                Grid.SetColumn(certificationPanel, columnOffset);
                if (theEvent.UploadSpecific == true)
                {
#pragma warning disable CA1416 // Validate platform compatibility -- Program isn't compiled for Windows < 7, but this warning pops up because...
                    Upload = new()
                    {
                        Content = "Upload Results",
                        FontSize = 16,
                        IsChecked = theDistance.Upload == true,
                        Margin = new Thickness(10, 5, 0, 5),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    };
#pragma warning restore CA1416 // Validate platform compatibility
                    numGrid.Children.Add(Upload);
                    Grid.SetColumn(Upload, 0);
                }
                thePanel.Children.Add(numGrid);
                DockPanel secondGrid = new()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                AddSubDistance = new Button()
                {
                    Content = "Add Linked",
                    FontSize = 14,
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(0, 5, 5, 5),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                AddSubDistance.Click += new RoutedEventHandler(this.AddSub_Click);
                secondGrid.Children.Add(AddSubDistance);
                Remove = new Button()
                {
                    Content = "Remove",
                    FontSize = 14,
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(5, 5, 0, 5),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Remove.Click += new RoutedEventHandler(this.Remove_Click);
                secondGrid.Children.Add(Remove);
                thePanel.Children.Add(secondGrid);
                if (theEvent.EventType == Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA)
                {
                    copyPanel.Visibility = Visibility.Collapsed;
                    AddSubDistance.Visibility = Visibility.Collapsed;
                    secondGrid.Children.Remove(Remove);
                }
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
                    WaveType.Text = "+";
                }
                else if (waveType > 0)
                {
                    WaveType.Text = "-";
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
                Log.D("UI.MainPages.DistancesPage", "Updating distance.");
                theDistance.Name = DistanceName.Text;
                double dist;
                try
                {
                    dist = Convert.ToDouble(Distance.Text);
                }
                catch
                {
                    dist = 0.0;
                }
                if (dist >= 0.0)
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
                if (Wave != null)
                {
                    if (!int.TryParse(Wave.Text, out wave))
                    {
                        theDistance.Wave = -1;
                    }
                }
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
                    DialogBox.Show("Error with values given.");
                }
                if (waveType < 0)
                {
                    Log.D("UI.MainPages.DistancesPage", "Recording negative values.");
                    theDistance.StartOffsetSeconds *= -1;
                    theDistance.StartOffsetMilliseconds *= -1;
                }
                if (Upload != null)
                {
                    theDistance.Upload = Upload.IsChecked == true;
                }
                else
                {
                    theDistance.Upload = true;
                }
                if (Certification != null)
                {
                    theDistance.Certification = Certification.Text;
                }
                else
                {
                    theDistance.Certification = "";
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
                    && distanceDictionary.TryGetValue(newDivId, out Distance newDiv))
                {
                    theDistance.Name = DistanceName.Text;
                    theDistance.DistanceValue = newDiv.DistanceValue;
                    theDistance.DistanceUnit = newDiv.DistanceUnit;
                    theDistance.FinishOccurrence = newDiv.FinishOccurrence;
                    theDistance.Wave = newDiv.Wave;
                    theDistance.StartOffsetSeconds = newDiv.StartOffsetSeconds;
                    theDistance.StartOffsetMilliseconds = newDiv.StartOffsetMilliseconds;
                    theDistance.Upload = newDiv.Upload;
                    page.UpdateDistance(theDistance);
                }
            }

            private void DotValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = AllowedWithDot().IsMatch(e.Text);
            }

            private void NumberValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = AllowedChars().IsMatch(e.Text);
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

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.DistancesPage", "Uploading distances.");
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
                // Save distances displayed
                UpdateDatabase();
                UpdateView();
                // Get Distances and Locations to get their names
                List<APIDistance> distances = [];
                foreach (Distance d in database.GetDistances(theEvent.Identifier))
                {
                    if (d.Certification.Trim().Length > 0)
                    {
                        distances.Add(new()
                        {
                            Name = d.Name.Trim(),
                            Certification = d.Certification.Trim(),
                        });
                    }
                }
                if (distances.Count > 0)
                {
                    Log.D("UI.MainPages.DistancesPage", "Attempting to upload " + distances.Count.ToString() + " distances.");
                    try
                    {
                        GetDistancesResponse response = await APIHandlers.AddDistances(api, event_ids[0], event_ids[1], distances);
                        if (response == null || response.Distances == null)
                        {
                            DialogBox.Show("Error uploading distances.");
                        }
                        else if (response.Distances.Count != distances.Count)
                        {
                            DialogBox.Show("Error uploading distances. Uploaded count doesn't match.");
                        }
                    }
                    catch (APIException ex)
                    {
                        DialogBox.Show(ex.Message);
                    }
                }
                UploadButton.IsEnabled = true;
                UploadButton.Content = "Upload";
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.DistancesPage", "Deleting uploaded distances.");
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
                    await APIHandlers.DeleteDistances(api, event_ids[0], event_ids[1]);
                }
                catch (APIException ex)
                {
                    DialogBox.Show(ex.Message);
                }
                DeleteButton.IsEnabled = true;
                DeleteButton.Content = "Delete Uploaded";
            }
        }
    }
}
