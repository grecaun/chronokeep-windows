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
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.Timing.ReaderSettings
{
    /// <summary>
    /// Interaction logic for RFIDSettings.xaml
    /// </summary>
    public partial class RFIDSettings : UiWindow
    {
        public RFIDSettings()
        {
            InitializeComponent();
        }

        private void timeZoneSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Time zone changed.");
            timeZoneDisplay.Text = timeZoneSlider.Value.ToString();
        }

        private void saveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Save button clicked.");
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Close button clicked.");
            this.Close();
        }

        private void gatingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Gating changed.");
            gatingDisplay.Text = gatingSlider.Value.ToString();
        }

        private void idSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "ID changed.");
            idDisplay.Text = idSlider.Value.ToString();
        }

        public void CloseWindow()
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "CloseWindow.");
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                Close();
            }));
        }
    }
}
