using ChronoKeep.Interfaces;
using ChronoKeep.UI.Import;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static ChronoKeep.UI.Import.ImportFilePage2Alt;

namespace ChronoKeep
{
    /// <summary>
    /// Interaction logic for ImportFileWindow.xaml
    /// </summary>
    public partial class ImportFileWindow : Window
    {
        IDataImporter importer;
        IMainWindow window = null;
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
            "Apparel",
            "Registration Date"
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
        private static readonly int REGISTRATIONDATE = 23;
        Page page1 = null;
        Page page2 = null;
        int[] keys;

        private ImportFileWindow(IMainWindow window, IDataImporter importer, IDBInterface database)
        {
            InitializeComponent();
            this.importer = importer;
            this.window = window;
            this.database = database;
            Header.Height = new GridLength(55);
            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            HeaderGrid.Children.Clear();
            HeaderGrid.Children.Add(Done);
            Done.HorizontalAlignment = HorizontalAlignment.Stretch;
            Done.VerticalAlignment = VerticalAlignment.Center;
            Done.Width = Double.NaN;
            Done.Height = 35;
            Done.FontSize = 16;
            Done.Margin = new Thickness(10, 10, 10, 10);
            Grid.SetColumn(Done, 1);
            HeaderGrid.Children.Add(Cancel);
            Grid.SetColumn(Cancel, 2);
            Cancel.HorizontalAlignment = HorizontalAlignment.Stretch;
            Cancel.VerticalAlignment = VerticalAlignment.Center;
            Cancel.Width = Double.NaN;
            Cancel.Height = 35;
            Cancel.FontSize = 16;
            Cancel.Margin = new Thickness(10, 10, 10, 10);
            if (importer.Data.Type == ImportData.FileType.EXCEL)
            {
                HeaderGrid.Children.Add(SheetsBox);
                Grid.SetColumn(SheetsBox, 0);
                SheetsBox.HorizontalAlignment = HorizontalAlignment.Stretch;
                SheetsBox.VerticalAlignment = VerticalAlignment.Center;
                SheetsBox.Width = Double.NaN;
                SheetsBox.Height = 35;
                SheetsBox.FontSize = 16;
                SheetsBox.VerticalContentAlignment = VerticalAlignment.Center;
                SheetsBox.Margin = new Thickness(10, 10, 10, 10);
                SheetsBox.Visibility = Visibility.Visible;
                SheetsBox.ItemsSource = ((ExcelImporter)importer).SheetNames;
                SheetsBox.SelectedIndex = 0;
                init = false;
            }
            page1 = new ImportFilePage1(importer);
            Frame.Content = page1;
        }

        public static ImportFileWindow NewWindow(IMainWindow window, IDataImporter importer, IDBInterface database)
        {
            return new ImportFileWindow(window, importer, database);
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Import - Done button clicked.");
            if (page1 != null)
            {
                List<String> repeats = ((ImportFilePage1)page1).RepeatHeaders();
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
                    StartImport(((ImportFilePage1)page1).GetListBoxItems());
                }
            }
            else if (page2 != null)
            {
                ImportWork(((ImportFilePage2Alt)page2).GetDivisions());
            }
            else
            {
                Log.D("Abort! Abort! Something went terribly wrong.");
            }
        }

        private void StartImport(HeaderListBoxItem[] headerListBoxItems)
        {
            importer.FetchData();
            keys = new int[human_fields.Length + 1];
            for (int i = 0; i< keys.Length; i++)
            {
                keys[i] = 0;
            }
            foreach (HeaderListBoxItem item in headerListBoxItems)
            {
                Log.D("Header is " + item.HeaderLabel.Content);
                if (item.HeaderBox.SelectedIndex != 0)
                {
                    keys[item.HeaderBox.SelectedIndex] = item.Index;
                }
            }
            ImportData data = importer.Data;
            string[] divisionsFromFile = data.GetDivisionNames(keys[DIVISION]);
            Log.D("Division key is " + keys[DIVISION] + " with a header name of " + headerListBoxItems[keys[DIVISION]-1].HeaderLabel.Content + " number of divisions found is " + divisionsFromFile.Length);
            if (divisionsFromFile.Length <= 0)
            {
                MessageBox.Show("No divisions found in file, or nothing selected for divisions/distance.  Please correct this or cancel.");
                return;
            }
            StringBuilder sb = new StringBuilder("Division names are");
            foreach (String s in divisionsFromFile)
            {
                sb.Append(" '" + s + "'");
            }
            Log.D(sb.ToString());
            page1 = null;
            Event theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                Log.E("No event selected.");
                this.Close();
            }
            List<Division> divisionsFromDatabase = database.GetDivisions(theEvent.Identifier);
            page2 = new ImportFilePage2Alt(divisionsFromFile, divisionsFromDatabase);
            SheetsBox.Visibility = Visibility.Collapsed;
            eventLabel.Width = 460;
            Done.Content = "Done";
            Frame.Content = page2;
        }

        private async void ImportWork(List<ImportDivision> fileDivisions)
        {
            bool valid = true;
            Event theEvent = database.GetCurrentEvent();
            await Task.Run(() =>
            {
                ImportData data = importer.Data;
                int thisYear = DateTime.Parse(theEvent.Date).Year;
                Hashtable divHashName = new Hashtable(500);
                Hashtable divHashId = new Hashtable(500);
                Hashtable divHash = new Hashtable(500);
                List<Division> divisions = database.GetDivisions(theEvent.Identifier);
                foreach (Division d in divisions)
                {
                    divHashName.Add(d.Name, d);
                    divHashId.Add(d.Identifier, d);
                }
                Division theDiv;
                foreach (ImportDivision id in fileDivisions)
                {
                    if (id.DivisionId == -1)
                    {
                        theDiv = (Division)divHashName[id.NameFromFile];
                        if (theDiv == null)
                        {
                            Division div = new Division(id.NameFromFile, theEvent.Identifier, 0);
                            database.AddDivision(div);
                            div.Identifier = database.GetDivisionID(div);
                            divHash.Add(div.Name, div);
                            Log.D("Div name is " + div.Name);
                        }
                        else
                        {
                            divHash.Add(id.NameFromFile, theDiv);
                        }
                    }
                    else
                    {
                        theDiv = (Division)divHashId[id.DivisionId];
                        if (theDiv != null)
                        {
                            divHash.Add(id.NameFromFile, theDiv);
                        }
                        else
                        {
                            Log.E("Division doesn't exist in the database...");
                        }
                    }
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
                        }
                        else if (keys[BIRTHDAY] != 0)
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
                                theEvent.Identifier,
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
                database.ResetTimingResultsEvent(theEvent.Identifier);
                window.NetworkClearResults(theEvent.Identifier);
                window.NotifyRecalculateAgeGroups();
                window.NotifyTimingWorker();
                this.Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
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
            else if (String.Equals(s, "Registration Date", StringComparison.OrdinalIgnoreCase))
            {
                return REGISTRATIONDATE;
            }
            return 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
            importer.Finish();
        }

        private void SheetsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (init) { return; }
            int selection = ((ComboBox)sender).SelectedIndex;
            Log.D("You've selected number " + selection);
            if (page1 != null)
            {
                ((ImportFilePage1)page1).UpdateSheetNo(selection);
            }
        }
    }
}
