using ChronoKeep.Interfaces.Timing;
using ChronoKeep.Objects;
using ChronoKeep.UI.MainPages;
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

namespace ChronoKeep.UI.Timing
{
    /// <summary>
    /// Interaction logic for SetTimeWindow.xaml
    /// </summary>
    public partial class SetTimeWindow : Window
    {
        TimingPage parent;
        TimingSystem timingSystem;

        public SetTimeWindow(TimingPage parent, TimingSystem timingSystem)
        {
            InitializeComponent();
            this.parent = parent;
            this.timingSystem = timingSystem;
        }

        public void UpdateTime()
        {
            TimeLabel.Content = string.Format("Time is {0}", timingSystem.SystemTime);
        }

        private void Set_Click(object sender, RoutedEventArgs e)
        {
            DateTime alternateDate;
            if (DateTime.TryParse(string.Format("{0} {1}", SpecificDateBox.Text, SpecificTimeBox.Text.Replace('_', '0')), out alternateDate) == false)
            {
                alternateDate = DateTime.Now;
            }
            if (SetAllCheckBox.IsChecked == true)
            {
                parent.SetAllTimingSystemsToTime(alternateDate, NowTimeRadioButton.IsChecked == true);
            }
            else if (NowTimeRadioButton.IsChecked == true)
            {
                timingSystem.SystemInterface.SetTime(DateTime.Now);
            }
            else
            {
                timingSystem.SystemInterface.SetTime(alternateDate);
            }
        }

        private void Check_Click(object sender, RoutedEventArgs e)
        {
            timingSystem.SystemInterface.GetTime();
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
