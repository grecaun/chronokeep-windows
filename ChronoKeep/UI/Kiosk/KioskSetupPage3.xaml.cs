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
    /// Interaction logic for KioskSettupPage3.xaml
    /// </summary>
    public partial class KioskSetupPage3 : Page
    {
        KioskSetup kiosk;

        public KioskSetupPage3(KioskSetup kiosk)
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
            kiosk.GotoPage4();
        }
    }
}
