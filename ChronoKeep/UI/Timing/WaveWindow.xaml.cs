using Chronokeep.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace Chronokeep.UI.Timing
{
    /// <summary>
    /// Interaction logic for WaveWindow.xaml
    /// </summary>
    public partial class WaveWindow : Window
    {
        IMainWindow window;
        IDBInterface database;
        Event theEvent;
        Dictionary<int, Distance> distanceDictionary = new Dictionary<int, Distance>();
        Dictionary<int, (long seconds, int milliseconds)> waveTimes = new Dictionary<int, (long, int)>();
        HashSet<int> waves = new HashSet<int>();

        private const string TimeFormat = "{0:D2}:{1:D2}:{2:D2}.{3:D3}";

        public WaveWindow(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier == -1) return;
            foreach (Distance div in database.GetDistances(theEvent.Identifier))
            {
                distanceDictionary[div.Identifier] = div;
                waves.Add(div.Wave);
                waveTimes[div.Wave] = (div.StartOffsetSeconds, div.StartOffsetMilliseconds);
            }
            List<int> sortedWaves = new List<int>(waves);
            sortedWaves.Sort();
            foreach (int waveNum in sortedWaves)
            {
                long seconds = waveTimes[waveNum].seconds;
                int milliseconds = waveTimes[waveNum].milliseconds;
                Log.D("UI.Timing.WaveWindow", string.Format("Seconds {0} - Milliseconds {1}", seconds, milliseconds));
                WaveList.Items.Add(new AWave(waveNum, waveTimes[waveNum].seconds, waveTimes[waveNum].milliseconds));
            }
        }

        private void SetButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.WaveWindow", "Aye aye! Updating!");
            foreach (AWave wave in WaveList.Items)
            {
                (int waveNo, long seconds, int milliseconds) = wave.GetValues();
                if (TimeofDayButton.IsChecked == true)
                {
                    seconds = seconds - theEvent.StartSeconds;
                    milliseconds = milliseconds - theEvent.StartMilliseconds;
                    if (milliseconds < 0)
                    {
                        seconds -= 1;
                        milliseconds = 1000 - milliseconds;
                    }
                    if (seconds < 0)
                    {
                        seconds = 0;
                        milliseconds = 0;
                    }
                }
                database.SetWaveTimes(theEvent.Identifier, waveNo, seconds, milliseconds);
            }
            List<Distance> newDistances = database.GetDistances(theEvent.Identifier);
            bool update = false;
            foreach (Distance div in newDistances)
            {
                if (!distanceDictionary.ContainsKey(div.Identifier)
                    || distanceDictionary[div.Identifier].StartOffsetSeconds != div.StartOffsetSeconds
                    || distanceDictionary[div.Identifier].StartOffsetMilliseconds != div.StartOffsetMilliseconds)
                {
                    update = true;
                }
            }
            if (update)
            {
                database.ResetTimingResultsEvent(theEvent.Identifier);
                window.UpdateTiming();
                window.NotifyTimingWorker();
            }
            this.Close();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.WaveWindow", "We don't really want to set the wave times.");
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
        }

        private void NetTimeButton_Checked(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.WaveWindow", "Net Time Selected.");
            foreach (AWave wave in WaveList.Items)
            {
                int waveId = wave.GetWave();
                wave.SetTime(waveTimes[waveId].seconds, waveTimes[waveId].milliseconds);
            }
        }

        private void TimeofDayButton_Checked(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.WaveWindow", "Time of day selected.");
            foreach (AWave wave in WaveList.Items)
            {
                int waveId = wave.GetWave();
                wave.SetTime(waveTimes[waveId].seconds + theEvent.StartSeconds, waveTimes[waveId].milliseconds + theEvent.StartMilliseconds);
            }
        }

        private class AWave : ListBoxItem
        {
            public MaskedTextBox StartOffset { get; private set; }
            public Image WaveTypeImg { get; private set; }
            private int Wave;
            private int waveType = 1;

            public AWave(int num, long startSeconds, int startMilliseconds)
            {
                Wave = num;
                DockPanel thePanel = new DockPanel();
                this.Content = thePanel;
                thePanel.Children.Add(new Label()
                {
                    Content = num.ToString(),
                    FontSize = 14,
                    Width = 60,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                });
                Uri imgUri = new Uri("pack://application:,,,/img/plus.png");
                waveType = 1;
                if (startSeconds < 0)
                {
                    Log.D("UI.Timing.WaveWindow", "Setting type to negative and making seconds/milliseconds positive for offset textbox.");
                    waveType = -1;
                    imgUri = new Uri("pack://application:,,,/img/dash.png");
                    startSeconds *= -1;
                    startMilliseconds *= -1;
                }
                WaveTypeImg = new Image()
                {
                    Width = 25,
                    Margin = new Thickness(0, 0, 3, 0),
                    Source = new BitmapImage(imgUri),
                };
                WaveTypeImg.MouseLeftButtonDown += new MouseButtonEventHandler(this.SwapWaveType_Click);
                thePanel.Children.Add(WaveTypeImg);
                string sOffset = string.Format(TimeFormat, startSeconds / 3600,
                    (startSeconds % 3600) / 60, startSeconds % 60,
                    startMilliseconds);
                StartOffset = new MaskedTextBox()
                {
                    Text = sOffset,
                    Mask = "00:00:00.000",
                    FontSize = 14,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                };
                StartOffset.GotFocus += new RoutedEventHandler(this.SelectAll);
                thePanel.Children.Add(StartOffset);
            }

            public void SetTime(long seconds, int milliseconds)
            {
                StartOffset.Text = string.Format(TimeFormat, seconds / 3600,
                    (seconds % 3600) / 60, seconds % 60,
                    milliseconds);
            }

            public int GetWave()
            {
                return Wave;
            }

            private void SwapWaveType_Click(object sender, RoutedEventArgs e)
            {
                Log.D("UI.Timing.WaveWindow", "Plus/Minus sign clicked. WaveType is: " + waveType);
                if (waveType < 0)
                {
                    WaveTypeImg.Source = new BitmapImage(new Uri("pack://application:,,,/img/plus.png"));
                }
                else if (waveType > 0)
                {
                    WaveTypeImg.Source = new BitmapImage(new Uri("pack://application:,,,/img/dash.png"));
                }
                else
                {
                    Log.E("UI.Timing.WaveWindow", "Something went wrong and the wave type was set to 0.");
                }
                waveType *= -1;
            }

            public (int, long, int) GetValues()
            {
                string[] firstparts = StartOffset.Text.Replace('_', '0').Split(':');
                string[] secondparts = firstparts[2].Split('.');
                try
                {
                    int hours = Convert.ToInt32(firstparts[0]),
                        minutes = Convert.ToInt32(firstparts[1]),
                        seconds = Convert.ToInt32(secondparts[0]),
                        milliseconds = Convert.ToInt32(secondparts[1]);
                    seconds = (hours * 3600) + (minutes * 60) + seconds;
                    if (waveType < 0)
                    {
                        Log.D("UI.Timing.WaveWindow", "Negative wave, setting values to match.");
                        seconds *= -1;
                        milliseconds *= -1;
                    }
                    return (Wave, seconds, milliseconds);
                }
                catch
                {
                    Log.D("UI.Timing.WaveWindow", "Error evaluating values.");
                }
                return (Wave, 0, 0);
            }

            private void SelectAll(object sender, RoutedEventArgs e)
            {
                TextBox src = (TextBox)e.OriginalSource;
                src.SelectAll();
            }
        }
    }
}
