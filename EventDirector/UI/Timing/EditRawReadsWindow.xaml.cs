﻿using EventDirector.Interfaces;
using EventDirector.UI.MainPages;
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

namespace EventDirector.UI.Timing
{
    /// <summary>
    /// Interaction logic for EditRawReadsWindow.xaml
    /// </summary>
    public partial class EditRawReadsWindow : Window
    {
        TimingPage parent;
        IDBInterface database;
        Event theEvent;
        List<ChipRead> chipReads;

        private const string allowedChars = "[^0-9]";
        private const string TimeFormat = "{0:D2}:{1:D2}:{2:D2}.{3:D3}";

        public EditRawReadsWindow(TimingPage parent, IDBInterface database, List<ChipRead> chipReads)
        {
            InitializeComponent();
            this.parent = parent;
            this.database = database;
            this.chipReads = chipReads;
            theEvent = database.GetCurrentEvent();
            TimeBox.Focus();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Submit clicked.");
            // Keep track of any bibs/chips we've changed.
            HashSet<int> bibsChanged = new HashSet<int>();
            HashSet<string> chipsChanged = new HashSet<string>();
            bool add = AddRadio.IsChecked == true;
            string[] firstparts = TimeBox.Text.Replace('_', '0').Split(':');
            string[] secondparts = firstparts[2].Split('.');
            int seconds, milliseconds;
            int.TryParse(DaysBox.Text, out int days);
            try
            {
                int hours = Convert.ToInt32(firstparts[0]),
                    minutes = Convert.ToInt32(firstparts[1]);
                seconds = Convert.ToInt32(secondparts[0]);
                milliseconds = Convert.ToInt32(secondparts[1]);
                seconds = (hours * 3600) + (minutes * 60) + seconds;
            }
            catch
            {
                Log.D("Somehow the time value wasn't valid.");
                MessageBox.Show("Something went wrong trying to figure out that time value.");
                return;
            }
            if (!add)
            {
                seconds = seconds * -1;
                milliseconds = milliseconds * -1;
                days = days * -1;
            }
            foreach (ChipRead read in chipReads)
            {
                if (Constants.Timing.CHIPREAD_DUMMYBIB == read.Bib)
                {
                    chipsChanged.Add(read.ChipNumber);
                }
                else
                {
                    bibsChanged.Add(read.Bib);
                }
                read.TimeSeconds = read.TimeSeconds + (86400 * days) + seconds;
                read.TimeMilliseconds = read.TimeMilliseconds + milliseconds;
                if (read.TimeMilliseconds < 0)
                {
                    read.TimeSeconds--;
                    read.TimeMilliseconds += 1000;
                }
                else if (read.TimeMilliseconds >= 1000)
                {
                    read.TimeSeconds++;
                    read.TimeMilliseconds -= 1000;
                }
            }
            database.UpdateChipReads(chipReads);
            foreach (int bib in bibsChanged)
            {
                database.ResetTimingResultsBib(theEvent.Identifier, bib);
            }
            foreach (string chip in chipsChanged)
            {
                database.ResetTimingResultsChip(theEvent.Identifier, chip);
            }
            parent.UpdateView();
            parent.NotifyTimingWorker();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Cancel clicked.");
            this.Close();
        }

        private void Enter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Submit_Click(null, null);
            }
        }

        private void DaysBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, allowedChars);
        }
    }
}
