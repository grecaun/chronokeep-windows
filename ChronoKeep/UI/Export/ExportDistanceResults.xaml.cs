using Chronokeep.Interfaces;
using Chronokeep.UI.IO;
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
        UltraSignup
    }

    /// <summary>
    /// Interaction logic for ExportDistanceResults.xaml
    /// </summary>
    public partial class ExportDistanceResults : UiWindow
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
            if (Constants.Timing.EVENT_TYPE_TIME == theEvent.EventType)
            {
                Dialog dialog1 = new()
                {
                    Title = "",
                    Message = "Time based events not supported.",
                    ButtonRightName = "OK",
                    ButtonLeftVisibility = Visibility.Collapsed,
                };
                dialog1.Show();
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
            if (distanceBox.Items.Count < 1)
            {
                Dialog dialog1 = new()
                {
                    Title = "",
                    Message = "Oops, you don't appear to have any distances set up.",
                    ButtonRightName = "OK",
                    ButtonLeftVisibility = Visibility.Collapsed,
                };
                dialog1.Show();
                noOpen = true;
                return;
            }
            this.type = type;
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
                    Dialog dialog1 = new()
                    {
                        Title = "",
                        Message = "Something went wrong with the distance. Exiting.",
                        ButtonRightName = "OK",
                        ButtonLeftVisibility = Visibility.Collapsed,
                    };
                    dialog1.Show();
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
                else
                {
                    Dialog dialog1 = new()
                    {
                        Title = "",
                        Message = "Something went wrong. No known output type specified.",
                        ButtonRightName = "OK",
                        ButtonLeftVisibility = Visibility.Collapsed,
                    };
                    dialog1.Show();
                }
                noOpen = true;
            }
        }

        public bool SetupError()
        {
            return noOpen;
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            Distance selected;
            if (distanceBox.SelectedItem != null && distanceDictionary.ContainsKey(((ComboBoxItem)distanceBox.SelectedItem).Uid))
            {
                selected = distanceDictionary[((ComboBoxItem)distanceBox.SelectedItem).Uid];
            }
            else
            {
                Dialog dialog1 = new()
                {
                    Title = "",
                    Message = "Something went wrong with the distance. Exiting.",
                    ButtonRightName = "OK",
                    ButtonLeftVisibility = Visibility.Collapsed,
                };
                dialog1.Show();
                Close();
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
            else
            {
                Dialog dialog1 = new()
                {
                    Title = "",
                    Message = "Something went wrong. No known output type specified.",
                    ButtonRightName = "OK",
                    ButtonLeftVisibility = Visibility.Collapsed,
                };
                dialog1.Show();
            }
            Close();
        }

        private void SaveBoston(string distance)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel File (*.xlsx,*xls)|*.xlsx;*xls|CSV (*.csv)|*.csv",
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                string[] headers = new string[]
                {
                        theEvent.Name,         // event name
                        "", "", "", "", "", "", "", "", ""
                };
                List<object[]> data = new List<object[]>();
                data.Add(new object[]
                {
                        theEvent.Date,         // event date
                        "", "", "", "", "", "", "", "", ""
                });
                data.Add(new object[]
                {
                        "INSERT EVENT CERTIFICATION HERE",         // event certification number
                        "", "", "", "", "", "", "", "", ""
                });
                // actual header
                data.Add(new object[]
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
                });
                List<Participant> participants = database.GetParticipants(theEvent.Identifier);
                Dictionary<int, Participant> participantDictionary = new Dictionary<int, Participant>();
                foreach (Participant person in participants)
                {
                    participantDictionary[person.Bib] = person;
                }
                List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
                foreach (TimeResult result in results)
                {
                    if (Constants.Timing.SEGMENT_FINISH == result.SegmentId && participantDictionary.ContainsKey(result.Bib) && (result.DistanceName == distance) && result.Time.Length > 4)
                    {
                        data.Add(new object[]
                        {
                            result.Last,
                            result.First,
                            participantDictionary[result.Bib].City,
                            participantDictionary[result.Bib].State,
                            result.Gender,
                            participantDictionary[result.Bib].Birthdate,
                            result.Age(theEvent.Date),
                            result.Time.Substring(0, result.Time.Length > 4 ? result.Time.Length - 4 : 0),
                            result.ChipTime.Substring(0, result.ChipTime.Length > 4 ? result.ChipTime.Length -4 : 0),
                            ""
                        });
                    }
                }
                IDataExporter exporter;
                string extension = Path.GetExtension(saveFileDialog.FileName);
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
                exporter.ExportData(saveFileDialog.FileName);
                Dialog dialog1 = new()
                {
                    Title = "",
                    Message = "File saved.",
                    ButtonRightName = "OK",
                    ButtonLeftVisibility = Visibility.Collapsed,
                };
                dialog1.Show();
            }
        }

        private void SaveUltraSignup(string distance)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value
            };
            if (saveFileDialog.ShowDialog() == true)
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
                Dictionary<int, Participant> participantDictionary = new Dictionary<int, Participant>();
                foreach (Participant person in database.GetParticipants(theEvent.Identifier))
                {
                    participantDictionary[person.Bib] = person;
                }
                List<object[]> data = new List<object[]>();
                foreach (TimeResult result in database.GetTimingResults(theEvent.Identifier))
                {
                    if (Constants.Timing.SEGMENT_FINISH == result.SegmentId && participantDictionary.ContainsKey(result.Bib) && (result.DistanceName == distance))
                    {
                        int status = 1;
                        if (Constants.Timing.TIMERESULT_STATUS_DNF == result.Status)
                        {
                            status = 2;
                        }
                        else if (Constants.Timing.DISTANCE_TYPE_UNOFFICIAL == result.Type || Constants.Timing.DISTANCE_TYPE_EARLY == result.Type)
                        {
                            status = 4;
                        }
                        data.Add(new object[]
                        {
                            result.Place > 0 ? result.Place.ToString() : "",
                            result.ChipTime,
                            result.First,
                            result.Last,
                            result.Gender,
                            result.Age(theEvent.Date),
                            participantDictionary[result.Bib].Birthdate,
                            result.Bib,
                            participantDictionary[result.Bib].City,
                            participantDictionary[result.Bib].State,
                            status
                        });
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
                exporter.ExportData(saveFileDialog.FileName);
                Dialog dialog1 = new()
                {
                    Title = "",
                    Message = "File saved.",
                    ButtonRightName = "OK",
                    ButtonLeftVisibility = Visibility.Collapsed,
                };
                dialog1.Show();
            }
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
