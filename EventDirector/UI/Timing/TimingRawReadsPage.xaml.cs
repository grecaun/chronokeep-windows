using EventDirector.Interfaces;
using EventDirector.UI.MainPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace EventDirector.UI.Timing
{
    /// <summary>
    /// Interaction logic for TimingRawReadsPage.xaml
    /// </summary>
    public partial class TimingRawReadsPage : ISubPage
    {
        IDBInterface database;
        TimingPage parent;
        Event theEvent;

        List<ChipRead> chipReads = new List<ChipRead>();

        public TimingRawReadsPage(TimingPage parent, IDBInterface database)
        {
            InitializeComponent();
            this.parent = parent;
            this.database = database;
            theEvent = database.GetCurrentEvent();
        }

        private void IgnoreButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Ignore Button clicked.");
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Done Button clicked.");
            parent.LoadMainDisplay();
        }

        public async void UpdateView()
        {
            List<ChipRead> reads = new List<ChipRead>();
            SortType sortType = parent.GetSortType();
            await Task.Run(() =>
            {
                reads.AddRange(database.GetChipReads(theEvent.Identifier));
            });
            chipReads.Clear();
            chipReads.AddRange(reads);
            string search = parent.GetSearchValue();
            await Task.Run(() =>
            {
                SortWorker(reads, sortType, search);
            });
            updateListView.SelectedItems.Clear();
            updateListView.ItemsSource = reads;
            updateListView.Items.Refresh();
        }

        public void Closing() { }

        public void UpdateDatabase() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public async void Search(string value)
        {
            List<ChipRead> reads = new List<ChipRead>(chipReads);
            SortType sortType = parent.GetSortType();
            string search = parent.GetSearchValue();
            await Task.Run(() =>
            {
                SortWorker(reads, sortType, search);
            });
            updateListView.SelectedItems.Clear();
            updateListView.ItemsSource = reads;
            updateListView.Items.Refresh();
        }

        public void EditSelected() { }

        private void UpdateListView_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateView();
        }

        private void SortWorker(List<ChipRead> reads, SortType sortType, string search)
        {
            reads.RemoveAll(read => read.IsNotMatch(search));
            if (sortType == SortType.BIB)
            {
                reads.Sort(ChipRead.CompareByBib);
            }
            else
            {
                reads.Sort();
            }
        }

        public void Show(PeopleType type) { }

        public async void SortBy(SortType sortType)
        {
            List<ChipRead> reads = new List<ChipRead>(chipReads);
            string search = parent.GetSearchValue();
            await Task.Run(() =>
            {
                SortWorker(reads, sortType, search);
            });
            updateListView.SelectedItems.Clear();
            updateListView.ItemsSource = reads;
            updateListView.Items.Refresh();
        }
    }
}
