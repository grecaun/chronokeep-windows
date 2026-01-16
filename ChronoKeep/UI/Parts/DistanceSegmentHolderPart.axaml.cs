using Avalonia.Controls;
using Chronokeep.Helpers;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages;

namespace Chronokeep.UI.Parts;

public partial class DistanceSegmentHolderPart : UserControl
{

    public Distance distance;
    private SegmentsPage page;
    private readonly int finish_occurrences;
    private readonly List<Distance> otherDistances;
    public readonly List<ListBoxItem> SegmentItems = [];

    public DistanceSegmentHolderPart(Event theEvent, SegmentsPage page, Distance distance,
                List<Distance> distances, List<Segment> segments, List<TimingLocation> locations)
    {
        InitializeComponent();
        this.distance = distance;
        this.page = page;
        otherDistances = [.. distances];
        otherDistances.RemoveAll(x => x.Identifier == (distance == null ? -1 : distance.Identifier));
        distanceName.Text = distance == null ? "All Distances" : distance.Name;
        copyFromDistance.Items.Add(new ComboBoxItem()
        {
            Content = "",
            Uid = "-1"
        });
        foreach (Distance d in otherDistances)
        {
            copyFromDistance.Items.Add(new ComboBoxItem()
            {
                Content = d.Name,
                Uid = d.Identifier.ToString()
            });
        }
        copyFromDistance.SelectedIndex = 0;
        finish_occurrences = 0;
        SegmentItems.Add(new ASegmentHeader(theEvent));
        //segmentHolder.Items.Add(new ASegmentHeader(theEvent));
        segments.Sort((x1, x2) => x1.CompareTo(x2));
        foreach (Segment s in segments)
        {
            ASegment newSeg = new(theEvent, page, s, locations);
            SegmentItems.Add(newSeg);
            //segmentHolder.Items.Add(newSeg);
            if (s.LocationId == Constants.Timing.LOCATION_FINISH || s.LocationId == Constants.Timing.LOCATION_START)
            {
                finish_occurrences = s.Occurrence > finish_occurrences ? s.Occurrence : finish_occurrences;
            }
        }
        finish_occurrences++;
    }

    private void AddClick(Object sender, RoutedEventArgs e)
    {
        Log.D("UI.MainPages.SegmentsPage", "Add segment clicked.");
        int selectedDistance = Constants.Timing.COMMON_SEGMENTS_DISTANCEID;
        if (distance != null)
        {
            selectedDistance = distance.Identifier;
        }
        int.TryParse(numAdd.Text, out int count);
        for (int i = 0; i < count; i++)
        {
            page.AddSegment(selectedDistance);
        }
    }

    private void CopyFromDistanceSelected(object? sender, SelectionChangedEventArgs e)
    {
        Log.D("UI.MainPages.SegmentsPage", "Copy from distance changed.");
        if (distance == null || copyFromDistance.SelectedIndex < 1)
        {
            return;
        }
        page.CopyFromDistance(distance.Identifier, Convert.ToInt32(((ComboBoxItem)copyFromDistance.SelectedItem).Uid));
    }

    private void NumberValidation(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }
}