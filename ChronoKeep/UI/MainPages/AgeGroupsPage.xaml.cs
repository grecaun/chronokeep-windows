using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chronokeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for AgeGroupsPage.xaml
    /// </summary>
    public partial class AgeGroupsPage : IMainPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;

        private bool touched = false;

        public AgeGroupsPage(IMainWindow mWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mWindow;
            this.database = database;
            UpdateView();
        }

        private void Distances_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.D("UI.MainPages.AgeGroupsPage", "Distance changed.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            UpdateAgeGroupsList();
        }

        public void UpdateView()
        {
            theEvent = database.GetCurrentEvent();
            if (theEvent.CommonAgeGroups)
            {
                DistanceRow.Height = new GridLength(0);
                UpdateAgeGroupsList();
            }
            else
            {
                DistanceRow.Height = new GridLength(55);
                UpdateDistancesBox();
            }
        }

        private void UpdateDistancesBox()
        {
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            Distances.Items.Clear();
            List<Distance> distances = database.GetDistances(theEvent.Identifier);
            distances.Sort();
            foreach (Distance d in distances)
            {
                Distances.Items.Add(new ComboBoxItem()
                {
                    Content = d.Name,
                    Uid = d.Identifier.ToString()
                });
            }
            Distances.SelectedIndex = 0;
        }

        private void UpdateAgeGroupsList()
        {
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            AgeGroupsBox.Items.Clear();
            AgeGroupsBox.Items.Add(new ALabel());
            List<AgeGroup> ageGroups = database.GetAgeGroups(theEvent.Identifier);
            ageGroups.RemoveAll(x => Constants.Timing.AGEGROUPS_CUSTOM_DISTANCEID == x.DistanceId);
            ageGroups.Sort();
            foreach (AgeGroup group in ageGroups)
            {
                AgeGroupsBox.Items.Add(new AAgeGroup(this, group));
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.AgeGroupsPage", "Adding group.");
            int divId = Constants.Timing.COMMON_AGEGROUPS_DISTANCEID;
            if (!theEvent.CommonAgeGroups)
            {
                divId = Convert.ToInt32(((ComboBoxItem)Distances.SelectedItem).Uid);
            }
            AgeGroupsBox.Items.Add(new AAgeGroup(this, new AgeGroup(theEvent.Identifier, divId, 0, 0)));
        }

        private void AddDefault_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.AgeGroupsPage", "Add default age groups button clicked.");
            int divId = Constants.Timing.COMMON_AGEGROUPS_DISTANCEID;
            if (!theEvent.CommonAgeGroups)
            {
                divId = Convert.ToInt32(((ComboBoxItem)Distances.SelectedItem).Uid);
            }
            database.RemoveAgeGroups(theEvent.Identifier, divId);
            int increment = 5;
            switch (DefaultGroupsBox.SelectedIndex)
            {
                case 2:
                    database.AddAgeGroup(new AgeGroup(theEvent.Identifier, divId, 0, 39));
                    database.AddAgeGroup(new AgeGroup(theEvent.Identifier, divId, 40, 59));
                    database.AddAgeGroup(new AgeGroup(theEvent.Identifier, divId, 60, 99));
                    break;
                case 3:
                    database.AddAgeGroup(new AgeGroup(theEvent.Identifier, divId, 0, 19));
                    for (int i = 20; i < 80; i += increment)
                    {
                        database.AddAgeGroup(new AgeGroup(theEvent.Identifier, divId, i, i + increment - 1));
                    }
                    database.AddAgeGroup(new AgeGroup(theEvent.Identifier, divId, 80, 99));
                    break;
                case 0:
                    increment = 10;
                    goto default;
                default:
                    for (int i = 0; i < 100; i += increment)
                    {
                        database.AddAgeGroup(new AgeGroup(theEvent.Identifier, divId, i, i + increment - 1));
                    }
                    break;
            }
            touched = true;
            UpdateAgeGroupsList();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.AgeGroupsPage", "Update age groups button clicked.");
            List<AgeGroup> ageGroups = new List<AgeGroup>();
            List<AgeGroup> toAdd = new List<AgeGroup>();
            foreach (ListBoxItem aAge in AgeGroupsBox.Items)
            {
                if (aAge is AAgeGroup)
                {
                    ageGroups.Add(((AAgeGroup)aAge).GetAgeGroup());
                }
            }
            ageGroups.Sort();
            bool conflict = false;
            AgeGroup previous = null;
            foreach (AgeGroup current in ageGroups)
            {
                if (previous != null)
                {
                    if (previous.EndAge >= current.StartAge)
                    {
                        conflict = true;
                        break;
                    }
                    else if (previous.EndAge != current.StartAge - 1)
                    {
                        toAdd.Add(new AgeGroup(current.EventId, current.DistanceId, previous.EndAge + 1, current.StartAge - 1));
                    }
                }
                else if (current.StartAge > 1)
                {
                    toAdd.Add(new AgeGroup(current.EventId, current.DistanceId, 0, current.StartAge - 1));
                }
                previous = current;
            }
            if (previous != null)
            {
                previous.LastGroup = true;
            }
            if (conflict)
            {
                DialogBox.Show("There is a conflict in the age groups. Unable to save.");
                return;
            }
            ageGroups.AddRange(toAdd);
            int divId = Constants.Timing.COMMON_AGEGROUPS_DISTANCEID;
            if (!theEvent.CommonAgeGroups)
            {
                divId = Convert.ToInt32(((ComboBoxItem)Distances.SelectedItem).Uid);
            }
            database.RemoveAgeGroups(theEvent.Identifier, divId);
            foreach (AgeGroup age in ageGroups)
            {
                database.AddAgeGroup(age);
            }
            touched = true;
            UpdateAgeGroupsList();
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            UpdateAgeGroupsList();
        }

        private void RemoveAgeGroup(AAgeGroup group)
        {
            Log.D("UI.MainPages.AgeGroupsPage", "Removing Age Group from view.");
            AgeGroupsBox.Items.Remove(group);
        }

        public void UpdateDatabase()
        {
            Update_Click(null, null);
        }

        public void Keyboard_Ctrl_A()
        {
            Add_Click(null, null);
        }

        public void Keyboard_Ctrl_S()
        {
            UpdateDatabase();
            UpdateAgeGroupsList();
        }

        public void Keyboard_Ctrl_Z()
        {
            UpdateAgeGroupsList();
        }

        public void Closing()
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            if (touched)
            {
                // Setup AgeGroup static variables
                Dictionary<(int, int), AgeGroup> AgeGroups = new();
                Dictionary<int, AgeGroup> LastAgeGroup = new();
                foreach (AgeGroup g in database.GetAgeGroups(theEvent.Identifier))
                {
                    for (int i = g.StartAge; i <= g.EndAge; i++)
                    {
                        AgeGroups[(g.DistanceId, i)] = g;
                    }
                    if (!LastAgeGroup.ContainsKey(g.DistanceId) || LastAgeGroup[g.DistanceId].StartAge < g.StartAge)
                    {
                        LastAgeGroup[g.DistanceId] = g;
                    }
                }
                List<Participant> participants = database.GetParticipants(theEvent.Identifier);
                foreach (Participant person in participants)
                {
                    int agDivId = theEvent.CommonAgeGroups ? Constants.Timing.COMMON_AGEGROUPS_DISTANCEID : person.EventSpecific.DistanceIdentifier;
                    int age = person.GetAge(theEvent.Date);
                    if (age < 0)
                    {
                        person.EventSpecific.AgeGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
                        person.EventSpecific.AgeGroupName = "";
                    }
                    else if (AgeGroups.TryGetValue((agDivId, age), out AgeGroup group))
                    {
                        person.EventSpecific.AgeGroupId = group.GroupId;
                        person.EventSpecific.AgeGroupName = group.PrettyName();
                    }
                    else if (LastAgeGroup.TryGetValue(agDivId, out AgeGroup lGroup))
                    {
                        person.EventSpecific.AgeGroupId = lGroup.GroupId;
                        person.EventSpecific.AgeGroupName = lGroup.PrettyName();
                    }
                    else
                    {
                        person.EventSpecific.AgeGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
                        person.EventSpecific.AgeGroupName = "";
                    }
                }
                database.UpdateParticipants(participants);

                database.ResetTimingResultsEvent(theEvent.Identifier);
                mWindow.NetworkClearResults();
                mWindow.NotifyTimingWorker();
            }
        }

        private class ALabel : ListBoxItem
        {
            public ALabel()
            {
                Grid theGrid = new Grid()
                {
                    MaxWidth = 400
                };
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                TextBlock l = new TextBlock()
                {
                    Text = "Start Age",
                    FontSize = 16,
                    Margin = new Thickness(10, 10, 10, 10),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                theGrid.Children.Add(l);
                Grid.SetColumn(l, 0);
                l = new TextBlock()
                {
                    Text = "End Age",
                    FontSize = 16,
                    Margin = new Thickness(10, 10, 10, 10),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                theGrid.Children.Add(l);
                Grid.SetColumn(l, 1);
                this.Content = theGrid;
                this.IsTabStop = false;
            }
        }

        private class AAgeGroup : ListBoxItem
        {
            public TextBox StartAge { get; private set; }
            public TextBox EndAge { get; private set; }
            public Button Remove { get; private set; }
            public Button Update { get; private set; }

            private AgeGroupsPage page;
            public AgeGroup MyGroup { get; private set; }

            private readonly Regex allowedChars = new Regex("[^0-9]+");

            public AAgeGroup(AgeGroupsPage page, AgeGroup group)
            {
                this.page = page;
                this.MyGroup = group;
                Grid theGrid = new Grid()
                {
                    MaxWidth = 400
                };
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                StartAge = new TextBox()
                {
                    Text = group.StartAge.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(10, 10, 10, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                StartAge.GotFocus += new RoutedEventHandler(this.SelectAll);
                StartAge.PreviewTextInput += new TextCompositionEventHandler(this.NumberValidation);
                theGrid.Children.Add(StartAge);
                Grid.SetColumn(StartAge, 0);
                EndAge = new TextBox()
                {
                    Text = group.EndAge.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(10, 10, 10, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                EndAge.GotFocus += new RoutedEventHandler(this.SelectAll);
                EndAge.PreviewTextInput += new TextCompositionEventHandler(this.NumberValidation);
                theGrid.Children.Add(EndAge);
                Grid.SetColumn(EndAge, 1);
                Remove = new Button()
                {
                    Content = "Remove",
                    FontSize = 16,
                    Height = 35,
                    Margin = new Thickness(10, 10, 10, 10)
                };
                Remove.Click += new RoutedEventHandler(this.Remove_Click);
                theGrid.Children.Add(Remove);
                Grid.SetColumn(Remove, 2);
                this.Content = theGrid;
                this.IsTabStop = false;
            }

            private void NumberValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = allowedChars.IsMatch(e.Text);
            }

            private void Remove_Click(object sender, RoutedEventArgs e)
            {
                Log.D("UI.MainPages.AgeGroupsPage", "Removing.");
                page.RemoveAgeGroup(this);
            }

            public AgeGroup GetAgeGroup()
            {
                int start = MyGroup.StartAge, end = MyGroup.EndAge;
                int.TryParse(StartAge.Text, out start);
                int.TryParse(EndAge.Text, out end);
                MyGroup.StartAge = start;
                MyGroup.EndAge = end;
                return MyGroup;
            }

            private void SelectAll(object sender, RoutedEventArgs e)
            {
                TextBox src = (TextBox)e.OriginalSource;
                src.SelectAll();
            }
        }

        private void AgeGroupsBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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
