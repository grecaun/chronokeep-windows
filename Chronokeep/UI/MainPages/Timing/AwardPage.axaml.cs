using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.IO.HtmlTemplates.Printables;
using Chronokeep.Objects;
using Chronokeep.UI.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace Chronokeep.UI.MainPages.Timing;

public partial class AwardPage : UserControl, ISubPage
{
    private readonly IDBInterface database;
    private readonly TimingPage parent;
    private readonly Event? theEvent;

    private readonly ObservableCollection<AgeGroup> customAgeGroups = [];

    [GeneratedRegex("[^0-9]")]
    private static partial Regex AllowedChars();

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
            if (d.LinkedDistance <= 0)
            {
                DistancesBox.Items.Add(new ListBoxItem()
                {
                    Content = d.Name
                });
            }
        }
        parent.SetReaders([], false);
        UpdateView();
    }

    private AwardOptions GetOptions()
    {
        return new()
        {
            PrintOverall = overallYes.IsChecked == true,
            PrintAgeGroups = agYes.IsChecked == true,
            PrintCustom = customYes.IsChecked == true,
            NumOverall = overallNumberParticipants.Text!.Length == 0 ? 3 : Convert.ToInt32(overallNumberParticipants.Text),
            NumAgeGroups = agNumberParticipants.Text!.Length == 0 ? 3 : Convert.ToInt32(agNumberParticipants.Text),
            NumCustom = customNumberParticipants.Text!.Length == 0 ? 3 : Convert.ToInt32(customNumberParticipants.Text),
            ExcludeOverallAG = overallExcludeAG.IsChecked == true,
            ExcludeOverallCustom = overallExcludeCustom.IsChecked == true,
            ExcludeAgeGroupsCustom = agExcludeCustom.IsChecked == true
        };
    }

    private void IsNumber(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
        e.Handled = AllowedChars().IsMatch(e.Text!);
    }

    private string GetPrintableAwards(List<string> distances, AwardOptions options)
    {
        // Get all results for the race.
        List<TimeResult> results = database.GetTimingResults(theEvent!.Identifier);
        // Remove all unknown participants.
        results.RemoveAll(x => x.Bib == Constants.Timing.CHIPREAD_DUMMYBIB);
        results.RemoveAll(x => x.DistanceName.Length < 1);
        // Remove all from unselected divisions.
        if (distances.Count > 0)
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
            Dictionary<string, TimeResult> lastResult = [];
            foreach (TimeResult individual in results)
            {
                if (lastResult.TryGetValue(individual.ParticipantName, out TimeResult? oResult))
                {
                    if (oResult.Occurrence < individual.Occurrence)
                    {
                        lastResult[individual.ParticipantName] = individual;
                    }
                }
                else
                {
                    lastResult[individual.ParticipantName] = individual;
                }
            }
            results = [.. lastResult.Values];
        }
        results.Sort(TimeResult.CompareByDistancePlace);
        // This dictionary stores the list of results with the key being the distance and the header for the grouping (Overall, Age Group, Custom)
        Dictionary<string, Dictionary<string, List<TimeResult>>> resultsDictionary = [];
        // This dictionary keeps track of the number of individuals in an age group so we can choose to not print age group awards
        // and still exclude them from the results.
        Dictionary<(string, string), int> ageGroupCounter = [];
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
            if (!resultsDictionary.TryGetValue(result.DistanceName, out Dictionary<string, List<TimeResult>>? distResultsDict))
            {
                distResultsDict = [];
                resultsDictionary[result.DistanceName] = distResultsDict;
            }
            // Get the overall (gender) results.
            if (result.GenderPlace <= options.NumOverall)
            {
                // Check if we're printing the overall results.
                if (options.PrintOverall == true)
                {
                    if (!distResultsDict.TryGetValue(gend, out List<TimeResult>? ovResults))
                    {
                        ovResults = [];
                        distResultsDict[gend] = ovResults;
                    }
                    ovResults.Add(result);
                }
                // Check if we were told to exclude overall from age group awards.
                // Also ensure we've been told to print age groups and that the person is in the age group results.
                // The place check is easy here because we can check the result.GenderPlace value.
                // Exclude any genders we don't know about.
                if (options.ExcludeOverallAG == false
                    && result.GenderPlace <= options.NumAgeGroups
                    && gend != "")
                {
                    string ageGroup = string.Format("{0} {1}", gend, result.AgeGroupName);
                    if (!ageGroupCounter.TryGetValue((result.DistanceName, ageGroup), out int oAGCount))
                    {
                        oAGCount = 0;
                    }
                    if (options.PrintAgeGroups == true)
                    {
                        if (!distResultsDict.TryGetValue(ageGroup, out List<TimeResult>? oResList))
                        {
                            oResList = [];
                            distResultsDict[ageGroup] = oResList;
                        }
                        oResList.Add(result);
                    }
                    ageGroupCounter[(result.DistanceName, ageGroup)] = oAGCount + 1;
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
                            if (!distResultsDict.TryGetValue(ageGroup, out List<TimeResult>? oResList))
                            {
                                oResList = [];
                                distResultsDict[ageGroup] = oResList;
                            }
                            // only add to the results if we're under the number of results we can print
                            if (oResList.Count < options.NumCustom)
                            {
                                oResList.Add(result);
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
                if (!distResultsDict.TryGetValue(ageGroup, out List<TimeResult>? oResList))
                {
                    oResList = [];
                    distResultsDict[ageGroup] = oResList;
                }
                // We're doing it this way so we can exclude people from custom if we want even if we don't print the age group.
                if (!ageGroupCounter.TryGetValue((result.DistanceName, ageGroup), out int oAGCount))
                {
                    oAGCount = 0;
                }
                if (oAGCount < options.NumAgeGroups)
                {
                    if (options.PrintAgeGroups == true)
                    {
                        oResList.Add(result);
                    }
                    ageGroupCounter[(result.DistanceName, ageGroup)] = oAGCount + 1;
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
                            // only add to the results if we're under the number of results we can print
                            if (oResList.Count < options.NumCustom)
                            {
                                oResList.Add(result);
                            }
                        }
                    }
                }
            }
        }
        // Collect all of the groups into lists according to their distance
        // We do this so we can sort them by age group.
        Dictionary<string, List<string>> distanceGroups = [];
        foreach (string dist in resultsDictionary.Keys)
        {
            Dictionary<string, List<TimeResult>> distResultsDictionary = resultsDictionary[dist];
            foreach (string group in distResultsDictionary.Keys)
            {
                // only add to our list if they actually have results in them
                if (distResultsDictionary[group].Count > 0)
                {
                    if (!distanceGroups.TryGetValue(dist, out List<string>? distGroupList))
                    {
                        distGroupList = [];
                        distanceGroups[dist] = distGroupList;
                    }
                    if (!distGroupList.Contains(group))
                    {
                        distGroupList.Add(group);
                    }
                }
            }
        }
        // sort our lists
        foreach (string dist in distanceGroups.Keys)
        {
            distanceGroups[dist].Sort((x1, x2) => CompareGroups(x1, x2));
        }
        AwardsPrintable output = new(theEvent, distanceGroups, resultsDictionary);
        return output.TransformText();
    }

    public static int CompareGroups(string group1, string group2)
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
        bool sOneOkay = int.TryParse(secondSplit1[0], out int start1);
        bool sTwoOkay = int.TryParse(secondSplit2[0], out int start2);
        if (!sOneOkay || !sTwoOkay)
        {
            return firstSplit1[1].CompareTo(firstSplit2[1]);
        }
        return start1.CompareTo(start2);
    }

    public void CancelableUpdateView(CancellationToken token) { }

    public void Search(CancellationToken token, string searchText) { }

    public void Show(PeopleType type) { }

    public void SortBy(SortType type) { }

    public void Location(string location) { }

    public void EditSelected() { }

    public void UpdateView()
    {
        customAgeGroups.Clear();
        foreach (AgeGroup age in database.GetAgeGroups(theEvent!.Identifier, Constants.Timing.AGEGROUPS_CUSTOM_DISTANCEID))
        {
            customAgeGroups.Add(age);
        }
    }

    public void Closing() { }

    public void Keyboard_Ctrl_A() { }

    public void Keyboard_Ctrl_S() { }

    public void Keyboard_Ctrl_Z() { }

    public void Reader(string reader) { }

    private void AddCustom_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.AwardPage", "Add custom group clicked.");
        try
        {
            int start = Convert.ToInt32(startCustom.Text);
            int end = Convert.ToInt32(endCustom.Text);
            string custom = customNameBox.Text!;
            custom ??= "";
            if (start > -1 || end < 101)
            {
                database.AddAgeGroup(
                    new(theEvent!.Identifier,
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

    private void DeleteCustom_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.AwardPage", "Deleting some entries... maybe.");
        List<AgeGroup> items = [];
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

    private async void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.AwardPage", "Save clicked.");
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            IStorageFolder? startingFolder;
            try
            {
                startingFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri(database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR)!.Value));
            }
            catch
            {
                startingFolder = null;
            }
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                FileTypeChoices = [Utils.PDFType],
                SuggestedFileName = string.Format("{0}-{1}-Awards.{2}", theEvent!.YearCode, theEvent.Name, "pdf").Replace(' ', '-'),
                SuggestedStartLocation = startingFolder,
            });
            AwardOptions options = GetOptions();
            List<string> divsToPrint = [];
            if (DistancesBox.SelectedItems != null)
            {
                foreach (object? divItem in DistancesBox.SelectedItems)
                {
                    if (divItem is ListBoxItem div && div.Content != null)
                    {
                        if (div.Content.Equals("All"))
                        {
                            divsToPrint.Clear();
                            break;
                        }
                        divsToPrint.Add(div.Content.ToString()!);
                    }
                }
            }
            if (options.PrintCustom != true && options.PrintAgeGroups != true && options.PrintOverall != true)
            {
                DialogBox.Show("No awards group selected to print/save.");
                return;
            }
            if (file is not null)
            {
                try
                {
                    string HTML_String = GetPrintableAwards(divsToPrint, options);
                    string weasyName;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        weasyName = Path.Combine(Directory.GetCurrentDirectory(), "weasyprint.exe");
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        weasyName = "weasyprint";
                        using Process test_weasy = new();
                        test_weasy.StartInfo.FileName = "which";
                        test_weasy.StartInfo.Arguments = weasyName;
                        test_weasy.Start();
                        await test_weasy.WaitForExitAsync();
                        test_weasy.Close();
                        if (test_weasy.ExitCode != 0)
                        {
                            DialogBox.Show("This function requires Weasyprint to function. Please install it and try again.",
                                "https://doc.courtbouillon.org/weasyprint/stable/first_steps.html");
                            return;
                        }
                    }
                    else
                    {
                        DialogBox.Show("Operating System detected does not support this function currently.");
                        return;
                    }
                    // Write HTML to a temp file.
                    string tmpFile = Path.Combine(Path.GetTempPath(), "print_temp.html");
                    using StreamWriter streamwriter = new(File.Open(tmpFile, FileMode.Create));
                    streamwriter.Write(HTML_String);
                    streamwriter.Close();
                    // Delete old file if it exists.
                    var filePath = file.TryGetLocalPath()!.Replace(' ', '-');
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    // Use weasyprint to convert our temp html file to a saved pdf file.
                    using Process create_pdf = new();
                    create_pdf.StartInfo.FileName = weasyName;
                    create_pdf.StartInfo.Arguments = $" {tmpFile} {filePath}";
                    create_pdf.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    create_pdf.Start();
                    // wait for it to exit then kill it, even if the wait timed out
                    await create_pdf.WaitForExitAsync();
                    create_pdf.Close();
                    // delete old file
                    File.Delete(tmpFile);
                    DialogBox.Show("File saved.");
                }
                catch
                {
                    DialogBox.Show("Unable to save file.");
                }
            }
        }
    }

    private void DoneButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.AwardPage", "Done clicked.");
        parent.LoadMainDisplay();
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