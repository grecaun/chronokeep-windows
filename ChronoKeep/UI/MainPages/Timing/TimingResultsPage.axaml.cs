using Avalonia.Controls;
using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chronokeep.UI.MainPages.Timing;

public partial class TimingResultsPage : UserControl, ISubPage
{
    private readonly TimingPage parent;
    private readonly IDBInterface database;
    private readonly Event? theEvent;

    public readonly List<TimeResult> Results = [];

    public TimingResultsPage(TimingPage parent, IDBInterface database)
    {
        InitializeComponent();
        this.parent = parent;
        this.database = database;
        theEvent = database.GetCurrentEvent();
        updateListView.ItemsSource = this.Results;
        if (Constants.Timing.EVENT_TYPE_TIME == theEvent!.EventType)
        {
            updateListView.Columns[4].Header = "Lap Time";
        }
        if (database is SQLiteInterface)
        {
            Database.SQLite.Results.GetStaticVariables(database);
        }
        parent.SetReaders([], false);
        UpdateView();
    }

    public void Closing() { }

    public void EditSelected() { }

    public void Keyboard_Ctrl_A() { }

    public void Keyboard_Ctrl_S() { }

    public void Keyboard_Ctrl_Z() { }

    private void Customize(
        SortType sortType,
        PeopleType peopleType,
        List<TimeResult> newResults,
        string search,
        string location)
    {
        if (peopleType == PeopleType.DEFAULT)
        {
            newResults.RemoveAll(TimeResult.StartTimes);
        }
        else if (peopleType == PeopleType.KNOWN)
        {
            newResults.RemoveAll(TimeResult.IsNotKnown);
        }
        else if (peopleType == PeopleType.UNKNOWN)

        {

            newResults.RemoveAll(TimeResult.IsKnown);

        }
        else if (peopleType == PeopleType.UNKNOWN_FINISHES)

        {
            if (Constants.Timing.EVENT_TYPE_TIME == theEvent!.EventType)
            {
                Log.D("UI.Timing.TimingResultsPage", "Time based event.");
                Dictionary<int, TimeResult> validResults = [];
                foreach (TimeResult result in newResults)
                {
                    if (Constants.Timing.TIMERESULT_DUMMYPERSON != result.EventSpecificId)
                    {
                        validResults[result.EventSpecificId] = result;
                    }
                }
                newResults.RemoveAll(x => !validResults.ContainsValue(x) && TimeResult.IsKnown(x));
            }
            else
            {
                newResults.RemoveAll(TimeResult.IsNotFinishOrKnown);
            }

        }
        else if (peopleType == PeopleType.UNKNOWN_STARTS)

        {
            newResults.RemoveAll(TimeResult.IsNotStartOrKnown);

        }
        else if (peopleType == PeopleType.FINISHES)
        {
            if (Constants.Timing.EVENT_TYPE_TIME == theEvent!.EventType)
            {
                Log.D("UI.Timing.TimingResultsPage", "Time based event.");
                Dictionary<int, TimeResult> validResults = [];
                foreach (TimeResult result in newResults)
                {
                    if (Constants.Timing.TIMERESULT_DUMMYPERSON != result.EventSpecificId)
                    {
                        validResults[result.EventSpecificId] = result;
                    }
                }
                newResults.RemoveAll(x => !validResults.ContainsValue(x));
            }
            else
            {
                newResults.RemoveAll(TimeResult.IsNotFinish);
            }
        }
        else if (peopleType == PeopleType.STARTS)
        {
            newResults.RemoveAll(TimeResult.IsNotStart);
        }
        newResults.RemoveAll(result => result.IsNotMatch(search));

        Log.D("UI.Timing.TimingResultsPage", "Removing all location based items. " + location);

        if (location != null && location.Length > 0 && !location.Equals("All Locations", StringComparison.OrdinalIgnoreCase))

        {

            newResults.RemoveAll(read => !read.LocationName.Equals(location, StringComparison.OrdinalIgnoreCase));
        }
        if (sortType == SortType.BIB)
        {
            newResults.Sort(TimeResult.CompareByBib);
        }
        else if (sortType == SortType.GUNTIME)
        {
            newResults.Sort(TimeResult.CompareByGunTime);
        }
        else if (sortType == SortType.DISTANCE)
        {
            newResults.Sort(TimeResult.CompareByDistance);
        }
        else if (sortType == SortType.AGEGROUP)
        {
            newResults.Sort(TimeResult.CompareByAgeGroup);
        }
        else if (sortType == SortType.GENDER)
        {
            newResults.Sort(TimeResult.CompareByGender);
        }
        else if (sortType == SortType.PLACE)
        {
            newResults.Sort(TimeResult.CompareByDistancePlace);
        }
        else
        {
            newResults.Sort(TimeResult.CompareBySystemTime);
        }
    }

