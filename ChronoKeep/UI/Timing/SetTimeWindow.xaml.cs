using Chronokeep.Objects;
using Chronokeep.UI.MainPages;
using System;
using System.Windows;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.Timing
{
    /// <summary>
    /// Interaction logic for SetTimeWindow.xaml
    /// </summary>
    public partial class SetTimeWindow : UiWindow
    {
        TimingPage parent;
        TimingSystem timingSystem;

        public SetTimeWindow(TimingPage parent, TimingSystem timingSystem)
        {
            InitializeComponent();
            this.parent = parent;
            this.timingSystem = timingSystem;
            this.MinHeight = 0;
            this.MinWidth = 0;
            this.Width = 400;
            this.SizeToContent = SizeToContent.Height;
        }

        public bool IsTimingSystem(TimingSystem timingSystem)
        {
            return this.timingSystem.Equals(timingSystem);
        }

        public void UpdateTime()
        {
            TimeLabel.Text = string.Format("Reader time is {0}", timingSystem.SystemTime);
            CurrentTimeLabel.Text = string.Format("System time is {0}", DateTime.Now.ToString("dd MMM yyyy HH:mm:ss"));
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
