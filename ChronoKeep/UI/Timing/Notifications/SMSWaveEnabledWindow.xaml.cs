using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.Timing.Notifications
{
    /// <summary>
    /// Interaction logic for SMSWaveEnabledWindow.xaml
    /// </summary>
    public partial class SMSWaveEnabledWindow : FluentWindow
    {
        IMainWindow window;
        IDBInterface database;
        Event theEvent;

        Dictionary<int, bool> initialValues = new Dictionary<int, bool>();
        Dictionary<int, bool> updatedValues = new Dictionary<int, bool>();
        Dictionary<int, List<Distance>> waveDistanceDictionary = new Dictionary<int, List<Distance>>();

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
                if (!waveDistanceDictionary.ContainsKey(dist.Wave))
                {
                    waveDistanceDictionary[dist.Wave] = new List<Distance>();
                }
                waveDistanceDictionary[dist.Wave].Add(dist);
            }
            List<int> sortedWaves = new List<int>(initialValues.Keys);
            sortedWaves.Sort();
            List<WaveSMS> waves = new List<WaveSMS>();
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
                if (waveDistanceDictionary.ContainsKey(wave))
                {
                    foreach (Distance dist in waveDistanceDictionary[wave])
                    {
                        dist.SMSEnabled = updatedValues[wave];
                        database.UpdateDistance(dist);
                    }
                }
            }
            this.Close();
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Enter_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
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
