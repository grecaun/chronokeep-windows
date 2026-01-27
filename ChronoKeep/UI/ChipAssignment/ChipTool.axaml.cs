using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;
using Chronokeep.UI.Parts;

namespace Chronokeep.UI.ChipAssignment;

public partial class ChipTool : Window
{
    private readonly IWindowCallback window;
    private readonly IDBInterface database;

    public bool ImportComplete = false;

    public ChipTool()
    {
        InitializeComponent();
        correlationBox.Items.Add(new ATagRange(correlationBox));
    }

    private ChipTool(IWindowCallback window, IDBInterface database)
    {
        InitializeComponent();
        correlationBox.Items.Add(new ATagRange(correlationBox));
        this.window = window;
        this.database = database;
        this.MinHeight = 100;
        this.MinWidth = 550;
        this.ResizeMode = ResizeMode.NoResize;
    }

    public static ChipTool NewWindow(IWindowCallback window, IDBInterface database)
    {
        return new(window, database);
    }

    private void AddRange_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        correlationBox.Items.Add(new ATagRange(correlationBox));
    }

    private void Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        List<Range> ranges = [];
        foreach (ATagRange tag in correlationBox.Items)
        {
            _ = int.TryParse(tag.StartBib.Text, out int startBib);
            _ = int.TryParse(tag.EndBib.Text, out int endBib);
            _ = int.TryParse(tag.StartChip.Text, out int startChip);
            _ = int.TryParse(tag.EndChip.Text.ToString(), out int endChip);
            Log.D("UI.ChipAssignment.ChipTool", "StartBib " + startBib + " EndBib " + endBib + " StartChip " + startChip + " EndChip " + endChip);
            Range curRange = new()
            {
                StartBib = startBib,
                EndBib = endBib,
                StartChip = startChip,
                EndChip = endChip
            };
            bool conflicts = !curRange.IsValid();
            foreach (Range r in ranges)
            {
                if (r.Violates(curRange))
                {
                    conflicts = true;
                }
            }
            if (conflicts)
            {
                DialogBox.Show("One or more values is in conflict. Please fix the error and try again.");
                return;
            }
            ranges.Add(curRange);
        }
        ranges.Sort();
        List<BibChipAssociation> list = [];
        foreach (Range r in ranges)
        {
            for (int bib = r.StartBib, tag = r.StartChip; bib <= r.EndBib && tag <= r.EndChip; bib++, tag++)
            {
                list.Add(new()
                {
                    Bib = bib.ToString(),
                    Chip = tag.ToString()
                });
            }
        }
        Event theEvent = database.GetCurrentEvent();
        database.AddBibChipAssociation(theEvent.Identifier, list);
        ImportComplete = true;
        this.Close();
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ImportComplete = false;
        this.Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        window?.WindowFinalize(this);
    }
}