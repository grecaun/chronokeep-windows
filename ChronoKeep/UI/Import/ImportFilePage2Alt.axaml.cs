using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Chronokeep.Objects;

namespace Chronokeep.UI.Import;

public partial class ImportFilePage2Alt : UserControl
{
    private readonly bool no_distance = false;

    public ImportFilePage2Alt(string[] fileDistances, List<Distance> dbDistances, bool noDistances)
    {
        InitializeComponent();
        no_distance = noDistances;
        if (no_distance)
        {
            distanceListBox.Items.Add(new DistanceListBoxItemAlternate("Default Distance", dbDistances));
        }
        else
        {
            foreach (string distance in fileDistances)
            {
                distanceListBox.Items.Add(new DistanceListBoxItemAlternate(distance, dbDistances));
            }
        }
    }

    public List<ImportDistance> GetDistances()
    {
        List<ImportDistance> output = [];
        foreach (DistanceListBoxItemAlternate distanceItem in distanceListBox.Items)
        {
            output.Add(new ImportDistance()
            {
                NameFromFile = distanceItem.NameFromFile(),
                DistanceId = distanceItem.DistanceId()
            });
        }
        return output;
    }

    public class ImportDistance
    {
        public string NameFromFile { get; set; }
        public int DistanceId { get; set; }
    }
}