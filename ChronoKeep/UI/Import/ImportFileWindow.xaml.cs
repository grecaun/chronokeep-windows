using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.UI.Import;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Chronokeep.UI.Import.ImportFilePage2Alt;

namespace Chronokeep
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
            "Phone",
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
            "Registration Date",
            "Anonymous"
        };
        internal static readonly int FIRST = 1;
        internal static readonly int LAST = 2;
        internal static readonly int GENDER = 3;
        internal static readonly int BIRTHDAY = 4;
        internal static readonly int STREET = 5;
        internal static readonly int STREET2 = 6;
        internal static readonly int CITY = 7;
        internal static readonly int STATE = 8;
        internal static readonly int ZIP = 9;
        internal static readonly int COUNTRY = 10;
        internal static readonly int EMAIL = 11;
        internal static readonly int PHONE = 12;
        internal static readonly int MOBILE = 13;
        internal static readonly int PARENT = 14;
        internal static readonly int BIB = 15;
        internal static readonly int OWES = 16;
        internal static readonly int COMMENTS = 17;
        internal static readonly int OTHER = 18;
        internal static readonly int DISTANCE = 19;
        internal static readonly int EMERGENCYNAME = 20;
        internal static readonly int EMERGENCYPHONE = 21;
        internal static readonly int AGE = 22;
        internal static readonly int APPARELITEM = 23;
        internal static readonly int REGISTRATIONDATE = 24;
        internal static readonly int ANONYMOUS = 25;
        Page page1 = null;
        Page page2 = null;
        Page multiplesPage = null;
        Page bibConflictsPage = null;
        int[] keys;

        private bool no_distance = false;

        Event theEvent;

        /**
         * VERIFICATION VARIABLES
         */
        List<Participant> existingParticipants;
        List<Participant> importParticipants;
        List<Participant> updatedParticipants = new List<Participant>();
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
            Log.D("ImportFileWindow", "Import - Done button clicked.");
            if (page1 != null)
            {
                List<string> repeats = ((ImportFilePage1)page1).RepeatHeaders();
                List<string> requiredNotFound = ((ImportFilePage1)page1).RequiredNotFound();
                if (repeats != null)
                {
                    StringBuilder sb = new StringBuilder("Repeats for the following headers were found:");
                    foreach (string s in repeats)
                    {
                        sb.Append("\n");
                        sb.Append(s);
                    }
                    DialogBox.Show(sb.ToString());
                }
                else if (requiredNotFound != null)
                {
                    StringBuilder sb = new StringBuilder("Required fields not found:");
                    foreach (string s in requiredNotFound)
                    {
                        sb.Append("\n");
                        sb.Append(s);
                    }
                    DialogBox.Show(sb.ToString());
                }
                else
                {
                    Log.D("ImportFileWindow", "No repeat headers found.");
                    try
                    {
                        StartImport(((ImportFilePage1)page1).GetListBoxItems());
                    }
                    catch
                    {
                        DialogBox.Show("Error importing participant data. Please check the file.");
                        Close();
                    }
                }
            }
            else if (page2 != null)
            {
                Log.D("ImportFileWindow", "Importing participants.");
                ImportWork(((ImportFilePage2Alt)page2).GetDistances());
            }
            else if (multiplesPage != null)
            {
                Log.D("ImportFileWindow", "Processing multiples to keep/remove.");
                ProcessMultiplestoRemove(((ImportFilePageConflicts)multiplesPage).GetParticipantsToRemove());
            }
            else if (bibConflictsPage != null)
            {
                Log.D("ImportFileWindow", "Processing bib conflicts to remove.");
                ProcessBibConflicts(((ImportFilePageConflicts)bibConflictsPage).GetParticipantsToRemove());
            }
            else
            {
                Log.D("ImportFileWindow", "Abort! Abort! Something went terribly wrong.");
            }
        }

        private void StartImport(HeaderListBoxItem[] headerListBoxItems)
        {
            importer.FetchData();
            keys = new int[human_fields.Length + 1];
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = 0;
            }
            foreach (HeaderListBoxItem item in headerListBoxItems)
            {
                Log.D("ImportFileWindow", "Header is " + item.HeaderLabel.Text);
                if (item.HeaderBox.SelectedIndex != 0)
                {
                    keys[item.HeaderBox.SelectedIndex] = item.Index;
                }
            }
            ImportData data = importer.Data;
            string[] distancesFromFile = data.GetDistanceNames(keys[DISTANCE]);
            if (keys[DISTANCE] != 0)
            {
                Log.D("ImportFileWindow", "Distance key is " + keys[DISTANCE] + " with a header name of " + headerListBoxItems[keys[DISTANCE] - 1].HeaderLabel.Text + " number of distances found is " + distancesFromFile.Length);
            }
            if (distancesFromFile.Length <= 0)
            {
                //DialogBox.Show("No distances found in file, or nothing selected for distance.  Please correct this and try again.");
                //return;
                no_distance = true;
                distancesFromFile = new string[]
                {
                    "",
                };
            }
            StringBuilder sb = new StringBuilder("Distance names are");
            foreach (string s in distancesFromFile)
            {
                sb.Append(" '" + s + "'");
            }
            Log.D("ImportFileWindow", sb.ToString());
            page1 = null;
            Event theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                Log.E("IO.ImportFileWindow", "No event selected.");
                this.Close();
            }
            List<Distance> distancesFromDatabase = database.GetDistances(theEvent.Identifier);
            page2 = new ImportFilePage2Alt(distancesFromFile, distancesFromDatabase, no_distance);
            SheetsBox.Visibility = Visibility.Collapsed;
            eventLabel.Width = 460;
            Frame.Content = page2;
        }

        private async void ImportWork(List<ImportDistance> fileDistances)
        {
            // Make sure Age Groups are set properly.
            Dictionary<(int, int), AgeGroup> AgeGroups = new();
            Dictionary<int, AgeGroup> LastAgeGroup = new();
            foreach (AgeGroup g in database.GetAgeGroups(theEvent.Identifier))
            {
                for (int i = g.StartAge; i <= g.EndAge; i++)
                {
                    AgeGroups[(g.DistanceId, i)] = g;
                }
                if (!LastAgeGroup.ContainsKey(g.DistanceId) || LastAgeGroup[g.DistanceId].StartAge < g.StartAge)
                {
                    LastAgeGroup[g.DistanceId] = g;
                }
            }

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
                bool BackYardUltra = Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA == theEvent.EventType ? true : false;
                Distance theDiv;
                Distance backyardDistance = null;
                // Ensure we don't add more distances for backyard ultra events.
                if (!BackYardUltra)
                {
                    bool newDistances = false;
                    foreach (ImportDistance id in fileDistances)
                    {
                        if (id.DistanceId == -1)
                        {
                            if (divHashName.ContainsKey(id.NameFromFile))
                            {
                                theDiv = (Distance)divHashName[id.NameFromFile];
                                divHash.Add(id.NameFromFile, theDiv);
                            }
                            else
                            {
                                Distance div = new Distance(id.NameFromFile, theEvent.Identifier);
                                database.AddDistance(div);
                                div.Identifier = database.GetDistanceID(div);
                                divHash.Add(div.Name, div);
                                Log.D("ImportFileWindow", "Div name is " + div.Name);
                                newDistances = true;
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
                                Log.E("IO.ImportFileWindow", "Distance doesn't exist in the database...");
                            }
                        }
                    }
                    if (newDistances)
                    {
                        window.UpdateRegistrationDistances();
                    }
                }
                else
                {
                    if (distances.Count > 0)
                    {
                        backyardDistance = distances[0];
                    }
                    else
                    {
                        backyardDistance = new Distance("Backyard", theEvent.Identifier);
                        database.AddDistance(backyardDistance);
                        backyardDistance.Identifier = database.GetDistanceID(backyardDistance);
                        window.UpdateRegistrationDistances();
                    }
                }
                int numEntries = data.Data.Count;
                importParticipants = new List<Participant>();
                // new distances might have been added
                distances = database.GetDistances(theEvent.Identifier);
                for (int counter = 0; counter < numEntries; counter++)
                {
                    Distance thisDiv = distances[0];
                    if (data.Data[counter][keys[DISTANCE]] != null && data.Data[counter][keys[DISTANCE]].Length > 0)
                    {
                        Log.D("ImportFileWindow", "Looking for... " + Utils.UppercaseFirst(data.Data[counter][keys[DISTANCE]].ToLower()));
                        // Always set distance to our backyard distance if we're importing for a backyard ultra event. Otherwise figure out the proper distance.
                        thisDiv = BackYardUltra ? backyardDistance : (Distance)divHash[Utils.UppercaseFirst(data.Data[counter][keys[DISTANCE]].Trim().ToLower())];
                    }
                    string birthday = "";
                    int age = -1;
                    if (keys[BIRTHDAY] == 0 && keys[AGE] != 0) // birthday not set but age is
                    {
                        Log.D("ImportFileWindow", string.Format("Counter is {0} and keys[AGE] is {1} - age of participant is {2}", counter, keys[AGE], data.Data[counter][keys[AGE]]));
                        try
                        {
                            age = Convert.ToInt32(data.Data[counter][keys[AGE]]);
                            birthday = string.Format("01/01/{0,4}", thisYear - age);
                        }
                        catch { }
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
                            data.Data[counter][keys[ANONYMOUS]] != null && data.Data[counter][keys[ANONYMOUS]].Trim().Length > 0, // Set Anonymous if anything is in the field
                            false, // always false, this field is no longer used
                            data.Data[counter][keys[APPARELITEM]]
                            ),
                        data.Data[counter][keys[EMAIL]], // email
                        data.Data[counter][keys[PHONE]], // phone
                        data.Data[counter][keys[MOBILE]], // mobile
                        data.Data[counter][keys[PARENT]], // parent
                        data.Data[counter][keys[COUNTRY]], // country
                        data.Data[counter][keys[STREET2]],  // street2
                        data.Data[counter][keys[GENDER]] != null ? data.Data[counter][keys[GENDER]] : "",  // gender
                        data.Data[counter][keys[EMERGENCYNAME]], // Emergency Name
                        data.Data[counter][keys[EMERGENCYPHONE]]  // Emergency Phone
                        );
                    int agDivId = theEvent.CommonAgeGroups ? Constants.Timing.COMMON_AGEGROUPS_DISTANCEID : output.EventSpecific.DistanceIdentifier;
                    age = output.GetAge(theEvent.Date);
                    if (age < 0)
                    {
                        output.EventSpecific.AgeGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
                        output.EventSpecific.AgeGroupName = "";
                    }
                    else if (AgeGroups.TryGetValue((agDivId, age), out AgeGroup group))
                    {
                        output.EventSpecific.AgeGroupId = group.GroupId;
                        output.EventSpecific.AgeGroupName = group.PrettyName();
                    }
                    else if (LastAgeGroup.TryGetValue(agDivId, out AgeGroup lGroup))
                    {
                        output.EventSpecific.AgeGroupId = lGroup.GroupId;
                        output.EventSpecific.AgeGroupName = lGroup.PrettyName();
                    }
                    else
                    {
                        output.EventSpecific.AgeGroupId = Constants.Timing.TIMERESULT_DUMMYAGEGROUP;
                        output.EventSpecific.AgeGroupName = "";
                    }
                    importParticipants.Add(output);
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
                        //Log.D("ImportFileWindow", string.Format("inner {1} outer {0}", outer, inner));
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
                            if ((importParticipants[inner].Bib == part.Bib || importParticipants[inner].Bib.Length < 1 && part.Bib.Length > 0)
                                && importParticipants[inner].Distance.Equals(part.Distance, StringComparison.OrdinalIgnoreCase))
                            {
                                // bib remains the same or isn't set in new import
                                duplicatesImport.Add(importParticipants[inner]);
                            }
                            else if (importParticipants[inner].Bib.Length > 0 && part.Bib.Length < 1
                                && importParticipants[inner].Distance.Equals(part.Distance, StringComparison.OrdinalIgnoreCase))
                            {
                                // bib is an update, add to duplicates so we don't add it again,
                                // then add to list of participants to update
                                duplicatesImport.Add(importParticipants[inner]);
                                updatedParticipants.Add(importParticipants[inner]);
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
                Dictionary<string, HashSet<Participant>> BibConflicts = new Dictionary<string, HashSet<Participant>>();
                Dictionary<string, Participant> ExistingParticipants = new Dictionary<string, Participant>();
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
                    // this is checking for bib repeats, so check if we're actually checking a specified bib
                    if (import.Bib.Trim().Length > 0 && ExistingParticipants.ContainsKey(import.Bib))
                    {
                        if (!ExistingParticipants[import.Bib].Is(import))
                        {
                            Log.D("ImportFileWindow", string.Format("We've found {0} {1} and {2} {3} for bib {4}", import.FirstName, import.LastName, ExistingParticipants[import.Bib].FirstName, ExistingParticipants[import.Bib].LastName, import.Bib));
                            if (!BibConflicts.ContainsKey(import.Bib))
                            {
                                BibConflicts[import.Bib] = new HashSet<Participant>();
                            }
                            BibConflicts[import.Bib].Add(import);
                            BibConflicts[import.Bib].Add(ExistingParticipants[import.Bib]);
                        }
                    }
                }
                foreach (string bib in BibConflicts.Keys)
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
                Log.D("ImportFileWindow", "Removing old participants we were told to.");
                database.RemoveParticipantEntries(existingToRemoveParticipants);
                Log.D("ImportFileWindow", "Updating participants.");
                database.UpdateParticipants(updatedParticipants);
                Log.D("ImportFileWindow", "Adding new participants.");
                database.AddParticipants(importParticipants);
            });
            Log.D("ImportFileWindow", "All done with the import.");
            database.ResetTimingResultsEvent(theEvent.Identifier);
            window.NetworkClearResults();
            window.NotifyTimingWorker();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("ImportFileWindow", "Import - Cancel button clicked.");
            this.Close();
        }

        internal static int GetHeaderBoxIndex(string s)
        {
            Log.D("ImportFileWindow", "Looking for a value for: " + s);
            if (s.Contains("First", StringComparison.OrdinalIgnoreCase))
            {
                return FIRST;
            }
            else if (s.Contains("Last", StringComparison.OrdinalIgnoreCase))
            {
                return LAST;
            }
            else if (s.Contains("Gender", StringComparison.OrdinalIgnoreCase)
                && !s.Contains("Race Group", StringComparison.OrdinalIgnoreCase))
            {
                return GENDER;
            }
            else if (string.Equals(s, "Birthday", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "Birthdate", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "DOB", StringComparison.OrdinalIgnoreCase)
                || (s.Contains("Date", StringComparison.OrdinalIgnoreCase) && s.Contains("Birth", StringComparison.OrdinalIgnoreCase)))
            {
                return BIRTHDAY;
            }
            else if (string.Equals(s, "Street", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "Address", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "Street Address", StringComparison.OrdinalIgnoreCase))
            {
                return STREET;
            }
            else if (string.Equals(s, "Street 2", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "Address 2", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "Apartment", StringComparison.OrdinalIgnoreCase))
            {
                return STREET2;
            }
            else if (string.Equals(s, "City", StringComparison.OrdinalIgnoreCase))
            {
                return CITY;
            }
            else if ((s.Contains("State", StringComparison.OrdinalIgnoreCase)
                || s.Contains("Province", StringComparison.OrdinalIgnoreCase))
                && !s.Contains("Statement", StringComparison.OrdinalIgnoreCase))
            {
                return STATE;
            }
            else if (s.Contains("Zip", StringComparison.OrdinalIgnoreCase)
                || s.Contains("Postal Code", StringComparison.OrdinalIgnoreCase))
            {
                return ZIP;
            }
            else if (string.Equals(s, "Country", StringComparison.OrdinalIgnoreCase))
            {
                return COUNTRY;
            }
            else if (string.Equals(s, "Phone", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "Phone Number", StringComparison.OrdinalIgnoreCase))
            {
                return PHONE;
            }
            else if (s.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
            {
                return MOBILE;
            }
            else if (s.Contains("Email", StringComparison.OrdinalIgnoreCase))
            {
                return EMAIL;
            }
            else if (string.Equals(s, "Parent", StringComparison.OrdinalIgnoreCase))
            {
                return PARENT;
            }
            else if ((s.Contains("Bib", StringComparison.OrdinalIgnoreCase)
                || s.Contains("pinney", StringComparison.OrdinalIgnoreCase))
                && !s.Contains("Race Group", StringComparison.OrdinalIgnoreCase))
            {
                return BIB;
            }
            else if (
                (s.Contains("Shirt", StringComparison.OrdinalIgnoreCase)
                    || s.Contains("Hat", StringComparison.OrdinalIgnoreCase)
                    || s.Contains("Fleece", StringComparison.OrdinalIgnoreCase)
                    || s.Contains("Apparel", StringComparison.OrdinalIgnoreCase)
                    || s.Contains("Hoodie", StringComparison.OrdinalIgnoreCase)
                )
                && !(s.Contains("Quantity", StringComparison.OrdinalIgnoreCase)
                    || s.Contains("Options", StringComparison.OrdinalIgnoreCase)
                    || s.Contains("Details", StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                return APPARELITEM;
            }
            else if (string.Equals(s, "Owes", StringComparison.OrdinalIgnoreCase))
            {
                return OWES;
            }
            else if (string.Equals(s, "Comments", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "Notes", StringComparison.OrdinalIgnoreCase))
            {
                return COMMENTS;
            }
            else if (string.Equals(s, "Other", StringComparison.OrdinalIgnoreCase))
            {
                return OTHER;
            }
            else if (s.Contains("Division", StringComparison.OrdinalIgnoreCase) 
                || s.Contains("Distance", StringComparison.OrdinalIgnoreCase)
                || s.Equals("Event", StringComparison.OrdinalIgnoreCase))
            {
                return DISTANCE;
            }
            else if ((s.Contains("emergency", StringComparison.OrdinalIgnoreCase) && s.Contains("name", StringComparison.OrdinalIgnoreCase))
                || string.Equals(s, "Emergency", StringComparison.OrdinalIgnoreCase))
            {
                return EMERGENCYNAME;
            }
            else if (s.Contains("emergency", StringComparison.OrdinalIgnoreCase)
                && (s.Contains("phone", StringComparison.OrdinalIgnoreCase) || s.Contains("cell", StringComparison.OrdinalIgnoreCase)))
            {
                return EMERGENCYPHONE;
            }
            else if (string.Equals(s, "Age", StringComparison.OrdinalIgnoreCase))
            {
                return AGE;
            }
            else if (string.Equals(s, "Registration Date", StringComparison.OrdinalIgnoreCase))
            {
                return REGISTRATIONDATE;
            }
            else if (s.Contains("Anonymous", StringComparison.OrdinalIgnoreCase)
                || s.Contains("Private", StringComparison.OrdinalIgnoreCase))
            {
                return ANONYMOUS;
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
            Log.D("ImportFileWindow", "You've selected number " + selection);
            if (page1 != null)
            {
                ((ImportFilePage1)page1).UpdateSheetNo(selection + 1);
            }
        }
    }
}
