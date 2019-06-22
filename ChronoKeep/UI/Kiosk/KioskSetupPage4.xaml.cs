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
    /// Interaction logic for KioskSettupPage4.xaml
    /// </summary>
    public partial class KioskSetupPage4 : Page
    {
        KioskSetup kioskWin;
        ExampleLiabilityWaiver example = null;

        public KioskSetupPage4(KioskSetup kioskWin, IDBInterface database)
        {
            InitializeComponent();
            this.kioskWin = kioskWin;
            liability.Text = database.GetAppSetting(Constants.Settings.DEFAULT_WAIVER).value;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            kioskWin.Close();
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            kioskWin.Finish(liability.Text, (yesRadio.IsChecked == true ? 1 : 0));
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            if (kioskWin.ExampleWaiverWindowOpen()) return;
            example = new ExampleLiabilityWaiver(kioskWin);
            example.ShowDialog();
        }
    }
}
