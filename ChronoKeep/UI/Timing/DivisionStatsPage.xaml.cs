﻿using ChronoKeep.Interfaces;
using ChronoKeep.UI.MainPages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChronoKeep.UI.Timing
{
    /// <summary>
    /// Interaction logic for DivisionStatsPage.xaml
    /// </summary>
    public partial class DivisionStatsPage : ISubPage
    {
        IDBInterface database;
        TimingPage parent;
        Event theEvent;
        int divisionId;

        private ObservableCollection<Participant> activeParticipants = new ObservableCollection<Participant>();
        private ObservableCollection<Participant> dnsParticipants = new ObservableCollection<Participant>();
        private ObservableCollection<Participant> dnfParticipants = new ObservableCollection<Participant>();
        private ObservableCollection<Participant> finishedParticipants = new ObservableCollection<Participant>();

        public DivisionStatsPage(TimingPage parent, IDBInterface database, int divisionId, string DivisionName)
        {
            InitializeComponent();
            this.parent = parent;
            this.database = database;
            this.divisionId = divisionId;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                Log.E("Something went wrong and no proper event was returned.");
                return;
            }
            activeListView.ItemsSource = activeParticipants;
            dnsListView.ItemsSource = dnsParticipants;
            dnfListView.ItemsSource = dnfParticipants;
            finishedListView.ItemsSource = finishedParticipants;
            this.DivisionName.Content = DivisionName;
            UpdateView();
        }

        public void Closing() { }

        public void EditSelected() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public void Search(string value, CancellationToken token) { }

        public void Show(PeopleType type) { }

        public void SortBy(SortType type) { }

        public void UpdateView()
        {
            activeParticipants.Clear();
            dnsParticipants.Clear();
            dnfParticipants.Clear();
            finishedParticipants.Clear();
            Dictionary<int, List<Participant>> partDict = database.GetDivisionParticipantsStatus(theEvent.Identifier, divisionId);
            if (partDict.ContainsKey(Constants.Timing.EVENTSPECIFIC_STARTED)) // ACTIVE
            {
                activePanel.Visibility = Visibility.Visible;
                foreach (Participant p in partDict[Constants.Timing.EVENTSPECIFIC_STARTED])
                {
                    activeParticipants.Add(p);
                }
            }
            else
            {
                activePanel.Visibility = Visibility.Collapsed;
            }
            if (partDict.ContainsKey(Constants.Timing.EVENTSPECIFIC_NOSHOW)) // DNS
            {
                dnsPanel.Visibility = Visibility.Visible;
                foreach (Participant p in partDict[Constants.Timing.EVENTSPECIFIC_NOSHOW])
                {
                    dnsParticipants.Add(p);
                }
            }
            else
            {
                dnsPanel.Visibility = Visibility.Collapsed;
            }
            if (partDict.ContainsKey(Constants.Timing.EVENTSPECIFIC_NOFINISH)) // DNF
            {
                dnfPanel.Visibility = Visibility.Visible;
                foreach (Participant p in partDict[Constants.Timing.EVENTSPECIFIC_NOFINISH])
                {
                    dnfParticipants.Add(p);
                }
            }
            else
            {
                dnfPanel.Visibility = Visibility.Collapsed;
            }
            if (partDict.ContainsKey(Constants.Timing.EVENTSPECIFIC_FINISHED)) // FINISHED
            {
                finishedPanel.Visibility = Visibility.Visible;
                foreach (Participant p in partDict[Constants.Timing.EVENTSPECIFIC_FINISHED])
                {
                    finishedParticipants.Add(p);
                }
            }
            else
            {
                finishedPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Done button clicked.");
            parent.LoadMainDisplay();
        }
    }
}
