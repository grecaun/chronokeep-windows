using ChronoKeep.Interfaces;
using ChronoKeep.Objects;
using ChronoKeep.Timing;
using ChronoKeep.UI.API;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
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
using System.Xml;

namespace ChronoKeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : Page, IMainPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent = null;

        public DashboardPage(IMainWindow mainWindow, IDBInterface db)
        {
            InitializeComponent();
            this.mWindow = mainWindow;
            this.database = db;
            UpdateView();
        }

        public void UpdateView()
        {
            int oldEventId = theEvent == null ? -1 : theEvent.Identifier;
            theEvent = database.GetCurrentEvent();
            if (theEvent != null && oldEventId != -1 && oldEventId != theEvent.Identifier)
            {
                mWindow.NotifyTimingWorker();

                // Setup AgeGroup static variables
                AgeGroup.SetAgeGroups(database.GetAgeGroups(theEvent.Identifier));
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
            commonAgeCheckBox.IsChecked = theEvent.CommonAgeGroups;
            commonStartCheckBox.IsChecked = theEvent.CommonStartFinish;
            segmentCheckBox.IsChecked = theEvent.DivisionSpecificSegments;
            ComboBoxItem eventType = null;
            foreach (ComboBoxItem item in TypeBox.Items)
            {
                if (item.Content.Equals(theEvent.EventTypeString))
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
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Edit Button Clicked.");
            if (editButton.Content.ToString() == Constants.DashboardLabels.EDIT)
            {
                Log.D("Editing.");
                editButton.Content = Constants.DashboardLabels.WORKING;
                EnableEditableFields();
                editButton.Content = Constants.DashboardLabels.SAVE;
                cancelButton.Visibility = Visibility.Visible;
            }
            else if (editButton.Content.ToString() == Constants.DashboardLabels.SAVE)
            {
                Log.D("Saving");
                editButton.Content = Constants.DashboardLabels.WORKING;
                DisableEditableFields();
                theEvent.Name = eventNameTextBox.Text;
                theEvent.YearCode = eventYearCodeTextBox.Text;
                theEvent.Date = eventDatePicker.Text;
                theEvent.RankByGun = (rankByGunCheckBox.IsChecked ?? false);
                theEvent.CommonAgeGroups = (commonAgeCheckBox.IsChecked ?? false);
                theEvent.CommonStartFinish = (commonStartCheckBox.IsChecked ?? false);
                theEvent.DivisionSpecificSegments = (segmentCheckBox.IsChecked ?? false);
                theEvent.EventType = Constants.Timing.EVENT_TYPE_DISTANCE;
                if (((ComboBoxItem)TypeBox.SelectedItem).Content.Equals("Time Based"))
                {
                    theEvent.EventType = Constants.Timing.EVENT_TYPE_TIME;
                }
                Log.D("Updating database.");
                // Check if we've changed the segment option
                Event oldEvent = database.GetCurrentEvent();
                if (oldEvent.DivisionSpecificSegments != theEvent.DivisionSpecificSegments)
                {
                    Log.D("Division Specific Segments value has changed.");
                    database.ResetSegments(theEvent.Identifier);
                }
                database.UpdateEvent(theEvent);
                Log.D("Updating view.");
                mWindow.NotifyTimingWorker(); ;
                UpdateView();
            }
            else
            {
                Log.D("Crying.");
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
            TypeBox.IsEnabled = false;
        }

        public void EnableEditableFields()
        {
            eventNameTextBox.IsEnabled = true;
            eventYearCodeTextBox.IsEnabled = true;
            eventDatePicker.IsEnabled = true;
            rankByGunCheckBox.IsEnabled = true;
            commonAgeCheckBox.IsEnabled = true;
            commonStartCheckBox.IsEnabled = true;
            segmentCheckBox.IsEnabled = true;
            TypeBox.IsEnabled = true;
        }

        private void NewEvent_Click(object sender, RoutedEventArgs e)
        {
            Log.D("New event clicked.");
            if (TimingController.IsRunning())
            {
                MessageBoxResult result = MessageBox.Show("You are currently connected to one or more Timing Systems.  Do you wish to close these connections and create a new event?",
                    "Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    mWindow.ShutdownTimingController();
                }
                else
                {
                    return;
                }
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
            Log.D("Import event clicked.");
            OpenFileDialog openFileDialog = new OpenFileDialog() { Filter = "SQLite Database Files (*.sqlite)|*.sqlite;|All files|*" };
            if (openFileDialog.ShowDialog() == true)
            {
                SQLiteInterface savedDatabase = new SQLiteInterface(openFileDialog.FileName);
                savedDatabase.Initialize();
                List<Event> events = savedDatabase.GetEvents();
                int lastID = -1;
                foreach (Event ev in events)
                {
                    int oldEventId = ev.Identifier, newEventId = -1;
                    ev.Identifier = -1;
                    ev.NextYear = -1;
                    database.AddEvent(ev);
                    newEventId = database.GetEventID(ev);
                    lastID = newEventId;
                    // Get all of the parts that don't depend on other parts, then parts that do.
                    // Order of operation matters here.
                    List<AgeGroup> ageGroups = savedDatabase.GetAgeGroups(oldEventId);
                    foreach (AgeGroup item in ageGroups)
                    {
                        item.EventId = newEventId;
                    }
                    database.AddAgeGroups(ageGroups);
                    List<BibChipAssociation> bibChipAssociations = savedDatabase.GetBibChips(oldEventId);
                    database.AddBibChipAssociation(newEventId, bibChipAssociations);
                    List<Distance> divisions = savedDatabase.GetDistances(oldEventId);
                    foreach (Distance item in divisions)
                    {
                        item.EventIdentifier = newEventId;
                    }
                    database.AddDistances(divisions);
                    List<Segment> segments = savedDatabase.GetSegments(oldEventId);
                    foreach (Segment item in segments)
                    {
                        item.EventId = newEventId;
                    }
                    database.AddSegments(segments);
                    List<TimingLocation> locations = savedDatabase.GetTimingLocations(oldEventId);
                    foreach (TimingLocation item in locations)
                    {
                        item.EventIdentifier = newEventId;
                    }
                    database.AddTimingLocations(locations);
                    List<Participant> participants = savedDatabase.GetParticipants(oldEventId);
                    foreach (Participant item in participants)
                    {
                        item.EventSpecific.EventIdentifier = newEventId;
                    }
                    database.AddParticipants(participants);
                    List<ChipRead> chipReads = savedDatabase.GetChipReads(oldEventId);
                    foreach (ChipRead item in chipReads)
                    {
                        item.EventId = newEventId;
                    }
                    database.AddChipReads(chipReads);
                    List<TimeResult> results = savedDatabase.GetTimingResults(oldEventId);
                    foreach (TimeResult item in results)
                    {
                        item.EventIdentifier = newEventId;
                    }
                    database.AddTimingResults(results);
                }
                database.SetAppSetting(Constants.Settings.CURRENT_EVENT, lastID.ToString());
                UpdateView();
            }
        }

        private void ChangeEvent_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Change event clicked.");
            if (TimingController.IsRunning())
            {
                MessageBoxResult result = MessageBox.Show("You are currently connected to one or more Timing Systems.  Do you wish to close these connections and change the viewed event?",
                    "Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    mWindow.ShutdownTimingController();
                }
                else
                {
                    return;
                }
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
            Log.D("Delete event clicked.");
            if (TimingController.IsRunning())
            {
                MessageBoxResult result = MessageBox.Show("You are currently connected to one or more Timing Systems.  Do you wish to close these connections and delete this event?",
                    "Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    mWindow.ShutdownTimingController();
                }
                else
                {
                    return;
                }
            }
            try
            {
                Log.D("Attempting to delete.");
                MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this event? This cannot be undone.",
                                                            "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    database.RemoveEvent(theEvent.Identifier);
                    database.SetAppSetting(Constants.Settings.CURRENT_EVENT, "-1");
                }
            }
            catch
            {
                Log.D("Unable to remove the event.");
                MessageBox.Show("Unable to remove the event.");
            }
            UpdateView();
            mWindow.UpdateStatus();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Cancel clicked.");
            DisableEditableFields();
            UpdateView();
            editButton.Content = Constants.DashboardLabels.EDIT;
            cancelButton.Visibility = Visibility.Collapsed;
        }

        private void TagTesterButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Tag Tester clicked.");
            ChipReaderWindow crWindow = ChipReaderWindow.NewWindow(mWindow, database);
            if (crWindow != null)
            {
                mWindow.AddWindow(crWindow);
                crWindow.ShowDialog();
            }
        }

        public void UpdateDatabase() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public void Closing()
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
        }

        private void SaveEvent_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Saving event.");
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "SQLite Database File (*.sqlite)|*.sqlite",
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                Log.D("Creating database file.");
                try
                {
                    SQLiteConnection.CreateFile(saveFileDialog.FileName);
                }
                catch
                {
                    MessageBox.Show("Unable to save to file.");
                    return;
                }
                SQLiteInterface savedDatabase = new SQLiteInterface(saveFileDialog.FileName);
                savedDatabase.Initialize();
                Event theEvent = database.GetCurrentEvent();
                int oldEventId = theEvent.Identifier, newEventId = -1;
                theEvent.Identifier = -1;
                theEvent.NextYear = -1;
                savedDatabase.AddEvent(theEvent);
                newEventId = savedDatabase.GetEventID(theEvent);
                // Get all of the parts that don't depend on other parts, then parts that do.
                // Order of operation matters here.
                List<AgeGroup> ageGroups = database.GetAgeGroups(oldEventId);
                foreach (AgeGroup item in ageGroups)
                {
                    item.EventId = newEventId;
                }
                savedDatabase.AddAgeGroups(ageGroups);
                List<BibChipAssociation> bibChipAssociations = database.GetBibChips(oldEventId);
                savedDatabase.AddBibChipAssociation(newEventId, bibChipAssociations);
                List<Distance> divisions = database.GetDistances(oldEventId);
                foreach (Distance item in divisions)
                {
                    item.EventIdentifier = newEventId;
                }
                savedDatabase.AddDistances(divisions);
                List<Segment> segments = database.GetSegments(oldEventId);
                foreach (Segment item in segments)
                {
                    item.EventId = newEventId;
                }
                savedDatabase.AddSegments(segments);
                List<TimingLocation> locations = database.GetTimingLocations(oldEventId);
                foreach (TimingLocation item in locations)
                {
                    item.EventIdentifier = newEventId;
                }
                savedDatabase.AddTimingLocations(locations);
                List<Participant> participants = database.GetParticipants(oldEventId);
                foreach (Participant item in participants)
                {
                    item.EventSpecific.EventIdentifier = newEventId;
                }
                savedDatabase.AddParticipants(participants);
                List<ChipRead> chipReads = database.GetChipReads(oldEventId);
                foreach (ChipRead item in chipReads)
                {
                    item.EventId = newEventId;
                }
                savedDatabase.AddChipReads(chipReads);
                List<TimeResult> results = database.GetTimingResults(oldEventId);
                foreach (TimeResult item in results)
                {
                    item.EventIdentifier = newEventId;
                }
                savedDatabase.AddTimingResults(results);
            }
            Log.D("Done saving file.");
        }

        private void apiLinkButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Link to API Event.");
            APIWindow apiWindow = APIWindow.NewWindow(mWindow, database);
            if (apiWindow != null) {
                mWindow.AddWindow(apiWindow);
                apiWindow.ShowDialog();
                UpdateView();
            }
        }

        private void apiPageButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Results API button clicked.");
            mWindow.SwitchPage(new APIPage(mWindow, database), true);
        }
    }
}
