using Chronokeep.Interfaces;
using Chronokeep.Timing.API;
using Chronokeep.UI.MainPages;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.Timing
{
    /// <summary>
    /// Interaction logic for EditRawReadsWindow.xaml
    /// </summary>
    public partial class EditRawReadsWindow : FluentWindow
    {
        ITimingPage parent;
        IDBInterface database;
        Event theEvent;
        List<ChipRead> chipReads;

        private const string allowedChars = "[^0-9]";
        private const string TimeFormat = "{0:D2}:{1:D2}:{2:D2}.{3:D3}";

        public EditRawReadsWindow(ITimingPage parent, IDBInterface database, List<ChipRead> chipReads)
        {
            InitializeComponent();
            this.parent = parent;
            this.database = database;
            this.chipReads = chipReads;
            this.MinWidth = 280;
            this.Width = 280;
            this.MinHeight = 230;
            this.Height = 230;
            theEvent = database.GetCurrentEvent();
            TimeBox.Focus();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.EditRawReadsWindow", "Submit clicked.");
            // Keep track of any bibs/chips we've changed.
            HashSet<string> bibsChanged = new HashSet<string>();
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
                Log.D("UI.Timing.EditRawReadsWindow", "Somehow the time value wasn't valid.");
                DialogBox.Show("Something went wrong trying to figure out that time value.");
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
            APIController.SetUploadableFalse(15000);
            database.ResetTimingResultsEvent(theEvent.Identifier);
            APIController.SetUploadableTrue(15000);
            parent.UpdateView();
            parent.NotifyTimingWorker();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.EditRawReadsWindow", "Cancel clicked.");
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
