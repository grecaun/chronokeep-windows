﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for ChipPersonWindow.xaml
    /// </summary>
    public partial class ChipPersonWindow : Window
    {
        private ChipReaderWindow readerWindow;
        private string eventDate;
        readonly object _locker = new object();

        public ChipPersonWindow(ChipReaderWindow reader, string eventDate)
        {
            this.readerWindow = reader;
            this.eventDate = eventDate;
            InitializeComponent();
        }

        public async void UpdateInfo(Participant person)
        {
            await Task.Run(() =>
            {
                lock (_locker)
                {
                    Monitor.Pulse(_locker);
                }
                Thread.Sleep(100);
            });
            if (person != null)
            {
                Bib.Content = "Bib: " + person.EventSpecific.Bib;
                Chip.Content = "Chip: " + person.EventSpecific.Chip;
                First.Content = "First: " + person.FirstName;
                Last.Content = "Last: " + person.LastName;
                Age.Content = "Age: " + person.Age(eventDate);
                Gender.Content = "Gender: " + person.Gender;
                Division.Content = "Division: " + person.EventSpecific.DivisionName;
                Unknown.Content = "";
            }
            else
            {
                Bib.Content = "";
                Chip.Content = "";
                First.Content = "";
                Last.Content = "";
                Age.Content = "";
                Gender.Content = "";
                Division.Content = "";
                Unknown.Content = "Information not found.";
            }
            await Task.Run(() =>
            {
                lock (_locker)
                {
                    Monitor.Wait(_locker, 5000);
                }
            });
            Bib.Content = "";
            Chip.Content = "";
            First.Content = "";
            Last.Content = "";
            Age.Content = "";
            Gender.Content = "";
            Division.Content = "";
            Unknown.Content = "";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            readerWindow.PersonWindowClosing();
        }
    }
}