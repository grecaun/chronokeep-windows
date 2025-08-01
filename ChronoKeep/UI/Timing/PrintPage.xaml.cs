﻿using Chronokeep.Interfaces;
using Chronokeep.IO.HtmlTemplates.Printables;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages;
using Chronokeep.UI.UIObjects;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Chronokeep.UI.Timing
{
    /// <summary>
    /// Interaction logic for PrintPage.xaml
    /// </summary>
    public partial class PrintPage : ISubPage
    {
        TimingPage parent;
        IDBInterface database;
        Event theEvent;

        public PrintPage(TimingPage parent, IDBInterface database)
        {
            InitializeComponent();
            this.parent = parent;
            this.database = database;
            theEvent = database.GetCurrentEvent();

            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }

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
        }

        public void UpdateView() { }

        public void CancelableUpdateView(CancellationToken token) { }

        public void Search(CancellationToken token, string searchText) { }

        private string GetOverallPrintableDocument(List<string> distances)
        {
            // Get all results for the race
            List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
            // Remove all unknown participants
            results.RemoveAll(x => x.Bib == Constants.Timing.CHIPREAD_DUMMYBIB);
            // REMOVE SOME DEPENDING ON WHO THEY WANT
            if (distances != null)
            {
                results.RemoveAll(x => !distances.Contains(x.DistanceName));
            }
            // remove all segments that are not finish segments
            results.RemoveAll(x => x.SegmentId != Constants.Timing.SEGMENT_FINISH);
            // if we're a time based event, exclude all but the last result
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
            Dictionary<string, List<TimeResult>> distanceResults = new Dictionary<string, List<TimeResult>>();
            Dictionary<string, List<TimeResult>> dnfResultDictionary = new Dictionary<string, List<TimeResult>>();
            foreach (TimeResult result in results)
            {
                if (!distanceResults.ContainsKey(result.DistanceName))
                {
                    distanceResults[result.DistanceName] = new List<TimeResult>();
                }
                if (result.Status == Constants.Timing.TIMERESULT_STATUS_DNF)
                {
                    if (dnfResultDictionary.ContainsKey(result.DistanceName) == false)
                    {
                        dnfResultDictionary[result.DistanceName] = new List<TimeResult>();
                    }
                    dnfResultDictionary[result.DistanceName].Add(result);
                }
                else
                {
                    distanceResults[result.DistanceName].Add(result);
                }
            }
            foreach (string divName in distanceResults.Keys.OrderBy(i => i))
            {
                // get rid of all non-finish segments
                distanceResults[divName].RemoveAll(x => x.SegmentId != Constants.Timing.SEGMENT_FINISH);
                // sort by distance place
                distanceResults[divName].Sort(TimeResult.CompareByDistancePlace);
            }
            ResultsPrintableOverall output = new ResultsPrintableOverall(theEvent, distanceResults, dnfResultDictionary);
            return output.TransformText();
        }

        private string GetGenderPrintableDocument(List<string> distances)
        {
            // Get all finish results for the race
            List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
            // Remove all unknown participants
            results.RemoveAll(x => x.Bib == Constants.Timing.CHIPREAD_DUMMYBIB);
            // REMOVE SOME DEPENDING ON WHO THEY WANT
            if (distances != null)
            {
                results.RemoveAll(x => !distances.Contains(x.DistanceName));
            }
            // remove all segments that are not finish segments
            results.RemoveAll(x => x.SegmentId != Constants.Timing.SEGMENT_FINISH);
            // remove all results without a gender specified
            results.RemoveAll(x => x.Gender == "Not Specified");
            // if we're a time based event, exclude all but the last result
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
            // separate each grouping by distance, then by gender
            Dictionary<string, Dictionary<string, List<TimeResult>>> distanceResults = new Dictionary<string, Dictionary<string, List<TimeResult>>>();
            Dictionary<string, Dictionary<string, List<TimeResult>>> dnfResultsDictionary = new Dictionary<string, Dictionary<string, List<TimeResult>>>();
            foreach (TimeResult result in results)
            {
                if (!distanceResults.ContainsKey(result.DistanceName))
                {
                    distanceResults[result.DistanceName] = new Dictionary<string, List<TimeResult>>();
                }
                if (!distanceResults[result.DistanceName].ContainsKey(result.Gender))
                {
                    distanceResults[result.DistanceName][result.Gender] = new List<TimeResult>();
                }
                if (result.Status == Constants.Timing.TIMERESULT_STATUS_DNF)
                {
                    if (!dnfResultsDictionary.ContainsKey(result.DistanceName))
                    {
                        dnfResultsDictionary[result.DistanceName] = new Dictionary<string, List<TimeResult>>();
                    }
                    if (!dnfResultsDictionary[result.DistanceName].ContainsKey(result.Gender))
                    {
                        dnfResultsDictionary[result.DistanceName][result.Gender] = new List<TimeResult>();
                    }
                    dnfResultsDictionary[result.DistanceName][result.Gender].Add(result);
                }
                else
                {
                    distanceResults[result.DistanceName][result.Gender].Add(result);
                }
            }
            foreach (string divName in distanceResults.Keys.OrderBy(i => i))
            {
                foreach (string gender in distanceResults[divName].Keys)
                {
                    // get rid of non-finish results
                    distanceResults[divName][gender].RemoveAll(x => x.SegmentId != Constants.Timing.SEGMENT_FINISH);
                    // sort results
                    distanceResults[divName][gender].Sort(TimeResult.CompareByDistancePlace);
                }
            }
            ResultsPrintableGender output = new ResultsPrintableGender(theEvent, distanceResults, dnfResultsDictionary);
            return output.TransformText();
        }

        private string GetAgeGroupPrintableDocument(List<string> distances)
        {
            // Get all of the age groups for the race
            Dictionary<int, AgeGroup> ageGroups = database.GetAgeGroups(theEvent.Identifier).ToDictionary(x => x.GroupId, x => x);
            // Add an age group for our unknown age people/
            ageGroups[Constants.Timing.TIMERESULT_DUMMYAGEGROUP] = new AgeGroup(theEvent.Identifier, Constants.Timing.COMMON_AGEGROUPS_DISTANCEID, -1, 3000);
            // Get all finish results for the race
            List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
            // Remove all unknown participants
            results.RemoveAll(x => x.Bib == Constants.Timing.CHIPREAD_DUMMYBIB);
            // REMOVE SOME DEPENDING ON WHO THEY WANT
            if (distances != null)
            {
                results.RemoveAll(x => !distances.Contains(x.DistanceName));
            }
            // remove all segments that are not finish segments
            results.RemoveAll(x => x.SegmentId != Constants.Timing.SEGMENT_FINISH);
            // remove all results without a gender specified
            results.RemoveAll(x => x.Gender == "Not Specified");
            // if we're a time based event, exclude all but the last result
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
            Dictionary<string, Dictionary<(int, string), List<TimeResult>>> distanceResults = new Dictionary<string, Dictionary<(int, string), List<TimeResult>>>();
            Dictionary<string, Dictionary<(int, string), List<TimeResult>>> dnfResultsDictionary = new Dictionary<string, Dictionary<(int, string), List<TimeResult>>>();
            foreach (TimeResult result in results)
            {
                if (!distanceResults.ContainsKey(result.DistanceName))
                {
                    distanceResults[result.DistanceName] = new Dictionary<(int, string), List<TimeResult>>();
                }
                if (!distanceResults[result.DistanceName].ContainsKey((result.AgeGroupId, result.Gender)))
                {
                    distanceResults[result.DistanceName][(result.AgeGroupId, result.Gender)] = new List<TimeResult>();
                }
                if (result.Status == Constants.Timing.TIMERESULT_STATUS_DNF)
                {
                    if (!dnfResultsDictionary.ContainsKey(result.DistanceName))
                    {
                        dnfResultsDictionary[result.DistanceName] = new Dictionary<(int, string), List<TimeResult>>();
                    }
                    if (!dnfResultsDictionary[result.DistanceName].ContainsKey((result.AgeGroupId, result.Gender)))
                    {
                        dnfResultsDictionary[result.DistanceName][(result.AgeGroupId, result.Gender)] = new List<TimeResult>();
                    }
                    dnfResultsDictionary[result.DistanceName][(result.AgeGroupId, result.Gender)].Add(result);
                }
                else
                {
                    distanceResults[result.DistanceName][(result.AgeGroupId, result.Gender)].Add(result);
                }
            }
            foreach (string divName in distanceResults.Keys.OrderBy(i => i))
            {
                foreach ((int ag, string gender) in distanceResults[divName].Keys)
                {
                    // get rid of non-finish results
                    distanceResults[divName][(ag, gender)].RemoveAll(x => x.SegmentId != Constants.Timing.SEGMENT_FINISH);
                    // sort results
                    distanceResults[divName][(ag, gender)].Sort(TimeResult.CompareByDistancePlace);
                }
            }
            ResultsPrintableAgeGroup output = new ResultsPrintableAgeGroup(theEvent, distanceResults, dnfResultsDictionary, ageGroups);
            return output.TransformText();
        }

        public void Show(PeopleType type) { }

        public void SortBy(SortType type) { }

        public void Location(string location) { }

        public void EditSelected() {}

        public void Closing() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            parent.LoadMainDisplay();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.PrintPage", "All times - print clicked.");
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
            string HTML_String = "";
            if (PlacementType.SelectedIndex == 0)
            {
                HTML_String = GetOverallPrintableDocument(divsToPrint);
            }
            else if (PlacementType.SelectedIndex == 1)
            {
                HTML_String = GetGenderPrintableDocument(divsToPrint);
            }
            else if (PlacementType.SelectedIndex == 2)
            {
                HTML_String = GetAgeGroupPrintableDocument(divsToPrint);
            }
            else
            {
                DialogBox.Show("Please select a type.");
                return;
            }
            try
            {
                // Printing is a very weird process that I would love to streamline... but printing is hard.
                // Get two temp file names.
                string tmpFile = Path.Combine(Path.GetTempPath(), "print_temp.html");
                string tmpPdf = Path.Combine(Path.GetTempPath(), "print_pdf.pdf");
                // Write the HTML file to a temp file because wkhtmltopdf requires a URI.
                using StreamWriter streamwriter = new StreamWriter(File.Open(tmpFile, FileMode.Create));
                streamwriter.Write(HTML_String);
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

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.PrintPage", "All times - save clicked.");
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = "PDF (*.pdf)|*.pdf",
                FileName = string.Format("{0} {1} Results.{2}", theEvent.YearCode, theEvent.Name, "pdf"),
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value
            };
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
            if (saveFileDialog.ShowDialog() == true)
            {
                string HTML_String = "";
                if (PlacementType.SelectedIndex == 0)
                {
                    HTML_String = GetOverallPrintableDocument(divsToPrint);
                }
                else if (PlacementType.SelectedIndex == 1)
                {
                    HTML_String = GetGenderPrintableDocument(divsToPrint);
                }
                else if (PlacementType.SelectedIndex == 2)
                {
                    HTML_String  = GetAgeGroupPrintableDocument(divsToPrint);
                }
                else
                {
                    DialogBox.Show("Please select a type.");
                    return;
                }
                try
                {
                    // Write HTML to a temp file.
                    string tmpFile = Path.Combine(Path.GetTempPath(), "print_temp.html");
                    using StreamWriter streamwriter = new StreamWriter(File.Open(tmpFile, FileMode.Create));
                    streamwriter.Write(HTML_String);
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
                catch
                {
                    DialogBox.Show("Unable to save file.");
                }
            }
        }
    }
}
