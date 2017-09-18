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
            UpdateAllBoxes();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            int menuId = Convert.ToInt32(((MenuItem)sender).Uid);
            switch (menuId)
            {
                case 1:     // Connection Settings
                    Log.D("Connection Settings");
                    break;
                case 2:     // Race Director Settings
                    Log.D("Race Director Settings");
                    break;
                case 3:     // Clear Database
                    Log.D("Clear Database");
                    break;
                case 4:     // Exit
                    Log.D("Goodbye");
                    break;
                case 5:     // Import participants
                    Log.D("Import");
                    break;
                case 6:     // Assign bibs/chips
                    Log.D("Assign");
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
            } else if (buttonName == "divisionsRemoveButton")
            {
            } else if (buttonName == "timingPointsRemoveButton")
            {
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            String buttonName = ((Button)sender).Name;
            if (buttonName == "eventsAddButton")
            {
                NewEventWindow eventWindow = new NewEventWindow(this);
                eventWindow.Show();
            }
            else if (buttonName == "divisionsAddButton")
            {
                NewDivisionWindow divisionWindow = new NewDivisionWindow(this);
                divisionWindow.Show();
            }
            else if (buttonName == "timingPointsAddButton")
            {
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
            await Task.Run(() =>
            {
                database.AddTimingPoint(new TimingPoint(eventId, nameString, distanceStr, unitString));
            });
            UpdateTimingPointBox();
        }

        internal async void AddDivision(string nameString)
        {
            int eventId = ((Event)eventsListView.SelectedItem).Identifier;
            await Task.Run(() =>
            {
                database.AddDivision(new Division(nameString, eventId));
            });
            UpdateDivisionBox();
        }

        private async void UpdateEventBox()
        {
            ArrayList events = null;
            await Task.Run(() =>
            {
                events = database.GetEvents();
            });
            foreach (Event e in events)
            {
                eventsListView.Items.Add(e);
            }
        }

        private void UpdateTimingPointBox()
        {

        }

        private void UpdateDivisionBox()
        {

        }

        private void UpdateChangesBox()
        {

        }

        private void UpdateAllBoxes()
        {
            UpdateEventBox();
            UpdateTimingPointBox();
            UpdateDivisionBox();
            UpdateChangesBox();
        }
    }
}
