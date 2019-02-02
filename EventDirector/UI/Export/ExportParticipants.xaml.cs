using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using EventDirector.Interfaces;
using EventDirector.UI.IO;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for ExportParticipants.xaml
    /// </summary>
    public partial class ExportParticipants : System.Windows.Window
    {
        IDBInterface database;
        IDataExporter exporter;
        IWindowCallback window = null;
        Utils.FileType fileType = Utils.FileType.CSV;
        Event theEvent = null;

        private ExportParticipants(IWindowCallback window, IDBInterface database, bool ExcelAllowed)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            eventList.Visibility = Visibility.Collapsed;
            ExportAs.Visibility = Visibility.Visible;
            this.Height = 180;
            theEvent = database.GetCurrentEvent();
            if (ExcelAllowed)
            {
                Type.Items.Add(new ComboBoxItem()
                {
                    Content = "Excel Spreadsheet (*.xlsx)",
                    Uid = "2"
                });
            }
        }

        public static ExportParticipants NewWindow(IWindowCallback window, IDBInterface database, bool ExcelAllowed)
        {
            return new ExportParticipants(window, database, ExcelAllowed);
        }

        private async void UpdateEventsList()
        {
            List<Event> events = null;
            await Task.Run(() =>
            {
                events = database.GetEvents();
            });
            eventList.ItemsSource = events;
        }

        private async void Done_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Done clicked.");
            if (this.theEvent == null)
            {
                theEvent = (Event)eventList.SelectedItem;
            }
            else
            {
                if (Type.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select a format to use for exporting the data.");
                }
                else if (Type.SelectedIndex == 0)
                {
                    this.fileType = Utils.FileType.CSV;
                }
                else if (Type.SelectedIndex == 1)
                {
                    this.fileType = Utils.FileType.EXCEL;
                }
            }
            if (theEvent != null)
            {
                await Task.Run(() =>
                {
                    Log.D("Event has name " + theEvent.Name + " and date of " + theEvent.Date + " and finally has ID " + theEvent.Identifier);
                    AppSetting directorySetting = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR);
                    if (directorySetting == null) return;
                    String directory = directorySetting.value;
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    String fullPath;
                    String fileExtension;
                    if (fileType == Utils.FileType.EXCEL)
                    {
                        fileExtension = ".xlsx";
                        exporter = new ExcelExporter();
                    }
                    else
                    {
                        fileExtension = ".csv";
                        exporter = new CSVExporter("\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\"," +
                            "\"{8}\",\"{9}\",\"{10}\",\"{11}\",\"{12}\",\"{13}\",\"{14}\",\"{15}\",\"{16}\",\"{17}\"," +
                            "\"{18}\",\"{19}\",\"{20}\",\"{21}\",\"{22}\",\"{23}\"");
                    }
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    fullPath = System.IO.Path.Combine(directory, theEvent.Name + fileExtension);
                    int number = 1;
                    while (File.Exists(fullPath))
                    {
                        fullPath = System.IO.Path.Combine(directory, theEvent.Name + " (" + number++ + ")" + fileExtension);
                    }
                    List<Participant> parts = database.GetParticipants(theEvent.Identifier);
                    string[] headers = new string[] { "Bib", "Distance", "Checked In", "Early Start", "First", "Last", "Birthday",
                                    "Age", "Street", "Apartment", "City", "State", "Zip", "Country", "Mobile", "Email", "Parent",
                                    "Gender", "Comments", "Other", "Owes", "Emergency Contact Name", "Emergency Contact Phone", "Division" };
                    List<object[]> data = new List<object[]>();
                    foreach (Participant p in parts)
                    {
                        data.Add(new object[] { p.Bib, p.Division, p.CheckedIn, p.EarlyStart, p.FirstName, p.LastName, p.Birthdate,
                                        p.Age(theEvent.Date), p.Street, p.Street2, p.City, p.State, p.Zip, p.Country, p.Mobile, p.Email,
                                        p.Parent, p.Gender, p.Comments, p.Other, p.Owes, p.ECName, p.ECPhone, p.Division +
                                        (p.EventSpecific.EarlyStart == 1 ? " Early Start" : "") });
                    }
                    if (exporter != null)
                    {
                        exporter.SetData(headers, data);
                        exporter.ExportData(fullPath);
                    }
                });
            }
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Cancel clicked.");
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
        }
    }
}
