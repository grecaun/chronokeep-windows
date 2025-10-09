using Chronokeep.Database;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.Timing.Notifications
{
    /// <summary>
    /// Interaction logic for SMSWaveEnabledWindow.xaml
    /// </summary>
    public partial class SMSWaveEnabledWindow : FluentWindow
    {
        private readonly IMainWindow window;
        private readonly IDBInterface database;
        private readonly Event theEvent;

        private readonly Dictionary<int, bool> initialValues = [];
        private readonly Dictionary<int, bool> updatedValues = [];
        private readonly Dictionary<int, List<Distance>> waveDistanceDictionary = [];

        public SMSWaveEnabledWindow(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.MinHeight = 275;
            this.MinWidth = 300;
            this.Height = 385;
            this.Width = 300;
            this.Topmost = true;
            this.window = window;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null)
            {
                return;
            }
            foreach (Distance dist in database.GetDistances(theEvent.Identifier))
            {
                initialValues[dist.Wave] = dist.SMSEnabled;
                if (!waveDistanceDictionary.TryGetValue(dist.Wave, out List<Distance> oDistList))
                {
                    oDistList = [];
                    waveDistanceDictionary[dist.Wave] = oDistList;
                }
                oDistList.Add(dist);
            }
            List<int> sortedWaves = [.. initialValues.Keys];
            sortedWaves.Sort();
            List<WaveSMS> waves = [];
            foreach (int waveNum in sortedWaves)
            {
                waves.Add(new WaveSMS {
                    Wave = waveNum,
                    SMSEnabled = initialValues[waveNum]
                });
            }
            WaveList.ItemsSource = waves;
        }


        private void Set_Click(object sender, RoutedEventArgs e)
        {
            foreach (WaveSMS waveSMS in WaveList.Items)
            {
                if (initialValues[waveSMS.Wave] != waveSMS.SMSEnabled)
                {
                    updatedValues[waveSMS.Wave] = waveSMS.SMSEnabled;
                }
            }
            foreach (int wave in updatedValues.Keys)
            {
                if (waveDistanceDictionary.TryGetValue(wave, out List<Distance> tDistList))
                {
                    foreach (Distance dist in tDistList)
                    {
                        dist.SMSEnabled = updatedValues[wave];
                        database.UpdateDistance(dist);
                    }
                }
            }
            Close();
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Enter_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            window?.WindowFinalize(this);
            if (updatedValues.Keys.Count > 0)
            {
                window.NotifyTimingWorker();
            }
        }

        internal class WaveSMS
        {
            public int Wave { get; set; }
            public string WaveName { get => string.Format("Wave {0}", Wave); }
            public bool SMSEnabled { get; set; }
        }
    }
}
