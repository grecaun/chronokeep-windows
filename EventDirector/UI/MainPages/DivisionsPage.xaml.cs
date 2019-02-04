using EventDirector.Interfaces;
using EventDirector.Objects;
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

namespace EventDirector.UI.MainPages
{
    /// <summary>
    /// Interaction logic for DivisionsPage.xaml
    /// </summary>
    public partial class DivisionsPage : Page, IMainPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;
        private List<TimingLocation> locations;
        private List<BibGroup> bibGroups;
        private int DivisionCount = 1;

        public DivisionsPage(IMainWindow mWindow, IDBInterface database)
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
            List<Division> divisions = database.GetDivisions(theEvent.Identifier);
            DivisionCount = 1;
            divisions.Sort();
            foreach (Division div in divisions)
            {
                DivisionsBox.Items.Add(new ADivision(this, div, locations, bibGroups));
                DivisionCount = div.Identifier > DivisionCount - 1 ? div.Identifier + 1 : DivisionCount;
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
            mWindow.Update();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Update clicked.");
            UpdateDatabase();
            mWindow.Update();
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Revert clicked.");
            mWindow.Update();
        }

        internal void RemoveDivision(Division division)
        {
            Log.D("Remove division clicked.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            database.RemoveDivision(division);
            mWindow.Update();
        }

        public void UpdateDatabase()
        {
            bool NotifyTimingWorker = false;
            Dictionary<int, Division> oldDivisions = new Dictionary<int, Division>();
            foreach (Division division in database.GetDivisions(theEvent.Identifier))
            {
                oldDivisions[division.Identifier] = division;
            }
            foreach (ADivision listDiv in DivisionsBox.Items)
            {
                listDiv.UpdateDivision();
                int divId = listDiv.theDivision.Identifier;
                if (oldDivisions.ContainsKey(divId) &&
                    (oldDivisions[divId].StartOffsetSeconds != listDiv.theDivision.StartOffsetSeconds ||
                    oldDivisions[divId].StartOffsetMilliseconds != listDiv.theDivision.StartOffsetMilliseconds) )
                {
                    NotifyTimingWorker = true;
                    database.ResetTimingResultsDivision(theEvent.Identifier, divId);
                }
                database.UpdateDivision(listDiv.theDivision);
            }
            if (NotifyTimingWorker)
            {
                mWindow.NotifyTimingWorker();
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
            mWindow.Update();
        }

        public void Keyboard_Ctrl_Z()
        {
            mWindow.Update();
        }

        public void Closing()
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
        }

        private class ADivision : ListBoxItem
        {
            public TextBox DivisionName { get; private set; }
            public TextBox Cost { get; private set; }
            public TextBox Distance { get; private set; }
            public ComboBox DistanceUnit { get; private set; }
            public ComboBox FinishLocation { get; private set; }
            public ComboBox FinishOccurrence { get; private set; }
            public ComboBox StartLocation { get; private set; }
            public TextBox Wave { get; private set; }
            public ComboBox BibGroupNumber { get; private set; }
            public MaskedTextBox StartOffset { get; private set; }
            public Button Remove { get; private set; }

            private const string TimeFormat = "{0:D2}:{1:D2}:{2:D2}.{3:D3}";
            readonly DivisionsPage page;
            public Division theDivision;
            private Dictionary<string, int> locationDictionary; // TimingLocation Identifier (Stored as UID, therefore string works best), MaxOccurances

            private readonly Regex allowedWithDot = new Regex("[^0-9.]");
            private readonly Regex allowedChars = new Regex("[^0-9]");

            public ADivision(DivisionsPage page, Division division, List<TimingLocation> locations, List<BibGroup> bibGroups)
            {
                this.page = page;
                this.theDivision = division;
                locationDictionary = new Dictionary<string, int>();
                StackPanel thePanel = new StackPanel()
                {
                    MaxWidth = 600
                };
                this.Content = thePanel;
                this.IsTabStop = false;

                // Name information.
                DockPanel namePanel = new DockPanel();
                namePanel.Children.Add(new Label()
                {
                    Content = "Name",
                    Width = 75,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                DivisionName = new TextBox()
                {
                    Text = theDivision.Name,
                    FontSize = 16,
                    Margin = new Thickness(0, 10, 0, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                DivisionName.GotFocus += new RoutedEventHandler(this.SelectAll);
                namePanel.Children.Add(DivisionName);
                thePanel.Children.Add(namePanel);

                // Cost - Distance - DistanceUnit
                Grid settingsGrid = new Grid();
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                settingsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(125) });
                DockPanel costPanel = new DockPanel();
                costPanel.Children.Add(new Label()
                {
                    Content = "Price",
                    Width = 75,
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
                    Margin = new Thickness(0, 10, 0, 10),
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
                    Margin = new Thickness(0, 10, 10, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                Distance.GotFocus += new RoutedEventHandler(this.SelectAll);
                Distance.PreviewTextInput += new TextCompositionEventHandler(this.DotValidation);
                distPanel.Children.Add(Distance);
                settingsGrid.Children.Add(distPanel);
                Grid.SetColumn(distPanel, 1);
                DistanceUnit = new ComboBox()
                {
                    FontSize = 16,
                    Margin = new Thickness(0, 10, 0, 10),
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
                thePanel.Children.Add(settingsGrid);

                // Start Location - Finish Location - Occurrence
                Grid locGrid = new Grid();
                locGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                locGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                locGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                DockPanel startPanel = new DockPanel();
                startPanel.Children.Add(new Label()
                {
                    Content = "Start",
                    FontSize = 16,
                    Width = 75,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                StartLocation = new ComboBox()
                {
                    FontSize = 16,
                    Margin = new Thickness(0, 10, 0, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                ComboBoxItem selected = null, current;
                foreach (TimingLocation loc in locations)
                {
                    current = new ComboBoxItem()
                    {
                        Content = loc.Name,
                        Uid = loc.Identifier.ToString()
                    };
                    StartLocation.Items.Add(current);
                    if (theDivision.StartLocation == loc.Identifier)
                    {
                        selected = current;
                    }
                }
                if (selected != null)
                {
                    StartLocation.SelectedItem = selected;
                }
                else
                {
                    StartLocation.SelectedIndex = 0;
                }
                startPanel.Children.Add(StartLocation);
                locGrid.Children.Add(startPanel);
                Grid.SetColumn(startPanel, 0);
                DockPanel finPanel = new DockPanel();
                finPanel.Children.Add(new Label()
                {
                    Content = "Finish",
                    FontSize = 16,
                    Width = 75,
                    Margin = new Thickness(0, 10, 0, 10),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                FinishLocation = new ComboBox()
                {
                    FontSize = 16,
                    Margin = new Thickness(0, 10, 0, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                selected = null;
                foreach (TimingLocation loc in locations)
                {
                    locationDictionary[loc.Identifier.ToString()] = loc.MaxOccurrences;
                    current = new ComboBoxItem()
                    {
                        Content = loc.Name,
                        Uid = loc.Identifier.ToString()
                    };
                    FinishLocation.Items.Add(current);
                    if (theDivision.FinishLocation == loc.Identifier)
                    {
                        selected = current;
                    }
                }
                if (selected != null)
                {
                    FinishLocation.SelectedItem = selected;
                }
                else
                {
                    FinishLocation.SelectedIndex = 0;
                }
                FinishLocation.SelectionChanged += new SelectionChangedEventHandler(this.FinishLocation_Changed);
                finPanel.Children.Add(FinishLocation);
                locGrid.Children.Add(finPanel);
                Grid.SetColumn(finPanel, 1);
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
                    Margin = new Thickness(0, 10, 0, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                if (FinishLocation.SelectedItem == null || !locationDictionary.TryGetValue(((ComboBoxItem)FinishLocation.SelectedItem).Uid, out int maxOccurrences))
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
                locGrid.Children.Add(occPanel);
                Grid.SetColumn(occPanel, 2);
                thePanel.Children.Add(locGrid);

                // Wave #, Bib Group #, Start Offset
                Grid numGrid = new Grid();
                numGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                numGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                numGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                DockPanel wavePanel = new DockPanel();
                wavePanel.Children.Add(new Label()
                {
                    Content = "Wave",
                    Width = 75,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                Wave = new TextBox()
                {
                    Text = theDivision.Wave.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(0, 10, 0, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                Wave.GotFocus += new RoutedEventHandler(this.SelectAll);
                Wave.PreviewTextInput += new TextCompositionEventHandler(this.NumberValidation);
                wavePanel.Children.Add(Wave);
                numGrid.Children.Add(wavePanel);
                Grid.SetColumn(wavePanel, 0);
                DockPanel bibPanel = new DockPanel();
                bibPanel.Children.Add(new Label()
                {
                    Content = "Bib Group",
                    Width = 100,
                    FontSize = 16,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                });
                BibGroupNumber = new ComboBox()
                {
                    FontSize = 16,
                    Margin = new Thickness(0, 10, 0, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                selected = null;
                foreach (BibGroup bg in bibGroups)
                {
                    current = new ComboBoxItem()
                    {
                        Content = bg.Name,
                        Uid = bg.Number.ToString()
                    };
                    if (bg.Number == theDivision.BibGroupNumber)
                    {
                        selected = current;
                    }
                    BibGroupNumber.Items.Add(current);
                }
                if (selected != null)
                {
                    BibGroupNumber.SelectedItem = selected;
                }
                else
                {
                    BibGroupNumber.SelectedIndex = 0;
                }
                bibPanel.Children.Add(BibGroupNumber);
                numGrid.Children.Add(bibPanel);
                Grid.SetColumn(bibPanel, 2);
                DockPanel offsetPanel = new DockPanel();
                offsetPanel.Children.Add(new Label()
                {
                    Content = "Start",
                    Width = 75,
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
                    Margin = new Thickness(0, 10, 0, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                StartOffset.GotFocus += new RoutedEventHandler(this.SelectAll);
                offsetPanel.Children.Add(StartOffset);
                numGrid.Children.Add(offsetPanel);
                Grid.SetColumn(offsetPanel, 1);
                thePanel.Children.Add(numGrid);

                Remove = new Button()
                {
                    Content = "Remove",
                    FontSize = 16,
                    Height = 35,
                    Width = 140,
                    Margin = new Thickness(10, 10, 10, 10),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Remove.Click += new RoutedEventHandler(this.Remove_Click);
                thePanel.Children.Add(Remove);
            }

            private void Remove_Click(object sender, RoutedEventArgs e)
            {
                Log.D("Removing division.");
                this.page.RemoveDivision(theDivision);
            }

            private void FinishLocation_Changed(object sender, SelectionChangedEventArgs e)
            {
                FinishOccurrence.Items.Clear();
                if (FinishLocation.SelectedItem == null || !locationDictionary.TryGetValue(((ComboBoxItem)FinishLocation.SelectedItem).Uid, out int maxOccurrences))
                {
                    maxOccurrences = 1;
                }
                for (int i = 1; i <= maxOccurrences; i++)
                {
                    FinishOccurrence.Items.Add(new ComboBoxItem()
                    {
                        Content = i.ToString(),
                        Uid = i.ToString()
                    });
                }
                FinishOccurrence.SelectedIndex = 0;
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
                theDivision.FinishLocation = Convert.ToInt32(((ComboBoxItem)FinishLocation.SelectedItem).Uid);
                theDivision.StartLocation = Convert.ToInt32(((ComboBoxItem)StartLocation.SelectedItem).Uid);
                if (FinishOccurrence.SelectedItem != null)
                {
                    theDivision.FinishOccurrence = Convert.ToInt32(((ComboBoxItem)FinishOccurrence.SelectedItem).Uid);
                }
                int wave = -1;
                int.TryParse(Wave.Text, out wave);
                if (wave >= 0)
                {
                    theDivision.Wave = wave;
                }
                theDivision.BibGroupNumber = Convert.ToInt32(((ComboBoxItem)BibGroupNumber.SelectedItem).Uid);
                string[] firstparts = StartOffset.Text.Replace('_', '0').Split(':');
                string[] secondparts = firstparts[2].Split('.');
                try
                {
                    int hours = Convert.ToInt32(firstparts[0]), minutes = Convert.ToInt32(firstparts[1]),
                        seconds = Convert.ToInt32(secondparts[0]), milliseconds = Convert.ToInt32(secondparts[1]);
                    theDivision.StartOffsetSeconds = (hours * 3600) + (minutes * 60) + seconds;
                    theDivision.StartOffsetMilliseconds = milliseconds;
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

            private void DotValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = allowedWithDot.IsMatch(e.Text);
            }

            private void NumberValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = allowedChars.IsMatch(e.Text);
            }
        }
    }
}
