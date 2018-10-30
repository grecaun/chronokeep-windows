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

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for ImportFilePage2.xaml
    /// </summary>
    public partial class ImportFilePage2 : Page
    {
        public ImportFilePage2(string[] divisions)
        {
            InitializeComponent();
            foreach (string div in divisions)
            {
                DListBoxItem newBox = new DListBoxItem(div, 7000);
                divisionListBox.Items.Add(newBox);
            }
        }

        public List<Division> GetDivisions()
        {
            List<Division> output = new List<Division>();
            foreach (DListBoxItem divItem in divisionListBox.Items)
            {
                output.Add(new Division(divItem.DivName(), -1, divItem.Cost()));
            }
            return output;
        }
    }
}
