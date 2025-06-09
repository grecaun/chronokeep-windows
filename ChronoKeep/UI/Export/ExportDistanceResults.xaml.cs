﻿using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.UI.IO;
using Chronokeep.UI.UIObjects;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.Export
{
    public enum OutputType
    {
        Boston,
        UltraSignup,
        Runsignup,
        Abbott
    }

    /// <summary>
    /// Interaction logic for ExportDistanceResults.xaml
    /// </summary>
    public partial class ExportDistanceResults : FluentWindow
    {
        IMainWindow window;
        IDBInterface database;
        Event theEvent;

        OutputType type;

        bool noOpen = false;

        Dictionary<string, Distance> distanceDictionary;

        public ExportDistanceResults(IMainWindow window, IDBInterface database, OutputType type = OutputType.Boston)
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
            distanceDictionary = new Dictionary<string, Distance>();
            Log.D("ExportDistanceResults", "Adding distances to combobox.");
            foreach (Distance distance in database.GetDistances(theEvent.Identifier))
            {
                // Don't list linked distances.
                if (Constants.Timing.DISTANCE_NO_LINKED_ID == distance.LinkedDistance)
                {
                    distanceDictionary[distance.Identifier.ToString()] = distance;
                    distanceBox.Items.Add(new ComboBoxItem()
                    {
                        Content = distance.Name,
                        Uid = distance.Identifier.ToString(),
                    });
                }
            }
            this.type = type;
            this.MinWidth = 300;
            this.MinHeight = 200;
            this.Width = 300;
            this.Height = 220;
            this.ResizeMode = ResizeMode.NoResize;
            bool supported = false;
            if (OutputType.Boston == type)
            {
                this.Title = "Export Boston Results";
            }
            else if (OutputType.UltraSignup == type)
            {
                this.Title = "Export UltraSignup Results";
                supported = true;
            }
            else if (OutputType.Runsignup == type)
            {
                this.Title = "Export Runsignup Results";
            }
            else if (OutputType.Abbott == type)
            {
                this.Title = "Export AbbottWMM Results";
            }
            if (Constants.Timing.EVENT_TYPE_TIME == theEvent.EventType && !supported)
            {
                DialogBox.Show("Exporting for a Time based event is not supported.");
                noOpen = true;
                return;
            }
            if (Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA == theEvent.EventType && !supported)
            {
                DialogBox.Show("Exporting for a Backyard Ultra event is not supported.");
                noOpen = true;
                return;
            }
            if (distanceBox.Items.Count < 1)
            {
                DialogBox.Show("Oops, you don't appear to have any distances set up.");
                noOpen = true;
                return;
            }
            // don't open the window if we've only got one to output
            if (distanceBox.Items.Count == 1)
            {
                distanceBox.SelectedIndex = 0;
                Distance selected;
                if (distanceBox.SelectedItem != null && distanceDictionary.ContainsKey(((ComboBoxItem)distanceBox.SelectedItem).Uid))
                {
                    selected = distanceDictionary[((ComboBoxItem)distanceBox.SelectedItem).Uid];
                }
                else
                {
                    DialogBox.Show("Something went wrong with the distance. Exiting.");
                    noOpen = true;
                    return;
                }
                if (OutputType.Boston == type)
                {
                    SaveBoston(selected.Name);
                }
                else if (OutputType.UltraSignup == type)
                {
                    SaveUltraSignup(selected.Name);
                }
                else if (OutputType.Runsignup == type)
                {
                    SaveRunsignup(selected.Name);
                }
                else if (OutputType.Abbott == type)
                {
                    SaveAbbot(selected.Name);
                }
                else
                {
                    DialogBox.Show("Something went wrong. No known output type specified.");
                }
                noOpen = true;
            }
            distanceBox.Items.Insert(0, new ComboBoxItem()
            {
                Content = "All",
                Uid = "ALL_DISTANCES",
            });
        }

        public bool SetupError()
        {
            return noOpen;
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            Distance selected;
            // Ensure we've selected a distance and that distance is either known
            if (distanceBox.SelectedItem != null
                && distanceDictionary.ContainsKey(((ComboBoxItem)distanceBox.SelectedItem).Uid))
            {
                selected = distanceDictionary[((ComboBoxItem)distanceBox.SelectedItem).Uid];
                if (OutputType.Boston == type)
                {
                    SaveBoston(selected.Name);
                }
                else if (OutputType.UltraSignup == type)
                {
                    SaveUltraSignup(selected.Name);
                }
                else if (OutputType.Runsignup == type)
                {
                    SaveRunsignup(selected.Name);
                }
                else if (OutputType.Abbott == type)
                {
                    SaveAbbot(selected.Name);
                }
                else
                {
                    DialogBox.Show("Something went wrong. No known output type specified.");
                }
            }
            // Check if they've told us to save all distances.
            else if (((ComboBoxItem)distanceBox.SelectedItem).Uid == "ALL_DISTANCES") {
                if (OutputType.Boston == type)
                {
                    SaveAllBoston();
                }
                else if (OutputType.UltraSignup == type)
                {
                    SaveAllUltraSignup();
                }
                else if (OutputType.Runsignup == type)
                {
                    SaveAllRunsignup();
                }
                else if (OutputType.Abbott == type)
                {
                    DialogBox.Show("Exporting all for Abbott is not supported.");
                    return;
                }
                else
                {
                    DialogBox.Show("Something went wrong. No known output type specified.");
                }
            }
            else
            {
                DialogBox.Show("Something went wrong with the distance. Exiting.");
            }
            Close();
        }

        private void SaveAllBoston()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel File (*.xlsx,*xls)|*.xlsx;*xls|CSV (*.csv)|*.csv",
                FileName = string.Format("{0} {1} Boston.{2}", theEvent.YearCode, theEvent.Name, "xlsx"),
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                string extension = Path.GetExtension(saveFileDialog.FileName);
                string fileName = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                string filePath = Path.GetDirectoryName(saveFileDialog.FileName);
                foreach (Distance distance in distanceDictionary.Values)
                {
                    SaveBostonInternal(
                        distance.Name,
                        Path.Combine(filePath, string.Format("{0} {1}{2}", fileName, distance.Name, extension)),
                        extension
                        );
                }
                DialogBox.Show("Files saved.");
            }
        }

        private void SaveAllUltraSignup()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = string.Format("{0} {1} Ultrasignup.{2}", theEvent.YearCode, theEvent.Name, "csv"),
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                string extension = Path.GetExtension(saveFileDialog.FileName);
                string fileName = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                string filePath = Path.GetDirectoryName(saveFileDialog.FileName);
                foreach (Distance distance in distanceDictionary.Values)
                {
                    SaveUltraSignupInternal(
                        distance.Name,
                        Path.Combine(filePath, string.Format("{0} {1}{2}", fileName, distance.Name, extension))
                        );
                }
                DialogBox.Show("Files saved.");
            }
        }

        private void SaveAllRunsignup()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = string.Format("{0} {1} Runsignup.{2}", theEvent.YearCode, theEvent.Name, "csv"),
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                string extension = Path.GetExtension(saveFileDialog.FileName);
                string fileName = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                string filePath = Path.GetDirectoryName(saveFileDialog.FileName);
                foreach (Distance distance in distanceDictionary.Values)
                {
                    SaveRunsignupInternal(
                        distance.Name,
                        Path.Combine(filePath, string.Format("{0} {1}{2}", fileName, distance.Name, extension))
                        );
                }
                DialogBox.Show("Files saved.");
            }
        }

        private void SaveAbbot(string distance)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel File (*.xlsx,*xls)|*.xlsx;*xls|CSV (*.csv)|*.csv",
                FileName = string.Format("{0} {1} {2} AbbotWMM.{3}", theEvent.YearCode, theEvent.Name, distance, "xlsx"),
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                SaveAbbotInternal(distance, saveFileDialog.FileName, Path.GetExtension(saveFileDialog.FileName));
                DialogBox.Show("File saved.");
            }
        }
        private void SaveAbbotInternal(string distance, string fileName, string extension)
        {
            string[] headers = new string[]
            {
                "name_prefix",  // leave empty
                "name_suffix",  // leave empty
                "first_name",
                "last_name",
                "email",        // leave empty
                "start_num",    // bib
                "date_of_birth",// DD/MM/YYYY or MM/DD/YYYY
                "nationality",  // IOC Code, ISO-3 CODE or IAAF Code
                "gender",       // M or F
                "finish_time",  // Chip time
                "place",        // overall
                "place_no_sex"  // gender place
            };
            List<object[]> data = new List<object[]>();
            List<Participant> participants = database.GetParticipants(theEvent.Identifier);
            Dictionary<string, Participant> participantDictionary = new Dictionary<string, Participant>();
            foreach (Participant person in participants)
            {
                participantDictionary[person.Bib] = person;
            }
            List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
            foreach (TimeResult result in results)
            {
                if (Constants.Timing.SEGMENT_FINISH == result.SegmentId && participantDictionary.ContainsKey(result.Bib) && (result.DistanceName == distance) && result.Time.Length > 4)
                {
                    string country = participantDictionary[result.Bib].Country;
                    if (country.Length != 3)
                    {
                        if (country.Equals("ca", System.StringComparison.OrdinalIgnoreCase) || country.Equals("canada", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "CAN";
                        }
                        else if (country.Equals("ae", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "ARE";
                        }
                        else if (country.Equals("au", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "AUS";
                        }
                        else if (country.Equals("br", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "BRA";
                        }
                        else if (country.Equals("United States of America", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "USA";
                        }
                        else if (country.Equals("cr", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "CRI";
                        }
                        else if (country.Equals("cw", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "CUW";
                        }
                        else if (country.Equals("ch", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "CHE";
                        }
                        else if (country.Equals("de", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "DEU";
                        }
                        else if (country.Equals("do", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "DOM";
                        }
                        else if (country.Equals("es", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "ESP";
                        }
                        else if (country.Equals("gb", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "GBR";
                        }
                        else if (country.Equals("hn", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "HND";
                        }
                        else if (country.Equals("ie", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "IRL";
                        }
                        else if (country.Equals("jp", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "JPN";
                        }
                        else if (country.Equals("lv", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "LVA";
                        }
                        else if (country.Equals("mx", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "MEX";
                        }
                        else if (country.Equals("nl", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "NLD";
                        }
                        else if (country.Equals("nz", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "NZL";
                        }
                        else if (country.Equals("ru", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "RUS";
                        }
                        else if (country.Equals("tw", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "TWN";
                        }
                        else if (country.Equals("um", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "UMI";
                        }
                        else if (country.Equals("za", System.StringComparison.OrdinalIgnoreCase))
                        {
                            country = "ZAF";
                        }
                        else
                        {
                            country = "";
                        }
                    }
                    data.Add(new object[]
                    {
                        "",
                        "",
                        result.Last,
                        result.First,
                        "",
                        result.Bib,
                        participantDictionary[result.Bib].Birthdate,
                        country,
                        result.Gender.Equals("Man", System.StringComparison.OrdinalIgnoreCase) ? "M" : result.Gender.Equals("Woman", System.StringComparison.OrdinalIgnoreCase) ? "F" : "",
                        result.ChipTime.Substring(0, result.ChipTime.Length > 4 ? result.ChipTime.Length -4 : 0),
                        result.PlaceStr,
                        result.GenderPlaceStr
                    });
                }
            }
            IDataExporter exporter;
            Log.D("UI.Export.ExportDistanceResults", string.Format("Extension is '{0}'", extension));
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
                Log.D("UI.Export.ExportDistanceResults", string.Format("The format is '{0}'", format.ToString()));
                exporter = new CSVExporter(format.ToString());
            }
            exporter.SetData(headers, data);
            exporter.ExportData(fileName);
        }

        private void SaveBoston(string distance)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel File (*.xlsx,*xls)|*.xlsx;*xls|CSV (*.csv)|*.csv",
                FileName = string.Format("{0} {1} {2} Boston.{3}", theEvent.YearCode, theEvent.Name, distance, "xlsx"),
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                SaveBostonInternal(distance, saveFileDialog.FileName, Path.GetExtension(saveFileDialog.FileName));
                DialogBox.Show("File saved.");
            }
        }

        private void SaveBostonInternal(string distance, string fileName, string extension)
        {
            Distance dist = null;
            foreach (Distance d in database.GetDistances(theEvent.Identifier))
            {
                if (d.Name == distance)
                {
                    dist = d;
                    break;
                }
            }
            List<string> headers = new List<string>
            {
                theEvent.Name,         // event name
                "", "", "", "", "", "", "", "", ""
            };
            List<Segment> segments = database.GetSegments(theEvent.Identifier);
            segments.RemoveAll(x => dist == null || x.DistanceId != dist.Identifier);
            segments.Sort((a, b) => a.CumulativeDistance.CompareTo(b.CumulativeDistance));
            for (int i=0; i<segments.Count; i++)
            {
                headers.Add("");
            }
            List<object[]> data = new List<object[]>();
            List<string> tmp = new()
            {
                theEvent.Date,         // event date
                "", "", "", "", "", "", "", "", ""
            };
            for (int i = 0; i < segments.Count; i++)
            {
                tmp.Add("");
            }
            data.Add(tmp.ToArray());
            tmp = new()
            {
                "INSERT EVENT CERTIFICATION HERE",         // event certification number
                "", "", "", "", "", "", "", "", ""
            };
            for (int i = 0; i < segments.Count; i++)
            {
                tmp.Add("");
            }
            data.Add(tmp.ToArray());
            // actual header
            tmp = new()
            {
                "Last Name",
                "First Name",
                "City",
                "State/Province",
                "Gender",
                "Date of Birth",
                "Age",
                "Gun Time",
                "Chip/Net Time",
                "Wheelchair"
            };
            // Get segments for header.
            Dictionary<int, int> segmentsHeaderPos = new Dictionary<int, int>();
            foreach (Segment seg in segments)
            {
                Log.D("UI.Export.ExportDistanceResults", "Segment:  " + seg.Name);
                segmentsHeaderPos[seg.Identifier] = data[0].Length;
                tmp.Add(string.Format("{0} {1}", seg.CumulativeDistance, Constants.Distances.DistanceString(seg.DistanceUnit)));
            }
            data.Add(tmp.ToArray());
            List<Participant> participants = database.GetParticipants(theEvent.Identifier);
            Dictionary<string, Participant> participantDictionary = new Dictionary<string, Participant>();
            foreach (Participant person in participants)
            {
                participantDictionary[person.Bib] = person;
            }
            Dictionary<(string, int), TimeResult> segmentResults = new();
            List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
            foreach (TimeResult result in results)
            {
                segmentResults[(result.Bib, result.SegmentId)] = result;
            }
            foreach (TimeResult result in results)
            {
                if (Constants.Timing.SEGMENT_FINISH == result.SegmentId && participantDictionary.ContainsKey(result.Bib) && (result.DistanceName == distance) && result.Time.Length > 4)
                {
                    List<string> values = new()
                    {
                            result.Last,
                            result.First,
                            participantDictionary[result.Bib].City,
                            participantDictionary[result.Bib].State,
                            result.Gender.Equals("Man", System.StringComparison.OrdinalIgnoreCase) ? "M" : result.Gender.Equals("Woman", System.StringComparison.OrdinalIgnoreCase) ? "F" : result.Gender.Equals("Non-Binary", System.StringComparison.OrdinalIgnoreCase) ? "X" : "",
                            participantDictionary[result.Bib].Birthdate,
                            result.Age(theEvent.Date).ToString(),
                            result.Time[..(result.Time.Length > 4 ? result.Time.Length - 4 : 0)],
                            result.ChipTime[..(result.ChipTime.Length > 4 ? result.ChipTime.Length -4 : 0)],
                            ""
                    };
                    foreach(Segment seg in segments)
                    {
                        if (segmentResults.TryGetValue((result.Bib, seg.Identifier), out TimeResult res))
                        {
                            values.Add(res.ChipTime[..(res.ChipTime.Length > 4 ? res.ChipTime.Length - 4 : 0)]);
                        }
                        else
                        {
                            values.Add("");
                        }
                    }
                    data.Add(values.ToArray());
                }
            }
            IDataExporter exporter;
            Log.D("UI.Export.ExportDistanceResults", string.Format("Extension is '{0}'", extension));
            if (extension.IndexOf("xls") != -1)
            {
                exporter = new ExcelExporter();
            }
            else
            {
                StringBuilder format = new StringBuilder();
                for (int i = 0; i < headers.Count; i++)
                {
                    format.Append("\"{");
                    format.Append(i);
                    format.Append("}\",");
                }
                format.Remove(format.Length - 1, 1);
                Log.D("UI.Export.ExportDistanceResults", string.Format("The format is '{0}'", format.ToString()));
                exporter = new CSVExporter(format.ToString());
            }
            exporter.SetData([.. headers], data);
            exporter.ExportData(fileName);
        }

        private void SaveUltraSignup(string distance)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = string.Format("{0} {1} {2} Ultrasignup.{3}", theEvent.YearCode, theEvent.Name, distance, "csv"),
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                string filename = saveFileDialog.FileName;
                string[] fileSplit = filename.Split('.');
                if (fileSplit.Length != 2)
                {
                    DialogBox.Show("Filename appears to be invalid.");
                    return;
                }
                if (!fileSplit[1].Equals("csv"))
                {
                    filename = string.Format("{0}.{1}", fileSplit[0], "csv");
                }
                SaveUltraSignupInternal(distance, filename);
                DialogBox.Show("File saved.");
            }
        }

        private void SaveRunsignup(string distance)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = string.Format("{0} {1} {2} Runsignup.{3}", theEvent.YearCode, theEvent.Name, distance, "csv"),
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                string filename = saveFileDialog.FileName;
                string[] fileSplit = filename.Split('.');
                if (fileSplit.Length != 2)
                {
                    DialogBox.Show("Filename appears to be invalid.");
                    return;
                }
                if (!fileSplit[1].Equals("csv"))
                {
                    filename = string.Format("{0}.{1}", fileSplit[0], "csv");
                }
                SaveRunsignupInternal(distance, filename);
                DialogBox.Show("File saved.");
            }
        }

        private void SaveUltraSignupInternal(string distance, string fileName)
        {
            string[] headers = new string[]
            {
                "place",
                "time",
                "first",
                "last",
                "gender",
                "age",
                "dob",
                "bib",
                "city",
                "state",
                "status"
            };
            Dictionary<string, Participant> participantDictionary = new Dictionary<string, Participant>();
            foreach (Participant person in database.GetParticipants(theEvent.Identifier))
            {
                participantDictionary[person.Bib] = person;
            }
            List<object[]> data = [];
            List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
            if (Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA == theEvent.EventType || Constants.Timing.EVENT_TYPE_TIME == theEvent.EventType)
            {
                headers[1] = "distance";
                Dictionary<string, TimeResult> finalResult = [];
                foreach (TimeResult result in results)
                {
                    if (!finalResult.TryGetValue(result.Identifier, out TimeResult other))
                    {
                        other = result;
                        finalResult[result.Identifier] = result;
                    }
                    if (other.Occurrence < result.Occurrence && result.Finish)
                    {
                        finalResult[result.Identifier] = result;
                    }
                }
                results.RemoveAll(x => !finalResult.ContainsValue(x));
            }
            foreach (TimeResult result in results)
            {
                if (Constants.Timing.SEGMENT_FINISH == result.SegmentId && participantDictionary.ContainsKey(result.Bib) && (result.DistanceName == distance))
                {
                    int status = 1;
                    if (Constants.Timing.TIMERESULT_STATUS_DNF == result.Status)
                    {
                        status = 2;
                    }
                    else if (Constants.Timing.DISTANCE_TYPE_UNOFFICIAL == result.Type)
                    {
                        status = 4;
                    }
                    var newLine = new object[]
                    {
                            result.Place > 0 ? result.Place.ToString() : "",
                            result.ChipTime,
                            result.First,
                            result.Last,
                            result.Gender.Equals("Man", System.StringComparison.OrdinalIgnoreCase) ? "M" : result.Gender.Equals("Woman", System.StringComparison.OrdinalIgnoreCase) ? "F" : result.Gender.Equals("Non-Binary", System.StringComparison.OrdinalIgnoreCase) ? "X" : "",
                            result.Age(theEvent.Date),
                            participantDictionary[result.Bib].Birthdate,
                            result.Bib,
                            participantDictionary[result.Bib].City,
                            participantDictionary[result.Bib].State,
                            status
                    };
                    if (Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA == theEvent.EventType || Constants.Timing.EVENT_TYPE_TIME == theEvent.EventType)
                    {
                        Dictionary<string, Distance> distances = [];
                        foreach (Distance dist in database.GetDistances(theEvent.Identifier))
                        {
                            distances[dist.Name] = dist;
                        }
                        int hour = (result.Occurrence / 2) + 1;
                        if (result.LinkedDistanceName.Length > 0
                            && distances.TryGetValue(result.LinkedDistanceName, out Distance localLinked)
                            && localLinked.DistanceValue > 0)
                        {
                            newLine[1] = (localLinked.DistanceValue * hour).ToString();
                        }
                        else if (result.DistanceName.Length > 0
                            && distances.TryGetValue(result.DistanceName, out Distance localDist)
                            && localDist.DistanceValue > 0)
                        {
                            newLine[1] = (localDist.DistanceValue * hour).ToString();
                        }
                        else
                        {
                            newLine[1] = "0";
                        }
                    }
                    data.Add(newLine);
                }
            }
            StringBuilder format = new StringBuilder();
            for (int i = 0; i < headers.Length; i++)
            {
                format.Append("\"{");
                format.Append(i);
                format.Append("}\",");
            }
            format.Remove(format.Length - 1, 1);
            Log.D("UI.Export.ExportDistanceResults", string.Format("The format is '{0}'", format.ToString()));
            IDataExporter exporter = new CSVExporter(format.ToString());
            exporter.SetData(headers, data);
            exporter.ExportData(fileName);
        }

        private void SaveRunsignupInternal(string distance, string fileName)
        {
            string[] headers = new string[]
            {
                "place",
                "clock time",
                "chip time",
                "first",
                "last",
                "gender",
                "age",
                "bib",
                "city",
                "state"
            };
            Dictionary<string, Participant> participantDictionary = new Dictionary<string, Participant>();
            foreach (Participant person in database.GetParticipants(theEvent.Identifier))
            {
                participantDictionary[person.Bib] = person;
            }
            List<object[]> data = new List<object[]>();
            foreach (TimeResult result in database.GetTimingResults(theEvent.Identifier))
            {
                if (Constants.Timing.SEGMENT_FINISH == result.SegmentId && participantDictionary.ContainsKey(result.Bib) && (result.DistanceName == distance))
                {
                    data.Add(
                    [
                            result.Place > 0 ? result.Place.ToString() : "",
                            result.Time,
                            result.ChipTime,
                            result.First,
                            result.Last,
                            result.Gender.Equals("Man", System.StringComparison.OrdinalIgnoreCase) ? "M" : result.Gender.Equals("Woman", System.StringComparison.OrdinalIgnoreCase) ? "F" : result.Gender.Equals("Non-Binary", System.StringComparison.OrdinalIgnoreCase) ? "X" : "",
                            result.Age(theEvent.Date),
                            result.Bib,
                            participantDictionary[result.Bib].City,
                            participantDictionary[result.Bib].State,
                    ]);
                }
            }
            IDataExporter exporter;
            StringBuilder format = new StringBuilder();
            for (int i = 0; i < headers.Length; i++)
            {
                format.Append("\"{");
                format.Append(i);
                format.Append("}\",");
            }
            format.Remove(format.Length - 1, 1);
            Log.D("UI.Export.ExportDistanceResults", string.Format("The format is '{0}'", format.ToString()));
            exporter = new CSVExporter(format.ToString());
            exporter.SetData(headers, data);
            exporter.ExportData(fileName);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Export.ExportDistanceResults", "Cancel clicked.");
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
        }
    }
}
