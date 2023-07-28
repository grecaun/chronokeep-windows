using Chronokeep.Interfaces;
using Chronokeep.UI.IO;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Chronokeep.UI.Export
{
    /// <summary>
    /// Interaction logic for ExportResults.xaml
    /// </summary>
    public partial class ExportResults : Window
    {
        IMainWindow window;
        IDBInterface database;
        Event theEvent;

        bool noOpen = false;

        int maxNumSegments;
        List<string> commonHeaders = new List<string>
        {
            "Place", "Age Group Place", "Gender Place",
            "Bib", "Distance", "Status", "First", "Last", "Birthday",
            "Age", "Gender", "Start", "Street", "Apartment",
            "City", "State", "Zip", "Country", "Mobile", "Email", "Parent", "Comments",
            "Other", "Owes", "Emergency Contact Name", "Emergency Contact Phone",
            "Anonymous"
        };
        List<string> distanceHeaders = new List<string>
        {
            "Gun Finish", "Chip Finish"
        };
        List<string> timeHeaders = new List<string>
        {
            "Laps Completed", "Ellapsed Time (Gun)", "Ellapsed Time (Chip)"
        };

        public bool SetupError()
        {
            return noOpen;
        }

        public ExportResults(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier == -1)
            {
                noOpen = true;
                return;
            }
            // Check if we're distance based or time based
            if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
            {
                commonHeaders.InsertRange(10, distanceHeaders);
                // Get the maximum number of segments.
                // if greater than 0, add (SEGMENT 1...X GUN TIME, SEGMENT 1...X
                // CHIP TIME and SEGMENT 1...X NAME) to the list of common headers
                maxNumSegments = database.GetMaxSegments(theEvent.Identifier);
                if (maxNumSegments > 0)
                {
                    // Go backwards so we don't have to recalculate where the insert is each lap
                    for (int i = maxNumSegments; i > 0; i--)
                    {
                        commonHeaders.Insert(10, string.Format("Segment {0} Chip Time", i));
                        commonHeaders.Insert(10, string.Format("Segment {0} Gun Time", i));
                    }
                    // then do it again so we can add to the end in the right order
                    for (int i = 1; i <= maxNumSegments; i++)
                    {
                        commonHeaders.Add(string.Format("Segment {0} Name", i));
                    }
                }
            }
            else // Time based
            {
                commonHeaders.InsertRange(10, timeHeaders);
                // Remove "Chip Finish" and "Gun Finish" from the headers list.
                commonHeaders.Remove("Chip Finish");
                commonHeaders.Remove("");
                // Get the maximum number of laps a person completed.
                // if greater than 0, add LAP 1...X to the list of common headers
                maxNumSegments = 0;
                foreach (TimeResult result in database.GetSegmentTimes(theEvent.Identifier, Constants.Timing.SEGMENT_FINISH))
                {
                    maxNumSegments = result.Occurrence > maxNumSegments ? result.Occurrence : maxNumSegments;
                }
                for (int i = maxNumSegments; i > 0; i--)
                {
                    commonHeaders.Insert(10, string.Format("Lap {0}", i));
                }
            }
            foreach (string name in commonHeaders)
            {
                headersList.Items.Add(new AHeaderBox(name));
            }
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Export.ExportResults", "Done clicked.");
            List<string> headersToOutput = new List<string>();
            Dictionary<string, int> headerIndex = new Dictionary<string, int>();
            foreach (AHeaderBox headerBox in headersList.Items)
            {
                if (headerBox.Include.IsChecked == true)
                {
                    headersToOutput.Add(headerBox.NameValue);
                }
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel File (*.xlsx,*xls)|*.xlsx;*xls|CSV (*.csv)|*.csv",
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                // write to file
                List<Participant> participants = database.GetParticipants(theEvent.Identifier);
                List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
                //results.RemoveAll(x => x.EventSpecificId == Constants.Timing.TIMERESULT_DUMMYPERSON);
                results.Sort(TimeResult.CompareBySystemTime);
                // Key is BIB -- Using BIB here instead of event specific because we want to know about unknown runners.
                Dictionary<int, List<TimeResult>> resultDictionary = new Dictionary<int, List<TimeResult>>();
                Dictionary<int, bool> outputDictionary = new Dictionary<int, bool>();
                // (Bib, Occurence) - for Time Based Race exporting.
                Dictionary<(int, int), TimeResult> occurrenceResultDictionary = new Dictionary<(int, int), TimeResult>();
                int maxLaps = 0;
                foreach (TimeResult result in results)
                {
                    if (!resultDictionary.ContainsKey(result.Bib))
                    {
                        resultDictionary[result.Bib] = new List<TimeResult>();
                    }
                    resultDictionary[result.Bib].Add(result);
                    if (result.SegmentId == Constants.Timing.SEGMENT_FINISH)
                    {
                        occurrenceResultDictionary[(result.Bib, result.Occurrence)] = result;
                        maxLaps = result.Occurrence > maxLaps ? result.Occurrence : maxLaps;
                    }
                    outputDictionary[result.Bib] = false;
                }
                string[] headers = new string[headersToOutput.Count];
                foreach (string header in headersToOutput)
                {
                    headerIndex[header] = headersToOutput.IndexOf(header);
                    headers[headerIndex[header]] = header;
                }
                List<object[]> data = new List<object[]>();
                // Output all known participants
                foreach (Participant participant in participants)
                {
                    outputDictionary[participant.Bib] = true;
                    object[] line = new object[headersToOutput.Count];
                    if (headerIndex.ContainsKey("Bib"))
                    {
                        line[headerIndex["Bib"]] = participant.Bib;
                    }
                    if (headerIndex.ContainsKey("Distance"))
                    {
                        line[headerIndex["Distance"]] = participant.Distance;
                    }
                    if (headerIndex.ContainsKey("Status"))
                    {
                        line[headerIndex["Status"]] = participant.EventSpecific.StatusStr;
                    }
                    if (headerIndex.ContainsKey("First"))
                    {
                        line[headerIndex["First"]] = participant.FirstName;
                    }
                    if (headerIndex.ContainsKey("Last"))
                    {
                        line[headerIndex["Last"]] = participant.LastName;
                    }
                    if (headerIndex.ContainsKey("Birthday"))
                    {
                        line[headerIndex["Birthday"]] = participant.Birthdate;
                    }
                    if (headerIndex.ContainsKey("Age"))
                    {
                        line[headerIndex["Age"]] = participant.Age(theEvent.Date);
                    }
                    if (headerIndex.ContainsKey("Gender"))
                    {
                        line[headerIndex["Gender"]] = participant.Gender;
                    }
                    if (headerIndex.ContainsKey("Street"))
                    {
                        line[headerIndex["Street"]] = participant.Street;
                    }
                    if (headerIndex.ContainsKey("Apartment"))
                    {
                        line[headerIndex["Apartment"]] = participant.Street2;
                    }
                    if (headerIndex.ContainsKey("City"))
                    {
                        line[headerIndex["City"]] = participant.City;
                    }
                    if (headerIndex.ContainsKey("State"))
                    {
                        line[headerIndex["State"]] = participant.State;
                    }
                    if (headerIndex.ContainsKey("Zip"))
                    {
                        line[headerIndex["Zip"]] = participant.Zip;
                    }
                    if (headerIndex.ContainsKey("Country"))
                    {
                        line[headerIndex["Country"]] = participant.Country;
                    }
                    if (headerIndex.ContainsKey("Mobile"))
                    {
                        line[headerIndex["Mobile"]] = participant.Mobile;
                    }
                    if (headerIndex.ContainsKey("Email"))
                    {
                        line[headerIndex["Email"]] = participant.Email;
                    }
                    if (headerIndex.ContainsKey("Parent"))
                    {
                        line[headerIndex["Parent"]] = participant.Parent;
                    }
                    if (headerIndex.ContainsKey("Comments"))
                    {
                        line[headerIndex["Comments"]] = participant.Comments;
                    }
                    if (headerIndex.ContainsKey("Other"))
                    {
                        line[headerIndex["Other"]] = participant.Other;
                    }
                    if (headerIndex.ContainsKey("Owes"))
                    {
                        line[headerIndex["Owes"]] = participant.Owes;
                    }
                    if (headerIndex.ContainsKey("Emergency Contact Name"))
                    {
                        line[headerIndex["Emergency Contact Name"]] = participant.ECName;
                    }
                    if (headerIndex.ContainsKey("Emergency Contact Phone"))
                    {
                        line[headerIndex["Emergency Contact Phone"]] = participant.ECPhone;
                    }
                    if (headerIndex.ContainsKey("Anonymous"))
                    {
                        line[headerIndex["Anonymous"]] = participant.PrettyAnonymous;
                    }
                    if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                    {
                        if (resultDictionary.ContainsKey(participant.EventSpecific.Bib))
                        {
                            int segmentNum = 1;
                            foreach (TimeResult result in resultDictionary[participant.EventSpecific.Bib])
                            {
                                if (Constants.Timing.SEGMENT_START == result.SegmentId)
                                {
                                    if (headerIndex.ContainsKey("Start"))
                                    {
                                        line[headerIndex["Start"]] = result.Time;
                                    }
                                }
                                else if (Constants.Timing.SEGMENT_FINISH == result.SegmentId)
                                {
                                    if (headerIndex.ContainsKey("Place"))
                                    {
                                        line[headerIndex["Place"]] = result.Place == -1 ? "" : result.Place;
                                    }
                                    if (headerIndex.ContainsKey("Age Group Place"))
                                    {
                                        line[headerIndex["Age Group Place"]] = result.AgePlace == -1 ? "" : result.AgePlace;
                                    }
                                    if (headerIndex.ContainsKey("Gender Place"))
                                    {
                                        line[headerIndex["Gender Place"]] = result.GenderPlace == -1 ? "" : result.GenderPlace;
                                    }
                                    if (headerIndex.ContainsKey("Chip Finish"))
                                    {
                                        line[headerIndex["Chip Finish"]] = result.ChipTime;
                                    }
                                    if (headerIndex.ContainsKey("Gun Finish"))
                                    {
                                        line[headerIndex["Gun Finish"]] = result.Time;
                                    }
                                }
                                else if (Constants.Timing.SEGMENT_NONE != result.SegmentId)
                                {
                                    string key = string.Format("Segment {0} Chip Time", segmentNum);
                                    if (headerIndex.ContainsKey(key))
                                    {
                                        line[headerIndex[key]] = result.ChipTime;
                                    }
                                    key = string.Format("Segment {0} Gun Time", segmentNum);
                                    if (headerIndex.ContainsKey(key))
                                    {
                                        line[headerIndex[key]] = result.Time;
                                    }
                                    key = string.Format("Segment {0} Name", segmentNum++);
                                    if (headerIndex.ContainsKey(key))
                                    {
                                        line[headerIndex[key]] = result.SegmentName;
                                    }
                                }
                            }
                        }
                    }
                    else // Time Based
                    {
                        int finalLap = -1;
                        if (headerIndex.ContainsKey("Start") && occurrenceResultDictionary.ContainsKey((participant.EventSpecific.Bib, 0)))
                        {
                            line[headerIndex["Start"]] = occurrenceResultDictionary[(participant.EventSpecific.Bib, 0)].Time;
                        }
                        for (int i=1; i<=maxLaps; i++)
                        {
                            string key = string.Format("Lap {0}", i);
                            if (occurrenceResultDictionary.ContainsKey((participant.EventSpecific.Bib, i)))
                            {
                                finalLap = i;
                                if (headerIndex.ContainsKey(key))
                                {
                                    line[headerIndex[key]] = occurrenceResultDictionary[(participant.EventSpecific.Bib, i)].LapTime;
                                }
                            }
                        }
                        if (occurrenceResultDictionary.ContainsKey((participant.EventSpecific.Bib, finalLap)))
                        {
                            if (headerIndex.ContainsKey("Place"))
                            {
                                line[headerIndex["Place"]] = occurrenceResultDictionary[(participant.EventSpecific.Bib, finalLap)].Place;
                            }
                            if (headerIndex.ContainsKey("Age Group Place"))
                            {
                                line[headerIndex["Age Group Place"]] = occurrenceResultDictionary[(participant.EventSpecific.Bib, finalLap)].AgePlace;
                            }
                            if (headerIndex.ContainsKey("Gender Place"))
                            {
                                line[headerIndex["Gender Place"]] = occurrenceResultDictionary[(participant.EventSpecific.Bib, finalLap)].GenderPlace;
                            }
                            if (headerIndex.ContainsKey("Laps Completed"))
                            {
                                line[headerIndex["Laps Completed"]] = occurrenceResultDictionary[(participant.EventSpecific.Bib, finalLap)].Occurrence;
                            }
                            if (headerIndex.ContainsKey("Ellapsed Time (Gun)"))
                            {
                                line[headerIndex["Ellapsed Time (Gun)"]] = occurrenceResultDictionary[(participant.EventSpecific.Bib, finalLap)].Time;
                            }
                            if (headerIndex.ContainsKey("Ellapsed Time (Chip)"))
                            {
                                line[headerIndex["Ellapsed Time (Chip)"]] = occurrenceResultDictionary[(participant.EventSpecific.Bib, finalLap)].ChipTime;
                            }
                        }
                    }
                    data.Add(line);
                }
                // Add data for unknown runners
                foreach (int bib in outputDictionary.Keys)
                {
                    if (!outputDictionary[bib] && bib > 0)
                    {
                        object[] line = new object[headersToOutput.Count];
                        if (headerIndex.ContainsKey("Bib"))
                        {
                            line[headerIndex["Bib"]] = bib;
                        }
                        if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                        {
                            if (resultDictionary.ContainsKey(bib))
                            {
                                int segmentNum = 1;
                                foreach (TimeResult result in resultDictionary[bib])
                                {
                                    if (Constants.Timing.SEGMENT_START == result.SegmentId)
                                    {
                                        if (headerIndex.ContainsKey("Start"))
                                        {
                                            line[headerIndex["Start"]] = result.Time;
                                        }
                                    }
                                    else if (Constants.Timing.SEGMENT_FINISH == result.SegmentId)
                                    {
                                        if (headerIndex.ContainsKey("Place"))
                                        {
                                            line[headerIndex["Place"]] = result.Place == -1 ? "" : result.Place;
                                        }
                                        if (headerIndex.ContainsKey("Age Group Place"))
                                        {
                                            line[headerIndex["Age Group Place"]] = result.AgePlace == -1 ? "" : result.AgePlace;
                                        }
                                        if (headerIndex.ContainsKey("Gender Place"))
                                        {
                                            line[headerIndex["Gender Place"]] = result.GenderPlace == -1 ? "" : result.GenderPlace;
                                        }
                                        if (headerIndex.ContainsKey("Chip Finish"))
                                        {
                                            line[headerIndex["Chip Finish"]] = result.ChipTime;
                                        }
                                        if (headerIndex.ContainsKey("Gun Finish"))
                                        {
                                            line[headerIndex["Gun Finish"]] = result.Time;
                                        }
                                    }
                                    else if (Constants.Timing.SEGMENT_NONE != result.SegmentId)
                                    {
                                        string key = string.Format("Segment {0} Chip Time", segmentNum);
                                        if (headerIndex.ContainsKey(key))
                                        {
                                            line[headerIndex[key]] = result.ChipTime;
                                        }
                                        key = string.Format("Segment {0} Gun Time", segmentNum);
                                        if (headerIndex.ContainsKey(key))
                                        {
                                            line[headerIndex[key]] = result.Time;
                                        }
                                        key = string.Format("Segment {0} Name", segmentNum++);
                                        if (headerIndex.ContainsKey(key))
                                        {
                                            line[headerIndex[key]] = result.SegmentName;
                                        }
                                    }
                                }
                            }
                        }
                        else // Time Based
                        {
                            int finalLap = -1;
                            if (headerIndex.ContainsKey("Start") && occurrenceResultDictionary.ContainsKey((bib, 0)))
                            {
                                line[headerIndex["Start"]] = occurrenceResultDictionary[(bib, 0)].Time;
                            }
                            for (int i = 1; i <= maxLaps; i++)
                            {
                                string key = string.Format("Lap {0}", i);
                                if (occurrenceResultDictionary.ContainsKey((bib, i)))
                                {
                                    finalLap = i;
                                    if (headerIndex.ContainsKey(key))
                                    {
                                        line[headerIndex[key]] = occurrenceResultDictionary[(bib, i)].LapTime;
                                    }
                                }
                            }
                            if (occurrenceResultDictionary.ContainsKey((bib, finalLap)))
                            {
                                if (headerIndex.ContainsKey("Place"))
                                {
                                    line[headerIndex["Place"]] = occurrenceResultDictionary[(bib, finalLap)].Place;
                                }
                                if (headerIndex.ContainsKey("Age Group Place"))
                                {
                                    line[headerIndex["Age Group Place"]] = occurrenceResultDictionary[(bib, finalLap)].AgePlace;
                                }
                                if (headerIndex.ContainsKey("Gender Place"))
                                {
                                    line[headerIndex["Gender Place"]] = occurrenceResultDictionary[(bib, finalLap)].GenderPlace;
                                }
                                if (headerIndex.ContainsKey("Laps Completed"))
                                {
                                    line[headerIndex["Laps Completed"]] = occurrenceResultDictionary[(bib, finalLap)].Occurrence;
                                }
                                if (headerIndex.ContainsKey("Ellapsed Time (Gun)"))
                                {
                                    line[headerIndex["Ellapsed Time (Gun)"]] = occurrenceResultDictionary[(bib, finalLap)].Time;
                                }
                                if (headerIndex.ContainsKey("Ellapsed Time (Chip)"))
                                {
                                    line[headerIndex["Ellapsed Time (Chip)"]] = occurrenceResultDictionary[(bib, finalLap)].ChipTime;
                                }
                            }
                        }
                        data.Add(line);
                    }
                }
                IDataExporter exporter;
                string extension = Path.GetExtension(saveFileDialog.FileName);
                Log.D("UI.Export.ExportResults", string.Format("Extension is '{0}'", extension));
                if (extension.IndexOf("xls") != -1)
                {
                    exporter = new ExcelExporter();
                }
                else
                {
                    StringBuilder format = new StringBuilder();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        format.Append("\"{");
                        format.Append(i);
                        format.Append("}\",");
                    }
                    format.Remove(format.Length - 1, 1);
                    Log.D("UI.Export.ExportResults", string.Format("The format is '{0}'", format.ToString()));
                    exporter = new CSVExporter(format.ToString());
                }
                exporter.SetData(headers, data);
                try
                {
                    exporter.ExportData(saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    Log.E("UI.Export.ExportResults.Error", ex.ToString());
                    MessageBox.Show("Error saving file.");
                }
            }
            MessageBox.Show("Results saved.");
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Export.ExportResults", "Cancel clicked.");
            this.Close();
        }

        private class AHeaderBox : ListBoxItem
        {
            public CheckBox Include;
            public string NameValue { get; private set; }

            public AHeaderBox(string name)
            {
                NameValue = name;
                Include = new CheckBox()
                {
                    Content = name,
                    FontSize = 16,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(20,10,0,10),
                    IsChecked = true
                };
                this.Content = Include;
                this.Selected += new RoutedEventHandler(this.This_Selected);
            }

            private void This_Selected(Object sender, RoutedEventArgs e)
            {
                Include.IsChecked = !Include.IsChecked;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
        }
    }
}
