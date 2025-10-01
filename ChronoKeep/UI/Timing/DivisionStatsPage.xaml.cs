using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
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
        private  readonly IDBInterface database;
        private readonly IMainWindow window;
        private readonly TimingPage parent;
        private readonly Event theEvent;
        private readonly int distanceId;

        private readonly ObservableCollection<StatsParticipant> activeParticipants = [];
        private readonly ObservableCollection<Participant> dnsParticipants = [];
        private readonly ObservableCollection<Participant> unknownParticipants = [];
        private readonly ObservableCollection<Participant> dnfParticipants = [];
        private readonly ObservableCollection<Participant> finishedParticipants = [];

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
            Dictionary<string, TimeResult> lastSeenDictionary = [];
            foreach (TimeResult timeResult in database.GetLastSeenResults(theEvent.Identifier))
            {
                if (timeResult.Bib != Constants.Timing.CHIPREAD_DUMMYBIB && timeResult.Bib.Length > 0)
                {
                    lastSeenDictionary[timeResult.Bib] = timeResult;
                }
            }
            if (partDict.TryGetValue(Constants.Timing.EVENTSPECIFIC_STARTED, out List<Participant> oActiveList)) // ACTIVE
            {
                activePanel.Visibility = Visibility.Visible;
                foreach (Participant p in oActiveList)
                {
                    bool lastSeenExists = lastSeenDictionary.TryGetValue(p.Bib, out TimeResult oLastSeenRes);
                    string lastSeen = lastSeenExists ? oLastSeenRes.SegmentName : "";
                    string lastSeenTime = lastSeenExists ? oLastSeenRes.SysTime : "";
                    activeParticipants.Add(new(p, lastSeen, lastSeenTime));
                }
            }
            else
            {
                activePanel.Visibility = Visibility.Collapsed;
            }
            if (partDict.TryGetValue(Constants.Timing.EVENTSPECIFIC_DNS, out List<Participant> oDNSList)) // DNS
            {
                dnsPanel.Visibility = Visibility.Visible;
                foreach (Participant p in oDNSList)
                {
                    dnsParticipants.Add(p);
                }
            }
            else
            {
                dnsPanel.Visibility = Visibility.Collapsed;
            }
            if (partDict.TryGetValue(Constants.Timing.EVENTSPECIFIC_UNKNOWN, out List<Participant> oUnknownList)) // UNKOWN
            {
                unknownPanel.Visibility = Visibility.Visible;
                foreach (Participant p in oUnknownList)
                {
                    unknownParticipants.Add(p);
                }
            }
            else
            {
                unknownPanel.Visibility = Visibility.Collapsed;
            }
            if (partDict.TryGetValue(Constants.Timing.EVENTSPECIFIC_DNF, out List<Participant> oDNFList)) // DNF
            {
                dnfPanel.Visibility = Visibility.Visible;
                foreach (Participant p in oDNFList)
                {
                    dnfParticipants.Add(p);
                }
            }
            else
            {
                dnfPanel.Visibility = Visibility.Collapsed;
            }
            if (partDict.TryGetValue(Constants.Timing.EVENTSPECIFIC_FINISHED, out List<Participant> oFinishedList)) // FINISHED
            {
                finishedPanel.Visibility = Visibility.Visible;
                foreach (Participant p in oFinishedList)
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
                ModifyParticipantWindow modifyParticipant = new(window, database, selected);
                modifyParticipant.ShowDialog();
            }
        }

        public void Location(string location) { }

        public void Reader(string reader) { }
    }
}
