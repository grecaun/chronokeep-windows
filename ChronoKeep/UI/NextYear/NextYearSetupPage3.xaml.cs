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

namespace ChronoKeep
{
    /// <summary>
    /// Interaction logic for NextYearSetupPage3.xaml
    /// </summary>
    public partial class NextYearSetupPage3 : Page
    {
        NextYearSetup kiosk;

        public NextYearSetupPage3(List<Distance> divs, NextYearSetup nextYear)
        {
            InitializeComponent();
            this.kiosk = nextYear;
            foreach (Distance div in divs)
            {
                DivisionListBoxItem2 newBox = new DivisionListBoxItem2(div.Name, div.Cost);
                divisionListBox.Items.Add(newBox);
            }
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            Submit();
        }

        private void Submit()
        {
            List<Distance> divisions = new List<Distance>();
            foreach (DivisionListBoxItem2 div in divisionListBox.Items)
            {
                divisions.Add(new Distance(div.DivName(), -1, div.Cost()));
            }
            kiosk.GoToPage4(divisions);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            kiosk.Close();
        }
    }
}
