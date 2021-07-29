using ChronoKeep.Interfaces;
using ChronoKeep.Objects;
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
            "Distance",
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
        private static readonly int DISTANCE = 18;
        private static readonly int EMERGENCYNAME = 19;
        private static readonly int EMERGENCYPHONE = 20;
        private static readonly int AGE = 21;
        internal static readonly int APPARELITEM = 22;
        private static readonly int REGISTRATIONDATE = 23;
        Page page1 = null;
        Page page2 = null;
        Page multiplesPage = null;
        Page bibConflictsPage = null;
        int[] keys;

        Dictionary<(int, int), AgeGroup> AgeGroups = AgeGroup.GetAgeGroups();
        Dictionary<int, AgeGroup> LastAgeGroup = AgeGroup.GetLastAgeGroup();

        Event theEvent;

        /**
         * VERIFICATION VARIABLES
         */
        List<Participant> existingParticipants;
        List<Participant> importParticipants;
        List<Participant> existingToRemoveParticipants = new List<Participant>();

        private ImportFileWindow(IMainWindow window, IDataImporter importer, IDBInterface database)
        {
            InitializeComponent();
            this.importer = importer;
            this.window = window;
            this.database = database;
            this.theEvent = database.GetCurrentEvent();
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
                Log.D("Importing participants.");
                ImportWork(((ImportFilePage2Alt)page2).GetDistances());
            }
            else if (multiplesPage != null)
            {
                Log.D("Processing multiples to keep/remove.");
                ProcessMultiplestoRemove(((ImportFilePageConflicts)multiplesPage).GetParticipantsToRemove());
            }
            else if (bibConflictsPage != null)
            {
                Log.D("Processing bib conflicts to remove.");
                ProcessBibConflicts(((ImportFilePageConflicts)bibConflictsPage).GetParticipantsToRemove());
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
            string[] distancesFromFile = data.GetDistanceNames(keys[DISTANCE]);
            Log.D("Distance key is " + keys[DISTANCE] + " with a header name of " + headerListBoxItems[keys[DISTANCE]-1].HeaderLabel.Content + " number of distances found is " + distancesFromFile.Length);
            if (distancesFromFile.Length <= 0)
            {
                MessageBox.Show("No distances found in file, or nothing selected for distance.  Please correct this or cancel.");
                return;
            }
            StringBuilder sb = new StringBuilder("Distance names are");
            foreach (String s in distancesFromFile)
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
            List<Distance> distancesFromDatabase = database.GetDistances(theEvent.Identifier);
            page2 = new ImportFilePage2Alt(distancesFromFile, distancesFromDatabase);
            SheetsBox.Visibility = Visibility.Collapsed;
            eventLabel.Width = 460;
            Frame.Content = page2;
        }

        private async void ImportWork(List<ImportDistance> fileDistances)
        {
            HashSet<Participant> multiples = new HashSet<Participant>();
            await Task.Run(() =>
            {
                ImportData data = importer.Data;
                int thisYear = DateTime.Parse(theEvent.Date).Year;
                Hashtable divHashName = new Hashtable(500);
                Hashtable divHashId = new Hashtable(500);
                Hashtable divHash = new Hashtable(500);
                List<Distance> distances = database.GetDistances(theEvent.Identifier);
                foreach (Distance d in distances)
                {
                    divHashName.Add(d.Name, d);
                    divHashId.Add(d.Identifier, d);
                }
                Distance theDiv;
                foreach (ImportDistance id in fileDistances)
                {
                    if (id.DistanceId == -1)
                    {
                        theDiv = (Distance)divHashName[id.NameFromFile];
                        if (theDiv == null)
                        {
                            Distance div = new Distance(id.NameFromFile, theEvent.Identifier, 0);
                            database.AddDistance(div);
                            div.Identifier = database.GetDistanceID(div);
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
                        theDiv = (Distance)divHashId[id.DistanceId];
                        if (theDiv != null)
                        {
                            divHash.Add(id.NameFromFile, theDiv);
                        }
                        else
                        {
                            Log.E("Distance doesn't exist in the database...");
                        }
                    }
                }
                int numEntries = data.Data.Count;
                importParticipants = new List<Participant>();
                for (int counter = 0; counter < numEntries; counter++)
                {
                    if (data.Data[counter][keys[DISTANCE]] != null && data.Data[counter][keys[DISTANCE]].Length > 0)
                    {
                        Log.D("Looking for... " + Utils.UppercaseFirst(data.Data[counter][keys[DISTANCE]].ToLower()));
                        Distance thisDiv = (Distance)divHash[Utils.UppercaseFirst(data.Data[counter][keys[DISTANCE]].Trim().ToLower())];
                        string birthday = "01/01/1900";
                        int age = -1;
                        if (keys[BIRTHDAY] == 0 && keys[AGE] != 0) // birthday not set but age is
                        {
                            Log.D(String.Format("Counter is {0} and keys[AGE] is {1}", counter, keys[AGE]));
                            Log.D("Age of participant is " + data.Data[counter][keys[AGE]]);
                            age = Convert.ToInt32(data.Data[counter][keys[AGE]]);
                            birthday = String.Format("01/01/{0,4}", thisYear - age);
                        }
                        else if (keys[BIRTHDAY] != 0)
                        {
                            birthday = data.Data[counter][keys[BIRTHDAY]]; // birthday
                        }
                        Participant output = new Participant(
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
                            );
                        int agDivId = theEvent.CommonAgeGroups ? Constants.Timing.COMMON_AGEGROUPS_DISTANCEID : output.EventSpecific.DistanceIdentifier;
                        age = output.GetAge(theEvent.Date);
                        if (AgeGroups == null || age < 0)
                        {
                            output.EventSpecific.AgeGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
                            output.EventSpecific.AgeGroupName = "0-110";
                        }
                        else if (AgeGroups.ContainsKey((agDivId, age)))
                        {
                            AgeGroup group = AgeGroups[(agDivId, age)];
                            output.EventSpecific.AgeGroupId = group.GroupId;
                            output.EventSpecific.AgeGroupName = String.Format("{0}-{1}", group.StartAge, group.EndAge);
                        }
                        else if (LastAgeGroup.ContainsKey(agDivId))
                        {
                            AgeGroup group = LastAgeGroup[agDivId];
                            output.EventSpecific.AgeGroupId = group.GroupId;
                            output.EventSpecific.AgeGroupName = String.Format("{0}-{1}", group.StartAge, group.EndAge);
                        }
                        else
                        {
                            output.EventSpecific.AgeGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
                            output.EventSpecific.AgeGroupName = "0-110";
                        }
                        importParticipants.Add(output);
                    }
                }
                /**
                 * 
                 * VERIFICATION CODE
                 * 
                 */
                // Check import participants for multiples.
                existingParticipants = database.GetParticipants(theEvent.Identifier);
                HashSet<Participant> duplicatesImport = new HashSet<Participant>();
                for (int inner=0; inner<importParticipants.Count; inner++)
                {
                    // Check against others imported
                    for (int outer=inner+1; outer<importParticipants.Count; outer++)
                    {
                        Log.D(String.Format("inner {1} outer {0}", outer, inner));
                        if (importParticipants[inner].Is(importParticipants[outer]))
                        {
                            // if they're a duplicate and not just a multiple
                            if (importParticipants[inner].Bib == importParticipants[outer].Bib
                                && importParticipants[inner].Distance.Equals(importParticipants[outer].Distance, StringComparison.OrdinalIgnoreCase))
                            {
                                duplicatesImport.Add(importParticipants[inner]);
                            }
                            else
                            {
                                multiples.Add(importParticipants[inner]);
                                multiples.Add(importParticipants[outer]);
                            }
                        }
                    }
                    // Check against everyone currently in the database.
                    foreach (Participant part in existingParticipants)
                    {
                        if (importParticipants[inner].Is(part))
                        {
                            // check if its someone who's already in the database thus we don't need to add to multiples and
                            // we can remove them from the import
                            if (importParticipants[inner].Bib == part.Bib 
                                && importParticipants[inner].Distance.Equals(part.Distance, StringComparison.OrdinalIgnoreCase))
                            {
                                duplicatesImport.Add(importParticipants[inner]);
                            }
                            else
                            {
                                multiples.Add(importParticipants[inner]);
                                multiples.Add(part);
                            }
                        }
                    }
                }
                // Remove anyone that was deemed a duplicate
                // This can happen if there was an X, Y, and Z in the import where X and Y are duplicates
                // but Z is the same person with diff bib/distance.
                // This can also happen if there are X and Z in the import but Y in the database,
                // where the situation is as above
                foreach (Participant dup in duplicatesImport)
                {
                    if (multiples.Contains(dup))
                    {
                        multiples.Remove(dup);
                    }
                }
                // remove all duplicates from the import
                importParticipants.RemoveAll(x => duplicatesImport.Contains(x));
            });
            // if we have multiples to mess around with display the page
            if (multiples.Count > 0)
            {
                page2 = null;
                multiplesPage = new ImportFilePageConflicts(new List<Participant>(multiples), theEvent);
                Frame.Content = multiplesPage;
            }
            // otherwise process the multiples (none)
            else
            {
                ProcessMultiplestoRemove(new List<Participant>());
            }
        }


        private async void ProcessMultiplestoRemove(List<Participant> toRemove)
        {
            List<Participant> conflicts = new List<Participant>();
            await Task.Run(() =>
            {
                Dictionary<int, HashSet<Participant>> BibConflicts = new Dictionary<int, HashSet<Participant>>();
                Dictionary<int, Participant> ExistingParticipants = new Dictionary<int, Participant>();
                // keep track of who we need to tell the database to remove
                existingToRemoveParticipants.AddRange(toRemove);
                existingToRemoveParticipants.RemoveAll(x => importParticipants.Contains(x));
                // Remove those we didn't select to keep from our lists.
                existingParticipants.RemoveAll(x => toRemove.Contains(x));
                importParticipants.RemoveAll(x => toRemove.Contains(x));
                foreach (Participant existing in existingParticipants)
                {
                    ExistingParticipants[existing.Bib] = existing;
                }
                foreach (Participant import in importParticipants)
                {
                    if (ExistingParticipants.ContainsKey(import.Bib))
                    {
                        if (!ExistingParticipants[import.Bib].Is(import))
                        {
                            Log.D(String.Format("We've found {0} {1} and {2} {3} for bib {4}", import.FirstName, import.LastName, ExistingParticipants[import.Bib].FirstName, ExistingParticipants[import.Bib].LastName, import.Bib));
                            if (!BibConflicts.ContainsKey(import.Bib))
                            {
                                BibConflicts[import.Bib] = new HashSet<Participant>();
                            }
                            BibConflicts[import.Bib].Add(import);
                            BibConflicts[import.Bib].Add(ExistingParticipants[import.Bib]);
                        }
                    }
                }
                foreach (int bib in BibConflicts.Keys)
                {
                    conflicts.AddRange(BibConflicts[bib]);
                }
            });
            // if we have multiples to mess around with display the page
            if (conflicts.Count > 0)
            {
                multiplesPage = null;
                bibConflictsPage = new ImportFilePageConflicts(conflicts, theEvent);
                Frame.Content = bibConflictsPage;
            }
            // otherwise process the multiples (none)
            else
            {
                ProcessBibConflicts(new List<Participant>());
            }
        }

        private async void ProcessBibConflicts(List<Participant> toRemove)
        {
            await Task.Run(() =>
            {
                // keep track of who we need to tell the database to get rid of
                existingToRemoveParticipants.AddRange(toRemove);
                existingToRemoveParticipants.RemoveAll(x => importParticipants.Contains(x));
                // Remove those we didn't select to keep from our import list
                // no need to remove from the existing because we're not re-adding those
                importParticipants.RemoveAll(x => toRemove.Contains(x));
                Log.D("Removing old participants we were told to.");
                database.RemoveParticipantEntries(existingToRemoveParticipants);
                Log.D("Adding new participants.");
                database.AddParticipants(importParticipants);
            });
            Log.D("All done with the import.");
            database.ResetTimingResultsEvent(theEvent.Identifier);
            window.NetworkClearResults(theEvent.Identifier);
            window.NotifyTimingWorker();
            this.Close();
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
            else if (s.IndexOf("Bib", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("pinney", StringComparison.OrdinalIgnoreCase) >= 0)
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
                return DISTANCE;
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
