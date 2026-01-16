using Avalonia.Controls;
using Chronokeep.Helpers;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages;

namespace Chronokeep.UI.Parts;

public partial class SegmentHeaderPart : UserControl
{
    public SegmentHeaderPart(Event theEvent)
    {
        InitializeComponent();
        if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
        {
            Occurrence.Height = 0;
            Occurrence.IsVisible = false;
        }
    }
}