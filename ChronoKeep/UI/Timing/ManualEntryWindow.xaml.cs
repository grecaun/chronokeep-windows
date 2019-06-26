﻿using ChronoKeep.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChronoKeep.UI.Timing
{
    /// <summary>
    /// Interaction logic for ManualEntryWindow.xaml
    /// </summary>
    public partial class ManualEntryWindow : Window
    {
        IMainWindow window;
        IDBInterface database;
        Event theEvent;

        HashSet<int> bibsAdded = new HashSet<int>();
        
        private const string allowedNums = "[^0-9]";

        private ManualEntryWindow(IMainWindow window, IDBInterface database, List<TimingLocation> locations)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null)
            {
                return;
            }
            DateBox.Text = theEvent.Date;
            UpdateLocations(locations);
        }

        public void UpdateLocations(List<TimingLocation> locations)
        {
            int selectedLoc;
            try
            {
                selectedLoc = Convert.ToInt32(((ComboBoxItem)LocationBox.SelectedItem).Uid);
            }
            catch
            {
                selectedLoc = Constants.Timing.LOCATION_FINISH;
            }
            ComboBoxItem current, selected = null;
            LocationBox.Items.Clear();
            foreach (TimingLocation loc in locations)
            {
                current = new ComboBoxItem()
                {
                    Content = loc.Name,
                    Uid = loc.Identifier.ToString()
                };
                LocationBox.Items.Add(current);
                if (loc.Identifier == selectedLoc)
                {
                    selected = current;
                }
            }
            if (selected != null)
            {
                LocationBox.SelectedItem = selected;
            }
            else
            {
                LocationBox.SelectedIndex = 0;
            }
        }

        public static ManualEntryWindow NewWindow(IMainWindow window, IDBInterface database, List<TimingLocation> locations)
        {
            return new ManualEntryWindow(window, database, locations);
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            AddEntry();
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Enter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddEntry();
            }
        }

        private void AddEntry()
        {
            int bib = -1;
            try
            {
                bib = Convert.ToInt32(BibBox.Text);
            }
            catch
            {
                MessageBox.Show("Invalid bib value given.");
                return;
            }
            if (bib < 0)
            {
                MessageBox.Show("Invalid bib value given.");
                return;
            }
            List<Participant> participants = database.GetParticipants(theEvent.Identifier);
            List<Division> divisions = database.GetDivisions(theEvent.Identifier);
            // Store the offset start values for each division by division ID
            Dictionary<int, (int seconds, int milliseconds)> divisionStartOffsetDictionary = new Dictionary<int, (int, int)>();
            // Store participants by their bib number
            Dictionary<int, Participant> participantsDictionary = new Dictionary<int, Participant>();
            foreach (Division div in divisions)
            {
                divisionStartOffsetDictionary[div.Identifier] = (div.StartOffsetSeconds, div.StartOffsetMilliseconds);
            }
            foreach (Participant part in participants)
            {
                participantsDictionary[part.EventSpecific.Bib] = part;
            }
            (int seconds, int milliseconds) startOffset = (0, 0);
            // Check if the bib corresponds to a person, then if that person has a valid division ID
            if (participantsDictionary.ContainsKey(bib) && divisionStartOffsetDictionary
                .ContainsKey(participantsDictionary[bib].EventSpecific.DivisionIdentifier))
            {
                startOffset = divisionStartOffsetDictionary[participantsDictionary[bib].EventSpecific.DivisionIdentifier];
            }
            String timeVal = TimeBox.Text.Replace('_', '0');
            int locationId = Convert.ToInt32(((ComboBoxItem)LocationBox.SelectedItem).Uid);
            DateTime time;
            long hours, minutes, seconds, milliseconds;
            hours = Convert.ToInt32(timeVal.Substring(0, 2));
            minutes = Convert.ToInt32(timeVal.Substring(3, 2));
            seconds = Convert.ToInt32(timeVal.Substring(6, 2));
            milliseconds = Convert.ToInt32(timeVal.Substring(9, 3));
            if (NetTimeButton.IsChecked == true)
            {
                time = DateTime.Parse(theEvent.Date + " 00:00:00.000");
                milliseconds += theEvent.StartMilliseconds + startOffset.milliseconds;
                seconds += (minutes * 60) + (hours * 3600) + theEvent.StartSeconds + startOffset.seconds;
            }
            else
            {
                time = DateTime.Parse(DateBox.Text + " 00:00:00.000");
                if (hours > 23)
                {
                    hours = 23;
                }
                seconds += (minutes * 60) + (hours * 3600);
            }
            time = time.AddSeconds(seconds);
            time = time.AddMilliseconds(milliseconds);
            ChipRead newEntry = new ChipRead(theEvent.Identifier, locationId, bib, time);
            Log.D("Bib " + BibBox + " LocationId " + locationId + " Time " + newEntry.TimeString);
            database.AddChipRead(newEntry);
            bibsAdded.Add(bib);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
            if (bibsAdded.Count > 0)
            {
                foreach (int bib in bibsAdded)
                {
                    database.ResetTimingResultsBib(theEvent.Identifier, bib);
                }
                window.NotifyTimingWorker();
            }
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, allowedNums);
        }
    }
}