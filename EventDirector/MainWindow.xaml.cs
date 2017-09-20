using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
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

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IDBInterface database;
        String dbName = "EventDirector.sqlite";
        ParticipantsList partList = null;

        public MainWindow()
        {
            InitializeComponent();
            Log.D("Looking for database file.");
            if (!File.Exists(dbName))
            {
                Log.D("Creating database file.");
                SQLiteConnection.CreateFile(dbName);
            }
            database = new SQLiteInterface(dbName);
            database.Initialize();
            UpdateEventBox();
        }

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            int menuId = Convert.ToInt32(((MenuItem)sender).Uid);
            switch (menuId)
            {
                case 1:     // Connection Settings
                    Log.D("Settings");
                    break;
                case 3:     // Clear Database
                    Log.D("Clear Database.");
                    await Task.Run( () =>
                    {
                        database.ResetDatabase();
                    });
                    Log.D("Database Reset.");
                    UpdateEventBox();
                    break;
                case 4:     // Exit
                    Log.D("Goodbye");
                    break;
                case 5:     // Import participants
                    Log.D("Import (CSV)");
                    break;
                case 6:     // Assign bibs/chips
                    Log.D("Assign");
                    break;
                case 7:     // List Participants
                    Log.D("List Participants");
                    if (partList == null)
                    {
                        partList = new ParticipantsList(database);
                    }
                    partList.Show();
                    break;
                default:
                    break;
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            String buttonName = ((Button)sender).Name;
            if (buttonName == "eventsRemoveButton")
            {
                Log.D("Events - Remove Button Pressed.");
                Event anEvent = (Event)eventsListView.SelectedItem;
                if (anEvent != null) database.RemoveEvent(anEvent);
                UpdateEventBox();
            }
            else if (buttonName == "divisionsRemoveButton")
            {
                Log.D("Divisions - Remove Button Pressed.");
                Division division = (Division)divisionsListView.SelectedItem;
                if (division != null) database.RemoveDivision(division);
                Event anEvent = (Event)eventsListView.SelectedItem;
                if (anEvent != null) UpdateDivisionsBox(anEvent.Identifier);
            }
            else if (buttonName == "timingPointsRemoveButton")
            {
                Log.D("TimingPoints - Remove Button Pressed.");
                TimingPoint timingPoint = (TimingPoint)timingPointsListView.SelectedItem;
                if (timingPoint != null) database.RemoveTimingPoint(timingPoint);
                Event anEvent = (Event)eventsListView.SelectedItem;
                if (anEvent != null) UpdateTimingPointsBox(anEvent.Identifier);
            }
        }

        private void ModifyButton_Click(object sender, RoutedEventArgs e)
        {
            String buttonName = ((Button)sender).Name;
            if (buttonName == "eventsModifyButton")
            {
                Log.D("Events - Modify Button Pressed.");
            }
            else if (buttonName == "divisionsModifyButton")
            {
                Log.D("Divisions - Modify Button Pressed.");
            }
            else if (buttonName == "timingPointsModifyButton")
            {
                Log.D("TimingPoints - Modify Button Pressed.");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            String buttonName = ((Button)sender).Name;
            if (buttonName == "eventsAddButton")
            {
                Log.D("Events - Add Button Pressed.");
                NewEventWindow eventWindow = new NewEventWindow(this);
                eventWindow.Show();
            }
            else if (buttonName == "divisionsAddButton")
            {
                Log.D("Divisions - Add Button Pressed.");
                NewDivisionWindow divisionWindow = new NewDivisionWindow(this);
                divisionWindow.Show();
            }
            else if (buttonName == "timingPointsAddButton")
            {
                Log.D("TimingPoints - Add Button Pressed.");
                NewTimingPointWindow timingPointWindow = new NewTimingPointWindow(this);
                timingPointWindow.Show();
            }
        }

        internal async void AddEvent(String name, long date)
        {
            await Task.Run(() =>
            {
                database.AddEvent(new Event(name, date));
            });
            UpdateEventBox();
        }

        internal async void AddTimingPoint(string nameString, string distanceStr, string unitString)
        {
            int eventId = ((Event)eventsListView.SelectedItem).Identifier;
            int divisionId = 0;
            await Task.Run(() =>
            {
                database.AddTimingPoint(new TimingPoint(eventId, divisionId, nameString, distanceStr, unitString));
            });
            UpdateTimingPointsBox(eventId);
        }

        internal async void AddDivision(string nameString)
        {
            int eventId = ((Event)eventsListView.SelectedItem).Identifier;
            await Task.Run(() =>
            {
                database.AddDivision(new Division(nameString, eventId));
            });
            UpdateDivisionsBox(eventId);
        }

        private async void UpdateEventBox()
        {
            ArrayList events = null;
            await Task.Run(() =>
            {
                events = database.GetEvents();
            });
            eventsListView.Items.Clear();
            foreach (Event e in events)
            {
                eventsListView.Items.Add(e);
            }
        }

        private void UpdateChangesBox(int eventId)
        {
            Log.D("Updating changes box.");

        }

        private async void UpdateTimingPointsBox(int eventId)
        {
            Log.D("Updating timing points box.");
            if (timingPointsListView.Visibility == Visibility.Hidden)
            {
                return;
            }
            ArrayList timingPoints = null;
            await Task.Run(() =>
            {
                timingPoints = database.GetTimingPoints(eventId);
            });
            timingPointsListView.Items.Clear();
            foreach (TimingPoint t in timingPoints)
            {
                timingPointsListView.Items.Add(t);
            }
        }

        private async void UpdateDivisionsBox(int eventId)
        {
            Log.D("Updating divisions box.");
            if (divisionsListView.Visibility == Visibility.Hidden)
            {
                return;
            }
            ArrayList divisions = null;
            await Task.Run(() =>
            {
                divisions = database.GetDivisions(eventId);
            });
            divisionsListView.Items.Clear();
            foreach (Division d in divisions)
            {
                divisionsListView.Items.Add(d);
            }
        }

        private void EventsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null) { return; }
            Event anEvent = (Event)eventsListView.SelectedItem;
            Log.D("Sender is as follows: " + sender + " the event associated with it has an eventId of " + (anEvent == null ? "null" : anEvent.Identifier.ToString()));
            if (anEvent != null)
            {
                eventsModifyButton.Visibility = Visibility.Visible;
                eventsRemoveButton.Visibility = Visibility.Visible;
                divisionsAddButton.Visibility = Visibility.Visible;
                divisionsListView.Visibility = Visibility.Visible;
                timingPointsAddButton.Visibility = Visibility.Visible;
                timingPointsListView.Visibility = Visibility.Visible;
                UpdateDivisionsBox(anEvent.Identifier);
                UpdateTimingPointsBox(anEvent.Identifier);
            }
            else
            {
                eventsModifyButton.Visibility = Visibility.Hidden;
                eventsRemoveButton.Visibility = Visibility.Hidden;
                divisionsAddButton.Visibility = Visibility.Hidden;
                divisionsListView.Visibility = Visibility.Hidden;
                timingPointsAddButton.Visibility = Visibility.Hidden;
                timingPointsListView.Visibility = Visibility.Hidden;
                timingPointsModifyButton.Visibility = Visibility.Hidden;
                timingPointsRemoveButton.Visibility = Visibility.Hidden;
                divisionsModifyButton.Visibility = Visibility.Hidden;
                divisionsRemoveButton.Visibility = Visibility.Hidden;
            }
        }

        private void TimingPointsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null) { return; }
            if (timingPointsListView.SelectedIndex < 0)
            {
                timingPointsModifyButton.Visibility = Visibility.Hidden;
                timingPointsRemoveButton.Visibility = Visibility.Hidden;
            }
            else
            {
                timingPointsModifyButton.Visibility = Visibility.Visible;
                timingPointsRemoveButton.Visibility = Visibility.Visible;
            }
        }

        private void DivisionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null) { return; }
            if (divisionsListView.SelectedIndex > 0)
            {
                divisionsModifyButton.Visibility = Visibility.Hidden;
                divisionsRemoveButton.Visibility = Visibility.Hidden;
            }
            else
            {
                divisionsModifyButton.Visibility = Visibility.Visible;
                divisionsRemoveButton.Visibility = Visibility.Visible;
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
