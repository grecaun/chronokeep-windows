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
using System.Windows.Threading;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for TimingWindow.xaml
    /// </summary>
    public partial class TimingWindow : Window
    {
        private IDBInterface database;
        private IMainWindow mainWindow;
        private DateTime startTime;
        DispatcherTimer Timer = new DispatcherTimer();
        private Boolean TimerStarted = false;

        public TimingWindow(IDBInterface database, IMainWindow mainWindow)
        {
            this.database = database;
            this.mainWindow = mainWindow;
            InitializeComponent();
            Timer.Tick += new EventHandler(Timer_Click);
            Timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
        }

        private void Timer_Click(object sender, EventArgs e)
        {
            TimeSpan ellapsed = DateTime.Now - startTime;
            EllapsedTime.Content = ellapsed.ToString(@"hh\:mm\:ss");
        }

        private void StartRaceClick(object sender, RoutedEventArgs e)
        {

            Log.D("Starting race.");
            StartTime.Text = DateTime.Now.ToString("HH:mm:ss");
            StartRace.IsEnabled = false;
            UpdateStartTime();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWindow.WindowClosed(this);
        }

        private void EditSelected(object sender, RoutedEventArgs e)
        {
            Log.D("Edit Selected");
        }

        private void Overrides(object sender, RoutedEventArgs e)
        {
            Log.D("Overrides selected.");
        }

        private void Search(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void SearchKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Search();
            }
        }

        private void Search()
        {
            Log.D("Searching");
        }

        private void StartTimeKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Log.D("Start Time Box return key found.");
                UpdateStartTime();
            }
        }

        private void StartTimeLostFocus(object sender, RoutedEventArgs e)
        {
            Log.D("Start Time Box has lost focus.");
            UpdateStartTime();
        }

        private void UpdateStartTime()
        {
            if (!TimerStarted)
            {
                TimerStarted = true;
                Timer.Start();
            }
            String startTimeValue = StartTime.Text.Replace('_', '0');
            StartRace.IsEnabled = false;
            StartTime.Text = startTimeValue;
            Log.D("Start time is " + startTimeValue);
            // Day is going to be coded improperly here for now.  I just want something set up.
            // This would work for 99% of races I'd bet, but not all of them.
            startTime = DateTime.ParseExact(startTimeValue + DateTime.Now.ToString("ddMMyyyy"), "HH:mm:ssddMMyyyy", null);
            Log.D("Start time is " + startTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}
