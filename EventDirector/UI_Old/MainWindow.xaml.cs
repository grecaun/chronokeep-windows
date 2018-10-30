using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IChangeUpdater, IMainWindow
    {
        IDBInterface database;
        String dbName = "EventDirector.sqlite";
        String programDir = "EventDirector";
        ParticipantsListWindow partList = null;
        DayOfParticipantsListWindow doPartList = null;
        bool closing = false;

        Thread tcpServerThread;
        TCPServer tcpServer;

        Thread zeroConfThread;
        ZeroConf zeroConf;

        List<Window> windows = new List<Window>();

        public MainWindow()
        {
            InitializeComponent();
            CheatingInit();
            String dirPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), programDir);
            String path = System.IO.Path.Combine(dirPath, dbName);
            Log.D("Looking for database file.");
            if (!Directory.Exists(dirPath))
            {
                Log.D("Creating directory.");
                Directory.CreateDirectory(dirPath);
            }
            if (!File.Exists(path))
            {
                Log.D("Creating database file.");
                SQLiteConnection.CreateFile(path);
            }
            database = new SQLiteInterface(path);
            database.Initialize();
            UpdateEventBox();
            Log.D("Starting TCP server thread.");
            tcpServer = new TCPServer(database, this);
            tcpServerThread = new Thread(new ThreadStart(tcpServer.Run));
            tcpServerThread.Start();
            Log.D("Starting zero configuration thread.");
            zeroConf = new ZeroConf(database.GetServerName());
            zeroConfThread = new Thread(new ThreadStart(zeroConf.Run));
            zeroConfThread.Start();
            AsyncUpdateChangesBox();
            UpdateExportOptions();
        }

        private async void UpdateExportOptions()
        {
            bool excelEnabled = false;
            await Task.Run(() =>
            {
                excelEnabled = Utils.ExcelEnabled();
            });
            if (excelEnabled)
            {
                Log.D("Excel is allowed.");
                exportPartMenuItem.Visibility = Visibility.Visible;
                exportChangesMenuItem.Visibility = Visibility.Visible;
                importExcel.Visibility = Visibility.Visible;
            }
            else
            {
                Log.D("Excel is not allowed.");
                exportPartMenuItem.Visibility = Visibility.Collapsed;
                exportPartMenuItem.Visibility = Visibility.Collapsed;
                importExcel.Visibility = Visibility.Collapsed;
            }
        }

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            int menuId = Convert.ToInt32(((MenuItem)sender).Uid);
            switch (menuId)
            {
                case 1:     // Connection Settings
                    Log.D("Settings");
                    string name = "Northwest Endurance Events";
                    await Task.Run(() =>
                    {
                        name = database.GetServerName();
                    });
                    Settings settings = new Settings(this, name);
                    windows.Add(settings);
                    settings.Show();
                    break;
                case 2:
                    Log.D("Rebuild Database.");
                    await Task.Run(() =>
                    {
                        ((SQLiteInterface)database).HardResetDatabase();
                    });
                    Log.D("Database Rebuilt.");
                    UpdateEventBox();
                    AsyncUpdateChangesBox();
                    break;
                case 3:     // Clear Database
                    Log.D("Clear Database.");
                    await Task.Run( () =>
                    {
                        database.ResetDatabase();
                    });
                    Log.D("Database Reset.");
                    UpdateEventBox();
                    AsyncUpdateChangesBox();
                    break;
                case 4:     // Exit
                    Log.D("Goodbye");
                    this.Close();
                    break;
                case 5:     // Import participants
                    Log.D("Import (CSV)");
                    OpenFileDialog dialog = new OpenFileDialog() { Filter = "CSV Files (*.csv)|*.csv|All files|*" };
                    if (dialog.ShowDialog() == true)
                    {
                        try
                        {
                            CSVImporter importer = new CSVImporter(dialog.FileName);
                            await Task.Run(() =>
                            {
                                importer.FetchHeaders();
                            });
                            ImportFileWindow win = new ImportFileWindow(this, importer, database);
                            windows.Add(win);
                            win.Show();
                        }
                        catch (Exception ex)
                        {
                            Log.E("Something went wrong when trying to read the CSV file.");
                            Log.E(ex.StackTrace);
                        }
                    }
                    break;
                case 6:     // Assign bibs/chips
                    Log.D("Assign");
                    break;
                case 7:     // List Participants
                    Log.D("List Participants");
                    partList = new ParticipantsListWindow(database, this);
                    partList.Show();
                    break;
                case 8:
                    Log.D("Export participants - CSV.");
                    ExportParticipants exportPartsCSV = new ExportParticipants(database, this);
                    windows.Add(exportPartsCSV);
                    exportPartsCSV.Show();
                    break;
                case 9:
                    Log.D("Setup kiosk");
                    KioskSetup kiosk = new KioskSetup(this, database);
                    windows.Add(kiosk);
                    kiosk.Show();
                    break;
                case 10:
                    Log.D("List day of registrants");
                    doPartList = new DayOfParticipantsListWindow(database, this);
                    doPartList.Show();
                    break;
                case 11:
                    Log.D("Import participants - Excel");
                    OpenFileDialog excel_dialog = new OpenFileDialog() { Filter = "Excel files (*.xlsx)|*.xlsx|All files|*" };
                    if (excel_dialog.ShowDialog() == true)
                    {
                        try
                        {
                            ExcelImporter excel = new ExcelImporter(excel_dialog.FileName);
                            await Task.Run(() =>
                            {
                                excel.FetchHeaders();
                            });
                            ImportFileWindow excelImp = new ImportFileWindow(this, excel, database);
                            windows.Add(excelImp);
                            excelImp.Show();
                        }
                        catch
                        {}
                    }
                    break;
                case 12:
                    Log.D("Export Participants - Excel");
                    ExportParticipants exportPartsExcel = new ExportParticipants(database, this, Utils.FileType.EXCEL);
                    windows.Add(exportPartsExcel);
                    exportPartsExcel.Show();
                    break;
                case 13:
                    Log.D("Export Changes - CSV");
                    ExportChanges exportChangeCSV = new ExportChanges(database, this);
                    windows.Add(exportChangeCSV);
                    exportChangeCSV.Show();
                    break;
                case 14:
                    Log.D("Export Changes - Excel");
                    ExportChanges exportChangesExcel = new ExportChanges(database, this, Utils.FileType.EXCEL);
                    windows.Add(exportChangesExcel);
                    exportChangesExcel.Show();
                    break;
                case 15:
                    Log.D("About");
                    About aboutWin = new About(this);
                    windows.Add(aboutWin);
                    aboutWin.Show();
                    break;
                case 16:
                    Log.D("Setup Next Year Registration");
                    NextYearSetup nysetup = new NextYearSetup(this, database);
                    windows.Add(nysetup);
                    nysetup.Show();
                    break;
                case 17:
                    Log.D("Associate chips and bibs - CSV");
                    OpenFileDialog bib_dialog = new OpenFileDialog() { Filter = "CSV Files (*.csv)|*.csv|All files|*" };
                    if (bib_dialog.ShowDialog() == true)
                    {
                        try
                        {
                            CSVImporter importer = new CSVImporter(bib_dialog.FileName);
                            await Task.Run(() =>
                            {
                                importer.FetchHeaders();
                            });
                            BibChipAssociationWindow win = new BibChipAssociationWindow(this, importer, database);
                            windows.Add(win);
                            win.Show();
                        }
                        catch (Exception ex)
                        {
                            Log.E("Something went wrong when trying to read the CSV file.");
                            Log.E(ex.StackTrace);
                        }
                    }
                    break;
                case 18:
                    Log.D("Associate chips and bibs - Excel");
                    OpenFileDialog bib_excel_dialog = new OpenFileDialog() { Filter = "Excel files (*.xlsx)|*.xlsx|All files|*" };
                    if (bib_excel_dialog.ShowDialog() == true)
                    {
                        try
                        {
                            ExcelImporter excel = new ExcelImporter(bib_excel_dialog.FileName);
                            await Task.Run(() =>
                            {
                                excel.FetchHeaders();
                            });
                            BibChipAssociationWindow excelImp = new BibChipAssociationWindow(this, excel, database);
                            windows.Add(excelImp);
                            excelImp.Show();
                        }
                        catch
                        { }
                    }
                    break;
                case 19:
                    Log.D("View Bib Chip Associations");
                    BibChipViewer bibChipViewer = new BibChipViewer(database, this);
                    windows.Add(bibChipViewer);
                    bibChipViewer.Show();
                    break;
                case 20:
                    Log.D("Chip reader");
                    ChipReaderWindow chipWindow = new ChipReaderWindow(database, this);
                    windows.Add(chipWindow);
                    chipWindow.Show();
                    break;
                case 21:
                    Log.D("TimingWindow");
                    TimingWindow timingWindow = new TimingWindow(database, this);
                    windows.Add(timingWindow);
                    timingWindow.Show();
                    break;
                default:
                    break;
            }
        }

        internal async void UpdateTimingPoint(int timingPointIdentifier, string nameString)
        {
            int eventId = ((Event)eventsListView.SelectedItem).Identifier;
            await Task.Run(() =>
            {
                database.UpdateTimingLocation(new TimingLocation(timingPointIdentifier, eventId, nameString));
            });
            tcpServer.UpdateEvent(eventId);
            UpdateTimingPointsBox(eventId);
        }

        internal async void UpdateEvent(int eventIdentifier, string nameString, long dateVal, int nextyear, int shirtoptional, int shirtprice)
        {
            await Task.Run(() =>
            {
                database.UpdateEvent(new Event(eventIdentifier, nameString, dateVal, nextyear, shirtoptional, shirtprice));
            });
            tcpServer.UpdateEvent(eventIdentifier);
            UpdateEventBox();
        }

        internal async void UpdateDivision(int eventId, int divisionIdentifier, string nameString, string costString)
        {
            await Task.Run(() =>
            {
                String[] split = costString.Split('.');
                int dollar = 70;
                if (split.Length > 0)
                {
                    int.TryParse(split[0], out dollar);
                }
                dollar = dollar * 100;
                int cents = 0;
                if (split.Length > 1)
                {
                    int.TryParse(split[1], out cents);
                }
                while (cents > 100)
                {
                    cents = cents / 10;
                }
                dollar += cents;
                database.UpdateDivision(new Division(divisionIdentifier, nameString, eventId, dollar));
            });
            tcpServer.UpdateEvent(eventId);
            UpdateDivisionsBox(eventId);
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            String buttonName = ((Button)sender).Name;
            if (buttonName == "eventsRemoveButton")
            {
                Log.D("Events - Remove Button Pressed.");
                Event anEvent = (Event)eventsListView.SelectedItem;
                if (anEvent != null)
                {
                    database.RemoveEvent(anEvent);
                    tcpServer.UpdateEventList();
                }
                UpdateEventBox();
            }
            else if (buttonName == "divisionsRemoveButton")
            {
                Log.D("Divisions - Remove Button Pressed.");
                Division division = (Division)divisionsListView.SelectedItem;
                if (division != null) database.RemoveDivision(division);
                Event anEvent = (Event)eventsListView.SelectedItem;
                if (anEvent != null)
                {
                    UpdateDivisionsBox(anEvent.Identifier);
                    tcpServer.UpdateEvent(anEvent.Identifier);
                }
            }
            else if (buttonName == "timingPointsRemoveButton")
            {
                Log.D("TimingPoints - Remove Button Pressed.");
                TimingLocation timingPoint = (TimingLocation)timingPointsListView.SelectedItem;
                if (timingPoint != null) database.RemoveTimingLocation(timingPoint);
                Event anEvent = (Event)eventsListView.SelectedItem;
                if (anEvent != null)
                {
                    UpdateTimingPointsBox(anEvent.Identifier);
                    tcpServer.UpdateEvent(anEvent.Identifier);
                }
            }
        }

        private void ModifyButton_Click(object sender, RoutedEventArgs e)
        {
            String buttonName = ((Button)sender).Name;
            if (buttonName == "eventsModifyButton")
            {
                Log.D("Events - Modify Button Pressed.");
                Event thisEvent = (Event)eventsListView.SelectedItem;
                if (thisEvent != null)
                {
                    NewEventWindow win = new NewEventWindow(this, thisEvent);
                    windows.Add(win);
                    win.Show();
                }
            }
            else if (buttonName == "divisionsModifyButton")
            {
                Log.D("Divisions - Modify Button Pressed.");
                Division div = (Division)divisionsListView.SelectedItem;
                Event thisEvent = (Event)eventsListView.SelectedItem;
                if (div != null)
                {
                    NewDivisionWindow win = new NewDivisionWindow(this, thisEvent.Identifier, div.Identifier, div.Name, div.Cost);
                    windows.Add(win);
                    win.Show();
                }
            }
            else if (buttonName == "timingPointsModifyButton")
            {
                Log.D("TimingPoints - Modify Button Pressed.");
                TimingLocation tp = (TimingLocation)timingPointsListView.SelectedItem;
                List<Division> divisions = database.GetDivisions(((Event)eventsListView.SelectedItem).Identifier);
                if (divisions != null && divisions.Count > 0 && tp != null)
                {
                    NewTimingPointWindow win = new NewTimingPointWindow(this, tp.Identifier, tp.Name);
                    windows.Add(win);
                    win.Show();
                }
                else
                {
                    MessageBox.Show("No divisions found. Please add divisions before adding timing points.");
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            String buttonName = ((Button)sender).Name;
            if (buttonName == "eventsAddButton")
            {
                Log.D("Events - Add Button Pressed.");
                NewEventWindow win = new NewEventWindow(this);
                windows.Add(win);
                win.Show();
            }
            else if (buttonName == "divisionsAddButton")
            {
                Log.D("Divisions - Add Button Pressed.");
                NewDivisionWindow win = new NewDivisionWindow(this, ((Event)eventsListView.SelectedItem).Identifier);
                windows.Add(win);
                win.Show();
            }
            else if (buttonName == "timingPointsAddButton")
            {
                Log.D("TimingPoints - Add Button Pressed.");
                List<Division> divisions = database.GetDivisions(((Event)eventsListView.SelectedItem).Identifier);
                if (divisions != null && divisions.Count > 0)
                {
                    NewTimingPointWindow win = new NewTimingPointWindow(this);
                    windows.Add(win);
                    win.Show();
                }
                else
                {
                    MessageBox.Show("No divisions found. Please add divisions before adding timing points.");
                }
            }
        }

        internal async void AddEvent(String name, long date, int shirtOptional, int shirtPrice)
        {
            Event anEvent = new Event(name, date, shirtOptional, shirtPrice);
            int eventId = -1;
            await Task.Run(() =>
            {
                database.AddEvent(anEvent);
                eventId = database.GetEventID(anEvent);
            });
            tcpServer.UpdateEvent(eventId);
            UpdateEventBox();
        }

        internal async void AddTimingPoint(string nameString)
        {
            int eventId = ((Event)eventsListView.SelectedItem).Identifier;
            await Task.Run(() =>
            {
                database.AddTimingLocation(new TimingLocation(eventId, nameString));
            });
            tcpServer.UpdateEvent(eventId);
            UpdateTimingPointsBox(eventId);
        }

        internal async void AddDivision(int eventId, string nameString, string costString)
        {
            await Task.Run(() =>
            {
                String[] split = costString.Split('.');
                int dollar = 70;
                if (split.Length > 0)
                {
                    int.TryParse(split[0], out dollar);
                }
                dollar = dollar * 100;
                int cents = 0;
                if (split.Length > 1)
                {
                    int.TryParse(split[1], out cents);
                }
                while (cents > 100)
                {
                    cents = cents / 10;
                }
                dollar += cents;
                database.AddDivision(new Division(nameString, eventId, dollar));
            });
            tcpServer.UpdateEvent(eventId);
            UpdateDivisionsBox(eventId);
        }

        internal async void UpdateEventBox()
        {
            List<Event> events = null;
            await Task.Run(() =>
            {
                events = database.GetEvents();
            });
            eventsListView.ItemsSource = events;
            if (partList != null)
            {
                partList.UpdateEventsBox();
            }
        }

        public void UpdateChangesBox()
        {
            Log.D("Updating changes box.");
            List<Change> changes = database.GetChanges();
            List<ChangeParticipant> changeParts = new List<ChangeParticipant>();
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                Event anEvent = (Event)eventsListView.SelectedItem;
                foreach (Change c in changes)
                {
                    if (c.OldParticipant != null && (anEvent == null || anEvent.Identifier == c.OldParticipant.EventIdentifier))
                    {
                        changeParts.Add(new ChangeParticipant(c.Identifier, "Old", c.OldParticipant));
                    }
                    if (c.NewParticipant != null && (anEvent == null || anEvent.Identifier == c.NewParticipant.EventIdentifier))
                    {
                        changeParts.Add(new ChangeParticipant(c.Identifier, "New", c.NewParticipant));
                    }
                }
                changeParts.Sort();
                updateListView.ItemsSource = changeParts;
            }));
        }

        internal async void AsyncUpdateChangesBox()
        {
            await Task.Run(() =>
            {
                UpdateChangesBox();
            });
        }

        private async void UpdateTimingPointsBox(int eventId)
        {
            Log.D("Updating timing points box.");
            if (timingPointsListView.Visibility == Visibility.Hidden)
            {
                return;
            }
            List<TimingLocation> timingPoints = null;
            await Task.Run(() =>
            {
                timingPoints = database.GetTimingLocations(eventId);
            });
            timingPointsListView.ItemsSource = timingPoints;
        }

        private async void UpdateDivisionsBox(int eventId)
        {
            Log.D("Updating divisions box.");
            if (divisionsListView.Visibility == Visibility.Hidden)
            {
                return;
            }
            List<Division> divisions = null;
            await Task.Run(() =>
            {
                divisions = database.GetDivisions(eventId);
            });
            divisionsListView.ItemsSource = divisions;
        }

        private void EventsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null) { return; }
            UpdateEventButtons();
        }

        private void UpdateEventButtons()
        {
            Event anEvent = (Event)eventsListView.SelectedItem;
            if (anEvent != null)
            {
                eventsModifyButton.Visibility = Visibility.Visible;
                eventsRemoveButton.Visibility = Visibility.Visible;
                eventsToggleEarlyStartButton.Visibility = Visibility.Visible;
                eventsToggleRegistrationButton.Visibility = Visibility.Visible;
                eventsToggleResultsButton.Visibility = Visibility.Visible;
                divisionsLabel.Visibility = Visibility.Visible;
                divisionsAddButton.Visibility = Visibility.Visible;
                divisionsListView.Visibility = Visibility.Visible;
                timingPointsLabel.Visibility = Visibility.Visible;
                timingPointsAddButton.Visibility = Visibility.Visible;
                timingPointsListView.Visibility = Visibility.Visible;
                UpdateDivisionsBox(anEvent.Identifier);
                UpdateTimingPointsBox(anEvent.Identifier);
                List<JsonOption> options = database.GetEventOptions(anEvent.Identifier);
                foreach (JsonOption opt in options)
                {
                    if (opt.Name == "results_open")
                    {
                        if (opt.Value == "true")
                        {
                            eventsToggleResultsButton.Opacity = 1;
                        }
                        else
                        {
                            eventsToggleResultsButton.Opacity = 0.5;
                        }
                    }
                    else if (opt.Name == "allow_early_start")
                    {
                        if (opt.Value == "true")
                        {
                            eventsToggleEarlyStartButton.Opacity = 1;
                        }
                        else
                        {
                            eventsToggleEarlyStartButton.Opacity = 0.5;
                        }
                    }
                    else if (opt.Name == "registration_open")
                    {
                        if (opt.Value == "true")
                        {
                            eventsToggleRegistrationButton.Opacity = 1;
                        }
                        else
                        {
                            eventsToggleRegistrationButton.Opacity = 0.5;
                        }
                    }
                    else if (opt.Name == "kiosk")
                    {
                        if (opt.Value == "true")
                        {
                            eventsToggleKioskButton.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            eventsToggleKioskButton.Visibility = Visibility.Hidden;
                        }
                    }
                }
            }
            else
            {
                eventsModifyButton.Visibility = Visibility.Hidden;
                eventsRemoveButton.Visibility = Visibility.Hidden;
                eventsToggleEarlyStartButton.Visibility = Visibility.Hidden;
                eventsToggleRegistrationButton.Visibility = Visibility.Hidden;
                eventsToggleResultsButton.Visibility = Visibility.Hidden;
                divisionsLabel.Visibility = Visibility.Hidden;
                divisionsAddButton.Visibility = Visibility.Hidden;
                divisionsListView.Visibility = Visibility.Hidden;
                divisionsModifyButton.Visibility = Visibility.Hidden;
                divisionsRemoveButton.Visibility = Visibility.Hidden;
                timingPointsLabel.Visibility = Visibility.Hidden;
                timingPointsAddButton.Visibility = Visibility.Hidden;
                timingPointsListView.Visibility = Visibility.Hidden;
                timingPointsModifyButton.Visibility = Visibility.Hidden;
                timingPointsRemoveButton.Visibility = Visibility.Hidden;
                eventsToggleKioskButton.Visibility = Visibility.Hidden;
            }
            AsyncUpdateChangesBox();
        }

        private void TimingPointsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null) { return; }
            if (timingPointsListView.SelectedIndex < 0)
            {
                timingPointsModifyButton.Visibility = Visibility.Hidden;
                timingPointsRemoveButton.Visibility = Visibility.Hidden;
            }
            else
            {
                timingPointsModifyButton.Visibility = Visibility.Visible;
                timingPointsRemoveButton.Visibility = Visibility.Visible;
            }
        }

        private void DivisionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == null) { return; }
            if (divisionsListView.SelectedIndex < 0)
            {
                divisionsModifyButton.Visibility = Visibility.Hidden;
                divisionsRemoveButton.Visibility = Visibility.Hidden;
            }
            else
            {
                divisionsModifyButton.Visibility = Visibility.Visible;
                divisionsRemoveButton.Visibility = Visibility.Visible;
            }
        }

        public void PartListClosed()
        {
            Log.D("Participants list has closed.");
            partList = null;
        }

        public void DoPartListClosed()
        {
            Log.D("Day of Registrants list has closed.");
            doPartList = null;
        }

        public void WindowClosed(Window window)
        {
            if (closing == false)
            {
                windows.Remove(window);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true;
            if (partList != null) partList.Close();
            if (doPartList != null) doPartList.Close();
            foreach (Window w in windows)
            {
                w.Close();
            }
            tcpServer.Stop();
            tcpServerThread.Abort();
            zeroConf.Stop();
            zeroConfThread.Abort();
            Utils.excelApp.Quit();
        }

        private void EventsToggleRegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            bool value = false;
            if (eventsToggleRegistrationButton.Opacity < 1)
            {
                value = true;
                eventsToggleRegistrationButton.Opacity = 1;
            }
            else
            {
                eventsToggleRegistrationButton.Opacity = 0.5;
            }
            int eventId = ((Event)eventsListView.SelectedItem).Identifier;
            List<JsonOption> list = database.GetEventOptions(eventId);
            foreach (JsonOption opt in list)
            {
                if (opt.Name == "registration_open")
                {
                    opt.Value = value.ToString().ToLower();
                }
            }
            database.SetEventOptions(eventId, list);
            tcpServer.UpdateEvent(eventId);
        }

        private void EventsToggleResultsButton_Click(object sender, RoutedEventArgs e)
        {
            bool value = false;
            if (eventsToggleResultsButton.Opacity < 1)
            {
                value = true;
                eventsToggleResultsButton.Opacity = 1;
            }
            else
            {
                eventsToggleResultsButton.Opacity = 0.5;
            }
            int eventId = ((Event)eventsListView.SelectedItem).Identifier;
            List<JsonOption> list = database.GetEventOptions(eventId);
            foreach (JsonOption opt in list)
            {
                if (opt.Name == "results_open")
                {
                    opt.Value = value.ToString().ToLower();
                }
            }
            database.SetEventOptions(eventId, list);
            tcpServer.UpdateEvent(eventId);
        }

        private void EventsToggleEarlyStartButton_Click(object sender, RoutedEventArgs e)
        {
            bool value = false;
            if (eventsToggleEarlyStartButton.Opacity < 1)
            {
                value = true;
                eventsToggleEarlyStartButton.Opacity = 1;
            }
            else
            {
                eventsToggleEarlyStartButton.Opacity = 0.5;
            }
            int eventId = ((Event)eventsListView.SelectedItem).Identifier;
            List<JsonOption> list = database.GetEventOptions(eventId);
            foreach (JsonOption opt in list)
            {
                if (opt.Name == "allow_early_start")
                {
                    opt.Value = value.ToString().ToLower();
                }
            }
            database.SetEventOptions(eventId, list);
            tcpServer.UpdateEvent(eventId);
        }

        private void KioskButton_Click(object sender, RoutedEventArgs e)
        {
            bool value = false;
            int eventId = ((Event)eventsListView.SelectedItem).Identifier;
            List<JsonOption> list = database.GetEventOptions(eventId);
            foreach (JsonOption opt in list)
            {
                if (opt.Name == "kiosk")
                {
                    opt.Value = value.ToString().ToLower();
                }
            }
            database.SetEventOptions(eventId, list);
            tcpServer.UpdateEvent(eventId);
            UpdateEventButtons();
        }

        public void EnableKiosk(int eventId)
        {
            bool value = true;
            List<JsonOption> list = database.GetEventOptions(eventId);
            foreach (JsonOption opt in list)
            {
                if (opt.Name == "kiosk")
                {
                    opt.Value = value.ToString().ToLower();
                }
            }
            database.SetEventOptions(eventId, list);
            tcpServer.UpdateEvent(eventId);
            tcpServer.UpdateEventKiosk(eventId);
            UpdateEventButtons();
        }

        public void NextYearSetupFinalize(int eventId)
        {
            tcpServer.UpdateEventList();
            UpdateEventBox();
        }

        public async void UpdateSettings(String name)
        {
            string newName = "Northwest Endurance Events";
            await Task.Run(() =>
            {
                database.SetServerName(name);
                newName = database.GetServerName();
            });
            zeroConf.SetName(newName);
        }

        public void AddWindow(Window win)
        {
            windows.Add(win);
        }

        private async void CheatingInit()
        {
            await Task.Run(() =>
            {
                Utils.UppercaseFirst("Something");
            });
        }

        void IMainWindow.UpdateEvent(int identifier, string nameString, long dateVal, int nextYear, int shirtOptionalVal, int shirtPrice)
        {
            this.UpdateEvent(identifier, nameString, dateVal, nextYear, shirtOptionalVal, shirtPrice);
        }

        void IMainWindow.AddEvent(string nameString, long dateVal, int shirtOptionalVal, int shirtPrice)
        {
            this.AddEvent(nameString, dateVal, shirtOptionalVal, shirtPrice);
        }
    }
}
