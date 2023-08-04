using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Chronokeep.UI.Import
{
    /// <summary>
    /// Interaction logic for ImportFilePage2Alt.xaml
    /// </summary>
    public partial class ImportFilePage2Alt
    {
        private bool no_distance = false;

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
            List<ImportDistance> output = new List<ImportDistance>();
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
