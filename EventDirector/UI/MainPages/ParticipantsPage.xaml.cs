using EventDirector.Interfaces;
using EventDirector.UI.Participants;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EventDirector.UI.MainPages
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
            Log.D("Updating Participants Page.");
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            int divisionId = -1;
            try
            {
                divisionId = Convert.ToInt32(((ComboBoxItem)DivisionBox.SelectedItem).Uid);
            }
            catch
            {
                divisionId = -1;
            }
            List<Participant> newParts = new List<Participant>();
            await Task.Run(() =>
            {
                if (divisionId == -1)
                {
                    newParts.AddRange(database.GetParticipants(theEvent.Identifier));
                }
                else
                {
                    newParts.AddRange(database.GetParticipants(theEvent.Identifier, divisionId));
                }
            });
            participants = newParts;
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
            Log.D("Participants updated.");
        }

        public void UpdateDivisionsBox()
        {
            Log.D("Updating divisions box.");
            theEvent = database.GetCurrentEvent();
            DivisionBox.Items.Clear();
            DivisionBox.Items.Add(new ComboBoxItem()
            {
                Content = "All",
                Uid = "-1"
            });
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            List<Division> divisions = database.GetDivisions(theEvent.Identifier);
            divisions.Sort();
            foreach (Division d in divisions)
            {
                DivisionBox.Items.Add(new ComboBoxItem()
                {
                    Content = d.Name,
                    Uid = d.Identifier.ToString()
                });
            }
            DivisionBox.SelectedIndex = 0;
        }

        private void UpdateImportOptions()
        {
            if (mWindow.ExcelEnabled())
            {
                Log.D("Excel is allowed.");
                ImportExcel.Visibility = Visibility.Visible;
                ImportCSV.Visibility = Visibility.Collapsed;
            }
            else
            {
                Log.D("Excel is not allowed.");
                ImportExcel.Visibility = Visibility.Collapsed;
                ImportCSV.Visibility = Visibility.Visible;
            }
        }

        private async void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Import Excel clicked.");
            OpenFileDialog excel_dialog = new OpenFileDialog() { Filter = "Excel files (*.xlsx,*.csv)|*.xlsx;*.csv|All files|*" };
            if (excel_dialog.ShowDialog() == true)
            {
                try
                {
                    ExcelImporter excel = new ExcelImporter(excel_dialog.FileName);
                    await Task.Run(() =>
                    {
                        excel.FetchHeaders();
                    });
                    ImportFileWindow excelImp = ImportFileWindow.NewWindow(mWindow, excel, database);
                    if (excelImp != null)
                    {
                        mWindow.AddWindow(excelImp);
                        excelImp.ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    Log.E("Something went wrong when trying to read the Excel file.");
                    Log.E(ex.StackTrace);
                }
            }
        }

        private async void ImportCSV_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Import CSV clicked.");
            OpenFileDialog csv_dialog = new OpenFileDialog() { Filter = "CSV Files (*.csv)|*.csv|All files|*" };
            if (csv_dialog.ShowDialog() == true)
            {
                try
                {
                    CSVImporter importer = new CSVImporter(csv_dialog.FileName);
                    await Task.Run(() =>
                    {
                        importer.FetchHeaders();
                    });
                    ImportFileWindow excelImp = ImportFileWindow.NewWindow(mWindow, importer, database);
                    if (excelImp != null)
                    {
                        mWindow.AddWindow(excelImp);
                        excelImp.ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    Log.E("Something went wrong when trying to read the CSV file.");
                    Log.E(ex.StackTrace);
                }
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Add clicked.");
            ModifyParticipantWindow addParticipant = ModifyParticipantWindow.NewWindow(mWindow, database);
            if (addParticipant != null)
            {
                mWindow.AddWindow(addParticipant);
                addParticipant.ShowDialog();
            }
        }

        private void Modify_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Modify clicked.");
            IList selected = ParticipantsList.SelectedItems;
            Log.D(selected.Count + " participants selected.");
            if (selected.Count > 1)
            {
                MessageBox.Show("You may only modify a single participant at a time.");
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
            Log.D("Remove clicked.");
            IList selected = ParticipantsList.SelectedItems;
            List<Participant> parts = new List<Participant>();
            foreach (Participant p in selected)
            {
                parts.Add(p);
            }
            database.RemoveEntries(parts);
            UpdateView();
        }

        private void DivisionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateView();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Export clicked.");
            ExportParticipants exportParticipants = ExportParticipants.NewWindow(mWindow, database, mWindow.ExcelEnabled());
            if (exportParticipants != null)
            {
                mWindow.AddWindow(exportParticipants);
                exportParticipants.ShowDialog();
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
            Log.D("Sort style changed.");
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
            Log.D("Done");
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
            Log.D("Page loaded.");
        }

        private void ParticipantsList_Loaded(object sender, RoutedEventArgs e)
        {
            Log.D("Participant list loaded.");
            UpdateDivisionsBox();
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
