using Chronokeep.Database;
using Chronokeep.Database.SQLite;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Network.API;
using Chronokeep.Objects;

namespace Chronokeep.UI.MainPages;

public partial class DistancesPage : UserControl
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
                || oDist.FinishOccurrence != listDiv.GetDistance().FinishOccurrence))
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

    private void Update_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.DistancesPage", "Update clicked.");
        UpdateDatabase();
        UpdateView();
        mWindow.NetworkUpdateResults();
    }

    private void Revert_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.DistancesPage", "Revert clicked.");
        UpdateView();
    }

    private async void DeleteButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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

    private async void UploadButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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

    private void Add_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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
}