using Avalonia.Controls;
using Avalonia.Interactivity;
using Chronokeep.Helpers;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages.Timing;

namespace Chronokeep.UI.Parts;

public partial class AlarmPart : UserControl
{
    readonly AlarmsPage page;
    private readonly Alarm theAlarm;

    public AlarmPart(AlarmsPage page, Alarm alarm)
    {
        InitializeComponent();
        this.page = page;
        this.theAlarm = alarm;
    }

    public Alarm GetUpdatedAlarm()
    {
        theAlarm.Bib = BibBox.Text!.Trim();
        if (theAlarm.Bib.Length > 0)
        {
            theAlarm.Chip = "";
        }
        else
        {
            theAlarm.Chip = ChipBox.Text!;
        }
        theAlarm.Enabled = EnabledBox.IsChecked == true;
        theAlarm.AlarmSound = AlarmSoundBox.SelectedIndex;
        return theAlarm;
    }

    private void Remove_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.Timing.AlarmsPage", "Removing alarm.");
        page.RemoveAlarm(this);
    }

    private void SelectAll(object sender, RoutedEventArgs e)
    {
        TextBox src = (TextBox)e.Source!;
        src.SelectAll();
    }
}