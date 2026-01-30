using Avalonia.Controls;
using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;

namespace Chronokeep.UI.MainPages;

public partial class AgeGroupsPage : UserControl, IMainPage
{
    private readonly IMainWindow mWindow;
    private readonly IDBInterface database;
    private readonly Event theEvent;

    private bool touched = false;

    public AgeGroupsPage(IMainWindow mWindow, IDBInterface database)
    {
        InitializeComponent();
        this.mWindow = mWindow;
        this.database = database;
        theEvent = database.GetCurrentEvent();
        UpdateView();
    }

    public void UpdateView()
    {
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

    internal void RemoveAgeGroup(AAgeGroup group)
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
            Dictionary<(int, int), AgeGroup> AgeGroups = [];
            Dictionary<int, AgeGroup> LastAgeGroup = [];
            foreach (AgeGroup g in database.GetAgeGroups(theEvent.Identifier))
            {
                for (int i = g.StartAge; i <= g.EndAge; i++)
                {
                    AgeGroups[(g.DistanceId, i)] = g;
                }
                if (!LastAgeGroup.TryGetValue(g.DistanceId, out AgeGroup lastAg) || lastAg.StartAge < g.StartAge)
                {
                    lastAg = g;
                    LastAgeGroup[g.DistanceId] = lastAg;
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

    private void Revert_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdateAgeGroupsList();
    }

    private void Update_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.AgeGroupsPage", "Update age groups button clicked.");
        List<AgeGroup> ageGroups = [];
        List<AgeGroup> toAdd = [];
        foreach (ListBoxItem aAge in AgeGroupsBox.Items)
        {
            if (aAge is AAgeGroup group)
            {
                ageGroups.Add(group.GetAgeGroup());
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
                    toAdd.Add(new(current.EventId, current.DistanceId, previous.EndAge + 1, current.StartAge - 1));
                }
            }
            else if (current.StartAge > 1)
            {
                toAdd.Add(new(current.EventId, current.DistanceId, 0, current.StartAge - 1));
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

    private void Add_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.AgeGroupsPage", "Adding group.");
        int divId = Constants.Timing.COMMON_AGEGROUPS_DISTANCEID;
        if (!theEvent.CommonAgeGroups)
        {
            divId = Convert.ToInt32(((ComboBoxItem)Distances.SelectedItem).Uid);
        }
        AgeGroupsBox.Items.Add(new AAgeGroup(this, new(theEvent.Identifier, divId, 0, 0)));
    }

    private void Distances_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Log.D("UI.MainPages.AgeGroupsPage", "Distance changed.");
        if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
        {
            UpdateDatabase();
        }
        UpdateAgeGroupsList();
    }

    private void AddDefault_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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
                database.AddAgeGroup(new(theEvent.Identifier, divId, 0, 39));
                database.AddAgeGroup(new(theEvent.Identifier, divId, 40, 59));
                database.AddAgeGroup(new(theEvent.Identifier, divId, 60, 99));
                break;
            case 3:
                database.AddAgeGroup(new(theEvent.Identifier, divId, 0, 19));
                for (int i = 20; i < 80; i += increment)
                {
                    database.AddAgeGroup(new(theEvent.Identifier, divId, i, i + increment - 1));
                }
                database.AddAgeGroup(new(theEvent.Identifier, divId, 80, 99));
                break;
            case 0:
                increment = 10;
                goto default;
            default:
                for (int i = 0; i < 100; i += increment)
                {
                    database.AddAgeGroup(new(theEvent.Identifier, divId, i, i + increment - 1));
                }
                break;
        }
        touched = true;
        UpdateAgeGroupsList();
    }

    private class ALabel : ListBoxItem
    {
        public ALabel()
        {
            Grid theGrid = new()
            {
                MaxWidth = 400
            };
            theGrid.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            TextBlock l = new()
            {
                Text = "Start Age",
                FontSize = 16,
                Margin = new Thickness(10, 10, 10, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            theGrid.Children.Add(l);
            Grid.SetColumn(l, 0);
            l = new()
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
}