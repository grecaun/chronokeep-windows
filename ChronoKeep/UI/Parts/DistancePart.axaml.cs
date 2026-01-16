using Chronokeep.Helpers;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages;

namespace Chronokeep.UI.Parts;

public partial class DistancePart : UserControl
{
    public bool PlusWave { get; set; } = true;
    public bool MinusWave { get => !PlusWave; }
    public bool IsMain { get; set; } = true;
    public bool IsLinked { get => !IsMain; }
    public bool DistanceEvent { get; set; } = true;
    public bool NotDistanceEvent { get => !DistanceEvent; }
    public bool NotBackyardEvent { get; set; } = true;

    private const string TimeFormat = "{0:D2}:{1:D2}:{2:D2}.{3:D3}";
    private const string LimitFormat = "{0:D2}:{1:D2}:{2:D2}";
    readonly DistancesPage page;
    public Distance theDistance;
    public DistancePart parent;
    private readonly Dictionary<int, Distance> distanceDictionary;

    [GeneratedRegex("[^0-9.]")]
    private static partial Regex AllowedWithDot();
    [GeneratedRegex("[^0-9]")]
    private static partial Regex AllowedChars();

    public DistancePart(DistancesPage page, Distance distance, int maxOccurrences,
                List<Distance> distances, Dictionary<int, Distance> distanceDictionary,
                Event theEvent, DistancePart parent)
    {
        InitializeComponent();
        List<Distance> otherDistances = [.. distances];
        this.distanceDictionary = distanceDictionary;
        otherDistances.Remove(distance);
        this.page = page;
        this.theDistance = distance;
        this.parent = parent;
        DistanceName.Text = theDistance.Name;
        CopyFromBox.Items.Add(new ComboBoxItem()
        {
            Content = "",
            Uid = "-1"
        });
        foreach (Distance div in otherDistances)
        {
            CopyFromBox.Items.Add(new ComboBoxItem()
            {
                Content = div.Name,
                Uid = div.Identifier.ToString()
            });
        }
        CopyFromBox.SelectedIndex = 0;
        DistanceBox.Text = theDistance.DistanceValue.ToString();
        DistanceUnit.Items.Add(new ComboBoxItem()
        {
            Content = "",
            Uid = Constants.Distances.UNKNOWN.ToString()
        });
        DistanceUnit.Items.Add(new ComboBoxItem()
        {
            Content = "Miles",
            Uid = Constants.Distances.MILES.ToString()
        });
        DistanceUnit.Items.Add(new ComboBoxItem()
        {
            Content = "Kilometers",
            Uid = Constants.Distances.KILOMETERS.ToString()
        });
        DistanceUnit.Items.Add(new ComboBoxItem()
        {
            Content = "Meters",
            Uid = Constants.Distances.METERS.ToString()
        });
        DistanceUnit.Items.Add(new ComboBoxItem()
        {
            Content = "Yards",
            Uid = Constants.Distances.YARDS.ToString()
        });
        DistanceUnit.Items.Add(new ComboBoxItem()
        {
            Content = "Feet",
            Uid = Constants.Distances.FEET.ToString()
        });
        if (theDistance.DistanceUnit == Constants.Distances.MILES)
        {
            DistanceUnit.SelectedIndex = 1;
        }
        else if (theDistance.DistanceUnit == Constants.Distances.KILOMETERS)
        {
            DistanceUnit.SelectedIndex = 2;
        }
        else if (theDistance.DistanceUnit == Constants.Distances.METERS)
        {
            DistanceUnit.SelectedIndex = 3;
        }
        else if (theDistance.DistanceUnit == Constants.Distances.YARDS)
        {
            DistanceUnit.SelectedIndex = 4;
        }
        else if (theDistance.DistanceUnit == Constants.Distances.FEET)
        {
            DistanceUnit.SelectedIndex = 5;
        }
        else
        {
            DistanceUnit.SelectedIndex = 0;
        }
        if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
        {
            ComboBoxItem selected = null, current;
            for (int i = 1; i <= maxOccurrences; i++)
            {
                current = new()
                {
                    Content = i.ToString(),
                    Uid = i.ToString()
                };
                if (i == theDistance.FinishOccurrence)
                {
                    selected = current;
                }
                FinishOccurrence.Items.Add(current);
            }
            if (selected != null)
            {
                FinishOccurrence.SelectedItem = selected;
            }
            else
            {
                FinishOccurrence.SelectedIndex = 0;
            }
        }
        else
        {
            DockPanel limitPanel = new();
            limitPanel.Children.Add(new TextBlock()
            {
                Text = "Max Time",
                Width = 65,
                FontSize = 12,
                Margin = new(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            string limit = string.Format(LimitFormat, theDistance.EndSeconds / 3600,
                theDistance.EndSeconds % 3600 / 60, theDistance.EndSeconds % 60);
            TimeLimit.Text = limit;
        }
        if (Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA != theEvent.EventType)
        {
            Wave.Text = theDistance.Wave.ToString();
            PlusWave = true;
            if (theDistance.StartOffsetSeconds < 0)
            {
                Log.D("UI.MainPages.DistancesPage", "Setting type to negative and making seconds/milliseconds positive for offset textbox.");
                PlusWave = false;
                theDistance.StartOffsetSeconds *= -1;
                theDistance.StartOffsetMilliseconds *= -1;
            }
        }
        StartOffset.Text = string.Format(TimeFormat, theDistance.StartOffsetSeconds / 3600,
            theDistance.StartOffsetSeconds % 3600 / 60, theDistance.StartOffsetSeconds % 60,
            theDistance.StartOffsetMilliseconds);
        Certification.Text = theDistance.Certification;
        if (theEvent.UploadSpecific == true)
        {
            // Upload Specific checkbox...
        }
        if (theEvent.EventType == Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA)
        {
            copyPanel.Visibility = Visibility.Collapsed;
            AddSubDistance.Visibility = Visibility.Collapsed;
            secondGrid.Children.Remove(Remove);
        }
        TypeBox.Items.Add(
            new ComboBoxItem
            {
                Content = "Normal",
                Uid = Constants.Timing.DISTANCE_TYPE_NORMAL.ToString()
            });
        TypeBox.Items.Add(
            new ComboBoxItem
            {
                Content = "Early Start",
                Uid = Constants.Timing.DISTANCE_TYPE_EARLY.ToString()
            });
        TypeBox.Items.Add(
            new ComboBoxItem
            {
                Content = "Late Start",
                Uid = Constants.Timing.DISTANCE_TYPE_LATE.ToString()
            });
        TypeBox.Items.Add(
            new ComboBoxItem
            {
                Content = "Drop",
                Uid = Constants.Timing.DISTANCE_TYPE_DROP.ToString()
            });
        TypeBox.Items.Add(
            new ComboBoxItem
            {
                Content = "Unranked",
                Uid = Constants.Timing.DISTANCE_TYPE_UNOFFICIAL.ToString()
            });
        TypeBox.Items.Add(
            new ComboBoxItem
            {
                Content = "Virtual",
                Uid = Constants.Timing.DISTANCE_TYPE_VIRTUAL.ToString()
            });
        if (theDistance.Type == Constants.Timing.DISTANCE_TYPE_EARLY)
        {
            TypeBox.SelectedIndex = 1;
        }
        else if (theDistance.Type == Constants.Timing.DISTANCE_TYPE_LATE)
        {
            Ranking.Text = "0";
            Ranking.IsEnabled = false;
            TypeBox.SelectedIndex = 2;
        }
        else if (theDistance.Type == Constants.Timing.DISTANCE_TYPE_DROP)
        {
            TypeBox.SelectedIndex = 3;
        }
        else if (theDistance.Type == Constants.Timing.DISTANCE_TYPE_UNOFFICIAL)
        {
            TypeBox.SelectedIndex = 4;
        }
        else if (theDistance.Type == Constants.Timing.DISTANCE_TYPE_VIRTUAL)
        {
            TypeBox.SelectedIndex = 5;
        }
        else if (theDistance.Type == Constants.Timing.DISTANCE_TYPE_NORMAL)
        {
            TypeBox.SelectedIndex = 0;
        }
    }

    public Distance GetDistance()
    {
        return theDistance;
    }

    public void UpdateDistance()
    {
        Log.D("UI.MainPages.DistancesPage", "Updating distance.");
        theDistance.Name = DistanceName.Text;
        double dist;
        try
        {
            dist = Convert.ToDouble(Distance.Text);
        }
        catch
        {
            dist = 0.0;
        }
        if (dist >= 0.0)
        {
            theDistance.DistanceValue = dist;
        }
        theDistance.DistanceUnit = Convert.ToInt32(((ComboBoxItem)DistanceUnit.SelectedItem).Uid);
        if (FinishOccurrence != null && FinishOccurrence.SelectedItem != null)
        {
            theDistance.FinishOccurrence = Convert.ToInt32(((ComboBoxItem)FinishOccurrence.SelectedItem).Uid);
        }
        theDistance.EndSeconds = 0;
        if (TimeLimit != null)
        {
            string[] limitParts = TimeLimit.Text.Replace('_', '0').Split(':');
            theDistance.EndSeconds = (Convert.ToInt32(limitParts[0]) * 3600)
                + (Convert.ToInt32(limitParts[1]) * 60)
                + Convert.ToInt32(limitParts[2]);
        }
        int wave = -1;
        if (Wave != null)
        {
            if (!int.TryParse(Wave.Text, out wave))
            {
                theDistance.Wave = -1;
            }
        }
        if (wave >= 0)
        {
            theDistance.Wave = wave;
        }
        string[] firstparts = StartOffset.Text.Replace('_', '0').Split(':');
        string[] secondparts = firstparts[2].Split('.');
        try
        {
            theDistance.StartOffsetSeconds = (Convert.ToInt32(firstparts[0]) * 3600)
                + (Convert.ToInt32(firstparts[1]) * 60)
                + Convert.ToInt32(secondparts[0]);
            theDistance.StartOffsetMilliseconds = Convert.ToInt32(secondparts[1]);
        }
        catch
        {
            DialogBox.Show("Error with values given.");
        }
        if (waveType < 0)
        {
            Log.D("UI.MainPages.DistancesPage", "Recording negative values.");
            theDistance.StartOffsetSeconds *= -1;
            theDistance.StartOffsetMilliseconds *= -1;
        }
        if (Upload != null)
        {
            theDistance.Upload = Upload.IsChecked == true;
        }
        else
        {
            theDistance.Upload = true;
        }
        if (Certification != null)
        {
            theDistance.Certification = Certification.Text;
        }
        else
        {
            theDistance.Certification = "";
        }
    }

    private void SelectAll(object? sender, Avalonia.Input.GotFocusEventArgs e)
    {
        TextBox src = (TextBox)e.OriginalSource;
        src.SelectAll();
    }

    private void NumberValidation(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
        e.Handled = AllowedChars().IsMatch(e.Text);
    }

    private void Remove_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.DistancesPage", "Removing distance.");
        page.RemoveDistance(theDistance);
    }

    private void TypeBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TypeBox.SelectedIndex == 2)
        {
            Ranking.Text = "0";
            Ranking.IsEnabled = false;
        }
        else
        {
            Ranking.Text = theDistance.Ranking.ToString();
            Ranking.IsEnabled = true;
        }
    }

