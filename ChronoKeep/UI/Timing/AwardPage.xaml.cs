using Chronokeep.Database.SQLite;
using Chronokeep.Interfaces;
using Chronokeep.IO;
using Chronokeep.IO.HtmlTemplates.Printables;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages;
using Chronokeep.UI.UIObjects;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.Timing
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
                Log.E("UI.Timing.AwardPage", "Something went wrong and no proper event was returned.");
                return;
            }
            customGroupsListView.ItemsSource = customAgeGroups;
            List<Distance> distances = database.GetDistances(theEvent.Identifier);
            distances.Sort((x1, x2) => x1.Name.CompareTo(x2.Name));
            foreach (Distance d in distances)
            {
                DistancesBox.Items.Add(new ListBoxItem()
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
            Log.D("UI.Timing.AwardPage", "Add custom group clicked.");
            try
            {
                int start = Convert.ToInt32(startCustom.Text);
                int end = Convert.ToInt32(endCustom.Text);
                string custom = customNameBox.Text;
                if (custom == null)
                {
                    custom = "";
                }
                if (start > -1 || end < 101)
                {
                    database.AddAgeGroup(
                        new AgeGroup(
                            theEvent.Identifier,
                            Constants.Timing.AGEGROUPS_CUSTOM_DISTANCEID,
                            start,
                            end,
                            custom
                            ));
                    UpdateView();
                    startCustom.Text = "";
                    endCustom.Text = "";
                    customNameBox.Text = "";
                    startCustom.Focus();
                }
                else
                {
                    DialogBox.Show("Ages are not in the range of 0 to 100.");
                }
            }
            catch (Exception ex)
            {
                Log.E("UI.Timing.AwardPage", ex.Message);
                DialogBox.Show("Start or end age not specified.");
            }
        }

        private void DeleteCustom_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.AwardPage", "Deleting some entries... maybe.");
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
            Log.D("UI.Timing.AwardPage", "Print clicked.");
            System.Windows.Forms.PrintDialog printDialog = new System.Windows.Forms.PrintDialog
            {
                AllowSomePages = true,
                UseEXDialog = true
            };
            AwardOptions options = GetOptions();
            List<string> divsToPrint = new List<string>();
            foreach (ListBoxItem divItem in DistancesBox.SelectedItems)
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
                try
                {
                    // Printing is a very weird process that I would love to streamline... but printing is hard.
                    // Get two temp file names.
                    string tmpFile = Path.Combine(Path.GetTempPath(), "print_temp.html");
                    string tmpPdf = Path.Combine(Path.GetTempPath(), "print_pdf.pdf");
                    // Write the HTML file to a temp file because wkhtmltopdf requires a URI.
                    using StreamWriter streamwriter = new StreamWriter(File.Open(tmpFile, FileMode.Create));
                    streamwriter.Write(GetPrintableAwards(divsToPrint, options));
                    streamwriter.Close();
                    // Use wkhtmltopdf to convert the temp HTML file to a temp PDF file.
                    using Process create_pdf = new Process();
                    create_pdf.StartInfo.FileName = Path.Combine(Directory.GetCurrentDirectory(), "wkhtmltopdf.exe");
                    create_pdf.StartInfo.Arguments = $"-s A4 -B 30mm {tmpFile} {tmpPdf}";
                    create_pdf.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    create_pdf.StartInfo.UseShellExecute = true;
                    create_pdf.Start();
                    // Process shouldn't take more than 15 seconds, so wait for it to finish and kill it when done (or not done).
                    create_pdf.WaitForExit(15000);
                    create_pdf.Kill();
                    create_pdf.Close();
                    // Use ghostscript to print the temp PDF file.
                    using Process print_pdf = new Process();
                    print_pdf.StartInfo.FileName = Path.Combine(Directory.GetCurrentDirectory(), "gswin32.exe");
                    print_pdf.StartInfo.Arguments = $"-dPrinted -dBATCH -dNOPAUSE -dNOSAFER -dNumCopies=1 -sDEVICE=mswinpr2 {tmpPdf}";
                    print_pdf.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    print_pdf.StartInfo.UseShellExecute = true;
                    print_pdf.Start();
                    // wait for up to two minutes and make sure to kill the process
                    print_pdf.WaitForExit(120000);
                    print_pdf.Kill();
                    print_pdf.Close();
                    // remove temp files
                    File.Delete(tmpFile);
                    File.Delete(tmpPdf);
                    DialogBox.Show("Printing is a go.");
                }
                catch
                {
                    DialogBox.Show("Something went wrong when attempting to print.");
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
            Log.D("UI.Timing.AwardPage", "Save clicked.");
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = string.Format("{0} {1} Awards.{2}", theEvent.YearCode, theEvent.Name, "pdf"),
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value
            };
            AwardOptions options = GetOptions();
            List<string> divsToPrint = new List<string>();
            foreach (ListBoxItem divItem in DistancesBox.SelectedItems)
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
            if (options.PrintCustom != true && options.PrintAgeGroups != true && options.PrintOverall != true)
            {
                DialogBox.Show("No awards group selected to print/save.");
                return;
            }
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Write HTML to a temp file.
                    string tmpFile = Path.Combine(Path.GetTempPath(), "print_temp.html");
                    using StreamWriter streamwriter = new StreamWriter(File.Open(tmpFile, FileMode.Create));
                    streamwriter.Write(GetPrintableAwards(divsToPrint, options));
                    streamwriter.Close();
                    // Delete old file if it exists.
                    if (File.Exists(saveFileDialog.FileName))
                    {
                        File.Delete(saveFileDialog.FileName);
                    }
                    // Use wkhtmltopdf to convert our temp html file to a saved pdf file.
                    using Process create_pdf = new Process();
                    create_pdf.StartInfo.FileName = Path.Combine(Directory.GetCurrentDirectory(), "wkhtmltopdf.exe");
                    create_pdf.StartInfo.Arguments = $"-s A4 {tmpFile} {saveFileDialog.FileName}";
                    create_pdf.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    create_pdf.StartInfo.UseShellExecute = true;
                    create_pdf.Start();
                    // wait for it to exit then kill it, even if the wait timed out
                    create_pdf.WaitForExit(15000);
                    create_pdf.Kill();
                    create_pdf.Close();
                    // delete old file
                    File.Delete(tmpFile);
                    DialogBox.Show("File saved.");
                }
                catch (Exception ex)
                {
                    Log.E("UI.Timing.AwardPage", "Exception caught: " + ex.Message);
                    DialogBox.Show("Unable to save file.");
                }
            }
        }

        private string GetPrintableAwards(List<string> distances, AwardOptions options)
        {
            // Get all results for the race.
            List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
            // Remove all unknown participants.
            results.RemoveAll(x => x.Bib == Constants.Timing.CHIPREAD_DUMMYBIB);
            // Remove all from unselected divisions.
            if (distances != null)
            {
                results.RemoveAll(x => !distances.Contains(x.DistanceName));
            }
            // Remove all results that are not finish results.
            results.RemoveAll(x => x.SegmentId != Constants.Timing.SEGMENT_FINISH);
            // Remove all DNF results.
            results.RemoveAll(x => x.Status == Constants.Timing.TIMERESULT_STATUS_DNF);
            // If we're a time based event, exclude all but the last result
            if (theEvent.EventType == Constants.Timing.EVENT_TYPE_TIME)
            {
                Dictionary<string, TimeResult> lastResult = new Dictionary<string, TimeResult>();
                foreach (TimeResult individual in results)
                {
                    if (lastResult.ContainsKey(individual.ParticipantName))
                    {
                        if (lastResult[individual.ParticipantName].Occurrence < individual.Occurrence)
                        {
                            lastResult[individual.ParticipantName] = individual;
                        }
                    }
                    else
                    {
                        lastResult[individual.ParticipantName] = individual;
                    }
                }
                results = lastResult.Values.ToList();
            }
            results.Sort(TimeResult.CompareByDistancePlace);
            // This dictionary stores the list of results with the key being the distance and the header for the grouping (Overall, Age Group, Custom)
            Dictionary<string, Dictionary<string, List<TimeResult>>> resultsDictionary = new Dictionary<string, Dictionary<string, List<TimeResult>>>();
            // This dictionary keeps track of the number of individuals in an age group so we can choose to not print age group awards
            // and still exclude them from the results.
            Dictionary<(string, string), int> ageGroupCounter = new Dictionary<(string, string), int>();
            foreach (TimeResult result in results)
            {
                // Gather the gender and modify it to what we want for use in results.
                string gend = result.Gender;
                if (result.Gender == "Woman")
                {
                    gend = "Women";
                }
                else if (result.Gender == "Man")
                {
                    gend = "Men";
                }
                else if (result.Gender == "Not Specified")
                {
                    gend = "";
                }
                bool addedToAgeGroupResults = false;
                if (!resultsDictionary.ContainsKey(result.DistanceName))
                {
                    resultsDictionary[result.DistanceName] = new Dictionary<string, List<TimeResult>>();
                }
                // Get the overall results.
                if (result.Place <= options.NumOverall)
                {
                    // Check if we're printing the overall results.
                    if (options.PrintOverall == true)
                    {
                        if (!resultsDictionary[result.DistanceName].ContainsKey("Overall"))
                        {
                            resultsDictionary[result.DistanceName]["Overall"] = new List<TimeResult>();
                        }
                        resultsDictionary[result.DistanceName]["Overall"].Add(result);
                    }
                    // Check if we were told to exclude overall from age group awards.
                    // Also ensure we've been told to print age groups and that the person is in the age group results.
                    // The place check is easy here because we can check the result.Place value.
                    // Exclude any genders we don't know about.
                    if (options.ExcludeOverallAG == false
                        && result.Place <= options.NumAgeGroups
                        && gend != "")
                    {
                        string ageGroup = string.Format("{0} {1}", gend, result.AgeGroupName);
                        if (!ageGroupCounter.ContainsKey((result.DistanceName, ageGroup)))
                        {
                            ageGroupCounter[(result.DistanceName, ageGroup)] = 0;
                        }
                        if (options.PrintAgeGroups == true)
                        {
                            if (!resultsDictionary[result.DistanceName].ContainsKey(ageGroup))
                            {
                                resultsDictionary[result.DistanceName][ageGroup] = new List<TimeResult>();
                            }
                            resultsDictionary[result.DistanceName][ageGroup].Add(result);
                        }
                        ageGroupCounter[(result.DistanceName, ageGroup)]++;
                        addedToAgeGroupResults = true;
                    }
                    // This is almost the same as the age groups category.
                    // Check if told to exclude from custom results.
                    // and if we were told to print custom results
                    // exclude any unknown genders
                    // check if we were told to exclude age group winners from custom winners, if so only include ones we didn't add above
                    // this will exclude any that would have won an age group award even if we didn't actually print it
                    // this is the behavior we want and should work the same for overall as well
                    if (options.ExcludeOverallCustom == false
                        && options.PrintCustom == true
                        && gend != ""
                        && (options.ExcludeAgeGroupsCustom == false || addedToAgeGroupResults == false))
                    {
                        int age = result.Age(theEvent.Date);
                        foreach (AgeGroup group in customAgeGroups)
                        {
                            if (age >= group.StartAge && age <= group.EndAge)
                            {
                                string ageGroup = string.Format("{0} {1}", gend, group.PrettyName());
                                if (!resultsDictionary[result.DistanceName].ContainsKey(ageGroup))
                                {
                                    resultsDictionary[result.DistanceName][ageGroup] = new List<TimeResult>();
                                }
                                // only add to the results if we're under the number of results we can print
                                if (resultsDictionary[result.DistanceName][ageGroup].Count < options.NumCustom)
                                {
                                    resultsDictionary[result.DistanceName][ageGroup].Add(result);
                                }
                            }
                        }
                    }
                }
                else if (gend != "")
                {
                    // We're not in the overall results.
                    // Check for age groups.
                    string ageGroup = string.Format("{0} {1}", gend, result.AgeGroupName);
                    if (!resultsDictionary[result.DistanceName].ContainsKey(ageGroup))
                    {
                        resultsDictionary[result.DistanceName][ageGroup] = new List<TimeResult>();
                    }
                    // We're doing it this way so we can exclude people from custom if we want even if we don't print the age group.
                    if (!ageGroupCounter.ContainsKey((result.DistanceName, ageGroup)))
                    {
                        ageGroupCounter[(result.DistanceName, ageGroup)] = 0;
                    }
                    if (ageGroupCounter[(result.DistanceName, ageGroup)] < options.NumAgeGroups)
                    {
                        if (options.PrintAgeGroups == true)
                        {
                            resultsDictionary[result.DistanceName][ageGroup].Add(result);
                        }
                        ageGroupCounter[(result.DistanceName, ageGroup)]++;
                        addedToAgeGroupResults = true;
                    }
                    // Check for custom groups.
                    // Ensure we don't care about excluding age group winners, or they didn't actually win
                    if (options.PrintCustom == true && (options.ExcludeAgeGroupsCustom == false || addedToAgeGroupResults == false))
                    {
                        Log.D("UI.Timing.AwardPage", "Checking to add to custom award group.");
                        int age = result.Age(theEvent.Date);
                        foreach (AgeGroup group in customAgeGroups)
                        {
                            if (age >= group.StartAge && age <= group.EndAge)
                            {
                                ageGroup = string.Format("{0} {1}", gend, group.PrettyName());
                                if (!resultsDictionary[result.DistanceName].ContainsKey(ageGroup))
                                {
                                    resultsDictionary[result.DistanceName][ageGroup] = new List<TimeResult>();
                                }
                                // only add to the results if we're under the number of results we can print
                                if (resultsDictionary[result.DistanceName][ageGroup].Count < options.NumCustom)
                                {
                                    resultsDictionary[result.DistanceName][ageGroup].Add(result);
                                }
                            }
                        }
                    }
                }
            }
            // Collect all of the groups into lists according to their distance
            // We do this so we can sort them by age group.
            Dictionary<string, List<string>> distanceGroups = new Dictionary<string, List<string>>();
            foreach (string dist in resultsDictionary.Keys)
            {
                foreach (string group in resultsDictionary[dist].Keys)
                {
                    // only add to our list if they actually have results in them
                    if (resultsDictionary[dist][group].Count > 0)
                    {
                        if (!distanceGroups.ContainsKey(dist))
                        {
                            distanceGroups[dist] = new List<string>();
                        }
                        if (!distanceGroups[dist].Contains(group))
                        {
                            distanceGroups[dist].Add(group);
                        }
                    }
                }
            }
            // sort our lists
            foreach (string dist in distanceGroups.Keys)
            {
                distanceGroups[dist].Sort((x1, x2) => CompareGroups(x1, x2));
            }
            AwardsPrintable output = new AwardsPrintable(theEvent, distanceGroups, resultsDictionary);
            return output.TransformText();
        }

        public int CompareGroups(string group1, string group2)
        {
            if (group1 == "Overall")
            {
                Log.D("Test", "Overall found1");
                return -1;
            }
            if (group2 == "Overall")
            {
                Log.D("Test", "Overall found2");
                return 1;
            }
            string[] firstSplit1 = group1.Split(' ');
            string[] firstSplit2 = group2.Split(' ');
            if (firstSplit1.Length < 2 || firstSplit2.Length < 2)
            {
                return group1.CompareTo(group2);
            }
            // if genders are not equal, sort by gender
            if (firstSplit1[0] != firstSplit2[0])
            {
                return firstSplit1[0].CompareTo(firstSplit2[0]);
            }
            if (firstSplit1[1].Equals("Under", StringComparison.OrdinalIgnoreCase))
            {
                Log.D("Test", "Under found1");
                return -1;
            }
            if (firstSplit2[1].Equals("Under", StringComparison.OrdinalIgnoreCase))
            {
                Log.D("Test", "Under found2");
                return 1;
            }
            if (firstSplit1[1].Equals("Over", StringComparison.OrdinalIgnoreCase))
            {
                Log.D("Test", "Over found1");
                return 1;
            }
            if (firstSplit2[1].Equals("Over", StringComparison.OrdinalIgnoreCase))
            {
                Log.D("Test", "Over found2");
                return -1;
            }
            string[] secondSplit1 = firstSplit1[0].Split('-');
            string[] secondSplit2 = firstSplit2[0].Split('-');
            if (secondSplit1.Length < 2 || secondSplit2.Length < 2)
            {
                return firstSplit1[1].CompareTo(firstSplit2[1]);
            }
            int start1 = -1;
            int.TryParse(secondSplit1[0], out start1);
            int start2 = -1;
            int.TryParse(secondSplit2[0], out start2);
            if (start1 < 0 || start2 < 0)
            {
                return firstSplit1[1].CompareTo(firstSplit2[1]);
            }
            return start1.CompareTo(start2);
        }
        
        public void CancelableUpdateView(CancellationToken token) { }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.AwardPage", "Done clicked.");
            parent.LoadMainDisplay();
        }

        public void Show(PeopleType type) { }

        public void SortBy(SortType type) { }

        public void EditSelected() { }

        public void UpdateView()
        {
            customAgeGroups.Clear();
            foreach (AgeGroup age in database.GetAgeGroups(theEvent.Identifier, Constants.Timing.AGEGROUPS_CUSTOM_DISTANCEID))
            {
                customAgeGroups.Add(age);
            }
        }

        public void Closing() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

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
 