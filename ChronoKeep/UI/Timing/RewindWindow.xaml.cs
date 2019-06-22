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
    /// Interaction logic for RewindWindow.xaml
    /// </summary>
    public partial class RewindWindow : Window
    {
        TimingSystem system;

        public RewindWindow(TimingSystem system)
        {
            InitializeComponent();
            this.system = system;
            string dateStr = DateTime.Now.ToString("MM/dd/yyyy");
            FromDate.Text = dateStr;
            ToDate.Text = dateStr;
            FromTime.Text = "00:00:00";
            ToTime.Text = "23:59:59";
        }

        private void SetYesterday_Click(object sender, RoutedEventArgs e)
        {
            string dateStr = DateTime.Now.AddDays(-1).ToString("MM/dd/yyyy");
            FromDate.Text = dateStr;
            ToDate.Text = dateStr;
            FromTime.Text = "00:00:00";
            ToTime.Text = "23:59:59";
        }

        private void SetToday_Click(object sender, RoutedEventArgs e)
        {
            string dateStr = DateTime.Now.ToString("MM/dd/yyyy");
            FromDate.Text = dateStr;
            ToDate.Text = dateStr;
            FromTime.Text = "00:00:00";
            ToTime.Text = "23:59:59";
        }

        private void SetTomorrow_Click(object sender, RoutedEventArgs e)
        {
            string dateStr = DateTime.Now.AddDays(1).ToString("MM/dd/yyyy");
            FromDate.Text = dateStr;
            ToDate.Text = dateStr;
            FromTime.Text = "00:00:00";
            ToTime.Text = "23:59:59";
        }

        private void Rewind_Click(object sender, RoutedEventArgs e)
        {
            if (!DateTime.TryParse(string.Format("{0} {1}", FromDate.Text, FromTime.Text.Replace('_', '0')), out DateTime from))
            {
                from = DateTime.Now;
            }
            if (!DateTime.TryParse(string.Format("{0} {1}", ToDate.Text, ToTime.Text.Replace('_','0')), out DateTime to))
            {
                to = DateTime.Now;
            }
            system.SystemInterface.Rewind(from, to);
            this.Close();
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