    public async void SortBy(SortType sortType)
    {
        List<TimeResult> newResults = [.. Results];
        PeopleType peopleType = parent.GetPeopleType();
        string search = parent.GetSearchValue();
        string location = parent.GetLocation();
        await Task.Run(() =>
        {
            Customize(sortType, peopleType, newResults, search, location);
        });
        updateListView.ItemsSource = newResults;
        updateListView.ScrollIntoView(newResults[^1], null);
    }

    public async void Location(string location)

    {

        List<TimeResult> newResults = [.. Results];
        PeopleType peopleType = parent.GetPeopleType();
        SortType sortType = parent.GetSortType();
        string search = parent.GetSearchValue();
        await Task.Run(() =>
        {
            Customize(sortType, peopleType, newResults, search, location);
        });
        updateListView.ItemsSource = newResults;
        updateListView.ScrollIntoView(newResults[^1], null);

    }

    public static void UpdateDatabase() { }

    public async void UpdateView()
    {
        List<TimeResult> newResults = [];
        SortType sortType = parent.GetSortType();
        PeopleType peopleType = parent.GetPeopleType();
        string search = parent.GetSearchValue();
        string location = parent.GetLocation();
        await Task.Run(() =>
        {
            newResults = database.GetTimingResults(theEvent!.Identifier);
        });
        Results.Clear();
        Results.AddRange(newResults);
        await Task.Run(() =>
        {
            Customize(sortType, peopleType, newResults, search, location);
        });
        updateListView.ItemsSource = newResults;
        if (newResults.Count > 0)
        {
            updateListView.ScrollIntoView(newResults[^1], null);
        }
        updateListView.SelectedItem = null;
        if (theEvent!.DisplayPlacements)
        {
            DisplayPlacements();
        }
        else
        {
            HidePlacements();
        }
    }

    public void DisplayPlacements()
    {
        updateListView.Columns[7].IsVisible = true;
        updateListView.Columns[9].IsVisible = true;
        updateListView.Columns[11].IsVisible = true;
        updateListView.Columns[13].IsVisible = true;
    }

    public void HidePlacements()
    {
        updateListView.Columns[7].IsVisible = false;
        updateListView.Columns[9].IsVisible = false;
        updateListView.Columns[11].IsVisible = false;
        updateListView.Columns[13].IsVisible = false;
    }

    public void CancelableUpdateView(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        UpdateView();
    }

    public void Search(CancellationToken token, string searchText)
    {
        token.ThrowIfCancellationRequested();
        UpdateView();
    }

    public async void Show(PeopleType peopleType)
    {
        List<TimeResult> newResults = [.. Results];
        SortType sortType = parent.GetSortType();
        string search = parent.GetSearchValue();
        string location = parent.GetLocation();
        await Task.Run(() =>
        {
            Customize(sortType, peopleType, newResults, search, location);
        });
        updateListView.ItemsSource = newResults;
        if (newResults.Count > 0)
        {
            updateListView.ScrollIntoView(newResults[^1], null);
        }
    }

    public void Reader(string reader) { }
}