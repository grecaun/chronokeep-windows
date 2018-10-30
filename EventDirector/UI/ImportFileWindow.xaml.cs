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
            "Email",
            "Mobile",
            "Parent",
            "Bib",
            "Owes",
            "Comments",
            "Other",
            "Division",
            "Emergency Contact Name",
            "Emergency Contact Phone",
            "Age",
            "Apparel"
        };
        private static readonly int FIRST = 1;
        private static readonly int LAST = 2;
        private static readonly int GENDER = 3;
        private static readonly int BIRTHDAY = 4;
        private static readonly int STREET = 5;
        private static readonly int STREET2 = 6;
        private static readonly int CITY = 7;
        private static readonly int STATE = 8;
        private static readonly int ZIP = 9;
        private static readonly int COUNTRY = 10;
        private static readonly int EMAIL = 11;
        private static readonly int MOBILE = 12;
        private static readonly int PARENT = 13;
        private static readonly int BIB = 14;
        private static readonly int OWES = 15;
        private static readonly int COMMENTS = 16;
        private static readonly int OTHER = 17;
        private static readonly int DIVISION = 18;
        private static readonly int EMERGENCYNAME = 19;
        private static readonly int EMERGENCYPHONE = 20;
        private static readonly int AGE = 21;
        public static readonly int APPARELITEM = 22;
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
            for (int i = 0; i< keys.Length; i++)
            {
                keys[i] = 0;
            }
            foreach (HListBoxItem item in headerListBoxItems)
            {
                Log.D("Header is " + item.HeaderLabel.Content);
                if (item.HeaderBox.SelectedIndex != 0)
                {
                    keys[item.HeaderBox.SelectedIndex] = item.Index;
                }
            }
            ImportData data = importer.Data;
            string[] divisions = data.GetDivisionNames(keys[DIVISION]);
            Log.D("Division key is " + keys[DIVISION] + " with a header name of " + headerListBoxItems[keys[DIVISION]-1].HeaderLabel.Content + " number of divisions found is " + divisions.Length);
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
            int thisYear = date.SelectedDate.Value.Year;
            date.Visibility = Visibility.Hidden;
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
                    if (data.Data[counter][keys[DIVISION]] != null && data.Data[counter][keys[DIVISION]].Length > 0)
                    {
                        Log.D("Looking for... " + Utils.UppercaseFirst(data.Data[counter][keys[DIVISION]].ToLower()));
                        Division thisDiv = (Division)divHash[Utils.UppercaseFirst(data.Data[counter][keys[DIVISION]].Trim().ToLower())];
                        string birthday = "01/01/1900";
                        if (keys[BIRTHDAY] == 0 && keys[AGE] != 0) // birthday not set but age is
                        {
                            Log.D(String.Format("Counter is {0} and keys[AGE] is {1}", counter, keys[AGE]));
                            Log.D("Age of participant is " + data.Data[counter][keys[AGE]]);
                            int age = Convert.ToInt32(data.Data[counter][keys[AGE]]);
                            birthday = String.Format("01/01/{0,4}", thisYear - age);
                        } else if (keys[BIRTHDAY] != 0)
                        {
                            birthday = data.Data[counter][keys[BIRTHDAY]]; // birthday
                        }
                        participants.Add(new Participant(
                            data.Data[counter][keys[FIRST]], // First Name
                            data.Data[counter][keys[LAST]], // Last Name
                            data.Data[counter][keys[STREET]], // Street
                            data.Data[counter][keys[CITY]], // City
                            data.Data[counter][keys[STATE]], // State
                            data.Data[counter][keys[ZIP]], // Zip
                            birthday, // Birthday
                            new EventSpecific(
                                anEvent.Identifier,
                                thisDiv.Identifier,
                                thisDiv.Name,
                                data.Data[counter][keys[BIB]], // Bib number
                                0,                            // checked in
                                data.Data[counter][keys[COMMENTS]], // comments
                                data.Data[counter][keys[OWES]], // owes
                                data.Data[counter][keys[OTHER]], // other
                                0,                            // early start
                                0                             // used next year registration option
                                ),
                            data.Data[counter][keys[EMAIL]], // email
                            data.Data[counter][keys[MOBILE]], // mobile
                            data.Data[counter][keys[PARENT]], // parent
                            data.Data[counter][keys[COUNTRY]], // country
                            data.Data[counter][keys[STREET2]],  // street2
                            data.Data[counter][keys[GENDER]],  // gender
                            data.Data[counter][keys[EMERGENCYNAME]], // Emergency Name
                            data.Data[counter][keys[EMERGENCYPHONE]]  // Emergency Phone
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
                return FIRST;
            }
            else if (String.Equals(s, "Last Name", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Last", StringComparison.OrdinalIgnoreCase))
            {
                return LAST;
            }
            else if (String.Equals(s, "Gender", StringComparison.OrdinalIgnoreCase))
            {
                return GENDER;
            }
            else if (String.Equals(s, "Birthday", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Birthdate", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "DOB", StringComparison.OrdinalIgnoreCase))
            {
                return BIRTHDAY;
            }
            else if (String.Equals(s, "Street", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Address", StringComparison.OrdinalIgnoreCase))
            {
                return STREET;
            }
            else if (String.Equals(s, "Street 2", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Address 2", StringComparison.OrdinalIgnoreCase))
            {
                return STREET2;
            }
            else if (String.Equals(s, "City", StringComparison.OrdinalIgnoreCase))
            {
                return CITY;
            }
            else if (String.Equals(s, "State", StringComparison.OrdinalIgnoreCase))
            {
                return STATE;
            }
            else if (String.Equals(s, "Zip", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Zip Code", StringComparison.OrdinalIgnoreCase))
            {
                return ZIP;
            }
            else if (String.Equals(s, "Country", StringComparison.OrdinalIgnoreCase))
            {
                return COUNTRY;
            }
            else if (String.Equals(s, "Phone", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Phone Number", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Mobile", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Mobile Phone Number", StringComparison.OrdinalIgnoreCase))
            {
                return MOBILE;
            }
            else if (String.Equals(s, "Email", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Email Address", StringComparison.OrdinalIgnoreCase))
            {
                return EMAIL;
            }
            else if (String.Equals(s, "Parent", StringComparison.OrdinalIgnoreCase))
            {
                return PARENT;
            }
            else if (s.IndexOf("Bib", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return BIB;
            }
            else if (String.Equals(s, "Shirt Size", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Shirt", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "T-Shirt", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "TShirt", StringComparison.OrdinalIgnoreCase))
            {
                return APPARELITEM;
            }
            else if (String.Equals(s, "Owes", StringComparison.OrdinalIgnoreCase))
            {
                return OWES;
            }
            else if (String.Equals(s, "Comments", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "Notes", StringComparison.OrdinalIgnoreCase))
            {
                return COMMENTS;
            }
            else if (String.Equals(s, "Second Shirt", StringComparison.OrdinalIgnoreCase) || String.Equals(s, "2nd Shirt", StringComparison.OrdinalIgnoreCase))
            {
                return APPARELITEM;
            }
            else if (String.Equals(s, "Hat", StringComparison.OrdinalIgnoreCase))
            {
                return APPARELITEM;
            }
            else if (String.Equals(s, "Other", StringComparison.OrdinalIgnoreCase))
            {
                return OTHER;
            }
            else if (s.IndexOf("Division", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Distance", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return DIVISION;
            }
            else if ((s.IndexOf("emergency", StringComparison.OrdinalIgnoreCase) >= 0 && s.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0) || String.Equals(s, "Emergency", StringComparison.OrdinalIgnoreCase))
            {
                return EMERGENCYNAME;
            }
            else if (s.IndexOf("emergency", StringComparison.OrdinalIgnoreCase) >= 0 && (s.IndexOf("phone", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("cell", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return EMERGENCYPHONE;
            }
            else if (String.Equals(s, "Fleece", StringComparison.OrdinalIgnoreCase))
            {
                return APPARELITEM;
            }
            else if (String.Equals(s, "Age", StringComparison.OrdinalIgnoreCase))
            {
                return AGE;
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
