using ChronoKeep.Interfaces;
using ChronoKeep.UI.Participants;
using ChronoKeep.UI.IO;
using Microsoft.Win32;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChronoKeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for ParticipantsPage.xaml
    /// </summary>
    public partial class ParticipantsPage : Page, IMainPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;
        List<Participant> participants = new List<Participant>();

        public ParticipantsPage(IMainWindow mainWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mainWindow;
            this.database = database;
            UpdateImportOptions();
        }

        public async void UpdateView()
        {
            Log.D("UI.MainPages.ParticipantsPage", "Updating Participants Page.");
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            int distanceId = -1;
            try
            {
                distanceId = Convert.ToInt32(((ComboBoxItem)DistanceBox.SelectedItem).Uid);
            }
            catch
            {
                distanceId = -1;
            }
            List<Participant> newParts = new List<Participant>();
            await Task.Run(() =>
            {
                if (distanceId == -1)
                {
                    newParts.AddRange(database.GetParticipants(theEvent.Identifier));
                }
                else
                {
                    newParts.AddRange(database.GetParticipants(theEvent.Identifier, distanceId));
                }
            });
            participants.Clear();
            participants.AddRange(newParts);
            switch (((ComboBoxItem)SortBox.SelectedItem).Content)
            {
                case "Name":
                    newParts.Sort(Participant.CompareByName);
                    break;
                case "Bib":
                    newParts.Sort(Participant.CompareByBib);
                    break;
                default:
                    newParts.Sort();
                    break;
            }
            string search = SearchBox != null ? SearchBox.Text.Trim() : "";
            newParts.RemoveAll(x => x.IsNotMatch(search));
            ParticipantsList.SelectedItems.Clear();
            ParticipantsList.ItemsSource = newParts;
            ParticipantsList.Items.Refresh();
            Log.D("UI.MainPages.ParticipantsPage", "Participants updated.");
        }

        public void UpdateDistancesBox()
        {
            Log.D("UI.MainPages.ParticipantsPage", "Updating distances box.");
            theEvent = database.GetCurrentEvent();
            DistanceBox.Items.Clear();
            DistanceBox.Items.Add(new ComboBoxItem()
            {
                Content = "All",
                Uid = "-1"
            });
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            List<Distance> distances = database.GetDistances(theEvent.Identifier);
            distances.Sort();
            foreach (Distance d in distances)
            {
                DistanceBox.Items.Add(new ComboBoxItem()
                {
                    Content = d.Name,
                    Uid = d.Identifier.ToString()
                });
            }
            DistanceBox.SelectedIndex = 0;
        }

        private void UpdateImportOptions()
        {
            if (mWindow.ExcelEnabled())
            {
                Log.D("UI.MainPages.ParticipantsPage", "Excel is allowed.");
                ImportExcel.Visibility = Visibility.Visible;
                ImportCSV.Visibility = Visibility.Collapsed;
            }
            else
            {
                Log.D("UI.MainPages.ParticipantsPage", "Excel is not allowed.");
                ImportExcel.Visibility = Visibility.Collapsed;
                ImportCSV.Visibility = Visibility.Visible;
            }
        }

        private void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.ParticipantsPage", "Import Excel clicked.");
            OpenFileDialog excel_dialog = new OpenFileDialog() { Filter = "Excel files (*.xlsx,*.csv)|*.xlsx;*.csv|All files|*" };
            if (excel_dialog.ShowDialog() == true)
            {
                try
                {
                    ExcelImporter excel = new ExcelImporter(excel_dialog.FileName);
                    excel.FetchHeaders();
                    ImportFileWindow excelImp = ImportFileWindow.NewWindow(mWindow, excel, database);
                    if (excelImp != null)
                    {
                        mWindow.AddWindow(excelImp);
                        excelImp.ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was a problem importing the file.");
                    Log.E("UI.MainPages.ParticipantsPage", "Something went wrong when trying to read the Excel file.");
                    Log.E("UI.MainPages.ParticipantsPage", ex.StackTrace);
                }
            }
        }

        private void ImportCSV_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.ParticipantsPage", "Import CSV clicked.");
            OpenFileDialog csv_dialog = new OpenFileDialog() { Filter = "CSV Files (*.csv)|*.csv|All files|*" };
            if (csv_dialog.ShowDialog() == true)
            {
                try
                {
                    CSVImporter importer = new CSVImporter(csv_dialog.FileName);
                    importer.FetchHeaders();
                    ImportFileWindow excelImp = ImportFileWindow.NewWindow(mWindow, importer, database);
                    if (excelImp != null)
                    {
                        mWindow.AddWindow(excelImp);
                        excelImp.ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was a problem importing the file.");
                    Log.E("UI.MainPages.ParticipantsPage", "Something went wrong when trying to read the CSV file.");
                    Log.E("UI.MainPages.ParticipantsPage", ex.StackTrace);
                }
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.ParticipantsPage", "Add clicked.");
            ModifyParticipantWindow addParticipant = ModifyParticipantWindow.NewWindow(mWindow, database);
            if (addParticipant != null)
            {
                mWindow.AddWindow(addParticipant);
                addParticipant.ShowDialog();
            }
        }

        private void Modify_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.ParticipantsPage", "Modify clicked.");
            List<Participant> selected = new List<Participant>();
            foreach (Participant p in ParticipantsList.SelectedItems)
            {
                selected.Add(p);
            }
            Log.D("UI.MainPages.ParticipantsPage", selected.Count + " participants selected.");
            if (selected.Count > 1)
            {
                ChangeMultiParticipantWindow changeMultiParticipantWindow = new ChangeMultiParticipantWindow(mWindow, database, selected);
                mWindow.AddWindow(changeMultiParticipantWindow);
                changeMultiParticipantWindow.ShowDialog();
                return;
            }
            Participant part = null;
            foreach (Participant p in selected)
            {
                part = p;
            }
            if (part == null) return;
            ModifyParticipantWindow modifyParticipant = ModifyParticipantWindow.NewWindow(mWindow, database, part);
            if (modifyParticipant != null)
            {
                mWindow.AddWindow(modifyParticipant);
                modifyParticipant.ShowDialog();
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.ParticipantsPage", "Remove clicked.");
            IList selected = ParticipantsList.SelectedItems;
            List<Participant> parts = new List<Participant>();
            foreach (Participant p in selected)
            {
                parts.Add(p);
            }
            database.RemoveEntries(parts);
            UpdateView();
        }

        private void DistanceBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateView();
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.ParticipantsPage", "Export clicked."); bool excel = mWindow.ExcelEnabled();
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = mWindow.ExcelEnabled() ? "Excel File (*.xlsx,*xls)|*.xlsx;*xls|CSV (*.csv)|*.csv" : "CSV (*.csv)|*.csv",
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                if (theEvent != null)
                {
                    await Task.Run(() =>
                    {
                        Log.D("UI.MainPages.ParticipantsPage", "Event has name " + theEvent.Name + " and date of " + theEvent.Date + " and finally has ID " + theEvent.Identifier);
                        List<Participant> parts = database.GetParticipants(theEvent.Identifier);
                        string[] headers = new string[] {
                            "Bib",
                            "Distance",
                            "Status",
                            "First",
                            "Last",
                            "Birthday",
                            "Age",
                            "Street",
                            "Apartment",
                            "City",
                            "State",
                            "Zip",
                            "Country",
                            "Mobile",
                            "Email",
                            "Parent",
                            "Gender",
                            "Comments",
                            "Other",
                            "Owes",
                            "Emergency Contact Name",
                            "Emergency Contact Phone"
                        };
                        List<object[]> data = new List<object[]>();
                        foreach (Participant p in parts)
                        {
                            data.Add(new object[] {
                                p.Bib,
                                p.Distance,
                                p.EventSpecific.StatusStr,
                                p.FirstName,
                                p.LastName,
                                p.Birthdate,
                                p.Age(theEvent.Date),
                                p.Street,
                                p.Street2,
                                p.City,
                                p.State,
                                p.Zip,
                                p.Country,
                                p.Mobile,
                                p.Email,
                                p.Parent,
                                p.Gender,
                                // Get rid of all the quote and newline characters.
                                p.Comments.Replace('\"', ' ').Replace('\n', ' ').Replace('\r', ' ').Replace('\'', ' '),
                                p.Other.Replace('\"', ' ').Replace('\n', ' ').Replace('\r', ' ').Replace('\'', ' '),
                                p.Owes,
                                p.ECName,
                                p.ECPhone,
                            });
                        }
                        IDataExporter exporter = null;
                        string extension = Path.GetExtension(saveFileDialog.FileName);
                        Log.D("UI.MainPages.ParticipantsPage", string.Format("Extension is '{0}'", extension));
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
                            Log.D("UI.MainPages.ParticipantsPage", string.Format("The format is '{0}'", format.ToString()));
                            exporter = new CSVExporter(format.ToString());
                        }
                        if (exporter != null)
                        {
                            exporter.SetData(headers, data);
                            exporter.ExportData(saveFileDialog.FileName);
                        }
                    });
                    MessageBox.Show("File saved.");
                }
            }
        }

        public void UpdateDatabase() { }

        public void Keyboard_Ctrl_A()
        {
            Add_Click(null, null);
        }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        private void SortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.D("UI.MainPages.ParticipantsPage", "Sort style changed.");
            if (participants != null)
            {
                switch (((ComboBoxItem)SortBox.SelectedItem).Content)
                {
                    case "Name":
                        participants.Sort(Participant.CompareByName);
                        break;
                    case "Bib":
                        participants.Sort(Participant.CompareByBib);
                        break;
                    default:
                        participants.Sort();
                        break;
                }
                if (ParticipantsList != null)
                {
                    ParticipantsList.ItemsSource = participants;
                    ParticipantsList.SelectedItems.Clear();
                    ParticipantsList.Items.Refresh();
                }
            }
            Log.D("UI.MainPages.ParticipantsPage", "Done");
        }

        public void Closing()
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.ParticipantsPage", "Page loaded.");
        }

        private void ParticipantsList_Loaded(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.ParticipantsPage", "Participant list loaded.");
            UpdateDistancesBox();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            List<Participant> newParts = new List<Participant>(participants);
            switch (((ComboBoxItem)SortBox.SelectedItem).Content)
            {
                case "Name":
                    newParts.Sort(Participant.CompareByName);
                    break;
                case "Bib":
                    newParts.Sort(Participant.CompareByBib);
                    break;
                default:
                    newParts.Sort();
                    break;
            }
            string search = SearchBox != null ? SearchBox.Text.Trim() : "";
            newParts.RemoveAll(x => x.IsNotMatch(search));
            ParticipantsList.SelectedItems.Clear();
            ParticipantsList.ItemsSource = newParts;
            ParticipantsList.Items.Refresh();
        }

        private void ParticipantsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ParticipantsList.SelectedItem == null) return;
            ModifyParticipantWindow modifyParticipant = ModifyParticipantWindow.NewWindow(mWindow, database, (Participant)ParticipantsList.SelectedItem);
            if (modifyParticipant != null)
            {
                mWindow.AddWindow(modifyParticipant);
                modifyParticipant.ShowDialog();
            }
        }
    }
}
