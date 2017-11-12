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
    /// Interaction logic for KioskSettupPage1.xaml
    /// </summary>
    public partial class KioskSetupPage1 : Page
    {
        KioskSetup kiosk;

        public KioskSetupPage1(KioskSetup kiosk)
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
            kiosk.GotoPage2();
        }
    }
}
