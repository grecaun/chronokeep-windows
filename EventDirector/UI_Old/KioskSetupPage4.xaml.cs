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
    /// Interaction logic for KioskSettupPage4.xaml
    /// </summary>
    public partial class KioskSetupPage4 : Page
    {
        MainWindow mainWindow;
        KioskSetup kioskWin;
        ExampleLiabilityWaiver example = null;

        public KioskSetupPage4(MainWindow mainWindow, KioskSetup kioskWin)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.kioskWin = kioskWin;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            kioskWin.Close();
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            kioskWin.Finish(liability.Text);
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            example = new ExampleLiabilityWaiver(mainWindow, kioskWin);
            mainWindow.AddWindow(example);
            example.Show();
        }
    }
}
