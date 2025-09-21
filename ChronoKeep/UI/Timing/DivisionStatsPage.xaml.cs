using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.UI.MainPages;
using Chronokeep.UI.Participants;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chronokeep.UI.Timing
{
    /// <summary>
    /// Interaction logic for DistanceStatsPage.xaml
    /// </summary>
    public partial class DistanceStatsPage : ISubPage
    {
        IDBInterface database;
        IMainWindow window;
        TimingPage parent;
        Event theEvent;
        int distanceId;

        private ObservableCollection<StatsParticipant> activeParticipants = new ObservableCollection<StatsParticipant>();
        private ObservableCollection<Participant> dnsParticipants = new ObservableCollection<Participant>();
        private ObservableCollection<Participant> unknownParticipants = new ObservableCollection<Participant>();
        private ObservableCollection<Participant> dnfParticipants = new ObservableCollection<Participant>();
        private ObservableCollection<Participant> finishedParticipants = new ObservableCollection<Participant>();

        public DistanceStatsPage(TimingPage parent, IMainWindow window, IDBInterface database, int distanceId, string DistanceName)
        {
            InitializeComponent();
            this.parent = parent;
            this.window = window;
            this.database = database;
            this.distanceId = distanceId;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                Log.E("UI.Timing.DivisionStatsPage", "Something went wrong and no proper event was returned.");
                return;
            }
            Participant.SetCurrentEventDate(theEvent.Date);
            activeListView.ItemsSource = activeParticipants;
            dnsListView.ItemsSource = dnsParticipants;
            unknownListView.ItemsSource = unknownParticipants;
            dnfListView.ItemsSource = dnfParticipants;
            finishedListView.ItemsSource = finishedParticipants;
            this.DistanceName.Text = DistanceName;
            parent.SetReaders([], false);
            UpdateView();
        }

        public void CancelableUpdateView(CancellationToken token) { }

        public void Search(CancellationToken token, string searchText) { }

        public void Closing() { }

        public void EditSelected() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public void Show(PeopleType type) { }

        public void SortBy(SortType type) { }

        public void UpdateView()
        {
            activeParticipants.Clear();
            dnsParticipants.Clear();
            unknownParticipants.Clear();
            dnfParticipants.Clear();
            finishedParticipants.Clear();
            Dictionary<int, List<Participant>> partDict = database.GetDistanceParticipantsStatus(theEvent.Identifier, distanceId);
            // Bib dictionary to add LastSeen string to active participants for display.
            Dictionary<string, TimeResult> lastSeenDictionary = new Dictionary<string, TimeResult>();
            foreach (TimeResult timeResult in database.GetLastSeenResults(theEvent.Identifier))
            {
                if (timeResult.Bib != Constants.Timing.CHIPREAD_DUMMYBIB && timeResult.Bib.Length > 0)
                {
                    lastSeenDictionary[timeResult.Bib] = timeResult;
                }
            }
            if (partDict.ContainsKey(Constants.Timing.EVENTSPECIFIC_STARTED)) // ACTIVE
            {
                activePanel.Visibility = Visibility.Visible;
                foreach (Participant p in partDict[Constants.Timing.EVENTSPECIFIC_STARTED])
                {
                    string lastSeen = p.Bib != Constants.Timing.CHIPREAD_DUMMYBIB
                                        && p.Bib.Length > 0
                                        && lastSeenDictionary.ContainsKey(p.Bib)
                                        ? lastSeenDictionary[p.Bib].SegmentName
                                        : "";
                    string lastSeenTime = p.Bib != Constants.Timing.CHIPREAD_DUMMYBIB
                                        && p.Bib.Length > 0
                                        && lastSeenDictionary.ContainsKey(p.Bib)
                                        ? lastSeenDictionary[p.Bib].SysTime
                                        : "";
                    activeParticipants.Add(new StatsParticipant(p, lastSeen, lastSeenTime));
                }
            }
            else
            {
                activePanel.Visibility = Visibility.Collapsed;
            }
            if (partDict.ContainsKey(Constants.Timing.EVENTSPECIFIC_DNS)) // DNS
            {
                dnsPanel.Visibility = Visibility.Visible;
                foreach (Participant p in partDict[Constants.Timing.EVENTSPECIFIC_DNS])
                {
                    dnsParticipants.Add(p);
                }
            }
            else
            {
                dnsPanel.Visibility = Visibility.Collapsed;
            }
            if (partDict.ContainsKey(Constants.Timing.EVENTSPECIFIC_UNKNOWN)) // UNKOWN
            {
                unknownPanel.Visibility = Visibility.Visible;
                foreach (Participant p in partDict[Constants.Timing.EVENTSPECIFIC_UNKNOWN])
                {
                    unknownParticipants.Add(p);
                }
            }
            else
            {
                unknownPanel.Visibility = Visibility.Collapsed;
            }
            if (partDict.ContainsKey(Constants.Timing.EVENTSPECIFIC_DNF)) // DNF
            {
                dnfPanel.Visibility = Visibility.Visible;
                foreach (Participant p in partDict[Constants.Timing.EVENTSPECIFIC_DNF])
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
            Log.D("UI.Timing.DistanceStatsPage", "Done button clicked.");
            parent.LoadMainDisplay();
        }

        private void activeListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = mainScroll;
            if (e.Delta < 0)
            {
                if (scv.VerticalOffset - e.Delta <= scv.ExtentHeight - scv.ViewportHeight)
                {
                    scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
                }
                else
                {
                    scv.ScrollToBottom();
                }
            }
            else
            {
                if (scv.VerticalOffset - e.Delta > 0)
                {
                    scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
                }
                else
                {
                    scv.ScrollToTop();
                }
            }
        }

        private class StatsParticipant
        {
            private Participant Participant;
            public string LastSeen { get; }
            public string LastSeenTime { get; }
            public string Bib { get => Participant.Bib; }
            public string FirstName { get => Participant.FirstName; }
            public string LastName { get => Participant.LastName; }
            public string Gender { get => Participant.Gender; }
            public string Phone { get => Participant.Phone; }
            public string Mobile { get => Participant.Mobile; }
            public string Email { get => Participant.Email; }
            public string CurrentAge { get => Participant.CurrentAge; }

            internal StatsParticipant(Participant participant, string lastSeen, string lastSeenTime)
            {
                Participant = participant;
                LastSeen = lastSeen;
                LastSeenTime = lastSeenTime;
            }

            public Participant GetParticipant()
            {
                return Participant;
            }
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Log.D("UI.Timing.DistanceStatsPage", "Mouse double clicked in a listview.");
            if (sender is ListView)
            {
                ListView listView = sender as ListView;
                if (listView.SelectedItem == null) return;
                Participant selected;
                if (listView.SelectedItem is StatsParticipant)
                {
                    selected = ((StatsParticipant)listView.SelectedItem).GetParticipant();
                }
                else
                {
                    selected = listView.SelectedItem as Participant;
                }
                ModifyParticipantWindow modifyParticipant = new ModifyParticipantWindow(window, database, selected);
                modifyParticipant.ShowDialog();
            }
        }

        public void Location(string location) { }

        public void Reader(string reader) { }
    }
}
