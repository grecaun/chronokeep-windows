using EventDirector.Interfaces;
using EventDirector.UI.MainPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventDirector.UI.Timing
{
    /// <summary>
    /// Interaction logic for TimingResultsPage.xaml
    /// </summary>
    public partial class TimingResultsPage : ISubPage
    {
        TimingPage parent;
        IDBInterface database;
        Event theEvent;

        List<TimeResult> results = new List<TimeResult>();

        public TimingResultsPage(TimingPage parent, IDBInterface database)
        {
            InitializeComponent();
            this.parent = parent;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            TimeResult.SetupStaticVariables(database);
        }

        public void Closing() { }

        public void EditSelected() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public void Search(string value) { }

        private void Customize(SortType sortType, PeopleType peopleType, List<TimeResult> newResults)
        {
            if (peopleType == PeopleType.KNOWN)
            {
                newResults.RemoveAll(TimeResult.IsNotKnown);
            }
            else if (peopleType == PeopleType.ONLYFINISH)
            {
                newResults.RemoveAll(TimeResult.IsNotFinish);
            }
            else if (peopleType == PeopleType.ONLYSTART)
            {
                newResults.RemoveAll(TimeResult.IsNotStart);
            }
            if (sortType == SortType.BIB)
            {
                newResults.Sort(TimeResult.CompareByBib);
            }
            else if (sortType == SortType.GUNTIME)
            {
                newResults.Sort(TimeResult.CompareByGunTime);
            }
            else if (sortType == SortType.DIVISION)
            {
                newResults.Sort(TimeResult.CompareByDivision);
            }
            else
            {
                newResults.Sort(TimeResult.CompareBySystemTime);
            }
        }

        public async void SortBy(SortType sortType)
        {
            List<TimeResult> newResults = new List<TimeResult>(results);
            PeopleType peopleType = parent.GetPeopleType();
            await Task.Run(() =>
            {
                Customize(sortType, peopleType, newResults);
            });
            updateListView.ItemsSource = newResults;
            updateListView.Items.Refresh();
        }

        public void UpdateDatabase() { }

        public async void UpdateView()
        {
            List<TimeResult> newResults = null;
            SortType sortType = parent.GetSortType();
            PeopleType peopleType = parent.GetPeopleType();
            await Task.Run(() =>
            {
                newResults = database.GetTimingResults(theEvent.Identifier);
            });
            results.Clear();
            results.AddRange(newResults);
            await Task.Run(() =>
            {
                Customize(sortType, peopleType, newResults);
            });
            updateListView.ItemsSource = newResults;
            updateListView.Items.Refresh();
        }

        public async void Show(PeopleType peopleType)
        {
            List<TimeResult> newResults = new List<TimeResult>(results);
            SortType sortType = parent.GetSortType();
            await Task.Run(() =>
            {
                Customize(sortType, peopleType, newResults);
            });
            updateListView.ItemsSource = newResults;
            updateListView.Items.Refresh();
        }

        private void UpdateListView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateView();
        }
    }
}
