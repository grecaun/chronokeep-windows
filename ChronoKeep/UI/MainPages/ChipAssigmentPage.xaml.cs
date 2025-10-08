using Chronokeep.UI.ChipAssignment;
using Chronokeep.UI.IO;
using Microsoft.Win32;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Chronokeep.UI.UIObjects;
using Chronokeep.Helpers;
using Chronokeep.IO;
using Chronokeep.Database;
using Chronokeep.Interfaces.IO;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;

namespace Chronokeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for ChipAssigmentPage.xaml
    /// </summary>
    public partial class ChipAssigmentPage : IMainPage
    {
        private readonly IMainWindow mWindow;
        private readonly IDBInterface database;
        private readonly Event theEvent;
        private AppSetting chipType;

        private bool BibsChanged = false;

        [GeneratedRegex("[^0-9]")]
        private static partial Regex AllowedChars();
        [GeneratedRegex("[^0-9a-fA-F]")]
        private static partial Regex AllowedHexChars();

        public ChipAssigmentPage(IMainWindow mWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mWindow;
            this.database = database;
            chipType = database.GetAppSetting(Constants.Settings.DEFAULT_CHIP_TYPE);
            if (chipType.Value == Constants.Settings.CHIP_TYPE_DEC)
            {
                ChipTypeBox.SelectedIndex = 0;
            }
            else if (chipType.Value == Constants.Settings.CHIP_TYPE_HEX)
            {
                ChipTypeBox.SelectedIndex = 1;
            }
            ChipTypeBox.SelectionChanged += new SelectionChangedEventHandler(ChipTypeBox_SelectionChanged);
            theEvent = database.GetCurrentEvent();
        }

        public async void UpdateView()
        {
            if (theEvent == null)
            {
                return;
            }
            List<BibChipAssociation> list = [];
            List<BibChipAssociation> ignored = [];
            await Task.Run(() =>
            {
                list = database.GetBibChips(theEvent.Identifier);
                list.Sort();
                ignored = database.GetBibChips(-1);
                ignored.Sort();
            });
            bibChipList.ItemsSource = list;
            ignoredChipList.ItemsSource = ignored;
            long maxChip = 0;
            long chip = -1;
            // check if hex before using a convert
            if (Constants.Settings.CHIP_TYPE_DEC == chipType.Value)
            {
                foreach (BibChipAssociation b in list)
                {
                    long.TryParse(b.Chip, out chip);
                    maxChip = chip > maxChip ? chip : maxChip;
                }
            }
            else if (Constants.Settings.CHIP_TYPE_HEX == chipType.Value)
            {
                foreach (BibChipAssociation b in list)
                {
                    long.TryParse(b.Chip, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out chip);
                    maxChip = chip > maxChip ? chip : maxChip;
                }
            }
            maxChip += 1;
            if (Constants.Settings.CHIP_TYPE_DEC == chipType.Value)
            {
                SingleChipBox.Text = maxChip.ToString();
                RangeStartChipBox.Text = maxChip.ToString();
            }
            else if (Constants.Settings.CHIP_TYPE_HEX == chipType.Value)
            {
                SingleChipBox.Text = maxChip.ToString("X");
                RangeStartChipBox.Text = maxChip.ToString("X");
            }
            List<Event> events = [];
            await Task.Run(() =>
            {
                events = database.GetEvents();
                events.Sort();
            });
            previousEvents.Items.Clear();
            ComboBoxItem boxItem = new()
            {
                Content = "None",
                Uid = "-1"
            };
            previousEvents.Items.Add(boxItem);
            foreach (Event e in events)
            {
                if (!e.Equals(theEvent))
                {
                    string name = e.YearCode + " " + e.Name;
                    name = name.Trim();
                    boxItem = new()
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
            Log.D("UI.MainPages.ChipAssignmentPage", "Save Single clicked.");
            long chip = -1, bib = -1;
            if (!long.TryParse(SingleBibBox.Text, out bib))
            {
                bib = -1;
            }
            if (Constants.Settings.CHIP_TYPE_DEC == chipType.Value)
            {
                long.TryParse(SingleChipBox.Text, out chip);
            }
            else if (Constants.Settings.CHIP_TYPE_HEX == chipType.Value)
            {
                long.TryParse(SingleChipBox.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out chip);
            }
            Log.D("UI.MainPages.ChipAssignmentPage", "Bib " + bib + " Chip " + chip);
            if (chip == -1)
            {
                DialogBox.Show("The chip is not valid.");
                return;
            }
            List<BibChipAssociation> bibChips =
            [
                new()
                {
                    Bib = SingleBibBox.Text,
                    Chip = Constants.Settings.CHIP_TYPE_DEC == chipType.Value ? chip.ToString() : chip.ToString("X")
                }
            ];
            database.AddBibChipAssociation(theEvent.Identifier, bibChips);
            BibsChanged = true;
            UpdateView();
            if (bib > -1)
            {
                SingleBibBox.Text = (bib + 1).ToString();
            }
            else
            {
                SingleBibBox.Text = "";
            }
            SingleBibBox.Focus();
        }

        private void SaveRangeButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.ChipAssignmentPage", "Save Range clicked.");
            long startChip = -1, endChip = -1, startBib, endBib;
            if (!long.TryParse(RangeStartBibBox.Text, out startBib) || !long.TryParse(RangeEndBibBox.Text, out endBib))
            {
                DialogBox.Show("Invalid bibs for range based assignment.");
                return;
            }
            ;
            if (Constants.Settings.CHIP_TYPE_DEC == chipType.Value)
            {
                if (!long.TryParse(RangeStartChipBox.Text, out startChip) ||
                    !long.TryParse(RangeEndChipLabel.Text.ToString(), out endChip))
                {
                    DialogBox.Show("Invalid chip values.");
                    return;
                }
            }
            else if (Constants.Settings.CHIP_TYPE_HEX == chipType.Value)
            {
                if (!long.TryParse(RangeStartChipBox.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out startChip) ||
                    !long.TryParse(RangeEndChipLabel.Text.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out endChip) )
                {
                    DialogBox.Show("Invalid chip values.");
                    return;
                }
            }
            Log.D("UI.MainPages.ChipAssignmentPage", "StartBib " + startBib + " EndBib " + endBib + " StartChip " + startChip + " EndChip " + endChip);
            if (startChip == -1 || endChip == -1 || startBib == -1 || endBib == -1)
            {
                DialogBox.Show("One or more values is not valid.");
                return;
            }
            List<BibChipAssociation> bibChips = [];
            for (long bib = startBib, tag = startChip; bib <= endBib && tag <= endChip; bib++, tag++)
            {
                bibChips.Add(new() {
                    Bib = bib.ToString(),
                    Chip = Constants.Settings.CHIP_TYPE_HEX == chipType.Value ? tag.ToString("X") : tag.ToString()
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
            Log.D("UI.MainPages.ChipAssignmentPage", "Import from file clicked.");
            OpenFileDialog bib_dialog = new() { Filter = "Excel files (*.xlsx,*.xls,*.csv,*.txt)|*.xlsx;*.xls;*.csv;*.txt|All files|*" };
            if (bib_dialog.ShowDialog() == true)
            {
                string ext = Path.GetExtension(bib_dialog.FileName);
                try
                {
                    IDataImporter importer;
                    if (ext == ".xlsx" || ext == ".xls")
                    {
                        importer = new ExcelImporter(bib_dialog.FileName);
                    }
                    else
                    {
                        importer = new CSVImporter(bib_dialog.FileName);
                    }
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
                    Log.E("UI.MainPages.ChipAssignmentPage", $"Something went wrong when trying to read the CSV file. {ex.StackTrace}");
                }
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.ChipAssignmentPage", "Copy clicked.");
            int oldEventId = Convert.ToInt32(((ComboBoxItem)previousEvents.SelectedItem).Uid);
            Log.D("UI.MainPages.ChipAssignmentPage", "Old event Id is " + oldEventId);
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
            Log.D("UI.MainPages.ChipAssignmentPage", "Delete clicked.");
            IList selected = bibChipList.SelectedItems;
            List<BibChipAssociation> items = [];
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
            Log.D("UI.MainPages.ChipAssignmentPage", "Use Tool clicked.");
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

        private void KeyPressHandlerIgnored(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveIgnored_Click(null, null);
            }
        }

        private void UpdateEndChip(object sender, TextChangedEventArgs e)
        {
            long startBib = -1, endBib = -1, startChip = -1, endChip = -1;
            long.TryParse(RangeStartBibBox.Text, out startBib);
            long.TryParse(RangeEndBibBox.Text, out endBib);
            if (Constants.Settings.CHIP_TYPE_DEC == chipType.Value)
            {
                if (!long.TryParse(RangeStartChipBox.Text, out startChip))
                {
                    return;
                }
            }
            else if (Constants.Settings.CHIP_TYPE_HEX == chipType.Value)
            {
                if (!long.TryParse(RangeStartChipBox.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out startChip))
                {
                    return;
                }
            }
            endChip = endBib - startBib + startChip;
            if (startBib > -1 && endBib > -1 && startChip > -1)
            {
                if (Constants.Settings.CHIP_TYPE_DEC == chipType.Value)
                {
                    RangeEndChipLabel.Text = endChip.ToString();
                }
                else if (Constants.Settings.CHIP_TYPE_HEX == chipType.Value)
                {
                    RangeEndChipLabel.Text = endChip.ToString("X");
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            DialogBox.Show(
                "Are you sure you want to delete everything? This cannot be undone.",
                "Yes",
                "No",
                () =>
                {
                    database.RemoveBibChipAssociations((List<BibChipAssociation>)bibChipList.ItemsSource);
                    BibsChanged = true;
                    UpdateView();
                }
                );
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.ChipAssignmentPage", "Export clicked.");
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "Excel File (*.xlsx,*xls)|*.xlsx;*xls|CSV (*.csv)|*.csv",
                FileName = string.Format("{0} {1} Chips.{2}", theEvent.YearCode, theEvent.Name, "xlsx"),
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                List<object[]> data = [];
                List<BibChipAssociation> associations = database.GetBibChips(theEvent.Identifier);
                associations.Sort();
                foreach (BibChipAssociation association in associations)
                {
                    Log.D("UI.MainPages.ChipAssignmentPage", "Checking associations ... Bib " + association.Bib + " Chip " + association.Chip);
                }
                string[] headers = ["Bib", "Chip"];
                foreach (BibChipAssociation bca in associations)
                {
                    data.Add([bca.Bib, bca.Chip]);
                }
                IDataExporter exporter;
                string extension = Path.GetExtension(saveFileDialog.FileName);
                Log.D("UI.MainPages.ChipAssignmentPage", string.Format("Extension is '{0}'", extension));
                if (extension.Contains("xls", StringComparison.CurrentCulture))
                {
                    exporter = new ExcelExporter();
                }
                else
                {
                    StringBuilder format = new();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        format.Append("\"{");
                        format.Append(i);
                        format.Append("}\",");
                    }
                    format.Remove(format.Length - 1, 1);
                    Log.D("UI.MainPages.ChipAssignmentPage", string.Format("The format is '{0}'", format.ToString()));
                    exporter = new CSVExporter(format.ToString());
                }
                exporter.SetData(headers, data);
                exporter.ExportData(saveFileDialog.FileName);
                DialogBox.Show("File saved.");
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
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            if (BibsChanged)
            {
                database.ResetTimingResultsEvent(theEvent.Identifier);
                mWindow.NetworkClearResults();
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

        private void ChipValidation(object sender, TextCompositionEventArgs e)
        {
            if (Constants.Settings.CHIP_TYPE_DEC == chipType.Value)
            {
                e.Handled = AllowedChars().IsMatch(e.Text);
            }
            else if (Constants.Settings.CHIP_TYPE_HEX == chipType.Value)
            {
                e.Handled = AllowedHexChars().IsMatch(e.Text);
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

        private void SaveIgnored_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.ChipAssignmentPage", "Save Ignored clicked.");
            long chip = -1, bib = -1;
            if (Constants.Settings.CHIP_TYPE_DEC == chipType.Value)
            {
                _ = long.TryParse(IgnoredChipBox.Text, out chip);
            }
            else if (Constants.Settings.CHIP_TYPE_HEX == chipType.Value)
            {
                _ = long.TryParse(IgnoredChipBox.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out chip);
            }
            Log.D("UI.MainPages.ChipAssignmentPage", "Bib " + bib + " Chip " + chip);
            if (chip == -1)
            {
                DialogBox.Show("The chip is not valid.");
                return;
            }
            List<BibChipAssociation> bibChips =
            [
                new()
                {
                    Bib = IgnoredChipBox.Text,
                    Chip = Constants.Settings.CHIP_TYPE_DEC == chipType.Value ? chip.ToString() : chip.ToString("X")
                }
            ];
            database.AddBibChipAssociation(-1, bibChips);
            Globals.UpdateIgnoredChips(database);
            BibsChanged = true;
            UpdateView();
            if (bib > -1)
            {
                IgnoredChipBox.Text = (bib + 1).ToString();
            }
            else
            {
                IgnoredChipBox.Text = "";
            }
            IgnoredChipBox.Focus();
        }

        private void ClearIgnored_Click(object sender, RoutedEventArgs e)
        {
            DialogBox.Show(
                "Are you sure you want to delete everything? This cannot be undone.",
                "Yes",
                "No",
                () =>
                {
                    database.RemoveBibChipAssociations((List<BibChipAssociation>)ignoredChipList.ItemsSource);
                    BibsChanged = true;
                    UpdateView();
                }
                );
        }

        private void DeleteIgnored_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.ChipAssignmentPage", "Delete ignored clicked.");
            IList selected = ignoredChipList.SelectedItems;
            List<BibChipAssociation> items = new();
            foreach (BibChipAssociation b in selected)
            {
                items.Add(b);
            }
            database.RemoveBibChipAssociations(items);
            BibsChanged = true;
            UpdateView();
        }
    }
}
