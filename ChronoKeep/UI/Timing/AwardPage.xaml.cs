using ChronoKeep.Interfaces;
using ChronoKeep.IO;
using ChronoKeep.Objects;
using ChronoKeep.UI.MainPages;
using Microsoft.Win32;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using MigraDoc.Rendering.Printing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChronoKeep.UI.Timing
{
    /// <summary>
    /// Interaction logic for AwardPage.xaml
    /// </summary>
    public partial class AwardPage : ISubPage
    {
        IDBInterface database;
        TimingPage parent;
        Event theEvent;

        private ObservableCollection<AgeGroup> customAgeGroups = new ObservableCollection<AgeGroup>();

        private readonly Regex allowedChars = new Regex("[^0-9]");

        public AwardPage(TimingPage parent, IDBInterface database)
        {
            InitializeComponent();
            this.parent = parent;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                Log.E("Something went wrong and no proper event was returned.");
                return;
            }
            customGroupsListView.ItemsSource = customAgeGroups;
            List<Division> divisions = database.GetDivisions(theEvent.Identifier);
            divisions.Sort((x1, x2) => x1.Name.CompareTo(x2.Name));
            foreach (Division d in divisions)
            {
                DivisionsBox.Items.Add(new ListBoxItem()
                {
                    Content = d.Name
                });
            }
            UpdateView();
        }

        private void IsNumber(object sender, TextCompositionEventArgs e)
        {
            e.Handled = allowedChars.IsMatch(e.Text);
        }

        private void AddCustom_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Add custom group clicked.");
            try
            {
                int start = Convert.ToInt32(startCustom.Text);
                int end = Convert.ToInt32(endCustom.Text);
                if (start > -1 || end < 111)
                {
                    database.AddAgeGroup(new AgeGroup(theEvent.Identifier, Constants.Timing.AGEGROUPS_CUSTOM_DIVISIONID, start, end));
                    UpdateView();
                    startCustom.Text = "";
                    endCustom.Text = "";
                    startCustom.Focus();
                }
                else
                {
                    MessageBox.Show("Ages not in the range of 0 to 110.");
                }
            }
            catch
            {
                MessageBox.Show("Start or end age not specified.");
            }
        }

        private void DeleteCustom_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Deleting some entries... maybe.");
            List<AgeGroup> items = new List<AgeGroup>();
            IList selected = customGroupsListView.SelectedItems;
            if (selected.Count < 1)
            {
                return;
            }
            foreach (AgeGroup ag in selected)
            {
                items.Add(ag);
            }
            database.RemoveAgeGroups(items);
            UpdateView();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Print clicked.");
            System.Windows.Forms.PrintDialog printDialog = new System.Windows.Forms.PrintDialog
            {
                AllowSomePages = true,
                UseEXDialog = true
            };
            AwardOptions options = GetOptions();
            List<string> divsToPrint = new List<string>();
            foreach (ListBoxItem divItem in DivisionsBox.SelectedItems)
            {
                if (divItem.Content.Equals("All"))
                {
                    divsToPrint = null;
                    break;
                }
                divsToPrint.Add(divItem.Content.ToString());
            }
            if (divsToPrint != null && divsToPrint.Count < 1)
            {
                divsToPrint = null;
            }
            if (printDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PdfDocumentRenderer renderer = new PdfDocumentRenderer();
                if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                {
                    renderer.Document = GetAwardsPrintableDocumentDistance(divsToPrint, options);
                }
                else
                {
                    MessageBox.Show("Award printing for time based races has not been implemented yet.");
                    return;
                }
                renderer.RenderDocument();
                MigraDocPrintDocument printDocument = new MigraDocPrintDocument
                {
                    Renderer = renderer.DocumentRenderer,
                    PrinterSettings = printDialog.PrinterSettings
                };
                try
                {
                    printDocument.Print();
                }
                catch
                {
                    MessageBox.Show("Something went wrong when attempting to print.");
                }
            }
        }

        private AwardOptions GetOptions()
        {
            return new AwardOptions()
            {
                PrintOverall = overallYes.IsChecked == true,
                PrintAgeGroups = agYes.IsChecked == true,
                PrintCustom = customYes.IsChecked == true,
                NumOverall = overallNumberParticipants.Text.Length == 0 ? 3 : Convert.ToInt32(overallNumberParticipants.Text),
                NumAgeGroups = agNumberParticipants.Text.Length == 0 ? 3 : Convert.ToInt32(agNumberParticipants.Text),
                NumCustom = customNumberParticipants.Text.Length == 0 ? 3 : Convert.ToInt32(customNumberParticipants.Text),
                ExcludeOverallAG = overallExcludeAG.IsChecked == true,
                ExcludeOverallCustom = overallExcludeCustom.IsChecked == true,
                ExcludeAgeGroupsCustom = agExcludeCustom.IsChecked == true
            };
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Save clicked.");
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = "PDF (*.pdf)|*.pdf",
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value
            };
            AwardOptions options = GetOptions();
            List<string> divsToPrint = new List<string>();
            foreach (ListBoxItem divItem in DivisionsBox.SelectedItems)
            {
                if (divItem.Content.Equals("All"))
                {
                    divsToPrint = null;
                    break;
                }
                divsToPrint.Add(divItem.Content.ToString());
            }
            if (divsToPrint != null && divsToPrint.Count < 1)
            {
                divsToPrint = null;
            }
            if (saveFileDialog.ShowDialog() == true)
            {
                PdfDocumentRenderer renderer = new PdfDocumentRenderer();
                if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                {
                    renderer.Document = GetAwardsPrintableDocumentDistance(divsToPrint, options);
                }
                else
                {
                    MessageBox.Show("Award printing for time based races has not been implemented yet.");
                    return;
                }
                renderer.RenderDocument();
                try
                {
                    renderer.PdfDocument.Save(saveFileDialog.FileName);
                    MessageBox.Show("File saved.");
                }
                catch
                {
                    MessageBox.Show("Unable to save file.");
                }
            }
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Done clicked.");
            parent.LoadMainDisplay();
        }

        public void Search(string value, CancellationToken token) { }

        public void Show(PeopleType type) { }

        public void SortBy(SortType type) { }

        public void EditSelected() { }

        public void UpdateView()
        {
            customAgeGroups.Clear();
            foreach (AgeGroup age in database.GetAgeGroups(theEvent.Identifier, Constants.Timing.AGEGROUPS_CUSTOM_DIVISIONID))
            {
                customAgeGroups.Add(age);
            }
        }

        public void Closing() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        private Document GetAwardsPrintableDocumentTime(List<string> divisions, AwardOptions options)
        {
            Document document = PrintingInterface.CreateDocument(theEvent.YearCode, theEvent.Name, database.GetAppSetting(Constants.Settings.COMPANY_NAME).value);

            return document;
        }

        private Document GetAwardsPrintableDocumentDistance(List<string> divisions, AwardOptions options)
        {
            // Ensure we were given options
            if (options == null)
            {
                options = new AwardOptions();
            }
            // Get all participants for the race and categorize them by their event specific identifier.
            Dictionary<int, Participant> participantDictionary = database.GetParticipants(theEvent.Identifier).ToDictionary(x => x.EventSpecific.Identifier, x => x);
            // Get all results for the race.
            List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
            // Remove all results where we don't have the person's information.
            // and all results that are not finish results
            // TODO - Make anonymouse entries possible.
            results.RemoveAll(x => !participantDictionary.ContainsKey(x.EventSpecificId) || x.SegmentId != Constants.Timing.SEGMENT_FINISH);
            // Remove some participants if we don't want their division.
            if (divisions != null)
            {
                results.RemoveAll(x => !divisions.Contains(x.DivisionName));
            }
            // Separate them based upon division.
            Dictionary<string, List<TimeResult>> divisionResults = new Dictionary<string, List<TimeResult>>();
            foreach (TimeResult result in results)
            {
                if (!divisionResults.ContainsKey(result.DivisionName))
                {
                    divisionResults[result.DivisionName] = new List<TimeResult>();
                }
                divisionResults[result.DivisionName].Add(result);
            }
            // Get a list of all our age groups + our custom age groups
            Dictionary<int, AgeGroup> ageGroups = database.GetAgeGroups(theEvent.Identifier).ToDictionary(x => x.GroupId, x => x);
            // Add an age group for our unknown age people/
            ageGroups[Constants.Timing.TIMERESULT_DUMMYAGEGROUP] = new AgeGroup(theEvent.Identifier, Constants.Timing.COMMON_AGEGROUPS_DIVISIONID, 0, 3000);
            Dictionary<int, AgeGroup> customAgeGroups = new Dictionary<int, AgeGroup>();
            foreach (AgeGroup group in database.GetAgeGroups(theEvent.Identifier))
            {
                if (Constants.Timing.AGEGROUPS_CUSTOM_DIVISIONID == group.DivisionId)
                {
                    customAgeGroups[group.GroupId] = group;
                }
            }
            Dictionary<string, Division> divisionsDictionary = new Dictionary<string, Division>();
            foreach (Division div in database.GetDivisions(theEvent.Identifier))
            {
                divisionsDictionary[div.Name] = div;
            }
            // Create document to output.
            Document document = PrintingInterface.CreateDocument(theEvent.YearCode, theEvent.Name, database.GetAppSetting(Constants.Settings.COMPANY_NAME).value);
            foreach (string divName in divisionResults.Keys.OrderBy(i => i))
            {
                // Create a dictionary for storing lists of award winners based upon category.
                // this is either OVERALL, AG<GROUP_ID>, or CUSTOM<GROUP_ID>
                Dictionary<string, List<TimeResult>> divisionAwards = new Dictionary<string, List<TimeResult>>();
                // Create three lists of results for each type (OVERALL, AGEGROUPS, CUSTOM)
                divisionResults[divName].Sort(TimeResult.CompareByDivisionPlace);
                List<TimeResult> ageGroupResults = new List<TimeResult>(divisionResults[divName]);
                ageGroupResults.Sort(TimeResult.CompareByDivisionPlace);
                List<TimeResult> customResults = new List<TimeResult>(divisionResults[divName]);
                customResults.Sort(TimeResult.CompareByDivisionPlace);
                // Check the number of winners we need for overall and remove all but those from divisionResults[divName]
                // Sort them into a gender based dictionary
                Dictionary<string, List<TimeResult>> overallResultDictionary = new Dictionary<string, List<TimeResult>>();
                foreach (TimeResult result in divisionResults[divName])
                {
                    if (!overallResultDictionary.ContainsKey(result.Gender))
                    {
                        overallResultDictionary[result.Gender] = new List<TimeResult>();
                    }
                    if (overallResultDictionary[result.Gender].Count < options.NumOverall)
                    {
                        overallResultDictionary[result.Gender].Add(result);
                    }
                }
                // Remove all results from the other two lists if we think we should.
                if (options.ExcludeOverallAG)
                {
                    foreach (string key in overallResultDictionary.Keys)
                    {
                        ageGroupResults.RemoveAll(x => overallResultDictionary[key].Contains(x));
                    }
                }
                if (options.ExcludeOverallCustom)
                {
                    foreach (string key in overallResultDictionary.Keys)
                    {
                        customResults.RemoveAll(x => overallResultDictionary[key].Contains(x));
                    }
                }
                // Get results for each age group + gender into lists with a MAX NUMBER of entries
                Dictionary<(int, string), List<TimeResult>> ageGroupResultDictionary = new Dictionary<(int, string), List<TimeResult>>();
                foreach (TimeResult result in ageGroupResults)
                {
                    if (!ageGroupResultDictionary.ContainsKey((result.AgeGroupId, result.Gender)))
                    {
                        ageGroupResultDictionary[(result.AgeGroupId, result.Gender)] = new List<TimeResult>();
                    }
                    if (ageGroupResultDictionary[(result.AgeGroupId, result.Gender)].Count < options.NumAgeGroups)
                    {
                        ageGroupResultDictionary[(result.AgeGroupId, result.Gender)].Add(result);
                    }
                }
                // Remove everything from CustomResults if we were told to.
                if (options.ExcludeAgeGroupsCustom)
                {
                    foreach ((int, string) key in ageGroupResultDictionary.Keys)
                    {
                        customResults.RemoveAll(x => ageGroupResultDictionary[key].Contains(x));
                    }
                }
                // Process results for custom age groups.  This is similar to the others but sort of different.
                Dictionary<(int, string), List<TimeResult>> customResultDictionary = new Dictionary<(int, string), List<TimeResult>>();
                List<TimeResult> processed = new List<TimeResult>();
                foreach (AgeGroup group in customAgeGroups.Values)
                {
                    foreach (TimeResult result in customResults)
                    {
                        int age = participantDictionary[result.EventSpecificId].GetAge(theEvent.Date);
                        if (age >= group.StartAge && age <= group.EndAge)
                        {
                            processed.Add(result);
                            if (!customResultDictionary.ContainsKey((group.GroupId, result.Gender)))
                            {
                                customResultDictionary[(group.GroupId, result.Gender)] = new List<TimeResult>();
                            }
                            if (customResultDictionary[(group.GroupId, result.Gender)].Count < options.NumCustom)
                            {
                                customResultDictionary[(group.GroupId, result.Gender)].Add(result);
                            }
                        }
                    }
                    customResults.RemoveAll(x => processed.Contains(x));
                    processed.Clear();
                }
                Section section = PrintingInterface.SetupMargins(document.AddSection());
                HeaderFooter header = section.Headers.Primary;
                Paragraph curPara = header.AddParagraph(theEvent.Name);
                curPara.Style = "Heading1";
                curPara = header.AddParagraph("Age Group Results");
                curPara.Style = "Heading2";
                curPara = header.AddParagraph(theEvent.Date);
                curPara.Style = "Heading3";
                curPara = header.AddParagraph(divName);
                curPara.Style = "DivisionName";
                List<(string subheading, List<TimeResult> results)> maleResults = new List<(string subheading, List<TimeResult> results)>();
                List<(string subheading, List<TimeResult> results)> femaleResults = new List<(string subheading, List<TimeResult> results)>();
                // check if we're printing overall
                if (options.PrintOverall)
                {
                    foreach (string gender in overallResultDictionary.Keys)
                    {
                        if (gender.Equals("M", StringComparison.OrdinalIgnoreCase))
                        {
                            maleResults.Add((subheading: "Male Overall", results: overallResultDictionary[gender]));
                        }
                        else
                        {
                            femaleResults.Add((subheading: "Female Overall", results: overallResultDictionary[gender]));
                        }
                    }
                }
                if (options.PrintAgeGroups)
                {
                    IOrderedEnumerable<(int, string)> ageGroupKeys;
                    try
                    {
                        ageGroupKeys = ageGroupResultDictionary.Keys
                           .OrderBy(c => c.Item2).ThenBy(i => ageGroups[i.Item1].StartAge);
                    }
                    catch
                    {
                        ageGroupKeys = ageGroupResultDictionary.Keys.OrderBy(c => c.Item2);
                    }
                    foreach ((int AgeGroupId, string gender) in ageGroupKeys)
                    {
                        if (AgeGroupId != Constants.Timing.TIMERESULT_DUMMYAGEGROUP)
                        {
                            string subheading = string.Format("{0} {1} - {2}",
                                        gender.Equals("M", System.StringComparison.OrdinalIgnoreCase) ? "Male" : "Female",
                                        ageGroups[AgeGroupId].StartAge,
                                        ageGroups[AgeGroupId].EndAge);
                            if (ageGroups[AgeGroupId].LastGroup)
                            {
                                subheading = string.Format("{0} {1}+",
                                        gender.Equals("M", System.StringComparison.OrdinalIgnoreCase) ? "Male" : "Female",
                                        ageGroups[AgeGroupId].StartAge);
                            }
                            if (gender.Equals("M", StringComparison.OrdinalIgnoreCase))
                            {
                                maleResults.Add((subheading, results: ageGroupResultDictionary[(AgeGroupId, gender)]));
                            }
                            else
                            {
                                femaleResults.Add((subheading, results: ageGroupResultDictionary[(AgeGroupId, gender)]));
                            }

                        }
                    }
                }
                if (options.PrintCustom)
                {
                    IOrderedEnumerable<(int, string)> customKeys;
                    try
                    {
                        customKeys = customResultDictionary.Keys
                           .OrderBy(c => c.Item2).ThenBy(i => ageGroups[i.Item1].StartAge);
                    }
                    catch
                    {
                        customKeys = customResultDictionary.Keys.OrderBy(c => c.Item2);
                    }
                    foreach ((int AgeGroupId, string gender) in customKeys)
                    {
                        if (AgeGroupId != Constants.Timing.TIMERESULT_DUMMYAGEGROUP)
                        {
                            string subheading = string.Format("{0} {1} - {2} (Custom)",
                                        gender.Equals("M", System.StringComparison.OrdinalIgnoreCase) ? "Male" : "Female",
                                        ageGroups[AgeGroupId].StartAge,
                                        ageGroups[AgeGroupId].EndAge);
                            if (ageGroups[AgeGroupId].LastGroup)
                            {
                                subheading = string.Format("{0} {1}+ (Custom)",
                                        gender.Equals("M", System.StringComparison.OrdinalIgnoreCase) ? "Male" : "Female",
                                        ageGroups[AgeGroupId].StartAge);
                            }
                            if (gender.Equals("M", StringComparison.OrdinalIgnoreCase))
                            {
                                maleResults.Add((subheading, results: customResultDictionary[(AgeGroupId, gender)]));
                            }
                            else
                            {
                                femaleResults.Add((subheading, results: customResultDictionary[(AgeGroupId, gender)]));
                            }
                        }
                    }
                }
                foreach ((string subheading, List<TimeResult> res) in femaleResults)
                {
                    AddAwardSection(section, subheading, res, participantDictionary);
                }
                foreach ((string subheading, List<TimeResult> res) in maleResults)
                {
                    AddAwardSection(section, subheading, res, participantDictionary);
                }
            }
            return document;
        }

        private void AddAwardSection(Section section, string subheading, List<TimeResult> results, Dictionary<int, Participant> participantDictionary)
        {
            section.AddParagraph(subheading, "SubHeading");
            // Create a tabel to display the results.
            Table table = new Table();
            table.Borders.Width = 0.0;
            table.Rows.Alignment = RowAlignment.Center;
            // Create the rows we're displaying
            table.AddColumn(Unit.FromCentimeter(1));   // place
            table.AddColumn(Unit.FromCentimeter(1.2)); // bib
            table.AddColumn(Unit.FromCentimeter(5));   // name
            table.AddColumn(Unit.FromCentimeter(0.6)); // gender
            table.AddColumn(Unit.FromCentimeter(0.6)); // age
            table.AddColumn(Unit.FromCentimeter(2.3)); // gun time
            table.AddColumn(Unit.FromCentimeter(2.3)); // chip time
                                                       // add the header row
            Row row = table.AddRow();
            row.Style = "ResultsHeader";
            row.Cells[0].AddParagraph("Place");
            row.Cells[1].AddParagraph("Bib");
            row.Cells[2].AddParagraph("Name");
            row.Cells[2].Style = "ResultsHeaderName";
            row.Cells[3].AddParagraph("G");
            row.Cells[4].AddParagraph("Age");
            row.Cells[5].AddParagraph("Finish Gun");
            row.Cells[6].AddParagraph("Finish Chip");
            int place = 1;
            foreach (TimeResult result in results)
            {
                row = table.AddRow();
                row.Style = "ResultsRow";
                row.Cells[0].AddParagraph(place.ToString()); // Place
                row.Cells[1].AddParagraph(result.Bib.ToString()); // Bib
                row.Cells[2].AddParagraph(result.ParticipantName); // Name
                row.Cells[2].Style = "ResultsRowName";
                row.Cells[3].AddParagraph(result.Gender); // Gender
                row.Cells[4].AddParagraph(participantDictionary[result.EventSpecificId].Age(theEvent.Date)); // Age
                row.Cells[5].AddParagraph(result.Time.Substring(0, result.Time.Length - 2)); // Gun time
                row.Cells[6].AddParagraph(result.ChipTime.Substring(0, result.ChipTime.Length - 2)); // Chip time
                place++;
            }
            row = table.AddRow();
            section.Add(table);
        }

        private class AwardOptions
        {
            public bool PrintOverall { get; set; } = true;
            public int NumOverall { get; set; } = 3;
            // Exclude overall winners from age group awards.
            public bool ExcludeOverallAG { get; set; } = false;
            // Exclude overall winners from custom group awards.
            public bool ExcludeOverallCustom { get; set; } = false;
            public bool PrintAgeGroups { get; set; } = true;
            public int NumAgeGroups { get; set; } = 3;
            // Exclude winners in the Age Groups sections from custom sections.
            public bool ExcludeAgeGroupsCustom { get; set; } = false;
            public bool PrintCustom { get; set; } = true;
            public int NumCustom { get; set; } = 3;
        }
    }
}
 