using Chronokeep.Objects;
using System.Collections.Generic;

namespace Chronokeep.UI.Import
{
    /// <summary>
    /// Interaction logic for ImportFilePage2Alt.xaml
    /// </summary>
    public partial class ImportFilePage2Alt
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
            List<ImportDistance> output = new();
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
}
