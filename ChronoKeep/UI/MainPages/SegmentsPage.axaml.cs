using Chronokeep.Database;
using Chronokeep.Database.SQLite;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Network.API;
using Chronokeep.Objects;

namespace Chronokeep.UI.MainPages;

public partial class SegmentsPage : UserControl
{
    private readonly IMainWindow mWindow;
    private readonly IDBInterface database;
    private readonly Event theEvent;
    private readonly List<TimingLocation> locations;
    private readonly List<Distance> distances;

    private bool UpdateTimingWorker = false;

    private readonly Dictionary<int, List<Segment>> allSegments = [];
    private readonly Dictionary<int, TimingLocation> LocationDict = [];

    public SegmentsPage(IMainWindow mWindow, IDBInterface database)
    {
        InitializeComponent();
        this.mWindow = mWindow;
        this.database = database;
        this.theEvent = database.GetCurrentEvent();
        if (theEvent != null)
        {
            locations = database.GetTimingLocations(theEvent.Identifier);
            if (theEvent.CommonStartFinish)
            {
                locations.Insert(0, new(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences - 1, theEvent.FinishIgnoreWithin));
            }
            else
            {
                locations.Insert(0, new(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences - 1, theEvent.FinishIgnoreWithin));
                locations.Insert(0, new(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", theEvent.StartMaxOccurrences - 1, theEvent.FinishIgnoreWithin));
            }
            foreach (TimingLocation loc in locations)
            {
                LocationDict.Add(loc.Identifier, loc);
            }
            distances = database.GetDistances(theEvent.Identifier);
            distances.Sort((x1, x2) => x1.Name.CompareTo(x2.Name));
            distances.RemoveAll(x => x.LinkedDistance != Constants.Timing.DISTANCE_NO_LINKED_ID);
            if (theEvent.API_ID > 0 && theEvent.API_Event_ID.Length > 1)
            {
                apiPanel.Visibility = Visibility.Visible;
            }
            else
            {
                apiPanel.Visibility = Visibility.Collapsed;
            }
        }
        UpdateSegments();
    }

    public void UpdateView()
    {
        if (theEvent == null || theEvent.Identifier < 0)
        {
            return;
        }
        List<ListBoxItem> items = [];
        if (theEvent.DistanceSpecificSegments)
        {
            foreach (Distance d in distances)
            {
                ADistanceSegmentHolder newHolder = new(theEvent, this, d, distances, allSegments[d.Identifier], locations);
                items.Add(newHolder);
                foreach (ListBoxItem item in newHolder.SegmentItems)
                {
                    items.Add(item);
                }
            }
        }
        else
        {
            ADistanceSegmentHolder newHolder = new(theEvent, this, null, distances, allSegments[Constants.Timing.COMMON_SEGMENTS_DISTANCEID], locations);
            items.Add(newHolder);
            foreach (ListBoxItem item in newHolder.SegmentItems)
            {
                items.Add(item);
            }
        }
        SegmentsBox.ItemsSource = items;
    }

    private void UpdateSegments()
    {
        allSegments.Clear();
        List<Segment> segments = database.GetSegments(theEvent.Identifier);
        if (theEvent.DistanceSpecificSegments)
        {
            foreach (Distance d in distances)
            {
                allSegments[d.Identifier] = [];
            }
            foreach (Segment seg in segments)
            {
                if (!allSegments.TryGetValue(seg.DistanceId, out List<Segment> segList))
                {
                    segList = [];
                    allSegments[seg.DistanceId] = segList;
                }
                segList.Add(seg);
            }
        }
        else
        {
            allSegments[Constants.Timing.COMMON_SEGMENTS_DISTANCEID] = segments;
            allSegments[Constants.Timing.COMMON_SEGMENTS_DISTANCEID].RemoveAll(x => x.DistanceId != Constants.Timing.COMMON_SEGMENTS_DISTANCEID);
        }
    }

    private void RemoveSegment(Segment mySegment)
    {
        Log.D("UI.MainPages.SegmentsPage", "Removing segment.");
        UpdateDatabase();
        allSegments[mySegment.DistanceId].Remove(mySegment);
        database.RemoveSegment(mySegment);
        UpdateView();
    }

    public void UpdateDatabase()
    {
        List<Segment> upSegs = [];
        List<Segment> newSegs = [];
        HashSet<int> segSet = [];
        foreach (Segment s in database.GetSegments(theEvent.Identifier))
        {
            segSet.Add(s.Identifier);
        }
        foreach (Object seg in SegmentsBox.Items)
        {
            if (seg is ASegment tSeg)
            {
                tSeg.UpdateSegment();
                Segment thisSegment = tSeg.mySegment;
                if (thisSegment.Identifier < 1)
                {
                    newSegs.Add(thisSegment);
                }
                else
                {
                    upSegs.Add(thisSegment);
                }
            }
        }
        newSegs.RemoveAll(x => x.Occurrence < 0);
        database.AddSegments(newSegs);
        UpdateTimingWorker = true;
        database.UpdateSegments(upSegs);
        if (database is SQLiteInterface)
        {
            Results.GetStaticVariables(database);
        }
    }

    public void Keyboard_Ctrl_A() { }

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
        if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
        {
            UpdateDatabase();
            bool occurrence_error = false;
            foreach (Object seg in SegmentsBox.Items)
            {
                if (seg is ASegment)
                {
                    Segment thisSegment = ((ASegment)seg).mySegment;
                    if (thisSegment.LocationId == Constants.Timing.LOCATION_FINISH && thisSegment.Occurrence >= theEvent.FinishMaxOccurrences)
                    {
                        occurrence_error = true;
                    }
                    Log.D("UI.MainPages.SegmentsPage", "Distance ID " + ((ASegment)seg).mySegment.DistanceId + " Segment Name " + ((ASegment)seg).mySegment.Name + " segment ID " + ((ASegment)seg).mySegment.Identifier);
                }
            }
            if (occurrence_error)
            {
                DialogBox.Show("Your finish lines has one or more segments beyond the maximum number it supports (" + (theEvent.FinishMaxOccurrences - 1) + ").  These will not be added. Update locations and max occurrences to fix this.");
            }
        }
        if (UpdateTimingWorker)
        {
            database.ResetTimingResultsEvent(theEvent.Identifier);
            mWindow.NetworkClearResults();
            mWindow.NotifyTimingWorker();
        }
    }

    public void AddSegment(int distanceId)
    {
        Log.D("UI.MainPages.SegmentsPage", "Adding segment.");
        Segment newSeg = new(theEvent.Identifier, distanceId, Constants.Timing.LOCATION_FINISH, 0, 0.0, 0.0, Constants.Distances.MILES, "", "", "");
        allSegments[distanceId].Add(newSeg);
        UpdateView();
    }

    public void CopyFromDistance(int intoDistance, int fromDistance)
    {
        Log.D("UI.MainPages.SegmentsPage", "Copying segments.");
        if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
        {
            UpdateDatabase();
        }
        database.RemoveSegments(allSegments[intoDistance]);
        allSegments[intoDistance].Clear();
        foreach (Segment seg in allSegments[fromDistance])
        {
            Segment newSeg = new(seg)
            {
                DistanceId = intoDistance
            };
            allSegments[intoDistance].Add(newSeg);
        }
        UpdateView();
    }

    private async void UploadButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.SegmentsPage", "Uploading segments.");
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
            // Save segments displayed
            UpdateDatabase();
            UpdateSegments();
            // Get Distances and Locations to get their names
            Dictionary<int, Distance> distances = [];
            foreach (Distance d in database.GetDistances(theEvent.Identifier))
            {
                distances.Add(d.Identifier, d);
            }
            Dictionary<int, TimingLocation> locations = [];
            foreach (TimingLocation l in database.GetTimingLocations(theEvent.Identifier))
            {
                locations.Add(l.Identifier, l);
            }
            locations.Add(Constants.Timing.LOCATION_ANNOUNCER, new(Constants.Timing.LOCATION_ANNOUNCER, theEvent.Identifier, "Announcer", 0, 0));
            locations.Add(Constants.Timing.LOCATION_FINISH, new(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", 0, 0));
            locations.Add(Constants.Timing.LOCATION_START, new(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, 0));
            Dictionary<int, string> distanceUnits = new()
                {
                    { Constants.Distances.FEET, "ft" },
                    { Constants.Distances.KILOMETERS, "km" },
                    { Constants.Distances.METERS, "m" },
                    { Constants.Distances.YARDS, "yd" },
                    { Constants.Distances.MILES, "mi" }
                };
            // Convert Segments to APISegments
            List<APISegment> segments = [];
            foreach (Segment seg in database.GetSegments(theEvent.Identifier))
            {
                if (locations.TryGetValue(seg.LocationId, out TimingLocation segmentLocation))
                {
                    if (theEvent.DistanceSpecificSegments)
                    {
                        if (distances.TryGetValue(seg.DistanceId, out Distance segmentDistance))
                        {
                            segments.Add(new()
                            {
                                Location = segmentLocation.Name,
                                DistanceName = segmentDistance.Name,
                                Name = seg.Name,
                                DistanceValue = seg.CumulativeDistance,
                                DistanceUnit = distanceUnits[seg.DistanceUnit],
                                GPS = seg.GPS,
                                MapLink = seg.MapLink,
                            });
                        }
                    }
                    else
                    {
                        foreach (Distance dist in distances.Values)
                        {
                            segments.Add(new()
                            {
                                Location = segmentLocation.Name,
                                DistanceName = dist.Name,
                                Name = seg.Name,
                                DistanceValue = seg.CumulativeDistance,
                                DistanceUnit = distanceUnits[seg.DistanceUnit],
                                GPS = seg.GPS,
                                MapLink = seg.MapLink,
                            });
                        }
                    }
                }
            }
            // add finish segments
            foreach (Distance d in distances.Values)
            {
                if (Constants.Timing.DISTANCE_NO_LINKED_ID == d.LinkedDistance && distanceUnits.TryGetValue(d.DistanceUnit, out string oDistUnit))
                {
                    segments.Add(new()
                    {
                        Location = "Finish",
                        DistanceName = d.Name,
                        Name = "Finish",
                        DistanceValue = d.DistanceValue,
                        DistanceUnit = oDistUnit,
                        GPS = "",
                        MapLink = "",
                    });
                }
            }
            // Remove all segments without a distance value set.
            segments.RemoveAll(x => x.DistanceValue <= 0);
            Log.D("UI.MainPages.SegmentsPage", "Attempting to upload " + segments.Count.ToString() + " segments.");
            try
            {
                AddSegmentsResponse response = await APIHandlers.AddSegments(api, event_ids[0], event_ids[1], segments);
                if (response == null || response.Segments == null)
                {
                    DialogBox.Show("Error uploading segments.");
                }
                else if (response.Segments.Count != segments.Count)
                {
                    DialogBox.Show("Error uploading segments. Uploaded count doesn't match.");
                }
            }
            catch (APIException ex)
            {
                DialogBox.Show(ex.Message);
            }
            UploadButton.IsEnabled = true;
            UploadButton.Content = "Upload";
        }
    }

    private async void DeleteButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.SegmentsPage", "Deleting uploaded segments.");
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
                await APIHandlers.DeleteSegments(api, event_ids[0], event_ids[1]);
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

    private void Update_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdateDatabase();
        UpdateSegments();
        foreach (Object seg in SegmentsBox.Items)
        {
            if (seg is ASegment segment)
            {
                Segment thisSegment = segment.mySegment;
                if (thisSegment.LocationId == Constants.Timing.LOCATION_FINISH && thisSegment.Occurrence >= theEvent.FinishMaxOccurrences)
                {
                    DialogBox.Show("Your finish line has one or more segments beyond the maximum number it supports (" + (theEvent.FinishMaxOccurrences - 1) + ").  This could cause errors.");
                }
                else if (thisSegment.LocationId == Constants.Timing.LOCATION_START && thisSegment.Occurrence >= theEvent.StartMaxOccurrences)
                {
                    DialogBox.Show("Your start line has one or more segments beyond the maximum number it supports (" + (theEvent.StartMaxOccurrences - 1) + ").  This could cause errors.");
                }
                Log.D("UI.MainPages.SegmentsPage", "Distance ID " + segment.mySegment.DistanceId + " Segment Name " + segment.mySegment.Name + " segment ID " + segment.mySegment.Identifier);
            }
        }
        UpdateView();
    }

    private void Reset_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        UpdateSegments();
        UpdateView();
    }
}