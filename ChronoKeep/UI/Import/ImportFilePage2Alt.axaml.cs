using Avalonia.Controls;
using Chronokeep.Objects;
using Chronokeep.UI.Parts;
using System.Collections.Generic;
using System.Linq;

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
            distanceListBox.Items.Add(new DistanceAlternatePart("Default Distance", dbDistances));
        }
        else
        {
            foreach (string distance in fileDistances)
            {
                distanceListBox.Items.Add(new DistanceAlternatePart(distance, dbDistances));
            }
        }
    }

    public List<ImportDistance> GetDistances()
    {
        List<ImportDistance> output = [];
        foreach (DistanceAlternatePart distanceItem in distanceListBox.Items.Cast<DistanceAlternatePart>())
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
        public string NameFromFile { get; set; } = "";
        public int DistanceId { get; set; }
    }
}