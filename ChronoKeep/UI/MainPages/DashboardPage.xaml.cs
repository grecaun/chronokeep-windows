using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;
using Chronokeep.UI.API;
using Chronokeep.UI.UIObjects;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;

namespace Chronokeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : IMainPage
    {
        private readonly IMainWindow mWindow;
        private readonly IDBInterface database;
        private Event theEvent = null;

        public DashboardPage(IMainWindow mainWindow, IDBInterface db)
        {
            InitializeComponent();
            this.mWindow = mainWindow;
            this.database = db;
            theEvent = database.GetCurrentEvent();
            UpdateView();
        }

        public void UpdateView()
        {
            int oldEventId = theEvent == null ? -1 : theEvent.Identifier;
            theEvent = database.GetCurrentEvent();
            if (theEvent != null && oldEventId != -1 && oldEventId != theEvent.Identifier)
            {
                mWindow.NotifyTimingWorker();
            }
            if (theEvent == null || theEvent.Identifier == -1)
            {
                LeftPanel.Visibility = Visibility.Hidden;
                RightPanel.Visibility = Visibility.Hidden;
                return;
            }
            LeftPanel.Visibility = Visibility.Visible;
            RightPanel.Visibility = Visibility.Visible;
            eventNameTextBox.Text = theEvent.Name;
            eventYearCodeTextBox.Text = theEvent.YearCode;
            eventDatePicker.Text = theEvent.Date;
            rankByGunCheckBox.IsChecked = theEvent.RankByGun;
            if (theEvent != null && Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA == theEvent.EventType)
            {
                rankByGunCheckBox.Content = "Rank by Elapsed Time";
            }
            else
            {
                rankByGunCheckBox.Content = "Rank by Clock Time";
            }
            commonAgeCheckBox.IsChecked = theEvent.CommonAgeGroups;
            commonStartCheckBox.IsChecked = theEvent.CommonStartFinish;
            segmentCheckBox.IsChecked = theEvent.DistanceSpecificSegments;
            placementsCheckBox.IsChecked = theEvent.DisplayPlacements;
            divisionsEnabledCheckbox.IsChecked = theEvent.DivisionsEnabled;
            uploadSpecificDistanceResults.IsChecked = theEvent.UploadSpecific;
            ComboBoxItem eventType = null;
            foreach (ComboBoxItem item in TypeBox.Items)
            {
                if (item.Uid == theEvent.EventType.ToString())
                {
                    eventType = item;
                }
            }
            if (eventType != null)
            {
                TypeBox.SelectedItem = eventType;
            }
            else
            {
                TypeBox.SelectedIndex = 0;
            }
            editButton.Content = Constants.DashboardLabels.EDIT;
            cancelButton.Visibility = Visibility.Collapsed;
            if (theEvent.API_ID > 0 && theEvent.API_Event_ID != "")
            {
                apiLinkButton.Content = "Event Linked";
            } else
            {
                apiLinkButton.Content = "Link to API Event";
            }
            if (mWindow.IsRegistrationRunning())
            {
                registrationButton.Content = "Stop Registration";
            }
            else
            {
                registrationButton.Content = "Start Registration";
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.DashboardPage", "Edit Button Clicked.");
            if (editButton.Content.ToString() == Constants.DashboardLabels.EDIT)
            {
                Log.D("UI.DashboardPage", "Editing.");
                editButton.Content = Constants.DashboardLabels.WORKING;
                EnableEditableFields();
                editButton.Content = Constants.DashboardLabels.SAVE;
                cancelButton.Visibility = Visibility.Visible;
            }
            else if (editButton.Content.ToString() == Constants.DashboardLabels.SAVE)
            {
                Log.D("UI.DashboardPage", "Saving");
                editButton.Content = Constants.DashboardLabels.WORKING;
                DisableEditableFields();
                // If distance specific segments are being enabled/disabled then reset all segments
                // so no residual segments stay around.
                if (theEvent.DistanceSpecificSegments != segmentCheckBox.IsChecked)
                {
                    database.ResetSegments(theEvent.Identifier);
                }
                theEvent.Name = eventNameTextBox.Text;
                theEvent.YearCode = eventYearCodeTextBox.Text;
                theEvent.Date = eventDatePicker.Text;
                theEvent.RankByGun = rankByGunCheckBox.IsChecked ?? false;
                theEvent.CommonAgeGroups = commonAgeCheckBox.IsChecked ?? false;
                theEvent.CommonStartFinish = commonStartCheckBox.IsChecked ?? false;
                theEvent.DistanceSpecificSegments = segmentCheckBox.IsChecked ?? false;
                theEvent.DisplayPlacements = placementsCheckBox.IsChecked ?? true;
                theEvent.DivisionsEnabled = divisionsEnabledCheckbox.IsChecked ?? false;
                theEvent.UploadSpecific = uploadSpecificDistanceResults.IsChecked ?? false;
                try
                {
                    theEvent.EventType = int.Parse(((ComboBoxItem)TypeBox.SelectedItem).Uid);
                }
                catch
                {
                    theEvent.EventType = Constants.Timing.EVENT_TYPE_DISTANCE;
                }
                Log.D("UI.DashboardPage", "Updating database.");
                // Check if we've changed the segment option
                Event oldEvent = database.GetCurrentEvent();
                if (oldEvent.DistanceSpecificSegments != theEvent.DistanceSpecificSegments)
                {
                    Log.D("UI.DashboardPage", "Distance Specific Segments value has changed.");
                    database.ResetSegments(theEvent.Identifier);
                }
                if (oldEvent.CommonAgeGroups != theEvent.CommonAgeGroups)
                {
                    Log.D("UI.DashboardPage", "Common Age Groups value has changed.");
                    database.ResetAgeGroups(theEvent.Identifier);
                }
                if (oldEvent.DivisionsEnabled != theEvent.DivisionsEnabled)
                {
                    Log.D("UI.DashboardPage", "Divisions Enabled value has changed.");
                    database.UpdateDivisionsEnabled();
                }
                database.UpdateEvent(theEvent);
                Log.D("UI.DashboardPage", "Updating view.");
                mWindow.NotifyTimingWorker(); ;
                UpdateView();
            }
            else
            {
                Log.D("UI.DashboardPage", "Crying.");
            }
        }

        public void DisableEditableFields()
        {
            eventNameTextBox.IsEnabled = false;
            eventYearCodeTextBox.IsEnabled = false;
            eventDatePicker.IsEnabled = false;
            rankByGunCheckBox.IsEnabled = false;
            commonAgeCheckBox.IsEnabled = false;
            commonStartCheckBox.IsEnabled = false;
            segmentCheckBox.IsEnabled = false;
            placementsCheckBox.IsEnabled = false;
            divisionsEnabledCheckbox.IsEnabled = false;
            uploadSpecificDistanceResults.IsEnabled = false;
            TypeBox.IsEnabled = false;
        }

        public void EnableEditableFields()
        {
            eventNameTextBox.IsEnabled = true;
            eventYearCodeTextBox.IsEnabled = true;
            eventDatePicker.IsEnabled = true;
            rankByGunCheckBox.IsEnabled = true;
            if (((ComboBoxItem)TypeBox.SelectedItem).Uid == Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA.ToString())
            {
                commonAgeCheckBox.IsEnabled = false;
                segmentCheckBox.IsEnabled = false;
                commonStartCheckBox.IsEnabled = false;
            }
            else
            {
                commonAgeCheckBox.IsEnabled = true;
                segmentCheckBox.IsEnabled = true;
                commonStartCheckBox.IsEnabled = true;
            }
            TypeBox.IsEnabled = true;
            placementsCheckBox.IsEnabled = true;
            divisionsEnabledCheckbox.IsEnabled = true;
            uploadSpecificDistanceResults.IsEnabled = true;
        }

        private bool CancelEventChangeAsync(EventClickType clickType)
        {
            Log.D("UI.DashboardPage", "Checking if we need to cancel the change.");
            if (mWindow.BackgroundProcessesRunning())
            {
                DialogBox.Show(
                    "There are processes running in the background. Do you wish to stop these and continue?",
                    "Yes",
                    "No",
                    () =>
                    {
                        mWindow.StopBackgroundProcesses();
                        switch (clickType)
                        {
                            case EventClickType.NewEvent:
                                NewEventWindow newEventWindow = NewEventWindow.NewWindow(mWindow, database);
                                if (newEventWindow != null)
                                {
                                    mWindow.AddWindow(newEventWindow);
                                    newEventWindow.ShowDialog();
                                }
                                break;
                            case EventClickType.ImportEvent:
                                OpenFileDialog openFileDialog = new() { Filter = "SQLite Database Files (*.sqlite)|*.sqlite;|All files|*" };
                                if (openFileDialog.ShowDialog() == true)
                                {
                                    SQLiteInterface savedDatabase = new(openFileDialog.FileName);
                                    savedDatabase.Initialize();
                                    List<Event> events = savedDatabase.GetEvents();
                                    int lastID = -1;
                                    foreach (Event ev in events)
                                    {
                                        int tmp = SaveEvent(ev, savedDatabase, database);
                                        if (tmp > 0)
                                        {
                                            lastID = tmp;
                                        }
                                    }
                                    database.SetCurrentEvent(lastID);
                                    UpdateView();
                                    mWindow.UpdateStatus();
                                }
                                break;
                            case EventClickType.ChangeEvent:
                                ChangeEventWindow changeEventWindow = ChangeEventWindow.NewWindow(mWindow, database);
                                if (changeEventWindow != null)
                                {
                                    mWindow.AddWindow(changeEventWindow);
                                    changeEventWindow.ShowDialog();
                                }
                                break;
                            case EventClickType.DeleteEvent:
                                try
                                {
                                    Log.D("UI.DashboardPage", "Attempting to delete.");
                                    DialogBox.Show(
                                        "Are you sure you want to delete this event? This cannot be undone.",
                                        "Yes",
                                        "No",
                                        () =>
                                        {
                                            database.RemoveEvent(theEvent.Identifier);
                                            database.SetCurrentEvent(-1);
                                        }
                                        );
                                }
                                catch
                                {
                                    Log.D("UI.DashboardPage", "Unable to remove the event.");
                                    DialogBox.Show("Unable to remove the event.");
                                }
                                UpdateView();
                                mWindow.UpdateStatus();
                                break;
                        }
                    }
                    );
                return true;
            }
            return false;
        }

        private enum EventClickType
        {
            NewEvent,
            ImportEvent,
            ChangeEvent,
            DeleteEvent,
        }

        private void NewEvent_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.DashboardPage", "New event clicked.");
            if (CancelEventChangeAsync(EventClickType.NewEvent))
            {
                return;
            }
            NewEventWindow newEventWindow = NewEventWindow.NewWindow(mWindow, database);
            if (newEventWindow != null)
            {
                mWindow.AddWindow(newEventWindow);
                newEventWindow.ShowDialog();
            }
        }

        private void ImportEvent_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.DashboardPage", "Import event clicked.");
            if (CancelEventChangeAsync(EventClickType.ImportEvent))
            {
                return;
            }
            OpenFileDialog openFileDialog = new() { Filter = "SQLite Database Files (*.sqlite)|*.sqlite;|All files|*" };
            if (openFileDialog.ShowDialog() == true)
            {
                SQLiteInterface savedDatabase = new(openFileDialog.FileName);
                savedDatabase.Initialize();
                List<Event> events = savedDatabase.GetEvents();
                int lastID = -1;
                foreach (Event ev in events)
                {
                    int tmp = SaveEvent(ev, savedDatabase, database);
                    if (tmp > 0)
                    {
                        lastID = tmp;
                    }
                }
                database.SetCurrentEvent(lastID);
                UpdateView();
                mWindow.UpdateStatus();
            }
        }

        private static int SaveEvent(Event oldEvent, IDBInterface loadFrom, IDBInterface saveTo)
        {
            // Make some modifications, note that we cannot guarantee API compatibility between events.
            Event newEvent = new();
            newEvent.CopyAll(oldEvent);
            newEvent.API_Event_ID = Constants.APIConstants.NULL_EVENT_ID;
            newEvent.API_ID = Constants.APIConstants.NULL_ID;
            newEvent.Identifier = -1;
            saveTo.AddEvent(newEvent);
            newEvent.Identifier = saveTo.GetEventID(newEvent);
            // Only proceed if we managed to add the event or we can find it.
            if (newEvent.Identifier > 0)
            {
                // Get all of the parts that don't depend on other parts, then parts that do.
                // Order of operation matters here.
                // Bib chip associations do not have any linked ID's.
                Log.D("UI.DashboardPage", "Adding bib chip associations.");
                List<BibChipAssociation> bibChipAssociations = loadFrom.GetBibChips(oldEvent.Identifier);
                saveTo.AddBibChipAssociation(newEvent.Identifier, bibChipAssociations);
                // Distances can link to themselves. DistanceID is also used by EVENTSPECIFIC, SEGMENTS, and AGE_GROUPS
                Log.D("UI.DashboardPage", "Adding distances.");
                Dictionary<int, int> distanceIDTranslation = [];
                Dictionary<string, int> oldDistanceIDDictionary = [];
                List<Distance> normalDistances = [];
                List<Distance> linkedDistances = [];
                foreach (Distance item in loadFrom.GetDistances(oldEvent.Identifier))
                {
                    // Set event identifier to new event id.
                    item.EventIdentifier = newEvent.Identifier;
                    // Check if its a linked distance and place it in the correct list.
                    if (item.LinkedDistance == Constants.Timing.DISTANCE_NO_LINKED_ID)
                    {
                        normalDistances.Add(item);
                    }
                    else
                    {
                        linkedDistances.Add(item);
                    }
                    // Set it so we can get the old ID by the name of the distance.
                    oldDistanceIDDictionary[item.Name] = item.Identifier;
                }
                // Insert the old distances
                saveTo.AddDistances(normalDistances);
                // Loop through all of the distances we just added and update our dictionary with their new ids.
                foreach (Distance item in saveTo.GetDistances(newEvent.Identifier))
                {
                    if (oldDistanceIDDictionary.TryGetValue(item.Name, out int oldDistId))
                    {
                        distanceIDTranslation[oldDistId] = item.Identifier;
                    }
                }
                // Update linked distances to their new division ID or set it to no linked if we can't find it.
                foreach (Distance item in linkedDistances)
                {
                    if (distanceIDTranslation.TryGetValue(item.LinkedDistance, out int linkedDistId))
                    {
                        item.LinkedDistance = linkedDistId;
                    }
                    else
                    {
                        item.LinkedDistance = Constants.Timing.DISTANCE_NO_LINKED_ID;
                    }
                }
                saveTo.AddDistances(linkedDistances);
                // Age groups rely only on the event, and the distance.
                // Age group id is used by EVENTSPECIFIC
                Log.D("UI.DashboardPage", "Adding age groups.");
                List<AgeGroup> ageGroups = [];
                Dictionary<int, int> ageGroupIDTranslation = [];
                // Key is START AGE
                Dictionary<int, int> oldAgeGroupDictionary = [];
                foreach (AgeGroup item in loadFrom.GetAgeGroups(oldEvent.Identifier))
                {
                    item.EventId = newEvent.Identifier;
                    oldAgeGroupDictionary[item.StartAge] = item.GroupId;
                    // Add the item to our list to save IFF it has a corred DistanceID set to it.
                    if (item.DistanceId != Constants.Timing.COMMON_AGEGROUPS_DISTANCEID)
                    {
                        if (distanceIDTranslation.TryGetValue(item.DistanceId, out int oDistId))
                        {
                            item.DistanceId = oDistId;
                            ageGroups.Add(item);
                        }
                    }
                    else
                    {
                        ageGroups.Add(item);
                    }
                }
                saveTo.AddAgeGroups(ageGroups);
                foreach (AgeGroup item in saveTo.GetAgeGroups(newEvent.Identifier))
                {
                    if (oldAgeGroupDictionary.TryGetValue(item.StartAge, out int oAGId))
                    {
                        ageGroupIDTranslation[oAGId] = item.GroupId;
                    }
                }
                // Locations are relied upon by SEGMENTS, CHIPREADS, and TIMERESULTS
                Log.D("UI.DashboardPage", "Adding locations.");
                List<TimingLocation> locations = loadFrom.GetTimingLocations(oldEvent.Identifier);
                Dictionary<int, int> locationIDTranslation = [];
                Dictionary<string, int> oldLocationDictionary = [];
                foreach (TimingLocation item in locations)
                {
                    item.EventIdentifier = newEvent.Identifier;
                    oldLocationDictionary[item.Name] = item.Identifier;
                }
                saveTo.AddTimingLocations(locations);
                // Update the location translation dictionary with oldID key and newid value.
                foreach (TimingLocation item in saveTo.GetTimingLocations(newEvent.Identifier))
                {
                    if (oldLocationDictionary.TryGetValue(item.Name, out int oLocId))
                    {
                        locationIDTranslation[oLocId] = item.Identifier;
                    }
                }
                locationIDTranslation[Constants.Timing.LOCATION_FINISH] = Constants.Timing.LOCATION_FINISH;
                locationIDTranslation[Constants.Timing.LOCATION_START] = Constants.Timing.LOCATION_START;
                locationIDTranslation[Constants.Timing.LOCATION_ANNOUNCER] = Constants.Timing.LOCATION_ANNOUNCER;
                locationIDTranslation[Constants.Timing.LOCATION_DUMMY] = Constants.Timing.LOCATION_DUMMY;
                // Segments rely on Locations and Distances
                // Segment ids are used by TIME_RESULTS
                Log.D("UI.DashboardPage", "Adding segments");
                List<Segment> segments = [];
                Dictionary<int, int> segmentIDTranslator = [];
                // key here is DISTANCE_ID, LOCATION_ID, OCCURRENCE (new values)
                Dictionary<(int, int, int), int> oldSegmentDictionary = [];
                foreach (Segment item in loadFrom.GetSegments(oldEvent.Identifier))
                {
                    item.EventId = newEvent.Identifier;
                    // only insert segments when there were no issues with the distance and location translations
                    // Make sure to check if we're using common segments.
                    if (item.DistanceId == Constants.Timing.COMMON_SEGMENTS_DISTANCEID)
                    {
                        if (locationIDTranslation.TryGetValue(item.LocationId, out int tLocIt))
                        {
                            item.LocationId = tLocIt;
                            oldSegmentDictionary[(item.DistanceId, item.LocationId, item.Occurrence)] = item.Identifier;
                            segments.Add(item);
                        }
                    }
                    else
                    {
                        if (distanceIDTranslation.TryGetValue(item.DistanceId, out int tDistId) && locationIDTranslation.TryGetValue(item.LocationId, out int yLocId))
                        {
                            item.DistanceId = tDistId;
                            item.LocationId = yLocId;
                            oldSegmentDictionary[(item.DistanceId, item.LocationId, item.Occurrence)] = item.Identifier;
                            segments.Add(item);
                        }
                    }
                }
                saveTo.AddSegments(segments);
                // Update our segmentIDTranslator
                foreach (Segment item in saveTo.GetSegments(newEvent.Identifier))
                {
                    if (oldSegmentDictionary.TryGetValue((item.DistanceId, item.LocationId, item.Occurrence), out int oSegId))
                    {
                        segmentIDTranslator[oSegId] = item.Identifier;
                    }
                }
                segmentIDTranslator[Constants.Timing.SEGMENT_FINISH] = Constants.Timing.SEGMENT_FINISH;
                segmentIDTranslator[Constants.Timing.SEGMENT_START] = Constants.Timing.SEGMENT_START;
                segmentIDTranslator[Constants.Timing.SEGMENT_NONE] = Constants.Timing.SEGMENT_NONE;
                // Participants contain EVENTSPECIFIC which relies on distance and age groups.
                // Eventspecific ID is used by TIME_RESULT
                Log.D("UI.DashboardPage", "Adding participants.");
                List<Participant> participants = [];
                Dictionary<int, int> eventspecificIDTranslation = [];
                // Bib is the key here
                Dictionary<string, int> oldEventSpecificDictionary = [];
                foreach (Participant item in loadFrom.GetParticipants(oldEvent.Identifier))
                {
                    item.EventSpecific.EventIdentifier = newEvent.Identifier;
                    oldEventSpecificDictionary[item.EventSpecific.Bib] = item.EventSpecific.Identifier;
                    // Only add the participant if we can translate their distance identifier.
                    if (distanceIDTranslation.TryGetValue(item.EventSpecific.DistanceIdentifier, out int oDistId))
                    {
                        item.EventSpecific.DistanceIdentifier = oDistId;
                        item.EventSpecific.AgeGroupId = ageGroupIDTranslation.TryGetValue(item.EventSpecific.AgeGroupId, out int oAGId) ? oAGId : Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
                        participants.Add(item);
                    }
                }
                saveTo.AddParticipants(participants);
                // Translate old ID's to new ID's
                foreach (Participant item in saveTo.GetParticipants(newEvent.Identifier))
                {
                    if (oldEventSpecificDictionary.TryGetValue(item.Bib, out int oESId))
                    {
                        eventspecificIDTranslation[oESId] = item.EventSpecific.Identifier;
                    }
                }
                // Chipreads depend on location_id.
                Log.D("UI.DashboardPage", "Adding chipreads.");
                List<ChipRead> chipReads = [];
                Dictionary<int, int> readIDTranslation = [];
                // (CHIPNUMBER, BIB, SECONDS, MILLISECONDS) for the key
                Dictionary<(string, string, long, int), int> oldReadDictionary = [];
                foreach (ChipRead item in loadFrom.GetChipReads(oldEvent.Identifier))
                {
                    item.EventId = newEvent.Identifier;
                    oldReadDictionary[(item.ChipNumber, item.Bib, item.Seconds, item.Milliseconds)] = item.ReadId;
                    // If the location is not a pre-set location, i.e. a custom location
                    if (item.LocationID != Constants.Timing.LOCATION_START && item.LocationID != Constants.Timing.LOCATION_FINISH && item.LocationID != Constants.Timing.LOCATION_ANNOUNCER)
                    {
                        if (locationIDTranslation.TryGetValue(item.LocationID, out int oLocId))
                        {
                            item.LocationID = oLocId;
                            chipReads.Add(item);
                        }
                    }
                    else
                    {
                        // this is a known location (start, finish, or announce)
                        chipReads.Add(item);
                    }
                }
                saveTo.AddChipReads(chipReads);
                foreach (ChipRead item in saveTo.GetChipReads(newEvent.Identifier))
                {
                    if (oldReadDictionary.TryGetValue((item.ChipNumber, item.Bib, item.Seconds, item.Milliseconds), out int oReadId))
                    {
                        readIDTranslation[oReadId] = item.ReadId;
                    }
                }
                // Results rely upon read_id, location_id, and segment_id.
                Log.D("UI.DashboardPage", "Adding results.");
                List<TimeResult> results = [];
                foreach (TimeResult item in loadFrom.GetTimingResults(oldEvent.Identifier))
                {
                    item.EventIdentifier = newEvent.Identifier;
                    if (readIDTranslation.TryGetValue(item.ReadId, out int tReadId) && locationIDTranslation.TryGetValue(item.LocationId, out int tLocId)
                        && segmentIDTranslator.TryGetValue(item.SegmentId, out int tSegId) && eventspecificIDTranslation.TryGetValue(item.EventSpecificId, out int tESId))
                    {
                        item.ReadId = tReadId;
                        item.LocationId = tSegId;
                        item.SegmentId = tSegId;
                        item.EventSpecificId = tESId;
                        results.Add(item);
                    }
                }
                saveTo.AddTimingResults(results);
            }
            return newEvent.Identifier;
        }

        private void ChangeEvent_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.DashboardPage", "Change event clicked.");
            if (CancelEventChangeAsync(EventClickType.ChangeEvent))
            {
                return;
            }
            ChangeEventWindow changeEventWindow = ChangeEventWindow.NewWindow(mWindow, database);
            if (changeEventWindow != null)
            {
                mWindow.AddWindow(changeEventWindow);
                changeEventWindow.ShowDialog();
            }
        }

        private void DeleteEvent_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.DashboardPage", "Delete event clicked.");
            if (CancelEventChangeAsync(EventClickType.DeleteEvent))
            {
                return;
            }
            try
            {
                Log.D("UI.DashboardPage", "Attempting to delete.");
                DialogBox.Show(
                    "Are you sure you want to delete this event? This cannot be undone.",
                    "Yes",
                    "No",
                    () =>
                    {
                        database.RemoveEvent(theEvent.Identifier);
                        database.SetCurrentEvent(-1);
                    }
                    );
            }
            catch
            {
                Log.D("UI.DashboardPage", "Unable to remove the event.");
                DialogBox.Show("Unable to remove the event.");
            }
            UpdateView();
            mWindow.UpdateStatus();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.DashboardPage", "Cancel clicked.");
            DisableEditableFields();
            UpdateView();
            editButton.Content = Constants.DashboardLabels.EDIT;
            cancelButton.Visibility = Visibility.Collapsed;
        }

        private void TagTesterButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.DashboardPage", "Tag Tester clicked.");
            ChipReaderWindow crWindow = ChipReaderWindow.NewWindow(mWindow, database);
            if (crWindow != null)
            {
                mWindow.AddWindow(crWindow);
                crWindow.Show();
            }
        }

        public void UpdateDatabase() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public void Closing()
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
        }

        private void SaveEvent_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.DashboardPage", "Saving event.");
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "SQLite Database File (*.sqlite)|*.sqlite",
                FileName = string.Format("{0} {1}.{2}", theEvent.YearCode, theEvent.Name, "sqlite"),
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                Log.D("UI.DashboardPage", "Creating database file.");
                try
                {
                    SQLiteConnection.CreateFile(saveFileDialog.FileName);
                }
                catch
                {
                    DialogBox.Show("Unable to save to file");
                    return;
                }
                SQLiteInterface savedDatabase = new(saveFileDialog.FileName);
                savedDatabase.Initialize();
                Event theEvent = database.GetCurrentEvent();
                SaveEvent(theEvent, database, savedDatabase);
                Log.D("UI.DashboardPage", "Done saving file.");
                DialogBox.Show("Event saved successfully.");
            }
        }

        private void ApiLinkButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.DashboardPage", "Link/Edit API Event.");
            if (theEvent.API_ID > 0 && theEvent.API_Event_ID != "")
            {
                EditAPIWindow editWindow = EditAPIWindow.NewWindow(mWindow, database);
                if (editWindow != null)
                {
                    mWindow.AddWindow(editWindow);
                    editWindow.ShowDialog();
                    UpdateView();
                }
            }
            else
            {
                APIWindow apiWindow = APIWindow.NewWindow(mWindow, database);
                if (apiWindow != null) {
                    mWindow.AddWindow(apiWindow);
                    apiWindow.ShowDialog();
                    UpdateView();
                }
            }
        }

        private void ApiPageButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.DashboardPage", "Results API button clicked.");
            mWindow.SwitchPage(new APIPage(mWindow, database));
        }

        private void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.D("UI.DashboardPage", "TypeBox selection changed.");
            int eventType = -1;
            try
            {
                eventType = int.Parse(((ComboBoxItem)TypeBox.SelectedItem).Uid);
            }
            catch
            {
                commonAgeCheckBox.IsEnabled = true;
                segmentCheckBox.IsEnabled = true;
                commonStartCheckBox.IsEnabled = true;
            }
            // Common age groups when backyard ultra is the event type.
            if (eventType == Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA)
            {
                commonAgeCheckBox.IsEnabled = false;
                commonAgeCheckBox.IsChecked = true;
                segmentCheckBox.IsEnabled = false;
                segmentCheckBox.IsChecked = false;
                commonStartCheckBox.IsEnabled = false;
                commonStartCheckBox.IsChecked = true;
                rankByGunCheckBox.Content = "Rank by Elapsed Time";
            }
            else if (editButton != null && editButton.Content.ToString() == Constants.DashboardLabels.SAVE)
            {
                commonAgeCheckBox.IsEnabled = true;
                segmentCheckBox.IsEnabled = true;
                commonStartCheckBox.IsEnabled = true;
                rankByGunCheckBox.Content = "Rank by Clock Time";
            }
            else
            {
                rankByGunCheckBox.Content = "Rank by Clock Time";
            }
        }

        private void RegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.DashboardPage", "Registration button clicked.");
            if (mWindow.IsRegistrationRunning())
            {
                mWindow.StopRegistration();
                registrationButton.Content = "Start Registration";
            }
            else
            {
                mWindow.StartRegistration();
                registrationButton.Content = "Stop Registration";
            }
        }
    }
}
