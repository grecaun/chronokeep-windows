using System;
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
                NewEventWindow eventWindow = new NewEventWindow();
                eventWindow.Show();
            }
            else if (buttonName == "divisionsAddButton")
            {
                NewDivisionWindow divisionWindow = new NewDivisionWindow();
                divisionWindow.Show();
            }
            else if (buttonName == "timingPointsAddButton")
            {
                NewTimingPointWindow timingPointWindow = new NewTimingPointWindow();
                timingPointWindow.Show();
            }
        }
    }
}
