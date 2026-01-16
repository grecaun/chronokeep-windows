using Avalonia.Controls;
using Chronokeep.Helpers;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages;

namespace Chronokeep.UI.Parts;

public partial class SegmentPart : UserControl
{
    readonly SegmentsPage page;
    public Segment mySegment;
    private readonly Dictionary<string, int> locationDictionary;
    public Event theEvent;

    [GeneratedRegex("[^0-9.]+")]
    private static partial Regex AllowedChars();
    [GeneratedRegex("[^0-9]+")]
    private static partial Regex AllowedNums();

    public SegmentPart(Event theEvent, SegmentsPage page, Segment segment, List<TimingLocation> locations)
    {
        InitializeComponent();
        this.page = page;
        this.theEvent = theEvent;
        this.mySegment = segment;
        this.locationDictionary = [];

        ComboBoxItem selected = null, current;
        foreach (TimingLocation loc in locations)
        {
            current = new ComboBoxItem()
            {
                Content = loc.Name,
                Uid = loc.Identifier.ToString()
            };
            Location.Items.Add(current);
            if (mySegment.LocationId == loc.Identifier)
            {
                selected = current;
            }
            locationDictionary[loc.Identifier.ToString()] = loc.MaxOccurrences;
        }
        if (selected != null)
        {
            Location.SelectedItem = selected;
        }
        SegName.Text = mySegment.Name;
        // Occurrence
        if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
        {
            if (Location.SelectedItem == null || !locationDictionary.TryGetValue(((ComboBoxItem)Location.SelectedItem).Uid, out int maxOccurrences))
            {
                maxOccurrences = 1;
            }
            selected = null;
            int start = 1;
            if ((theEvent.CommonStartFinish == true && mySegment.LocationId == Constants.Timing.LOCATION_FINISH)
                || mySegment.LocationId == Constants.Timing.LOCATION_START)
            {
                start = 0;
            }
            for (int i = start; i <= maxOccurrences; i++)
            {
                current = new()
                {
                    Content = i.ToString(),
                    Uid = i.ToString()
                };
                if (i == mySegment.Occurrence)
                {
                    selected = current;
                }
                Occurrence.Items.Add(current);
            }
            if (selected != null)
            {
                Occurrence.SelectedItem = selected;
            }
            else
            {
                Occurrence.SelectedIndex = 0;
            }
            thePanel.Children.Add(Occurrence);
        }
        CumDistance.Text = mySegment.CumulativeDistance.ToString();
        DistanceUnit.SelectedIndex = 0;
        if (mySegment.DistanceUnit == Constants.Distances.KILOMETERS)
        {
            DistanceUnit.SelectedIndex = 1;
        }
        else if (mySegment.DistanceUnit == Constants.Distances.METERS)
        {
            DistanceUnit.SelectedIndex = 2;
        }
        else if (mySegment.DistanceUnit == Constants.Distances.YARDS)
        {
            DistanceUnit.SelectedIndex = 3;
        }
        else if (mySegment.DistanceUnit == Constants.Distances.FEET)
        {
            DistanceUnit.SelectedIndex = 4;
        }
        GPS.Text = mySegment.GPS;
        MapLink.Text = mySegment.MapLink;
    }

    public void UpdateSegment()
    {
        Log.D("UI.MainPages.SegmentsPage", "Segments - Updating segment.");
        try
        {
            mySegment.Name = SegName.Text;
            try
            {
                mySegment.LocationId = Convert.ToInt32(((ComboBoxItem)Location.SelectedItem).Uid);
            }
            catch
            {
                mySegment.LocationId = Constants.Timing.LOCATION_DUMMY;
            }
            mySegment.CumulativeDistance = Convert.ToDouble(CumDistance.Text);
            mySegment.DistanceUnit = Convert.ToInt32(((ComboBoxItem)DistanceUnit.SelectedItem).Uid);
            if (Occurrence != null && Occurrence.SelectedItem != null) mySegment.Occurrence = Convert.ToInt32(((ComboBoxItem)Occurrence.SelectedItem).Uid);
            else mySegment.Occurrence = -1;
            mySegment.GPS = GPS.Text;
            mySegment.MapLink = MapLink.Text;
        }
        catch
        {
            DialogBox.Show("Error with values given.");
            return;
        }
    }

    private void SelectAll(object? sender, Avalonia.Input.GotFocusEventArgs e)
    {
        TextBox src = (TextBox)e.OriginalSource;
        src.SelectAll();
    }

    private void Location_Changed(object? sender, SelectionChangedEventArgs e)
    {
        Occurrence.Items.Clear();
        if (Location.SelectedItem == null || !locationDictionary.TryGetValue(((ComboBoxItem)Location.SelectedItem).Uid, out int maxOccurrences))
        {
            maxOccurrences = 1;
        }
        int start = 1;
        if ((theEvent.CommonStartFinish == true && mySegment.LocationId == Constants.Timing.LOCATION_FINISH)
            || mySegment.LocationId == Constants.Timing.LOCATION_START)
        {
            start = 0;
        }
        for (int i = start; i <= maxOccurrences; i++)
        {

            Occurrence.Items.Add(new ComboBoxItem()
            {
                Content = i.ToString(),
                Uid = i.ToString()
            });
        }
        Occurrence.SelectedIndex = 0;
    }

    private void DoubleValidation(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
        e.Handled = AllowedChars().IsMatch(e.Text);
    }

    private void Remove_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.SegmentsPage", "Removing an item.");
        this.page.RemoveSegment(mySegment);
    }
}