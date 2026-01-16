using Chronokeep;
using Chronokeep.Database;
using Chronokeep.Database.SQLite;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;

namespace Chronokeep.UI.MainPages;

public partial class LocationsPage : UserControl, IMainPage
{
    private readonly IMainWindow mWindow;
    private readonly IDBInterface database;
    private readonly Event theEvent;
    private int LocationCount = 1;
    private bool UpdateTimingWorker = false;

    public LocationsPage(IMainWindow mWindow, IDBInterface database)
    {
        InitializeComponent();
        this.mWindow = mWindow;
        this.database = database;
        theEvent = database.GetCurrentEvent();
        UpdateView();
    }

    public void UpdateView()
    {
        if (theEvent == null || theEvent.Identifier < 0)
        {
            return;
        }
        LocationsBox.Items.Clear();
        LocationsBox.Items.Add(new ALocation(this, new(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", theEvent.StartMaxOccurrences, theEvent.StartWindow), theEvent));
        LocationsBox.Items.Add(new ALocation(this, new(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin), theEvent));
        List<TimingLocation> locations = database.GetTimingLocations(theEvent.Identifier);
        LocationCount = 1;
        locations.Sort();
        foreach (TimingLocation loc in locations)
        {
            LocationsBox.Items.Add(new ALocation(this, loc, theEvent));
            LocationCount = loc.Identifier > LocationCount - 1 ? loc.Identifier + 1 : LocationCount;
        }
    }

    internal void RemoveLocation(TimingLocation location)
    {
        Log.D("UI.MainPages.LocationsPage", "Removing a location.");
        if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
        {
            UpdateDatabase();
        }
        if (location.Identifier == Constants.Timing.LOCATION_FINISH || location.Identifier == Constants.Timing.LOCATION_START)
        {
            Log.E("UI.MainPages.LocationsPage", "Somehow they told us to delete the start/finish location.");
        }
        else
        {
            database.RemoveTimingLocation(location);
        }
        UpdateTimingWorker = true;
        UpdateView();
    }

    public void UpdateDatabase()
    {
        foreach (ALocation locItem in LocationsBox.Items)
        {
            locItem.UpdateLocation();
            if (locItem.myLocation.Identifier == Constants.Timing.LOCATION_FINISH)
            {
                if (theEvent.FinishMaxOccurrences != locItem.myLocation.MaxOccurrences
                    || theEvent.FinishIgnoreWithin != locItem.myLocation.IgnoreWithin)
                {
                    theEvent.FinishMaxOccurrences = locItem.myLocation.MaxOccurrences;
                    theEvent.FinishIgnoreWithin = locItem.myLocation.IgnoreWithin;
                    database.SetFinishOptions(theEvent);
                    UpdateTimingWorker = true;
                }
            }
            else if (locItem.myLocation.Identifier == Constants.Timing.LOCATION_START)
            {
                if (theEvent.StartWindow != locItem.myLocation.IgnoreWithin
                    || theEvent.StartMaxOccurrences != locItem.myLocation.MaxOccurrences)
                {
                    theEvent.StartWindow = locItem.myLocation.IgnoreWithin;
                    theEvent.StartMaxOccurrences = locItem.myLocation.MaxOccurrences;
                    database.SetStartOptions(theEvent);
                    UpdateTimingWorker = true;
                }
            }
            else
            {
                if (locItem.IsUpdated())
                {
                    database.UpdateTimingLocation(locItem.myLocation);
                    UpdateTimingWorker = true;
                }
            }
        }
        if (database is SQLiteInterface)
        {
            Results.GetStaticVariables(database);
        }
    }

    public void Keyboard_Ctrl_A()
    {
        Add_Click(null, null);
    }

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
        Log.D("UI.MainPages.LocationsPage", "Location page closing.");
        if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
        {
            UpdateDatabase();
        }
        if (UpdateTimingWorker)
        {
            Log.D("UI.MainPages.LocationsPage", "Resetting results.");
            database.ResetTimingResultsEvent(theEvent.Identifier);
            mWindow.NetworkClearResults();
            mWindow.NotifyTimingWorker();
        }
    }

    private void Add_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.LocationsPage", "Add Location clicked.");
        if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
        {
            UpdateDatabase();
        }
        database.AddTimingLocation(new(theEvent.Identifier, "Location " + LocationCount));
        UpdateTimingWorker = true;
        UpdateView();
    }

    private void Update_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.LocationsPage", "Update all clicked.");
        UpdateDatabase();
        UpdateView();
    }

    private void ResetBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {}
}