using Avalonia.Controls;
using Chronokeep.Helpers;
using Chronokeep.Objects;
using Chronokeep.UI.Timing;

namespace Chronokeep.UI.Parts;

public partial class ClockPart : UserControl
{
    private Chronoclock clock;
    private readonly Event theEvent;

    public bool IsLocked { get; private set; }
    public bool IsOpen { get => !IsLocked; }

    public ClockPart(Chronoclock clock, ClockControl parent, Event theEvent)
    {
        InitializeComponent();
        this.clock = clock;
        this.theEvent = theEvent;

    }

    private void SelectAll(object sender, Avalonia.Input.GotFocusEventArgs e)
    {
        TextBox src = (TextBox)e.OriginalSource;
        src.SelectAll();
    }

    private void LockedChanged(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsLocked = !IsLocked;
    }
}