using ChronoKeep.Interfaces;
using ChronoKeep.UI.IO;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ChronoKeep.UI.Export
{
    /// <summary>
    /// Interaction logic for ExportBAAResults.xaml
    /// </summary>
    public partial class ExportBAAResults : Window
    {
        IMainWindow window;
        IDBInterface database;
        Event theEvent;

        bool noOpen = false;

        Dictionary<string, Distance> distanceDictionary;

        public ExportBAAResults(IMainWindow window, IDBInterface database)
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
                MessageBox.Show("Time based events not supported.");
                noOpen = true;
                return;
            }
            distanceDictionary = new Dictionary<string, Distance>();
            Log.D("ExportBAAResults", "Adding distances to combobox.");
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
                MessageBox.Show("Oops, you don't appear to have any distances set up.");
                noOpen = true;
                return;
            }
        }

        public bool SetupError()
        {
            return noOpen;
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = window.ExcelEnabled() ? "Excel File (*.xlsx,*xls)|*.xlsx;*xls|CSV (*.csv)|*.csv" : "CSV (*.csv)|*.csv",
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                Distance selected;
                if (distanceBox.SelectedItem != null && distanceDictionary.ContainsKey(((ComboBoxItem)distanceBox.SelectedItem).Uid))
                {
                    selected = distanceDictionary[((ComboBoxItem)distanceBox.SelectedItem).Uid];
                }
                else
                {
                    MessageBox.Show("Something went wrong with the distance. Exiting.");
                    Close();
                    return;
                }
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
                    if (Constants.Timing.SEGMENT_FINISH == result.SegmentId && participantDictionary.ContainsKey(result.Bib) && (result.DistanceName == selected.Name))
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
                            result.Time.Substring(0, result.Time.Length - 4),
                            result.ChipTime.Substring(0, result.ChipTime.Length -4),
                            ""
                        });
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
                exporter.ExportData(saveFileDialog.FileName);
                MessageBox.Show("File saved.");
            }
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Export.ExportResults", "Cancel clicked.");
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
        }
    }
}
