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

namespace EventDirector.UI.Import
{
    /// <summary>
    /// Interaction logic for ImportFilePage2Alt.xaml
    /// </summary>
    public partial class ImportFilePage2Alt : Page
    {
        public ImportFilePage2Alt(string[] fileDivisions, List<Division> dbDivisions)
        {
            InitializeComponent();
            foreach (string div in fileDivisions)
            {
                divisionListBox.Items.Add(new DivisionListBoxItemAlternate(div, dbDivisions));
            }
        }

        public List<ImportDivision> GetDivisions()
        {
            List<ImportDivision> output = new List<ImportDivision>();
            foreach (DivisionListBoxItemAlternate divItem in divisionListBox.Items)
            {
                output.Add(new ImportDivision()
                {
                    NameFromFile = divItem.NameFromFile(),
                    DivisionId = divItem.DivisionId()
                });
            }
            return output;
        }

        public class ImportDivision
        {
            public string NameFromFile { get; set; }
            public int DivisionId { get; set; }
        }
    }
}