    private void SwapWaveType_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.DistancesPage", "Plus/Minus sign clicked. PlusWave is: " + PlusWave);
        PlusWave = !PlusWave;
    }

    private void DotValidation(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
        e.Handled = AllowedWithDot().IsMatch(e.Text);
    }

    private void AddSub_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.DistancesPage", "Adding sub distance.");
        page.AddSubDistance(theDistance);
    }

    private void CopyFromBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Log.D("UI.MainPages.DistancesPage", "Attempting to copy from a different distance! Here we go!");
        // Ensure we've got something selected, it has a parseable UID,
        // and there's a distance related to it
        if (CopyFromBox.SelectedItem != null
            && int.TryParse(((ComboBoxItem)CopyFromBox.SelectedItem).Uid, out int newDivId)
            && distanceDictionary.TryGetValue(newDivId, out Distance newDiv))
        {
            theDistance.Name = DistanceName.Text;
            theDistance.DistanceValue = newDiv.DistanceValue;
            theDistance.DistanceUnit = newDiv.DistanceUnit;
            theDistance.FinishOccurrence = newDiv.FinishOccurrence;
            theDistance.Wave = newDiv.Wave;
            theDistance.StartOffsetSeconds = newDiv.StartOffsetSeconds;
            theDistance.StartOffsetMilliseconds = newDiv.StartOffsetMilliseconds;
            theDistance.Upload = newDiv.Upload;
            page.UpdateDistance(theDistance);
        }
    }
}