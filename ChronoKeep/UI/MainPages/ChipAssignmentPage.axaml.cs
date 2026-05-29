using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.IO;
using Chronokeep.Interfaces.UI;
using Chronokeep.IO;
using Chronokeep.Objects;
using Chronokeep.UI.ChipAssignment;
using Chronokeep.UI.Parts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Chronokeep.UI.MainPages;

public partial class ChipAssignmentPage : UserControl, IMainPage
{

    private readonly IMainWindow mWindow;
    private readonly IDBInterface database;
    private readonly Event? theEvent;
    private AppSetting chipType;

    private bool BibsChanged = false;

    [GeneratedRegex("[^0-9]")]
    private static partial Regex AllowedChars();
    [GeneratedRegex("[^0-9a-fA-F]")]
    private static partial Regex AllowedHexChars();

    public ChipAssignmentPage(IMainWindow mWindow, IDBInterface database)
    {
        InitializeComponent();
        this.mWindow = mWindow;
        this.database = database;
        chipType = database.GetAppSetting(Constants.Settings.DEFAULT_CHIP_TYPE)!;
        if (chipType.Value == Constants.Settings.CHIP_TYPE_DEC)
        {
            ChipTypeBox.SelectedIndex = 0;
        }
        else if (chipType.Value == Constants.Settings.CHIP_TYPE_HEX)
        {
            ChipTypeBox.SelectedIndex = 1;
        }
        ChipTypeBox.SelectionChanged += ChipTypeBox_SelectionChanged;
        theEvent = database.GetCurrentEvent();
        UpdateView();
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
                _ = long.TryParse(b.Chip, out chip);
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
            Tag = "-1"
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
                    Tag = e.Identifier.ToString()
                };
                previousEvents.Items.Add(boxItem);
            }
        }
        previousEvents.SelectedIndex = 0;
    }

    public static void UpdateDatabase() { }

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
        if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE)!.Value == Constants.Settings.SETTING_TRUE)
        {
            UpdateDatabase();
        }
        if (BibsChanged)
        {
            database.ResetTimingResultsEvent(theEvent!.Identifier);
            mWindow.NetworkClearResults();
            mWindow.NotifyTimingWorker();
        }
    }

    private void Delete_Click(object? sender, RoutedEventArgs? e)
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

    private void Clear_Click(object? sender, RoutedEventArgs? e)
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

    private void DeleteIgnored_Click(object? sender, RoutedEventArgs? e)
    {
        Log.D("UI.MainPages.ChipAssignmentPage", "Delete ignored clicked.");
        IList selected = ignoredChipList.SelectedItems;
        List<BibChipAssociation> items = [];
        foreach (BibChipAssociation b in selected)
        {
            items.Add(b);
        }
        database.RemoveBibChipAssociations(items);
        BibsChanged = true;
        UpdateView();
    }

    private void ClearIgnored_Click(object? sender, RoutedEventArgs? e)
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

    private void KeyPressHandlerSingle(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SaveSingleButton_Click(null, null);
        }
    }

    private void SelectAll(object? sender, FocusChangedEventArgs e)
    {
        TextBox? src = e.Source as TextBox;
        src?.SelectAll();
    }

    private void ChipValidation(object? sender, TextInputEventArgs e)
    {
        if (Constants.Settings.CHIP_TYPE_DEC == chipType.Value)
        {
            e.Handled = e.Text != null && AllowedChars().IsMatch(e.Text);
        }
        else if (Constants.Settings.CHIP_TYPE_HEX == chipType.Value)
        {
            e.Handled = e.Text != null && AllowedHexChars().IsMatch(e.Text);
        }
    }

    private void SaveSingleButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs? e)
    {
        Log.D("UI.MainPages.ChipAssignmentPage", "Save Single clicked.");
        long chip = -1;
        if (!long.TryParse(SingleBibBox.Text, out long bib))
        {
            bib = -1;
        }
        if (Constants.Settings.CHIP_TYPE_DEC == chipType.Value)
        {
            _ = long.TryParse(SingleChipBox.Text, out chip);
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
                    Bib = SingleBibBox.Text!,
                    Chip = Constants.Settings.CHIP_TYPE_DEC == chipType.Value ? chip.ToString() : chip.ToString("X")
                }
        ];
        database.AddBibChipAssociation(theEvent!.Identifier, bibChips);
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

    private async void FileImport_Click(object? sender, RoutedEventArgs? e)
    {
        Log.D("UI.MainPages.ChipAssignmentPage", "Import from file clicked.");

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            IStorageFolder? startingFolder;
            try
            {
                startingFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri(database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR)!.Value));
            }
            catch
            {
                startingFolder = null;
            }
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                FileTypeFilter = [Utils.ExcelType, FilePickerFileTypes.All],
                AllowMultiple = false,
                SuggestedStartLocation = startingFolder,
            });
            if (files.Count > 0)
            {
                string ext = Path.GetExtension(files[0].Name);
                try
                {
                    string? filePath = files[0].TryGetLocalPath();
                    Log.E("TEST", string.Format("Name: {0} -- Path: {1}", files[0].Name, filePath ?? "null"));
                    IDataImporter importer;
                    if (ext == ".xlsx" || ext == ".xls")
                    {
                        importer = new ExcelImporter(filePath!);
                    }
                    else
                    {
                        importer = new CSVImporter(filePath!);
                    }
                    await Task.Run(() =>
                    {
                        importer.FetchHeaders();
                    });
                    BibChipAssociationWindow bcWindow = BibChipAssociationWindow.NewWindow(mWindow, importer, database);
                    if (bcWindow != null)
                    {
                        mWindow.AddWindow(bcWindow);
                        await bcWindow.ShowDialog((Window)mWindow);
                        if (bcWindow.ImportComplete)
                        {
                            BibsChanged = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.E("UI.MainPages.ChipAssignmentPage", $"Something went wrong when trying to read the CSV file. {ex.StackTrace}");
                    DialogBox.Show("Unable to open file.");
                }
            }
        }
    }

    private void UseTool_Click(object? sender, RoutedEventArgs? e)
    {
        Log.D("UI.MainPages.ChipAssignmentPage", "Use Tool clicked.");
        ChipTool chipTool = ChipTool.NewWindow(mWindow, database);
        if (chipTool != null)
        {
            mWindow.AddWindow(chipTool);
            chipTool.ShowDialog((Window)mWindow);
            if (chipTool.ImportComplete)
            {
                BibsChanged = true;
            }
        }
    }

    private async void Export_Click(object? sender, RoutedEventArgs? e)
    {
        Log.D("UI.MainPages.ChipAssignmentPage", "Export clicked.");
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            IStorageFolder? startingFolder;
            try
            {
                startingFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri(database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR)!.Value));
            }
            catch
            {
                startingFolder = null;
            }
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                FileTypeChoices = [Utils.ExcelType],
                SuggestedFileName = string.Format("{0} {1} Chips.{2}", theEvent!.YearCode, theEvent.Name, "xlsx"),
                SuggestedStartLocation = startingFolder,
            });
            if (file is not null)
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
                string extension = Path.GetExtension(file.Name);
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
                exporter.ExportData(file.Name);
                DialogBox.Show("File saved.");
            }
        }
    }

    private void KeyPressHandlerRange(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SaveRangeButton_Click(null, null);
        }
    }

    private void UpdateEndChip(object? sender, TextChangedEventArgs e)
    {
        long startChip = -1, endChip;
        _ = long.TryParse(RangeStartBibBox.Text, out long startBib);
        _ = long.TryParse(RangeEndBibBox.Text, out long endBib);
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

    private void SaveRangeButton_Click(object? sender, RoutedEventArgs? e)
    {
        Log.D("UI.MainPages.ChipAssignmentPage", "Save Range clicked.");
        long startChip = -1, endChip = -1;
        if (!long.TryParse(RangeStartBibBox.Text, out long startBib) || !long.TryParse(RangeEndBibBox.Text, out long endBib))
        {
            DialogBox.Show("Invalid bibs for range based assignment.");
            return;
        }
            ;
        if (Constants.Settings.CHIP_TYPE_DEC == chipType.Value)
        {
            if (!long.TryParse(RangeStartChipBox.Text, out startChip) ||
                !long.TryParse(RangeEndChipLabel.Text!.ToString(), out endChip))
            {
                DialogBox.Show("Invalid chip values.");
                return;
            }
        }
        else if (Constants.Settings.CHIP_TYPE_HEX == chipType.Value)
        {
            if (!long.TryParse(RangeStartChipBox.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out startChip) ||
                !long.TryParse(RangeEndChipLabel.Text!.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out endChip))
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
            bibChips.Add(new()
            {
                Bib = bib.ToString(),
                Chip = Constants.Settings.CHIP_TYPE_HEX == chipType.Value ? tag.ToString("X") : tag.ToString()
            });
        }
        database.AddBibChipAssociation(theEvent!.Identifier, bibChips);
        BibsChanged = true;
        UpdateView();
        RangeStartBibBox.Text = (endBib + 1).ToString();
        RangeEndBibBox.Text = (endBib + 1).ToString();
        RangeStartBibBox.Focus();
    }

    private void Copy_Click(object? sender, RoutedEventArgs? e)
    {
        Log.D("UI.MainPages.ChipAssignmentPage", "Copy clicked.");
        int oldEventId = Convert.ToInt32((string)((ComboBoxItem)previousEvents.SelectedItem!).Tag!);
        Log.D("UI.MainPages.ChipAssignmentPage", "Old event Id is " + oldEventId);
        if (oldEventId > 0)
        {
            List<BibChipAssociation> assocs = database.GetBibChips(oldEventId);
            database.AddBibChipAssociation(theEvent!.Identifier, assocs);
            BibsChanged = true;
            UpdateView();
        }
    }

    private void KeyPressHandlerIgnored(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SaveIgnored_Click(null, null);
        }
    }

    private void SaveIgnored_Click(object? sender, RoutedEventArgs? e)
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
                    Bib = IgnoredChipBox.Text!,
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

    private void ChipTypeBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
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
        chipType = database.GetAppSetting(Constants.Settings.DEFAULT_CHIP_TYPE)!;
    }
}