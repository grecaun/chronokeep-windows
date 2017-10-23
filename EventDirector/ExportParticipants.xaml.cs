using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.IO;
using Microsoft.Office.Interop.Excel;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for ExportParticipants.xaml
    /// </summary>
    public partial class ExportParticipants : System.Windows.Window
    {
        IDBInterface database;
        MainWindow mainWindow;
        String programDir = "EventDirector";
        String exportDir = "Exports";
        Utils.FileType fileType = Utils.FileType.CSV;

        public ExportParticipants(IDBInterface database, MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            this.database = database;
            InitializeComponent();
            UpdateEventsList();
        }

        public ExportParticipants(IDBInterface database, MainWindow mainWindow, Utils.FileType fileType)
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
            Event anEvent = (Event) eventList.SelectedItem;
            if (anEvent != null)
            {
                await Task.Run(() =>
                {
                    Log.D("Event has name " + anEvent.Name + " and date of " + anEvent.Date + " and finally has ID " + anEvent.Identifier);
                    String directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), programDir, exportDir);
                    String fullPath;
                    String dotFile = ".csv";
                    if (fileType == Utils.FileType.EXCEL)
                    {
                        dotFile = ".xlsx";
                    }
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    fullPath = System.IO.Path.Combine(directory, anEvent.Name + dotFile);
                    int number = 1;
                    while (File.Exists(fullPath))
                    {
                        fullPath = System.IO.Path.Combine(directory, anEvent.Name + " (" + number++ + ")" + dotFile);
                    }
                    List<Participant> parts = database.GetParticipants(anEvent.Identifier);
                    switch (fileType)
                    {
                        case Utils.FileType.CSV:
                            FileStream outFile = File.Create(fullPath);
                            String format = "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",\"{11}\",\"{12}\",\"{13}\"," +
                            "\"{14}\",\"{15}\",\"{16}\",\"{17}\",\"{18}\",\"{19}\",\"{20}\",\"{21}\",\"{22}\",\"{23}\",\"{24}\",\"{25}\",\"{26}\",\"{27}\",\"{28}\",\"{29}\"";
                            using (StreamWriter outWriter = new StreamWriter(outFile))
                            {
                                outWriter.WriteLine(String.Format(format, "Bib", "Distance", "Checked In", "Early Start", "First", "Last", "Birthday",
                                    "Age", "Street", "Apartment", "City", "State", "Zip", "Country", "Phone", "Mobile", "Email", "Parent", "Gender", "Shirt",
                                    "Second Shirt", "Fleece", "Hat", "Comments", "Other", "Owes", "Emergency Contact Name", "Emergency Contact Phone", "Emergency Contact Email", "Division"));
                                foreach (Participant p in parts)
                                {
                                    outWriter.WriteLine(String.Format(format, p.Bib, p.Division, p.CheckedIn, p.EarlyStart, p.FirstName, p.LastName, p.Birthdate,
                                        p.Age(anEvent.Date), p.Street, p.Street2, p.City, p.State, p.Zip, p.Country, p.Phone, p.Mobile, p.Email, p.Parent, p.Gender, p.ShirtSize,
                                        p.SecondShirt, p.Fleece, p.Hat, p.Comments, p.Other, p.Owes, p.ECName, p.ECPhone, p.ECEmail, p.Division + (p.EventSpecific.EarlyStart == 1 ? " Early Start" : "")));
                                }
                            }
                            outFile.Close();
                            // Bib, Distance, Checked In, Early Start, First, Last, Birthday, Age, Street, Street2, City, State, Zip, Country, Phone,
                            // Mobile, Email, Parent, Gender, Shirt, Second Shirt, Fleece, Hat, Comments, Other, Owes
                            // Emergency Contact Name, Emergency Contact Phone, Emergency Contact Email, Division
                            break;
                        case Utils.FileType.EXCEL:
                            Utils.excelApp.ScreenUpdating = false;
                            Workbook wBook = Utils.excelApp.Workbooks.Add("");
                            Worksheet wSheet = wBook.ActiveSheet;
                            List<object[]> data = new List<object[]>();
                            data.Add(new object[] { "Bib", "Distance", "Checked In", "Early Start", "First", "Last", "Birthday",
                                    "Age", "Street", "Apartment", "City", "State", "Zip", "Country", "Phone", "Mobile", "Email", "Parent", "Gender", "Shirt",
                                    "Second Shirt", "Fleece", "Hat", "Comments", "Other", "Owes", "Emergency Contact Name", "Emergency Contact Phone", "Emergency Contact Email", "Division" });
                            foreach (Participant p in parts)
                            {
                                data.Add(new object[] { p.Bib, p.Division, p.CheckedIn, p.EarlyStart, p.FirstName, p.LastName, p.Birthdate,
                                        p.Age(anEvent.Date), p.Street, p.Street2, p.City, p.State, p.Zip, p.Country, p.Phone, p.Mobile, p.Email, p.Parent, p.Gender, p.ShirtSize,
                                        p.SecondShirt, p.Fleece, p.Hat, p.Comments, p.Other, p.Owes, p.ECName, p.ECPhone, p.ECEmail, p.Division + (p.EventSpecific.EarlyStart == 1 ? " Early Start" : "") });
                            }
                            object[,] outData = new object[data.Count, data[0].Length];
                            for (int i = 0; i < data.Count; i++)
                            {
                                for (int j = 0; j < data[0].Length; j++)
                                {
                                    outData[i,j] = data[i][j];
                                }
                            }
                            Range startCell = wSheet.Cells[1, 1];
                            Range endCell = wSheet.Cells[data.Count, data[0].Length];
                            Range writeRange = wSheet.get_Range(startCell, endCell);
                            writeRange.Value2 = outData;
                            writeRange.EntireColumn.AutoFit();
                            wBook.SaveAs(fullPath, XlFileFormat.xlWorkbookDefault, Type.Missing, Type.Missing, false, false, XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                            wBook.Close();
                            Utils.excelApp.ScreenUpdating = true;
                            break;
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
