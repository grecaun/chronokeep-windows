using Chronokeep.Interfaces;
using Chronokeep.UI.MainPages;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.Timing
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
            Log.D("UI.Timing.TimingRawReadsPage", "Page initialized.");
            this.parent = parent;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            Log.D("UI.Timing.TimingRawReadsPage", "Current event fetched.");
            UpdateView();
            Log.D("UI.Timing.TimingRawReadsPage", "View updated.");
            scrollViewer.ScrollToBottom();
            Log.D("UI.Timing.TimingRawReadsPage", "We're at the bottom.");
        }

        private void IgnoreButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.TimingRawReadsPage", "Ignore Button clicked.");
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
            Log.D("UI.Timing.TimingRawReadsPage", "Done Button clicked.");
            parent.LoadMainDisplay();
        }

        private void Shift_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.TimingRawReadsPage", "Shift button clicked.");
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
            Log.D("UI.Timing.TimingRawReadsPage", "Update View called.");
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

        public void CancelableUpdateView(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            UpdateView();
        }

        public void Closing() { }

        public void UpdateDatabase() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public void EditSelected() { }

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
            Log.D("UI.Timing.TimingRawReadsPage", "Delete clicked.");
            Dialog dialog = new()
            {
                Title = "Confirmation",
                Message = "Are you sure you wish to delete these records? They cannot be recovered if you have no other record of them.",
                ButtonLeftName = "Yes",
                ButtonRightName = "No",
            };
            dialog.ButtonLeftClick += (sender, e) =>
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
            };
            dialog.Show();
        }

        private void updateListView_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScrollViewer scv = scrollViewer;
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
