using EventDirector.Interfaces;
using EventDirector.UI.IO;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace EventDirector.UI.Export
{
    /// <summary>
    /// Interaction logic for ExportResults.xaml
    /// </summary>
    public partial class ExportResults : Window
    {
        IMainWindow window;
        IDBInterface database;
        Event theEvent;

        int maxNumSegments;
        List<String> commonHeaders = new List<String>
        {
            "Bib", "Distance", "Checked In", "Early Start", "First", "Last", "Birthday",
            "Age", "Gender", "Start", "Street", "Apartment",
            "City", "State", "Zip", "Country", "Mobile", "Email", "Parent", "Comments",
            "Other", "Owes", "Emergency Contact Name", "Emergency Contact Phone", "Division"
        };
        List<String> distanceHeaders = new List<string>
        {
            "Gun Finish", "Chip Finish"
        };
        List<String> timeHeaders = new List<string>
        {
            "Laps Completed", "Ellapsed Time (Gun)", "Ellapsed Time (Chip)"
        };

        public ExportResults(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier == -1)
            {
                this.Close();
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
                        commonHeaders.Insert(10, String.Format("Segment {0} Chip Time", i));
                        commonHeaders.Insert(10, String.Format("Segment {0} Gun Time", i));
                    }
                    // then do it again so we can add to the end in the right order
                    for (int i = 1; i <= maxNumSegments; i++)
                    {
                        commonHeaders.Add(String.Format("Segment {0} Name", i));
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
                    commonHeaders.Insert(10, String.Format("Lap {0}", i));
                }
            }
            foreach (String name in commonHeaders)
            {
                headersList.Items.Add(new AHeaderBox(name));
            }
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Done clicked.");
            List<string> headersToOutput = new List<string>();
            Dictionary<string, int> headerIndex = new Dictionary<string, int>();
            foreach (AHeaderBox headerBox in headersList.Items)
            {
                if (headerBox.Include.IsChecked == true)
                {
                    headersToOutput.Add(headerBox.NameValue);
                }
            }
            bool excel = window.ExcelEnabled();
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = window.ExcelEnabled() ? "Excel File (*.xlsx,*xls)|*.xlsx;*xls|CSV (*.csv)|*.csv" : "CSV (*.csv)|*.csv",
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                // write to file
                List<Participant> participants = database.GetParticipants(theEvent.Identifier);
                List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
                results.RemoveAll(x => x.EventSpecificId == Constants.Timing.TIMERESULT_DUMMYPERSON);
                results.Sort(TimeResult.CompareBySystemTime);
                Dictionary<int, List<TimeResult>> resultDictionary = new Dictionary<int, List<TimeResult>>();
                // (EventSpecificID, Occurence) - for Time Based Race exporting.
                Dictionary<(int, int), TimeResult> occurrenceResultDictionary = new Dictionary<(int, int), TimeResult>();
                int maxLaps = 0;
                foreach (TimeResult result in results)
                {
                    if (!resultDictionary.ContainsKey(result.EventSpecificId))
                    {
                        resultDictionary[result.EventSpecificId] = new List<TimeResult>();
                    }
                    resultDictionary[result.EventSpecificId].Add(result);
                    if (result.SegmentId == Constants.Timing.SEGMENT_FINISH)
                    {
                        occurrenceResultDictionary[(result.EventSpecificId, result.Occurrence)] = result;
                        maxLaps = result.Occurrence > maxLaps ? result.Occurrence : maxLaps;
                    }
                }
                string[] headers = new string[headersToOutput.Count];
                foreach (string header in headersToOutput)
                {
                    headerIndex[header] = headersToOutput.IndexOf(header);
                    headers[headerIndex[header]] = header;
                }
                List<object[]> data = new List<object[]>();
                foreach (Participant participant in participants)
                {
                    object[] line = new object[headersToOutput.Count];
                    if (headerIndex.ContainsKey("Bib"))
                    {
                        line[headerIndex["Bib"]] = participant.Bib;
                    }
                    if (headerIndex.ContainsKey("Distance"))
                    {
                        line[headerIndex["Distance"]] = participant.Division;
                    }
                    if (headerIndex.ContainsKey("Checked In"))
                    {
                        line[headerIndex["Checked In"]] = participant.CheckedIn;
                    }
                    if (headerIndex.ContainsKey("Early Start"))
                    {
                        line[headerIndex["Early Start"]] = participant.EarlyStart;
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
                    if (headerIndex.ContainsKey("Division"))
                    {
                        line[headerIndex["Division"]] = participant.Division + (participant.IsEarlyStart ? "Early" : "");
                    }
                    if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                    {
                        if (resultDictionary.ContainsKey(participant.EventSpecific.Identifier))
                        {
                            int segmentNum = 1;
                            foreach (TimeResult result in resultDictionary[participant.EventSpecific.Identifier])
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
                                    string key = String.Format("Segment {0} Chip Time", segmentNum);
                                    if (headerIndex.ContainsKey(key))
                                    {
                                        line[headerIndex[key]] = result.ChipTime;
                                    }
                                    key = String.Format("Segment {0} Gun Time", segmentNum);
                                    if (headerIndex.ContainsKey(key))
                                    {
                                        line[headerIndex[key]] = result.Time;
                                    }
                                    key = String.Format("Segment {0} Name", segmentNum++);
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
                        if (headerIndex.ContainsKey("Start") && occurrenceResultDictionary.ContainsKey((participant.EventSpecific.Identifier, 0)))
                        {
                            line[headerIndex["Start"]] = occurrenceResultDictionary[(participant.EventSpecific.Identifier, 0)].Time;
                        }
                        for (int i=1; i<=maxLaps; i++)
                        {
                            string key = String.Format("Lap {0}", i);
                            if (occurrenceResultDictionary.ContainsKey((participant.EventSpecific.Identifier, i)))
                            {
                                finalLap = i;
                                if (headerIndex.ContainsKey(key))
                                {
                                    line[headerIndex[key]] = occurrenceResultDictionary[(participant.EventSpecific.Identifier, i)].LapTime;
                                }
                            }
                        }
                        if (occurrenceResultDictionary.ContainsKey((participant.EventSpecific.Identifier, finalLap)))
                        {
                            if (headerIndex.ContainsKey("Laps Completed"))
                            {
                                line[headerIndex["Laps Completed"]] = occurrenceResultDictionary[(participant.EventSpecific.Identifier, finalLap)].Occurrence;
                            }
                            if (headerIndex.ContainsKey("Ellapsed Time (Gun)"))
                            {
                                line[headerIndex["Ellapsed Time (Gun)"]] = occurrenceResultDictionary[(participant.EventSpecific.Identifier, finalLap)].Time;
                            }
                            if (headerIndex.ContainsKey("Ellapsed Time (Chip)"))
                            {
                                line[headerIndex["Ellapsed Time (Chip)"]] = occurrenceResultDictionary[(participant.EventSpecific.Identifier, finalLap)].ChipTime;
                            }
                        }
                    }
                    data.Add(line);
                }
                IDataExporter exporter = null;
                string extension = Path.GetExtension(saveFileDialog.FileName);
                Log.D(String.Format("Extension is '{0}'", extension));
                if (extension.IndexOf("xls") != -1)
                {
                    exporter = new ExcelExporter();
                }
                else
                {
                    StringBuilder format = new StringBuilder();
                    for (int i = 0; i < headersToOutput.Count; i++)
                    {
                        format.Append("\"{");
                        format.Append(i);
                        format.Append("}\",");
                    }
                    format.Remove(format.Length - 1, 1);
                    Log.D(String.Format("The format is '{0}'", format.ToString()));
                    exporter = new CSVExporter(format.ToString());
                }
                exporter.SetData(headers, data);
                exporter.ExportData(saveFileDialog.FileName);
            }
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Cancel clicked.");
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
