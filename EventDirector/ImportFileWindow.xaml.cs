using System;
using System.Collections;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for ImportFileWindow.xaml
    /// </summary>
    public partial class ImportFileWindow : Window
    {
        CSVImporter importer;
        MainWindow mainWindow;
        IDBInterface database;
        static string[] human_fields = new string[] {
            "",
            "First Name",
            "Last Name",
            "Gender",
            "Birthday",
            "Street",
            "Street 2",
            "City",
            "State",
            "Zip",
            "Country",
            "Phone",
            "Email",
            "Mobile",
            "Parent",
            "Bib",
            "Chip",
            "Shirt Size",
            "Owes",
            "Comments",
            "Second Shirt",
            "Hat",
            "Other",
            "Division",
            "Emergency Contact Name",
            "Emergency Contact Phone",
            "Emergency Contact Email"
        };

        public ImportFileWindow(MainWindow mainWindow, CSVImporter importer, IDBInterface database)
        {
            InitializeComponent();
            this.importer = importer;
            this.mainWindow = mainWindow;
            this.database = database;
            date.SelectedDate = DateTime.Today;
            eventLabel.Content = importer.Data.FileName;
            for (int i = 1; i < importer.Data.GetNumHeaders(); i++)
            {
                headerListBox.Items.Add(new AListBoxItem(importer.Data.Headers[i], i));
            }
        }

        internal static int GetHeaderBoxIndex(string s)
        {
            Log.D("Looking for a value for: " + s);
            if (String.Equals(s, "First Name", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "First", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }
            else if (String.Equals(s, "Last Name", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Last", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }
            else if (String.Equals(s, "Gender", StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }
            else if (String.Equals(s, "Birthday", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Birthdate", StringComparison.OrdinalIgnoreCase))
            {
                return 4;
            }
            else if (String.Equals(s, "Street", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Address", StringComparison.OrdinalIgnoreCase))
            {
                return 5;
            }
            else if (String.Equals(s, "Street 2", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Address 2", StringComparison.OrdinalIgnoreCase))
            {
                return 6;
            }
            else if (String.Equals(s, "City", StringComparison.OrdinalIgnoreCase))
            {
                return 7;
            }
            else if (String.Equals(s, "State", StringComparison.OrdinalIgnoreCase))
            {
                return 8;
            }
            else if (String.Equals(s, "Zip", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Zip Code", StringComparison.OrdinalIgnoreCase))
            {
                return 9;
            }
            else if (String.Equals(s, "Country", StringComparison.OrdinalIgnoreCase))
            {
                return 10;
            }
            else if (String.Equals(s, "Phone", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Phone Number", StringComparison.OrdinalIgnoreCase))
            {
                return 11;
            }
            else if (String.Equals(s, "Email", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Email Address", StringComparison.OrdinalIgnoreCase))
            {
                return 12;
            }
            else if (String.Equals(s, "Mobile", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Mobile Phone Number", StringComparison.OrdinalIgnoreCase))
            {
                return 13;
            }
            else if (String.Equals(s, "Parent", StringComparison.OrdinalIgnoreCase))
            {
                return 14;
            }
            else if (String.Equals(s, "Bib", StringComparison.OrdinalIgnoreCase))
            {
                return 15;
            }
            else if (String.Equals(s, "Chip", StringComparison.OrdinalIgnoreCase))
            {
                return 16;
            }
            else if (String.Equals(s, "Shirt Size", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Shirt", StringComparison.OrdinalIgnoreCase))
            {
                return 17;
            }
            else if (String.Equals(s, "Owes", StringComparison.OrdinalIgnoreCase))
            {
                return 18;
            }
            else if (String.Equals(s, "Comments", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Notes", StringComparison.OrdinalIgnoreCase))
            {
                return 19;
            }
            else if (String.Equals(s, "Second Shirt", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "2nd Shirt", StringComparison.OrdinalIgnoreCase))
            {
                return 20;
            }
            else if (String.Equals(s, "Hat", StringComparison.OrdinalIgnoreCase))
            {
                return 21;
            }
            else if (String.Equals(s, "Other", StringComparison.OrdinalIgnoreCase))
            {
                return 22;
            }
            else if (String.Equals(s, "Division", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Distance", StringComparison.OrdinalIgnoreCase))
            {
                return 23;
            }
            else if (s.Contains("emergency") && s.Contains("name"))
            {
                return 24;
            }
            else if (s.Contains("emergency") && (s.Contains("phone") || s.Contains("cell")))
            {
                return 25;
            }
            else if (s.Contains("emergency") && s.Contains("email"))
            {
                return 26;
            }
            return 0;
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Import - Done button clicked.");
            List<String> repeats = RepeatHeaders();
            if (repeats != null)
            {
                StringBuilder sb = new StringBuilder("Repeats for the following headers were found:");
                foreach (string s in repeats)
                {
                    sb.Append(" " + s);
                }
                MessageBox.Show(sb.ToString());
            }
            else
            {
                Log.D("No repeat headers found.");
                ImportWork();
                this.Close(); //*/
            }
        }

        private async void ImportWork()
        {
            Log.D("Starting the import.");
            // Keys is an array of integers representing which field in the row of incoming data
            // represents a specific field in the database.  These fields are defined by the array
            // from which the user can select.
            int[] keys = new int[human_fields.Length + 1];
            foreach (AListBoxItem item in headerListBox.Items)
            {
                if (item.HeaderBox.SelectedIndex != 0)
                {
                    keys[item.HeaderBox.SelectedIndex] = item.Index;
                }
            }
            Event anEvent = new Event(importer.Data.FileName, date.SelectedDate.Value.Ticks);
            await Task.Run(() =>
            {
                importer.FetchData();
                ImportData data = importer.Data;
                string[] divisions = data.GetDivisionNames(keys[23]);
                StringBuilder sb = new StringBuilder("Division names are");
                foreach (String s in divisions)
                {
                    sb.Append(" '" + s + "'");
                }
                Log.D(sb.ToString());
                database.AddEvent(anEvent);
                anEvent.Identifier = database.GetEventID(anEvent);
                Hashtable divHash = new Hashtable(500);
                foreach (string divName in divisions)
                {
                    Division newDiv = new Division(divName, anEvent.Identifier);
                    database.AddDivision(newDiv);
                    newDiv.Identifier = database.GetDivisionID(newDiv);
                    divHash.Add(divName, newDiv);
                }
                int numEntries = data.Data.Count;
                List<Participant> participants = new List<Participant>();
                for (int counter = 0; counter < numEntries; counter++)
                {
                    Division thisDiv = (Division)divHash[data.Data[counter][keys[23]].ToLower()];
                    participants.Add(new Participant(
                        data.Data[counter][keys[1]], // First Name
                        data.Data[counter][keys[2]], // Last Name
                        data.Data[counter][keys[5]], // Street
                        data.Data[counter][keys[7]], // City
                        data.Data[counter][keys[8]], // State
                        data.Data[counter][keys[9]], // Zip
                        data.Data[counter][keys[4]], // Birthday
                        new EmergencyContact(
                            data.Data[counter][keys[24]], // Name
                            data.Data[counter][keys[25]], // Phone
                            data.Data[counter][keys[26]]  // Email
                            ),
                        new EventSpecific(
                            anEvent.Identifier,
                            thisDiv.Identifier,
                            thisDiv.Name,
                            data.Data[counter][keys[15]], // Bib number
                            data.Data[counter][keys[16]], // chip
                            0,                            // checked in
                            data.Data[counter][keys[17]], // shirt size
                            data.Data[counter][keys[19]], // comments
                            data.Data[counter][keys[20]], // second shirt
                            data.Data[counter][keys[18]], // owes
                            data.Data[counter][keys[21]], // hat
                            data.Data[counter][keys[22]], // other
                            0                             // early start
                            ),
                        data.Data[counter][keys[11]], // phone
                        data.Data[counter][keys[12]], // email
                        data.Data[counter][keys[13]], // mobile
                        data.Data[counter][keys[14]], // parent
                        data.Data[counter][keys[10]], // country
                        data.Data[counter][keys[6]], // street2
                        data.Data[counter][keys[3]] // gender
                        ));
                }
                database.AddParticipants(participants);
            });
            Log.D("All done with the import.");
            mainWindow.UpdateEventBox();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Import - Cancel button clicked.");
            this.Close();
        }

        private List<String> RepeatHeaders()
        {
            Log.D("Checking for repeat headers in user selection.");
            int[] check = new int[human_fields.Length];
            bool repeat = false;
            List<String> output = new List<String>();
            foreach (ListBoxItem item in headerListBox.Items)
            {
                int val = ((AListBoxItem)item).HeaderBox.SelectedIndex;
                if (val > 0)
                {
                    if (check[val] > 0)
                    {
                        output.Add(((AListBoxItem)item).HeaderBox.SelectedItem.ToString());
                        repeat = true;
                    }
                    else
                    {
                        check[val] = 1;
                    }
                }
            }
            return repeat == true ? output : null;
        }

        private class AListBoxItem : ListBoxItem
        {
            public Label HeaderLabel { get; private set; }
            public ComboBox HeaderBox { get; private set; }
            public int Index { get; private set; }

            public AListBoxItem(String s, int ix)
            {
                Index = ix;
                Grid theGrid = new Grid();
                this.Content = theGrid;
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                HeaderLabel = new Label
                {
                    Content = s,
                };
                theGrid.Children.Add(HeaderLabel);
                Grid.SetColumn(HeaderLabel, 0);
                HeaderBox = new ComboBox
                {
                    ItemsSource = human_fields,
                    SelectedIndex = GetHeaderBoxIndex(s.ToLower()),
                };
                theGrid.Children.Add(HeaderBox);
                Grid.SetColumn(HeaderBox, 1);
            }
        }
    }
}
