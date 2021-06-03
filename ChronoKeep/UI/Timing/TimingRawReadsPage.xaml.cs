using ChronoKeep.Interfaces;
using ChronoKeep.UI.MainPages;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ChronoKeep.UI.Timing
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
        HashSet<int> bibsToReset = new HashSet<int>();
        HashSet<string> chipsToReset = new HashSet<string>();

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
            List<ChipRead> newChipReads = new List<ChipRead>();
            foreach (ChipRead read in updateListView.SelectedItems)
            {
                if (read.Status == Constants.Timing.CHIPREAD_STATUS_FORCEIGNORE)
                {
                    read.Status = Constants.Timing.CHIPREAD_STATUS_NONE;
                }
                else
                {
                    read.Status = Constants.Timing.CHIPREAD_STATUS_FORCEIGNORE;
                }
                newChipReads.Add(read);
                if (read.ChipBib != Constants.Timing.CHIPREAD_DUMMYBIB)
                {
                    bibsToReset.Add(read.ChipBib);
                }
                else if (read.ReadBib != Constants.Timing.CHIPREAD_DUMMYBIB)
                {
                    bibsToReset.Add(read.ReadBib);
                }
                else
                {
                    chipsToReset.Add(read.ChipNumber);
                }
            }
            database.SetChipReadStatuses(newChipReads);
            database.ResetTimingResultsEvent(theEvent.Identifier);
            UpdateView();
            parent.NotifyTimingWorker();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Done Button clicked.");
            parent.LoadMainDisplay();
        }

        private void Shift_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Shift button clicked.");
            List<ChipRead> localReads = new List<ChipRead>();
            foreach (ChipRead read in updateListView.SelectedItems)
            {
                localReads.Add(read);
            }
            EditRawReadsWindow editRawReadsWindow = new EditRawReadsWindow(parent, database, localReads);
            editRawReadsWindow.ShowDialog();
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

        public async void Search(string value, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
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

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Delete clicked.");
            if (MessageBoxResult.Yes == MessageBox.Show("Are you sure you wish to delete these records? They " +
                "cannot be recovered if you have no other record of them.", "Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Hand))
            {
                List<ChipRead> readsToDelete = new List<ChipRead>();
                foreach (ChipRead read in updateListView.SelectedItems)
                {
                    readsToDelete.Add(read);
                    if (read.ChipBib != Constants.Timing.CHIPREAD_DUMMYBIB)
                    {
                        bibsToReset.Add(read.ChipBib);
                    }
                    else if (read.ReadBib != Constants.Timing.CHIPREAD_DUMMYBIB)
                    {
                        bibsToReset.Add(read.ReadBib);
                    }
                    else
                    {
                        chipsToReset.Add(read.ChipNumber);
                    }
                }
                database.DeleteChipReads(readsToDelete);
                database.ResetTimingResultsEvent(theEvent.Identifier);
                UpdateView();
                parent.NotifyTimingWorker();
            }
        }
    }
}
