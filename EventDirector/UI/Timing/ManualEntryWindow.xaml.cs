﻿using EventDirector.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EventDirector.UI.Timing
{
    /// <summary>
    /// Interaction logic for ManualEntryWindow.xaml
    /// </summary>
    public partial class ManualEntryWindow : Window
    {
        IMainWindow window;
        IDBInterface database;
        Event theEvent;
        
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
            String timeVal = TimeBox.Text.Replace('_', '0');
            int locationId = Convert.ToInt32(((ComboBoxItem)LocationBox.SelectedItem).Uid);
            DateTime time;
            long hours, minutes, seconds, milliseconds;
            hours = Convert.ToInt32(timeVal.Substring(0, 3));
            minutes = Convert.ToInt32(timeVal.Substring(4, 2));
            seconds = Convert.ToInt32(timeVal.Substring(7, 2));
            milliseconds = Convert.ToInt32(timeVal.Substring(10, 3));
            if (NetTimeButton.IsChecked == true)
            {
                time = DateTime.Parse(theEvent.Date + " 00:00:00.000");
                milliseconds += theEvent.StartMilliseconds;
                seconds += (minutes * 60) + (hours * 3600) + theEvent.StartSeconds;
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
            window.NonUIUpdate();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, allowedNums);
        }
    }
}