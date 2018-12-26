﻿using EventDirector.Interfaces;
using EventDirector.UI.Participants;
using Microsoft.Win32;
using System;
using System.Collections;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EventDirector.UI.MainPages
{
    /// <summary>
    /// Interaction logic for ParticipantsPage.xaml
    /// </summary>
    public partial class ParticipantsPage : Page, IMainPage
    {
        private INewMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;

        public ParticipantsPage(INewMainWindow mainWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mainWindow;
            this.database = database;
            UpdateDivisionsBox();
            UpdateView();
            UpdateImportOptions();
        }

        public void UpdateView()
        {
            Log.D("Updating Participants Page.");
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            List<Participant> participants;
            int divisionId = -1;
            try
            {
                divisionId = Convert.ToInt32(((ComboBoxItem)DivisionBox.SelectedItem).Uid);
            }
            catch
            {
                divisionId = -1;
            }
            if (divisionId == -1)
            {
                participants = database.GetParticipants(theEvent.Identifier);
            }
            else
            {
                participants = database.GetParticipants(theEvent.Identifier, divisionId);
            }
            participants.Sort();
            ParticipantsList.ItemsSource = participants;
            ParticipantsList.SelectedItems.Clear();
        }

        public void UpdateDivisionsBox()
        {
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
                        excelImp.Show();
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
                        excelImp.Show();
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
                addParticipant.Show();
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
                modifyParticipant.Show();
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
                exportParticipants.Show();
            }
        }

        public void UpdateDatabase() { }

        public void Keyboard_Ctrl_A()
        {
            Add_Click(null, null);
        }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }
    }
}
