using EventDirector.Interfaces;
using EventDirector.UI.ChipAssignment;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for ChipAssigmentPage.xaml
    /// </summary>
    public partial class ChipAssigmentPage : Page, IMainPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;
        private AppSetting chipType;

        private bool BibsChanged = false;
        private readonly Regex allowedChars = new Regex("[^0-9]");
        private readonly Regex allowedHexChars = new Regex("[^0-9a-fA-F]");

        public ChipAssigmentPage(IMainWindow mWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mWindow;
            this.database = database;
            chipType = database.GetAppSetting(Constants.Settings.DEFAULT_CHIP_TYPE);
            if (chipType.value == Constants.Settings.CHIP_TYPE_DEC)
            {
                ChipTypeBox.SelectedIndex = 0;
            }
            else if (chipType.value == Constants.Settings.CHIP_TYPE_HEX)
            {
                ChipTypeBox.SelectedIndex = 1;
            }
            ChipTypeBox.SelectionChanged += new SelectionChangedEventHandler(ChipTypeBox_SelectionChanged);
        }

        private void BibChipList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Log.D("BibChipList size has changed.");
            ListView listView = sender as ListView;
            GridView gView = listView.View as GridView;

            var workingWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth - 10;
            gView.Columns[0].Width = workingWidth * 0.5;
            gView.Columns[1].Width = workingWidth * 0.5;
        }

        public async void UpdateView()
        {
            theEvent = database.GetCurrentEvent();
            if (theEvent == null)
            {
                return;
            }
            List<BibChipAssociation> list = new List<BibChipAssociation>();
            await Task.Run(() =>
            {
                list = database.GetBibChips(theEvent.Identifier);
                list.Sort();
            });
            bibChipList.ItemsSource = list;
            long maxChip = 0;
            long chip = -1;
            // check if hex before using a convert
            if (Constants.Settings.CHIP_TYPE_DEC == chipType.value)
            {
                foreach (BibChipAssociation b in list)
                {
                    chip = Convert.ToInt64(b.Chip);
                    maxChip = chip > maxChip ? chip : maxChip;
                }
            }
            else if (Constants.Settings.CHIP_TYPE_HEX == chipType.value)
            {
                foreach (BibChipAssociation b in list)
                {
                    chip = Convert.ToInt64(b.Chip, 16);
                    maxChip = chip > maxChip ? chip : maxChip;
                }
            }
            maxChip += 1;
            if (Constants.Settings.CHIP_TYPE_DEC == chipType.value)
            {
            }
            else if (Constants.Settings.CHIP_TYPE_HEX == chipType.value)
            {
                SingleChipBox.Text = maxChip.ToString("X");
                RangeStartChipBox.Text = maxChip.ToString("X");
            }
            List<Event> events = new List<Event>();
            await Task.Run(() =>
            {
                events = database.GetEvents();
                events.Sort();
            });
            previousEvents.Items.Clear();
            ComboBoxItem boxItem = new ComboBoxItem
            {
                Content = "None",
                Uid = "-1"
            };
            previousEvents.Items.Add(boxItem);
            foreach (Event e in events)
            {
                if (!e.Equals(theEvent))
                {
                    String name = e.YearCode + " " + e.Name;
                    name = name.Trim();
                    boxItem = new ComboBoxItem
                    {
                        Content = name,
                        Uid = e.Identifier.ToString()
                    };
                    previousEvents.Items.Add(boxItem);
                }
            }
            previousEvents.SelectedIndex = 0;
        }

        private void SaveSingleButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Save Single clicked.");
            long chip = -1, bib = -1;
            long.TryParse(SingleBibBox.Text, out bib);
            if (Constants.Settings.CHIP_TYPE_DEC == chipType.value)
            {
                long.TryParse(SingleChipBox.Text, out chip);
            }
            else if (Constants.Settings.CHIP_TYPE_HEX == chipType.value)
            {
                long.TryParse(SingleChipBox.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out chip);
            }
            Log.D("Bib " + bib + " Chip " + chip);
            if (chip == -1 || bib == -1)
            {
                MessageBox.Show("The bib or chip is not valid.");
                return;
            }
            List<BibChipAssociation> bibChips = new List<BibChipAssociation>
            {
                new BibChipAssociation()
                {
                    Bib = (int) bib,
                    Chip = Constants.Settings.CHIP_TYPE_DEC == chipType.value ? chip.ToString() : chip.ToString("X")
                }
            };
            database.AddBibChipAssociation(theEvent.Identifier, bibChips);
            BibsChanged = true;
            UpdateView();
            SingleBibBox.Text = (bib + 1).ToString();
            SingleBibBox.Focus();
        }

        private void SaveRangeButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Save Range clicked.");
            long startChip = -1, endChip = -1, startBib = -1, endBib = -1;
            long.TryParse(RangeStartBibBox.Text, out startBib);
            long.TryParse(RangeEndBibBox.Text, out endBib);
            if (Constants.Settings.CHIP_TYPE_DEC == chipType.value)
            {
                long.TryParse(RangeStartChipBox.Text, out startChip);
                long.TryParse(RangeEndChipLabel.Content.ToString(), out endChip);
            }
            else if (Constants.Settings.CHIP_TYPE_HEX == chipType.value)
            {
                long.TryParse(RangeStartChipBox.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out startChip);
                long.TryParse(RangeEndChipLabel.Content.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out endChip);
            }
            Log.D("StartBib " + startBib + " EndBib " + endBib + " StartChip " + startChip + " EndChip " + endChip);
            if (startChip == -1 || endChip == -1 || startBib == -1 || endBib == -1)
            {
                MessageBox.Show("One or more values is not valid.");
                return;
            }
            List<BibChipAssociation> bibChips = new List<BibChipAssociation>();
            for (long bib = startBib, tag = startChip; bib <= endBib && tag <= endChip; bib++, tag++)
            {
                bibChips.Add(new BibChipAssociation() {
                    Bib = (int)bib,
                    Chip = Constants.Settings.CHIP_TYPE_HEX == chipType.value ? tag.ToString("X") : tag.ToString()
                });
            }
            database.AddBibChipAssociation(theEvent.Identifier, bibChips);
            BibsChanged = true;
            UpdateView();
            RangeStartBibBox.Text = (endBib + 1).ToString();
            RangeEndBibBox.Text = (endBib + 1).ToString();
            RangeStartBibBox.Focus();
        }

        private async void FileImport_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Import from file clicked.");
            OpenFileDialog bib_dialog = new OpenFileDialog() { Filter = "CSV Files (*.csv,*.txt)|*.csv;*.txt|All files|*" };
            if (bib_dialog.ShowDialog() == true) {
                try
                {
                    CSVImporter importer = new CSVImporter(bib_dialog.FileName);
                    await Task.Run(() =>
                    {
                        importer.FetchHeaders();
                    });
                    BibChipAssociationWindow bcWindow = BibChipAssociationWindow.NewWindow(mWindow, importer, database);
                    if (bcWindow != null)
                    {
                        mWindow.AddWindow(bcWindow);
                        bcWindow.ShowDialog();
                        if (bcWindow.ImportComplete)
                        {
                            BibsChanged = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.E("Something went wrong when trying to read the CSV file.");
                    Log.E(ex.StackTrace);
                }
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Copy clicked.");
            int oldEventId = Convert.ToInt32(((ComboBoxItem)previousEvents.SelectedItem).Uid);
            Log.D("Old event Id is " + oldEventId);
            if (oldEventId > 0)
            {
                List<BibChipAssociation> assocs = database.GetBibChips(oldEventId);
                database.AddBibChipAssociation(theEvent.Identifier, assocs);
                BibsChanged = true;
                UpdateView();
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Delete clicked.");
            Log.D("Attempting to delete.");
            IList selected = bibChipList.SelectedItems;
            List<BibChipAssociation> items = new List<BibChipAssociation>();
            foreach (BibChipAssociation b in selected)
            {
                items.Add(b);
            }
            database.RemoveBibChipAssociations(items);
            BibsChanged = true;
            UpdateView();
        }

        private void UseTool_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Use Tool clicked.");
            ChipTool chipTool = ChipTool.NewWindow(mWindow, database);
            if (chipTool != null)
            {
                mWindow.AddWindow(chipTool);
                chipTool.ShowDialog();
                if (chipTool.ImportComplete)
                {
                    BibsChanged = true;
                }
            }
        }

        private void KeyPressHandlerRange(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveRangeButton_Click(null, null);
            }
        }

        private void KeyPressHandlerSingle(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveSingleButton_Click(null, null);
            }
        }

        private void UpdateEndChip(object sender, TextChangedEventArgs e)
        {
            long startBib = -1, endBib = -1, startChip = -1, endChip = -1;
            long.TryParse(RangeStartBibBox.Text, out startBib);
            long.TryParse(RangeEndBibBox.Text, out endBib);
            if (Constants.Settings.CHIP_TYPE_DEC == chipType.value)
            {
                long.TryParse(RangeStartChipBox.Text, out startChip);
            }
            else if (Constants.Settings.CHIP_TYPE_HEX == chipType.value)
            {
                long.TryParse(RangeStartChipBox.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out startChip);
            }
            endChip = endBib - startBib + startChip;
            if (startBib > -1 && endBib > -1 && startChip > -1)
            {
                if (Constants.Settings.CHIP_TYPE_DEC == chipType.value)
                {
                    RangeEndChipLabel.Content = endChip.ToString();
                }
                else if (Constants.Settings.CHIP_TYPE_HEX == chipType.value)
                {
                    RangeEndChipLabel.Content = endChip.ToString("X");
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete everything? This cannot be undone.",
                                                        "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                List<BibChipAssociation> list = (List<BibChipAssociation>) bibChipList.ItemsSource;
                database.RemoveBibChipAssociations(list);
                BibsChanged = true;
                UpdateView();
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Export clicked.");
            ExportBibChip exportBibChip = ExportBibChip.NewWindow(mWindow, database, mWindow.ExcelEnabled());
            if (exportBibChip != null)
            {
                mWindow.AddWindow(exportBibChip);
                exportBibChip.ShowDialog();
            }
        }

        public void UpdateDatabase() { }

        public void Keyboard_Ctrl_A()
        {
            UseTool_Click(null, null);
        }

        public void Keyboard_Ctrl_S()
        {
            Export_Click(null, null);
        }

        public void Keyboard_Ctrl_Z() { }

        public void Closing()
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            if (BibsChanged)
            {
                database.ResetTimingResultsEvent(theEvent.Identifier);
                mWindow.NetworkClearResults(theEvent.Identifier);
                mWindow.NotifyTimingWorker();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateView();
            SingleBibBox.Focus();
        }

        private void SelectAll(object sender, RoutedEventArgs e)
        {
            TextBox src = (TextBox)e.OriginalSource;
            src.SelectAll();
        }

        private void BibValidation(object sender, TextCompositionEventArgs e)
        {
            e.Handled = allowedChars.IsMatch(e.Text);
        }

        private void ChipValidation(object sender, TextCompositionEventArgs e)
        {
            if (Constants.Settings.CHIP_TYPE_DEC == chipType.value)
            {
                e.Handled = allowedChars.IsMatch(e.Text);
            }
            else if (Constants.Settings.CHIP_TYPE_HEX == chipType.value)
            {
                e.Handled = allowedHexChars.IsMatch(e.Text);
            }
        }

        private void ChipTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (0 == ChipTypeBox.SelectedIndex)
            {
                database.SetAppSetting(Constants.Settings.DEFAULT_CHIP_TYPE, Constants.Settings.CHIP_TYPE_DEC);
                SingleChipBox.Text = "0";
                RangeStartChipBox.Text = "0";
            }
            else if (1 == ChipTypeBox.SelectedIndex)
            {
                database.SetAppSetting(Constants.Settings.DEFAULT_CHIP_TYPE, Constants.Settings.CHIP_TYPE_HEX);
            }
            chipType = database.GetAppSetting(Constants.Settings.DEFAULT_CHIP_TYPE);
        }
    }
}
