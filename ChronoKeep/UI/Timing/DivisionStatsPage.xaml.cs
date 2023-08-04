using Chronokeep.Interfaces;
using Chronokeep.UI.MainPages;
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

namespace Chronokeep.UI.Timing
{
    /// <summary>
    /// Interaction logic for DistanceStatsPage.xaml
    /// </summary>
    public partial class DistanceStatsPage : ISubPage
    {
        IDBInterface database;
        TimingPage parent;
        Event theEvent;
        int distanceId;

        private ObservableCollection<Participant> activeParticipants = new ObservableCollection<Participant>();
        private ObservableCollection<Participant> dnsParticipants = new ObservableCollection<Participant>();
        private ObservableCollection<Participant> dnfParticipants = new ObservableCollection<Participant>();
        private ObservableCollection<Participant> finishedParticipants = new ObservableCollection<Participant>();

        public DistanceStatsPage(TimingPage parent, IDBInterface database, int distanceId, string DistanceName)
        {
            InitializeComponent();
            this.parent = parent;
            this.database = database;
            this.distanceId = distanceId;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                Log.E("UI.Timing.DivisionStatsPage", "Something went wrong and no proper event was returned.");
                return;
            }
            activeListView.ItemsSource = activeParticipants;
            dnsListView.ItemsSource = dnsParticipants;
            dnfListView.ItemsSource = dnfParticipants;
            finishedListView.ItemsSource = finishedParticipants;
            this.DistanceName.Text = DistanceName;
            UpdateView();
        }

        public void CancelableUpdateView(CancellationToken token) { }

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
            dnfParticipants.Clear();
            finishedParticipants.Clear();
            Dictionary<int, List<Participant>> partDict = database.GetDistanceParticipantsStatus(theEvent.Identifier, distanceId);
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
    }
}
