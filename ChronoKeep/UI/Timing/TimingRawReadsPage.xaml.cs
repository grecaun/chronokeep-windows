using Chronokeep.Interfaces;
using Chronokeep.UI.MainPages;
using Chronokeep.UI.UIObjects;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

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
            updateListView.SelectedIndex = updateListView.Items.Count - 1;
            updateListView.ScrollIntoView(updateListView.SelectedItem);
            updateListView.SelectedItem = null;
            Log.D("UI.Timing.TimingRawReadsPage", "We're at the bottom.");
        }

        private void IgnoreButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.TimingRawReadsPage", "Ignore Button clicked.");
            List<ChipRead> newChipReads = new List<ChipRead>();
            foreach (ChipRead read in updateListView.SelectedItems)
            {
                // Check what the previous status was. If it was FORCEIGNORE, then we can set to NONE
                if (read.Status == Constants.Timing.CHIPREAD_STATUS_IGNORE)
                {
                    read.Status = Constants.Timing.CHIPREAD_STATUS_NONE;
                }
                // Else if it's DNF, we need to use the special status of DNF ignore
                // so we can restore it to DNF status if we want to un-ignore the read.
                else if (read.Status == Constants.Timing.CHIPREAD_STATUS_DNF)
                {
                    read.Status = Constants.Timing.CHIPREAD_STATUS_DNF_IGNORE;
                }
                else if (read.Status == Constants.Timing.CHIPREAD_STATUS_DNF_IGNORE)
                {
                    read.Status = Constants.Timing.CHIPREAD_STATUS_DNF;
                }
                // Treat DNS the same as DNF.
                else if (read.Status == Constants.Timing.CHIPREAD_STATUS_DNS)
                {
                    read.Status = Constants.Timing.CHIPREAD_STATUS_DNS_IGNORE;
                }
                else if(read.Status == Constants.Timing.CHIPREAD_STATUS_DNS_IGNORE)
                {
                    read.Status = Constants.Timing.CHIPREAD_STATUS_DNS;
                }
                // These reads are not DNF or DNS. Don't modify announcer reads.
                else if (read.Status != Constants.Timing.CHIPREAD_STATUS_ANNOUNCER_SEEN &&
                    read.Status != Constants.Timing.CHIPREAD_STATUS_ANNOUNCER_USED)
                {
                    read.Status = Constants.Timing.CHIPREAD_STATUS_IGNORE;
                }
                newChipReads.Add(read);
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
            bool manualOnly = onlyManualBox.IsChecked == true;
            await Task.Run(() =>
            {
                SortWorker(reads, sortType, search, manualOnly);
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

        private void SortWorker(List<ChipRead> reads, SortType sortType, string search, bool manualOnly)
        {
            reads.RemoveAll(read => read.IsNotMatch(search));
            if (manualOnly)
            {
                reads.RemoveAll(read => read.Type == Constants.Timing.CHIPREAD_TYPE_CHIP);
            }
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
            bool manualOnly = onlyManualBox.IsChecked == true;
            await Task.Run(() =>
            {
                SortWorker(reads, sortType, search, manualOnly);
            });
            updateListView.SelectedItems.Clear();
            updateListView.ItemsSource = reads;
            updateListView.Items.Refresh();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.TimingRawReadsPage", "Delete clicked.");
            DialogBox.Show(
                "Are you sure you wish to delete these records? They cannot be recovered if you have no other record of them.",
                "Yes",
                "No",
                () =>
                {
                    List<ChipRead> readsToDelete = new List<ChipRead>();
                    foreach (ChipRead read in updateListView.SelectedItems)
                    {
                        readsToDelete.Add(read);
                    }
                    database.DeleteChipReads(readsToDelete);
                    database.ResetTimingResultsEvent(theEvent.Identifier);
                    UpdateView();
                    parent.NotifyTimingWorker();
                });
        }

        private void ChangeDNS_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.TimingRawReadsPage", "ChangeDNS Button clicked.");
            List<ChipRead> newChipReads = new List<ChipRead>();
            foreach (ChipRead read in updateListView.SelectedItems)
            {
                // Check what the previous status was. If it was CHIPREAD_STATUS_DNS we change it to NONE
                if (read.Status == Constants.Timing.CHIPREAD_STATUS_DNS)
                {
                    read.Status = Constants.Timing.CHIPREAD_STATUS_NONE;
                }
                // Else set it to DNS
                else
                {
                    read.Status = Constants.Timing.CHIPREAD_STATUS_DNS;
                }
                newChipReads.Add(read);
            }
            database.SetChipReadStatuses(newChipReads);
            database.ResetTimingResultsEvent(theEvent.Identifier);
            UpdateView();
            parent.NotifyTimingWorker();
        }

        private void ChangeDNF_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.TimingRawReadsPage", "ChangeDNF Button clicked.");
            List<ChipRead> newChipReads = new List<ChipRead>();
            foreach (ChipRead read in updateListView.SelectedItems)
            {
                // Check what the previous status was. If it was CHIPREAD_STATUS_DNF we change it to NONE
                if (read.Status == Constants.Timing.CHIPREAD_STATUS_DNF)
                {
                    read.Status = Constants.Timing.CHIPREAD_STATUS_NONE;
                }
                // Else set it to DNF
                else
                {
                    read.Status = Constants.Timing.CHIPREAD_STATUS_DNF;
                }
                newChipReads.Add(read);
            }
            database.SetChipReadStatuses(newChipReads);
            database.ResetTimingResultsEvent(theEvent.Identifier);
            UpdateView();
            parent.NotifyTimingWorker();
        }

        private void onlyManualBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.TimingRawReadsPage", "Manual entries only box checked status changed.");
            UpdateView();
        }
    }
}
