using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IChangeUpdater
    {
        IDBInterface database;
        String dbName = "EventDirector.sqlite";
        ParticipantsListWindow partList = null;
        bool closing = false;

        Thread tcpServerThread;
        TCPServer tcpServer;

        Thread zeroConfThread;
        ZeroConf zeroConf = new ZeroConf();

        List<Window> windows = new List<Window>();

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
            Log.D("Starting TCP server thread.");
            tcpServer = new TCPServer(database, this);
            tcpServerThread = new Thread(new ThreadStart(tcpServer.Run));
            tcpServerThread.Start();
            Log.D("Starting zero configuration thread.");
            zeroConfThread = new Thread(new ThreadStart(zeroConf.Run));
            zeroConfThread.Start();
            InitialUpdateChangesBox();
        }

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            int menuId = Convert.ToInt32(((MenuItem)sender).Uid);
            switch (menuId)
            {
                case 1:     // Connection Settings
                    Log.D("Settings");
                    break;
                case 2:
                    Log.D("Rebuild Database.");
                    await Task.Run(() =>
                    {
                        ((SQLiteInterface)database).HardResetDatabase();
                    });
                    Log.D("Database Rebuilt.");
                    UpdateEventBox();
                    InitialUpdateChangesBox();
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
                    OpenFileDialog dialog = new OpenFileDialog() { Filter = "CSV Files (*.csv)|*.csv|All files|*" };
                    if (dialog.ShowDialog() == true)
                    {
                        try
                        {
                            CSVImporter importer = new CSVImporter(dialog.FileName);
                            importer.FetchHeaders();
                            ImportFileWindow win = new ImportFileWindow(this, importer, database);
                            win.Show();
                        }
                        catch (Exception ex)
                        {
                            Log.E("Something went wrong when trying to read the CSV file.");
                            Log.E(ex.StackTrace);
                        }
                    }
                    break;
                case 6:     // Assign bibs/chips
                    Log.D("Assign");
                    break;
                case 7:     // List Participants
                    Log.D("List Participants");
                    partList = new ParticipantsListWindow(database, this);
                    partList.Show();
                    break;
                default:
                    break;
            }
        }

        internal async void UpdateTimingPoint(int timingPointIdentifier, string nameString, string distanceStr, string unitString, int divisionId)
        {
            int eventId = ((Event)eventsListView.SelectedItem).Identifier;
            await Task.Run(() =>
            {
                database.UpdateTimingPoint(new TimingPoint(timingPointIdentifier, eventId, divisionId, nameString, distanceStr, unitString));
            });
            UpdateTimingPointsBox(eventId);
        }

        internal async void UpdateEvent(int eventIdentifier, string nameString, long dateVal)
        {
            await Task.Run(() =>
            {
                database.UpdateEvent(new Event(eventIdentifier, nameString, dateVal));
            });
            UpdateEventBox();
        }

        internal async void UpdateDivision(int eventId, int divisionIdentifier, string nameString)
        {
            await Task.Run(() =>
            {
                database.UpdateDivision(new Division(divisionIdentifier, nameString, eventId));
            });
            UpdateDivisionsBox(eventId);
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
                Event thisEvent = (Event)eventsListView.SelectedItem;
                if (thisEvent != null)
                {
                    NewEventWindow win = new NewEventWindow(this, thisEvent.Identifier, thisEvent.Name, thisEvent.Date);
                    windows.Add(win);
                    win.Show();
                }
            }
            else if (buttonName == "divisionsModifyButton")
            {
                Log.D("Divisions - Modify Button Pressed.");
                Division div = (Division)divisionsListView.SelectedItem;
                Event thisEvent = (Event)eventsListView.SelectedItem;
                if (div != null)
                {
                    NewDivisionWindow win = new NewDivisionWindow(this, thisEvent.Identifier, div.Identifier, div.Name);
                    windows.Add(win);
                    win.Show();
                }
            }
            else if (buttonName == "timingPointsModifyButton")
            {
                Log.D("TimingPoints - Modify Button Pressed.");
                TimingPoint tp = (TimingPoint)timingPointsListView.SelectedItem;
                List<Division> divisions = database.GetDivisions(((Event)eventsListView.SelectedItem).Identifier);
                if (divisions != null && divisions.Count > 0 && tp != null)
                {
                    NewTimingPointWindow win = new NewTimingPointWindow(this, divisions, tp.Identifier, tp.Name, tp.Distance, tp.Unit);
                    windows.Add(win);
                    win.Show();
                }
                else
                {
                    MessageBox.Show("No divisions found. Please add divisions before adding timing points.");
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            String buttonName = ((Button)sender).Name;
            if (buttonName == "eventsAddButton")
            {
                Log.D("Events - Add Button Pressed.");
                NewEventWindow win = new NewEventWindow(this);
                windows.Add(win);
                win.Show();
            }
            else if (buttonName == "divisionsAddButton")
            {
                Log.D("Divisions - Add Button Pressed.");
                NewDivisionWindow win = new NewDivisionWindow(this, ((Event)eventsListView.SelectedItem).Identifier);
                windows.Add(win);
                win.Show();
            }
            else if (buttonName == "timingPointsAddButton")
            {
                Log.D("TimingPoints - Add Button Pressed.");
                List<Division> divisions = database.GetDivisions(((Event)eventsListView.SelectedItem).Identifier);
                if (divisions != null && divisions.Count > 0)
                {
                    NewTimingPointWindow win = new NewTimingPointWindow(this, divisions);
                    windows.Add(win);
                    win.Show();
                }
                else
                {
                    MessageBox.Show("No divisions found. Please add divisions before adding timing points.");
                }
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

        internal async void AddTimingPoint(string nameString, string distanceStr, string unitString, int divisionId)
        {
            int eventId = ((Event)eventsListView.SelectedItem).Identifier;
            await Task.Run(() =>
            {
                database.AddTimingPoint(new TimingPoint(eventId, divisionId, nameString, distanceStr, unitString));
            });
            UpdateTimingPointsBox(eventId);
        }

        internal async void AddDivision(int eventId, string nameString)
        {
            await Task.Run(() =>
            {
                database.AddDivision(new Division(nameString, eventId));
            });
            UpdateDivisionsBox(eventId);
        }

        internal async void UpdateEventBox()
        {
            List<Event> events = null;
            await Task.Run(() =>
            {
                events = database.GetEvents();
            });
            eventsListView.ItemsSource = events;
            if (partList != null)
            {
                partList.UpdateEventsBox();
            }
        }

        public void UpdateChangesBox()
        {
            Log.D("Updating changes box.");
            List<Change> changes = database.GetChanges();
            List<ChangeParticipant> changeParts = new List<ChangeParticipant>();
            foreach (Change c in changes)
            {
                if (c.OldParticipant != null)
                {
                    changeParts.Add(new ChangeParticipant(c.Identifier, "Old", c.OldParticipant));
                }
                if (c.NewParticipant != null)
                {
                    changeParts.Add(new ChangeParticipant(c.Identifier, "New", c.NewParticipant));
                }
            }
            changeParts.Sort();
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                updateListView.ItemsSource = changeParts;
            }));
        }

        internal async void InitialUpdateChangesBox()
        {
            await Task.Run(() =>
            {
                UpdateChangesBox();
            });
        }

        private async void UpdateTimingPointsBox(int eventId)
        {
            Log.D("Updating timing points box.");
            if (timingPointsListView.Visibility == Visibility.Hidden)
            {
                return;
            }
            List<TimingPoint> timingPoints = null;
            await Task.Run(() =>
            {
                timingPoints = database.GetTimingPoints(eventId);
            });
            timingPointsListView.ItemsSource = timingPoints;
        }

        private async void UpdateDivisionsBox(int eventId)
        {
            Log.D("Updating divisions box.");
            if (divisionsListView.Visibility == Visibility.Hidden)
            {
                return;
            }
            List<Division> divisions = null;
            await Task.Run(() =>
            {
                divisions = database.GetDivisions(eventId);
            });
            divisionsListView.ItemsSource = divisions;
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
                eventsToggleEarlyStartButton.Visibility = Visibility.Visible;
                eventsToggleRegistrationButton.Visibility = Visibility.Visible;
                eventsToggleResultsButton.Visibility = Visibility.Visible;
                divisionsAddButton.Visibility = Visibility.Visible;
                divisionsListView.Visibility = Visibility.Visible;
                timingPointsAddButton.Visibility = Visibility.Visible;
                timingPointsListView.Visibility = Visibility.Visible;
                UpdateDivisionsBox(anEvent.Identifier);
                UpdateTimingPointsBox(anEvent.Identifier);
                List<JsonOption> options = database.GetEventOptions(anEvent.Identifier);
                foreach (JsonOption opt in options)
                {
                    if (opt.Name == "results_open")
                    {
                        if (opt.Value == "true")
                        {
                            eventsToggleResultsButton.Opacity = 1;
                        }
                        else
                        {
                            eventsToggleResultsButton.Opacity = 0.5;
                        }
                    }
                    else if (opt.Name == "allow_early_start")
                    {
                        if (opt.Value == "true")
                        {
                            eventsToggleEarlyStartButton.Opacity = 1;
                        }
                        else
                        {
                            eventsToggleEarlyStartButton.Opacity = 0.5;
                        }
                    }
                    else if (opt.Name == "registration_open")
                    {
                        if (opt.Value == "true")
                        {
                            eventsToggleRegistrationButton.Opacity = 1;
                        }
                        else
                        {
                            eventsToggleRegistrationButton.Opacity = 0.5;
                        }
                    }
                }
            }
            else
            {
                eventsModifyButton.Visibility = Visibility.Hidden;
                eventsRemoveButton.Visibility = Visibility.Hidden;
                eventsToggleEarlyStartButton.Visibility = Visibility.Hidden;
                eventsToggleRegistrationButton.Visibility = Visibility.Hidden;
                eventsToggleResultsButton.Visibility = Visibility.Hidden;
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
            if (divisionsListView.SelectedIndex < 0)
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

        public void PartListClosed()
        {
            Log.D("Participants list has closed.");
            partList = null;
        }

        public void WindowClosed(Window window)
        {
            if (closing == false)
            {
                windows.Remove(window);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true;
            if (partList != null) partList.Close();
            foreach (Window w in windows)
            {
                w.Close();
            }
            tcpServer.Stop();
            tcpServerThread.Abort();
            zeroConf.Stop();
            zeroConfThread.Abort();
        }

        private void EventsToggleRegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            bool value = false;
            if (eventsToggleRegistrationButton.Opacity < 1)
            {
                value = true;
                eventsToggleRegistrationButton.Opacity = 1;
            }
            else
            {
                eventsToggleRegistrationButton.Opacity = 0.5;
            }
            int eventId = ((Event)eventsListView.SelectedItem).Identifier;
            List<JsonOption> list = database.GetEventOptions(eventId);
            foreach (JsonOption opt in list)
            {
                if (opt.Name == "registration_open")
                {
                    opt.Value = value.ToString().ToLower();
                }
            }
            database.SetEventOptions(eventId, list);
            tcpServer.UpdateEvent(eventId);
        }

        private void EventsToggleResultsButton_Click(object sender, RoutedEventArgs e)
        {
            bool value = false;
            if (eventsToggleResultsButton.Opacity < 1)
            {
                value = true;
                eventsToggleResultsButton.Opacity = 1;
            }
            else
            {
                eventsToggleResultsButton.Opacity = 0.5;
            }
            int eventId = ((Event)eventsListView.SelectedItem).Identifier;
            List<JsonOption> list = database.GetEventOptions(eventId);
            foreach (JsonOption opt in list)
            {
                if (opt.Name == "results_open")
                {
                    opt.Value = value.ToString().ToLower();
                }
            }
            database.SetEventOptions(eventId, list);
            tcpServer.UpdateEvent(eventId);
        }

        private void EventsToggleEarlyStartButton_Click(object sender, RoutedEventArgs e)
        {
            bool value = false;
            if (eventsToggleEarlyStartButton.Opacity < 1)
            {
                value = true;
                eventsToggleEarlyStartButton.Opacity = 1;
            }
            else
            {
                eventsToggleEarlyStartButton.Opacity = 0.5;
            }
            int eventId = ((Event)eventsListView.SelectedItem).Identifier;
            List<JsonOption> list = database.GetEventOptions(eventId);
            foreach (JsonOption opt in list)
            {
                if (opt.Name == "allow_early_start")
                {
                    opt.Value = value.ToString().ToLower();
                }
            }
            database.SetEventOptions(eventId, list);
            tcpServer.UpdateEvent(eventId);
        }
    }
}
