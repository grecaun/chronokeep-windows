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
    /// Interaction logic for NextYearSetupPage0.xaml
    /// </summary>
    public partial class NextYearSetupPage0 : Page
    {
        NextYearSetup kiosk;

        public NextYearSetupPage0(NextYearSetup kiosk)
        {
            InitializeComponent();
            this.kiosk = kiosk;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            kiosk.Close();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            kiosk.GotoPage1();
        }
    }
}
