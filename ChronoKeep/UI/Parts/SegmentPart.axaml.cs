using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Chronokeep.Helpers;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages;
using Chronokeep.UI.Util;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Chronokeep.UI.Parts;

public partial class SegmentPart : UserControl
{
    readonly SegmentsPage page;
    public Segment mySegment;
    private readonly Dictionary<string, int> locationDictionary;
    public Event theEvent;

    [GeneratedRegex("[^0-9.]+")]
    private static partial Regex AllowedChars();

    public SegmentPart(Event theEvent, SegmentsPage page, Segment segment, List<TimingLocation> locations)
    {
        InitializeComponent();
        this.page = page;
        this.theEvent = theEvent;
        this.mySegment = segment;
        this.locationDictionary = [];

        ComboBoxItem? selected = null, current;
        foreach (TimingLocation loc in locations)
        {
            current = new ComboBoxItem()
            {
                Content = loc.Name,
                Tag = loc.Identifier.ToString()
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
            if (Location.SelectedItem == null || !locationDictionary.TryGetValue((string)((ComboBoxItem)Location.SelectedItem).Tag!, out int maxOccurrences))
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
                    Tag = i.ToString()
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
            mySegment.Name = SegName.Text!;
            try
            {
                mySegment.LocationId = Convert.ToInt32(((ComboBoxItem)Location.SelectedItem!).Tag!);
            }
            catch
            {
                mySegment.LocationId = Constants.Timing.LOCATION_DUMMY;
            }
            mySegment.CumulativeDistance = Convert.ToDouble(CumDistance.Text);
            mySegment.DistanceUnit = DistanceUnit.SelectedIndex switch
            {
                1 => Constants.Distances.KILOMETERS,
                2 => Constants.Distances.METERS,
                3 => Constants.Distances.YARDS,
                4 => Constants.Distances.FEET,
                _ => Constants.Distances.MILES,
            };
            if (Occurrence != null && Occurrence.SelectedItem != null) mySegment.Occurrence = Convert.ToInt32(((ComboBoxItem)Occurrence.SelectedItem).Tag!);
            else mySegment.Occurrence = -1;
            mySegment.GPS = GPS.Text!;
            mySegment.MapLink = MapLink.Text!;
        }
        catch
        {
            DialogBox.Show("Error with values given.");
            return;
        }
    }

    private void SelectAll(object? sender, FocusChangedEventArgs e)
    {
        TextBox src = (TextBox)e.Source!;
        src.SelectAll();
    }

    private void Location_Changed(object? sender, SelectionChangedEventArgs e)
    {
        Occurrence.Items.Clear();
        if (Location.SelectedItem == null || !locationDictionary.TryGetValue((string)((ComboBoxItem)Location.SelectedItem).Tag!, out int maxOccurrences))
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
                Tag = i.ToString()
            });
        }
        Occurrence.SelectedIndex = 0;
    }

    private void DoubleValidation(object? sender, TextInputEventArgs e)
    {
        e.Handled = AllowedChars().IsMatch(e.Text!);
    }

    private void Remove_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.MainPages.SegmentsPage", "Removing an item.");
        page.RemoveSegment(mySegment);
    }
}