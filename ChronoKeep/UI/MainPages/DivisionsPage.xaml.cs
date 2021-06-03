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
    /// Interaction logic for DivisionsPage.xaml
    /// </summary>
    public partial class DivisionsPage : Page, IMainPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;
        private List<BibGroup> bibGroups;
        private List<Division> divisions;
        private Dictionary<int, Division> divisionDictionary = new Dictionary<int, Division>();
        private Dictionary<int, List<Division>> subDivisionDictionary = new Dictionary<int, List<Division>>();
        private HashSet<int> divisionsChanged = new HashSet<int>();
        private bool UpdateTimingWorker = false;
        private int DivisionCount = 1;

        public DivisionsPage(IMainWindow mWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mWindow;
            this.database = database;
            this.theEvent = database.GetCurrentEvent();
            if (theEvent != null)
            {
                bibGroups = database.GetBibGroups(theEvent.Identifier);
                bibGroups.Insert(0, new BibGroup(theEvent.Identifier));
            }
            UpdateView();
        }

        public void UpdateView()
        {
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            DivisionsBox.Items.Clear();
            divisions = database.GetDivisions(theEvent.Identifier);
            DivisionCount = 1;
            divisions.Sort();
            divisionDictionary.Clear();
            subDivisionDictionary.Clear();
            List<Division> superDivs = new List<Division>();
            foreach (Division div in divisions)
            {
                // Check if we're a linked division
                if (div.LinkedDivision > 0)
                {
                    if (!subDivisionDictionary.ContainsKey(div.LinkedDivision))
                    {
                        subDivisionDictionary[div.LinkedDivision] = new List<Division>();
                    }
                    subDivisionDictionary[div.LinkedDivision].Add(div);
                }
                else
                {
                    superDivs.Add(div);
                }
            }
            foreach (Division div in superDivs)
            {
                divisionDictionary[div.Identifier] = div;
                DivisionsBox.Items.Add(new ADivision(this, div, theEvent.FinishMaxOccurrences, bibGroups, divisions, divisionDictionary, theEvent));
                DivisionCount = div.Identifier > DivisionCount - 1 ? div.Identifier + 1 : DivisionCount;
                // Add linked divisions
                if (subDivisionDictionary.ContainsKey(div.Identifier))
                {
                    foreach (Division sub in subDivisionDictionary[div.Identifier])
                    {
                        DivisionsBox.Items.Add(new ASubDivision(this, sub, theEvent));
                        DivisionCount = sub.Identifier > DivisionCount - 1 ? sub.Identifier + 1 : DivisionCount;
                    }
                }
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Add division clicked.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            database.AddDivision(new Division("New Division " + DivisionCount, theEvent.Identifier, 0));
            UpdateTimingWorker = true;
            UpdateView();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Update clicked.");
            UpdateDatabase();
            UpdateView();
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Revert clicked.");
            UpdateView();
        }

        internal void RemoveDivision(Division division)
        {
            Log.D("Remove division clicked.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            database.RemoveDivision(division);
            UpdateTimingWorker = true;
            UpdateView();
        }

        public void UpdateDatabase()
        {
            Dictionary<int, Division> oldDivisions = new Dictionary<int, Division>();
            foreach (Division division in database.GetDivisions(theEvent.Identifier))
            {
                oldDivisions[division.Identifier] = division;
            }
            foreach (ADivisionInterface listDiv in DivisionsBox.Items)
            {
                listDiv.UpdateDivision();
                int divId = listDiv.GetDivision().Identifier;
                if (oldDivisions.ContainsKey(divId) &&
                    (oldDivisions[divId].StartOffsetSeconds != listDiv.GetDivision().StartOffsetSeconds
                    || oldDivisions[divId].StartOffsetMilliseconds != listDiv.GetDivision().StartOffsetMilliseconds
                    || oldDivisions[divId].FinishOccurrence != listDiv.GetDivision().FinishOccurrence) )
                {
                    divisionsChanged.Add(divId);
                    UpdateTimingWorker = true;
                }
                database.UpdateDivision(listDiv.GetDivision());
            }
        }

        public void Keyboard_Ctrl_A()
        {
            Log.D("Ctrl + A Passed to this page.");
            Add_Click(null, null);
        }

        public void Keyboard_Ctrl_S()
        {
            Log.D("Ctrl + S Passed to this page.");
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
            if (UpdateTimingWorker || divisionsChanged.Count > 0)
            {
                database.ResetTimingResultsEvent(theEvent.Identifier);
                mWindow.NotifyTimingWorker();
            }
        }

        public void UpdateDivision(Division division)
        {
            int divId = division.Identifier;
            Division oldDiv = database.GetDivision(divId);
            if (oldDiv.StartOffsetSeconds != division.StartOffsetSeconds ||
                oldDiv.StartOffsetMilliseconds != division.StartOffsetMilliseconds
                || oldDiv.FinishOccurrence != division.FinishOccurrence)
            {
                divisionsChanged.Add(divId);
            }
            database.UpdateDivision(division);
            UpdateView();
        }

        public void AddSubDivision(Division theDivision)
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            database.AddDivision(new Division(theDivision.Name + " Linked " + DivisionCount, theDivision.EventIdentifier, theDivision.Identifier, Constants.Timing.DIVISION_TYPE_EARLY, 1, theDivision.Wave, theDivision.StartOffsetSeconds, theDivision.StartOffsetMilliseconds));
            UpdateTimingWorker = true;
            UpdateView();
        }

        private interface ADivisionInterface
        {
            Division GetDivision();
            void UpdateDivision();
        }

        private class ASubDivision : ListBoxItem, ADivisionInterface
        {
            public TextBox DivisionName { get; private set; }
            public TextBox Wave { get; private set; }
            public TextBox Ranking { get; private set; }
            public MaskedTextBox StartOffset { get; private set; }
            public ComboBox TypeBox { get; private set; }
            public Button Remove { get; private set; }

            private const string TimeFormat = "{0:D2}:{1:D2}:{2:D2}.{3:D3}";

            readonly DivisionsPage page;
            public Division theDivision;

            private readonly Regex allowedWithDot = new Regex("[^0-9.]");
            private readonly Regex allowedChars = new Regex("[^0-9]");

            public ASubDivision(DivisionsPage page, Division division, Event theEvent)
            {
                this.page = page;
                this.theDivision = division;
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
                DivisionName = new TextBox()
                {
                    Text = theDivision.Name,
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                DivisionName.GotFocus += new RoutedEventHandler(this.SelectAll);
                namePanel.Children.Add(DivisionName);
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
                    Text = theDivision.Ranking.ToString(),
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
                Wave = new TextBox()
                {
                    Text = theDivision.Wave.ToString(),
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
                string sOffset = string.Format(TimeFormat, theDivision.StartOffsetSeconds / 3600,
                    (theDivision.StartOffsetSeconds % 3600) / 60, theDivision.StartOffsetSeconds % 60,
                    theDivision.StartOffsetMilliseconds);
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
                        Uid = Constants.Timing.DIVISION_TYPE_EARLY.ToString()
                    });
                TypeBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = "Unranked",
                        Uid = Constants.Timing.DIVISION_TYPE_UNOFFICIAL.ToString()
                    });
                if (theDivision.Type == Constants.Timing.DIVISION_TYPE_EARLY)
                {
                    TypeBox.SelectedIndex = 0;
                }
                else if (theDivision.Type == Constants.Timing.DIVISION_TYPE_UNOFFICIAL)
                {
                    TypeBox.SelectedIndex = 1;
                }
                wavePanel.Children.Add(TypeBox);
                thePanel.Children.Add(wavePanel);
            }

            private void Remove_Click(object sender, RoutedEventArgs e)
            {
                Log.D("Removing division.");
                this.page.RemoveDivision(theDivision);
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

            public Division GetDivision()
            {
                return theDivision;
            }

            public void UpdateDivision()
            {
                Log.D("Updating sub division.");
                theDivision.Name = DivisionName.Text;
                theDivision.Cost = 0;
                theDivision.Distance = 0.0;
                theDivision.EndSeconds = 0;
                theDivision.Type = TypeBox.SelectedIndex == 0 ? Constants.Timing.DIVISION_TYPE_EARLY : Constants.Timing.DIVISION_TYPE_UNOFFICIAL;
                int ranking;
                int.TryParse(Ranking.Text, out ranking);
                if (ranking >= 0)
                {
                    theDivision.Ranking = ranking;
                }
                int wave;
                int.TryParse(Wave.Text, out wave);
                if (wave >= 0)
                {
                    theDivision.Wave = wave;
                }
                theDivision.BibGroupNumber = Constants.Timing.DEFAULT_BIB_GROUP;
                string[] firstparts = StartOffset.Text.Replace('_', '0').Split(':');
                string[] secondparts = firstparts[2].Split('.');
                try
                {
                    theDivision.StartOffsetSeconds = (Convert.ToInt32(firstparts[0]) * 3600)
                        + (Convert.ToInt32(firstparts[1]) * 60)
                        + Convert.ToInt32(secondparts[0]);
                    theDivision.StartOffsetMilliseconds = Convert.ToInt32(secondparts[1]);
                }
                catch
                {
                    System.Windows.MessageBox.Show("Error with values given.");
                }
            }

        }

        private class ADivision : ListBoxItem, ADivisionInterface
        {
            public TextBox DivisionName { get; private set; }
            public ComboBox CopyFromBox { get; private set; }
            public TextBox Cost { get; private set; }
            public TextBox Distance { get; private set; }
            public ComboBox DistanceUnit { get; private set; }
            public ComboBox FinishOccurrence { get; private set; } = null;
            public TextBox Wave { get; private set; }
            public ComboBox BibGroupNumber { get; private set; }
            public MaskedTextBox StartOffset { get; private set; }
            public MaskedTextBox TimeLimit { get; private set; } = null;
            public Button AddSubDivision { get; private set; }
            public Button Remove { get; private set; }

            private const string TimeFormat = "{0:D2}:{1:D2}:{2:D2}.{3:D3}";
            private const string LimitFormat = "{0:D2}:{1:D2}:{2:D2}";
            readonly DivisionsPage page;
            public Division theDivision;
            private Dictionary<int, Division> divisionDictionary;

            private readonly Regex allowedWithDot = new Regex("[^0-9.]");
            private readonly Regex allowedChars = new Regex("[^0-9]");

            public ADivision(DivisionsPage page, Division division, int maxOccurrences, List<BibGroup> bibGroups,
                List<Division> divisions, Dictionary<int, Division> divisionDictionary, Event theEvent)
            {
                List<Division> otherDivisions = new List<Division>(divisions);
                this.divisionDictionary = divisionDictionary;
                otherDivisions.Remove(division);
                this.page = page;
                this.theDivision = division;
                StackPanel thePanel = new StackPanel()
                {
                    MaxWidth = 600
                };
                this.Content = thePanel;
                this.IsTabStop = false;

                // Name Grid (Name NameBox -- Copy From DivisionsBox)
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
                DivisionName = new TextBox()
                {
                    Text = theDivision.Name,
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                DivisionName.GotFocus += new RoutedEventHandler(this.SelectAll);
                namePanel.Children.Add(DivisionName);
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
                foreach (Division div in otherDivisions)
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

                // Cost - Distance - DistanceUnit - Occurrence
                Grid settingsGrid = new Grid();
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                DockPanel costPanel = new DockPanel();
                costPanel.Children.Add(new Label()
                {
                    Content = "Price",
                    Width = 55,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                string costStr = String.Format("{0:D1}.{1:D2}", theDivision.Cost / 100, theDivision.Cost % 100);
                Cost = new TextBox()
                {
                    Text = costStr,
                    FontSize = 16,
                    Margin = new Thickness(0, 5, 0, 5),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                Cost.GotFocus += new RoutedEventHandler(this.SelectAll);
                Cost.PreviewTextInput += new TextCompositionEventHandler(this.DotValidation);
                costPanel.Children.Add(Cost);
                settingsGrid.Children.Add(costPanel);
                Grid.SetColumn(costPanel, 0);
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
                    Text = theDivision.Distance.ToString(),
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
                if (theDivision.DistanceUnit == Constants.Distances.MILES)
                {
                    DistanceUnit.SelectedIndex = 1;
                }
                else if (theDivision.DistanceUnit == Constants.Distances.KILOMETERS)
                {
                    DistanceUnit.SelectedIndex = 2;
                }
                else if (theDivision.DistanceUnit == Constants.Distances.METERS)
                {
                    DistanceUnit.SelectedIndex = 3;
                }
                else if (theDivision.DistanceUnit == Constants.Distances.YARDS)
                {
                    DistanceUnit.SelectedIndex = 4;
                }
                else if (theDivision.DistanceUnit == Constants.Distances.FEET)
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
                        if (i == theDivision.FinishOccurrence)
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
                    string limit = string.Format(LimitFormat, theDivision.EndSeconds / 3600,
                        (theDivision.EndSeconds % 3600) / 60, theDivision.EndSeconds % 60);
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
                    Text = theDivision.Wave.ToString(),
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
                string sOffset = string.Format(TimeFormat, theDivision.StartOffsetSeconds / 3600,
                    (theDivision.StartOffsetSeconds % 3600) / 60, theDivision.StartOffsetSeconds % 60,
                    theDivision.StartOffsetMilliseconds);
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
                AddSubDivision = new Button()
                {
                    Content = "Add Linked",
                    FontSize = 14,
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(0, 5, 0, 5),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                AddSubDivision.Click += new RoutedEventHandler(this.AddSub_Click);
                secondGrid.Children.Add(AddSubDivision);
                Grid.SetColumn(AddSubDivision, 0);
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
                Log.D("Adding sub division.");
                this.page.AddSubDivision(theDivision);
            }

            private void Remove_Click(object sender, RoutedEventArgs e)
            {
                Log.D("Removing division.");
                this.page.RemoveDivision(theDivision);
            }

            public Division GetDivision()
            {
                return theDivision;
            }

            public void UpdateDivision()
            {
                Log.D("Updating division.");
                theDivision.Name = DivisionName.Text;
                string[] costVals = Cost.Text.Split('.');
                int cost = 0;
                if (costVals.Length > 0)
                {
                    int.TryParse(costVals[0].Trim(), out cost);
                }
                cost = cost * 100;
                int cents = 0;
                if (costVals.Length > 1)
                {
                    int.TryParse(costVals[1].Trim(), out cents);
                }
                while (cents > 100)
                {
                    cents = cents / 100;
                }
                cost += cents;
                theDivision.Cost = cost;
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
                    theDivision.Distance = dist;
                }
                theDivision.DistanceUnit = Convert.ToInt32(((ComboBoxItem)DistanceUnit.SelectedItem).Uid);
                if (FinishOccurrence != null && FinishOccurrence.SelectedItem != null)
                {
                    theDivision.FinishOccurrence = Convert.ToInt32(((ComboBoxItem)FinishOccurrence.SelectedItem).Uid);
                }
                theDivision.EndSeconds = 0;
                if (TimeLimit != null)
                {
                    string[] limitParts = TimeLimit.Text.Replace('_', '0').Split(':');
                    theDivision.EndSeconds = (Convert.ToInt32(limitParts[0]) * 3600)
                        + (Convert.ToInt32(limitParts[1]) * 60)
                        + Convert.ToInt32(limitParts[2]);
                }
                int wave = -1;
                int.TryParse(Wave.Text, out wave);
                if (wave >= 0)
                {
                    theDivision.Wave = wave;
                }
                theDivision.BibGroupNumber = Constants.Timing.DEFAULT_BIB_GROUP;
                string[] firstparts = StartOffset.Text.Replace('_', '0').Split(':');
                string[] secondparts = firstparts[2].Split('.');
                try
                {
                    theDivision.StartOffsetSeconds = (Convert.ToInt32(firstparts[0]) * 3600)
                        + (Convert.ToInt32(firstparts[1]) * 60)
                        + Convert.ToInt32(secondparts[0]);
                    theDivision.StartOffsetMilliseconds = Convert.ToInt32(secondparts[1]);
                }
                catch
                {
                    System.Windows.MessageBox.Show("Error with values given.");
                }
            }

            private void SelectAll(object sender, RoutedEventArgs e)
            {
                TextBox src = (TextBox)e.OriginalSource;
                src.SelectAll();
            }

            private void CopyFromBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                Log.D("Attempting to copy from a different division! Here we go!");
                // Ensure we've got something selected, it has a parseable UID,
                // and there's a division related to it
                if (CopyFromBox.SelectedItem != null
                    && int.TryParse(((ComboBoxItem)CopyFromBox.SelectedItem).Uid, out int newDivId)
                    && divisionDictionary.ContainsKey(newDivId))
                {
                    Division newDiv = divisionDictionary[newDivId];
                    theDivision.Name = DivisionName.Text;
                    theDivision.BibGroupNumber = newDiv.BibGroupNumber;
                    theDivision.Cost = newDiv.Cost;
                    theDivision.Distance = newDiv.Distance;
                    theDivision.DistanceUnit = newDiv.DistanceUnit;
                    theDivision.FinishOccurrence = newDiv.FinishOccurrence;
                    theDivision.Wave = newDiv.Wave;
                    theDivision.StartOffsetSeconds = newDiv.StartOffsetSeconds;
                    theDivision.StartOffsetMilliseconds = newDiv.StartOffsetMilliseconds;
                    page.UpdateDivision(theDivision);
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

        private void DivisionsBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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
