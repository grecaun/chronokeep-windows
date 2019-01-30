using EventDirector.Interfaces;
using EventDirector.UI.IO;
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for ExportChanges.xaml
    /// </summary>
    public partial class ExportChanges : System.Windows.Window
    {
        IDBInterface database;
        IDataExporter exporter;
        MainWindow mainWindow;
        String changeDir = "Changes";
        Utils.FileType fileType = Utils.FileType.CSV;

        public ExportChanges(IDBInterface database, MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            this.database = database;
            InitializeComponent();
            UpdateEventsList();
        }

        public ExportChanges(IDBInterface database, MainWindow mainWindow, Utils.FileType fileType)
        {
            this.mainWindow = mainWindow;
            this.database = database;
            this.fileType = fileType;
            InitializeComponent();
            UpdateEventsList();
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
            Event anEvent = (Event)eventList.SelectedItem;
            if (anEvent != null)
            {
                await Task.Run(() =>
                {
                    Log.D("Event has name " + anEvent.Name + " and date of " + anEvent.Date + " and finally has ID " + anEvent.Identifier);
                    AppSetting directorySetting = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR);
                    if (directorySetting == null) return;
                    String directory = directorySetting.value;
                    String fullPath, fileExtension;
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    directory = Path.Combine(directory, changeDir);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
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
                            "\"{18}\",\"{19}\",\"{20}\",\"{21}\",\"{22}\",\"{23}\",\"{24}\",\"{25}\"");
                    }
                    String fileName = anEvent.Name + " Changes";
                    fullPath = Path.Combine(directory, fileName + fileExtension);
                    int number = 1;
                    while (File.Exists(fullPath))
                    {
                        fullPath = Path.Combine(directory, fileName + " (" + number++ + ")" + fileExtension);
                    }
                    List<Change> changes = database.GetChanges();
                    List<ChangeParticipant> parts = new List<ChangeParticipant>();
                    foreach (Change c in changes)
                    {
                        if ((c.OldParticipant != null && c.NewParticipant != null) && (c.NewParticipant.EventIdentifier == anEvent.Identifier || c.OldParticipant.EventIdentifier == anEvent.Identifier))
                        {
                            parts.Add(new ChangeParticipant(c.Identifier, "Old", c.OldParticipant));
                            parts.Add(new ChangeParticipant(c.Identifier, "New", c.NewParticipant));
                        }
                    }
                    string[] headers = new string[] {"Change Id", "New/Old", "Participant Id", "Event Id", "Bib", "Distance", "Checked In", "Early Start", "First", "Last",
                                    "Birthday", "Street", "Apartment", "City", "State", "Zip", "Country", "Mobile", "Email",
                                    "Parent", "Gender", "Comments", "Other", "Owes",
                                    "Emergency Contact Name", "Emergency Contact Phone"};
                    List<object[]> data = new List<object[]>();
                    foreach (ChangeParticipant p in parts)
                    {
                        data.Add(new object[] {p.ChangeIdentifier, p.Which, p.Identifier, p.EventIdentifier, p.Bib, p.Division, p.CheckedIn,
                                p.EarlyStart, p.FirstName, p.LastName,
                                p.Birthdate, p.Street, p.Street2, p.City, p.State, p.Zip, p.Country, p.Mobile, p.Email,
                                p.Parent, p.Gender, p.Comments, p.Other, p.Owes,
                                p.ECName, p.ECPhone});
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

        private void Window_Closed(object sender, EventArgs e)
        {
            mainWindow.PartListClosed();
        }
    }
}
