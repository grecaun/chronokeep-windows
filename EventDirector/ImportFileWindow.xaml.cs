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
        IDataImporter importer;
        MainWindow mainWindow;
        IDBInterface database;
        Boolean init = true;
        internal static string[] human_fields = new string[] {
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
            "Shirt Size",
            "Owes",
            "Comments",
            "Second Shirt",
            "Hat",
            "Other",
            "Division",
            "Emergency Contact Name",
            "Emergency Contact Phone",
            "Emergency Contact Email",
            "Fleece"
        };
        ImportFilePage1 page1 = null;
        ImportFilePage2 page2 = null;
        int[] keys;

        public ImportFileWindow(MainWindow mainWindow, IDataImporter importer, IDBInterface database)
        {
            InitializeComponent();
            this.importer = importer;
            this.mainWindow = mainWindow;
            this.database = database;
            date.SelectedDate = DateTime.Today;
            eventLabel.Text = importer.Data.FileName;
            shirtPriceBox.Text = "20.00";
            if (importer.Data.Type == ImportData.FileType.EXCEL)
            {
                SheetsLabel.Visibility = Visibility.Visible;
                SheetsBox.Visibility = Visibility.Visible;
                eventLabel.Width = 315;
                SheetsBox.ItemsSource = ((ExcelImporter)importer).SheetNames;
                SheetsBox.SelectedIndex = 0;
                init = false;
            }
            page1 = new ImportFilePage1(importer);
            Frame.Content = page1;
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Import - Done button clicked.");
            if (page1 != null)
            {
                List<String> repeats = page1.RepeatHeaders();
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
                    StartImport(page1.GetListBoxItems());
                }
            }
            else if (page2 != null)
            {
                ImportWork(page2.GetDivisions());
            }
            else
            {
                Log.D("Abort! Abort! Something went terribly wrong.");
            }
        }

        private void StartImport(HListBoxItem[] headerListBoxItems)
        {
            importer.FetchData();
            keys = new int[human_fields.Length + 1];
            foreach (HListBoxItem item in headerListBoxItems)
            {
                Log.D("Header is " + item.HeaderLabel.Content);
                if (item.HeaderBox.SelectedIndex != 0)
                {
                    keys[item.HeaderBox.SelectedIndex] = item.Index;
                }
            }
            ImportData data = importer.Data;
            string[] divisions = data.GetDivisionNames(keys[22]);
            Log.D("Division key is " + keys[22] + " with a header name of " + headerListBoxItems[keys[22]-1].HeaderLabel.Content + " number of divisions found is " + divisions.Length);
            if (divisions.Length <= 0)
            {
                MessageBox.Show("No divisions found in file, or nothing selected for divisions/distance.  Please correct this or cancel.");
                return;
            }
            StringBuilder sb = new StringBuilder("Division names are");
            foreach (String s in divisions)
            {
                sb.Append(" '" + s + "'");
            }
            Log.D(sb.ToString());
            page1 = null;
            page2 = new ImportFilePage2(divisions);
            SheetsLabel.Visibility = Visibility.Collapsed;
            SheetsBox.Visibility = Visibility.Collapsed;
            eventLabel.Width = 460;
            Done.Content = "Done";
            Frame.Content = page2;
        }

        private async void ImportWork(List<Division> divisions)
        {
            Log.D("Starting the import.");
            // Keys is an array of integers representing which field in the row of incoming data
            // represents a specific field in the database.  These fields are defined by the array
            // from which the user can select.
            string[] shirtVals = shirtPriceBox.Text.Split('.');
            int shirtPrice = 20;
            if (shirtVals.Length > 0)
            {
                int.TryParse(shirtVals[0].Trim(), out shirtPrice);
            }
            shirtPrice = shirtPrice * 100;
            int cents = 0;
            if (shirtVals.Length > 1)
            {
                int.TryParse(shirtVals[1].Trim(), out cents);
            }
            while (cents > 100)
            {
                cents = cents / 100;
            }
            shirtPrice += cents;
            int shirtOption = shirtOptionalBox.IsChecked == true ? 1 : 0;
            Event anEvent = new Event(eventLabel.Text.Trim(), date.SelectedDate.Value.Ticks, shirtOption, shirtPrice);
            bool valid = true;
            await Task.Run(() =>
            {
                ImportData data = importer.Data;
                database.AddEvent(anEvent);
                anEvent.Identifier = database.GetEventID(anEvent);
                Hashtable divHash = new Hashtable(500);
                foreach (Division div in divisions)
                {
                    div.EventIdentifier = anEvent.Identifier;
                    database.AddDivision(div);
                    div.Identifier = database.GetDivisionID(div);
                    divHash.Add(div.Name, div);
                    Log.D("Div name is " + div.Name);
                }
                int numEntries = data.Data.Count;
                List<Participant> participants = new List<Participant>();
                for (int counter = 0; counter < numEntries; counter++)
                {
                    if (data.Data[counter][keys[22]] != null && data.Data[counter][keys[22]].Length > 0)
                    {
                        Log.D("Looking for... " + Utils.UppercaseFirst(data.Data[counter][keys[22]].ToLower()));
                        Division thisDiv = (Division)divHash[Utils.UppercaseFirst(data.Data[counter][keys[22]].Trim().ToLower())];
                        participants.Add(new Participant(
                            data.Data[counter][keys[1]], // First Name
                            data.Data[counter][keys[2]], // Last Name
                            data.Data[counter][keys[5]], // Street
                            data.Data[counter][keys[7]], // City
                            data.Data[counter][keys[8]], // State
                            data.Data[counter][keys[9]], // Zip
                            data.Data[counter][keys[4]], // Birthday
                            new EmergencyContact(
                                data.Data[counter][keys[23]], // Name
                                data.Data[counter][keys[24]], // Phone
                                data.Data[counter][keys[25]]  // Email
                                ),
                            new EventSpecific(
                                anEvent.Identifier,
                                thisDiv.Identifier,
                                thisDiv.Name,
                                data.Data[counter][keys[15]], // Bib number
                                0,                            // checked in
                                data.Data[counter][keys[16]], // shirt size
                                data.Data[counter][keys[18]], // comments
                                data.Data[counter][keys[19]], // second shirt
                                data.Data[counter][keys[17]], // owes
                                data.Data[counter][keys[20]], // hat
                                data.Data[counter][keys[21]], // other
                                0,                            // early start
                                data.Data[counter][keys[26]], // fleece
                                0                             // used next year registration option
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
                }
                database.AddParticipants(participants);
            });
            if (valid)
            {
                Log.D("All done with the import.");
                mainWindow.UpdateEventBox();
                this.Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (page1 != null) { importer.Finish(); }
            Log.D("Import - Cancel button clicked.");
            this.Close();
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
            else if (String.Equals(s, "Birthday", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Birthdate", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "DOB", StringComparison.OrdinalIgnoreCase))
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
            else if (s.IndexOf("Bib", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 15;
            }
            else if (String.Equals(s, "Shirt Size", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Shirt", StringComparison.OrdinalIgnoreCase))
            {
                return 16;
            }
            else if (String.Equals(s, "Owes", StringComparison.OrdinalIgnoreCase))
            {
                return 17;
            }
            else if (String.Equals(s, "Comments", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Notes", StringComparison.OrdinalIgnoreCase))
            {
                return 18;
            }
            else if (String.Equals(s, "Second Shirt", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "2nd Shirt", StringComparison.OrdinalIgnoreCase))
            {
                return 19;
            }
            else if (String.Equals(s, "Hat", StringComparison.OrdinalIgnoreCase))
            {
                return 20;
            }
            else if (String.Equals(s, "Other", StringComparison.OrdinalIgnoreCase))
            {
                return 21;
            }
            else if (s.IndexOf("Division", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Distance", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 22;
            }
            else if ((s.IndexOf("emergency", StringComparison.OrdinalIgnoreCase) >= 0 && s.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0) || String.Equals(s, "Emergency", StringComparison.OrdinalIgnoreCase))
            {
                return 23;
            }
            else if (s.IndexOf("emergency", StringComparison.OrdinalIgnoreCase) >= 0 && (s.IndexOf("phone", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("cell", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return 24;
            }
            else if (s.IndexOf("emergency", StringComparison.OrdinalIgnoreCase) >= 0 && s.IndexOf("email", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 25;
            }
            else if (String.Equals(s, "Fleece", StringComparison.OrdinalIgnoreCase))
            {
                return 26;
            }
            return 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWindow.WindowClosed(this);
        }

        private void SheetsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (init) { return; }
            int selection = ((ComboBox)sender).SelectedIndex;
            Log.D("You've selected number " + selection);
            if (page1 != null)
            {
                page1.UpdateSheetNo(selection);
            }
        }
    }
}
