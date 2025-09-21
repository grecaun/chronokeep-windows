using Chronokeep.Interfaces;
using Chronokeep.UI.MainPages;
using Chronokeep.UI.UIObjects;
using System;
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
        ITimingPage parent;
        Event theEvent;

        List<ChipRead> chipReads = new List<ChipRead>();

        public TimingRawReadsPage(ITimingPage parent, IDBInterface database)
        {
            InitializeComponent();
            Log.D("UI.Timing.TimingRawReadsPage", "Page initialized.");
            this.parent = parent;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            Log.D("UI.Timing.TimingRawReadsPage", "Current event fetched.");
            if (parent is TimingPage)
            {
                PrivateUpdateView();
            }
            else if (parent is MinTimingPage)
            {
                SafemodeUpdateView();
            }
            Log.D("UI.Timing.TimingRawReadsPage", "View updated.");
            updateListView.SelectedIndex = updateListView.Items.Count - 1;
            updateListView.ScrollIntoView(updateListView.SelectedItem);
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
                else if (read.Status == Constants.Timing.CHIPREAD_STATUS_DNS_IGNORE)
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
            if (parent is TimingPage)
            {
                PrivateUpdateView();
            }
            else if (parent is MinTimingPage)
            {
                SafemodeUpdateView();
            }
            parent.NotifyTimingWorker();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.TimingRawReadsPage", "Done Button clicked.");
            parent.SetReaders([], false);
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

        public void UpdateView()
        {
            Log.D("UI.Timing.TimingRawReadsPage", "Update View called.");
        }

        public void CancelableUpdateView(CancellationToken token) { }

        public void Search(CancellationToken token, string searchText)
        {
            token.ThrowIfCancellationRequested();
            PrivateUpdateView();
        }

        internal void PrivateUpdateView()
        {
            List<ChipRead> reads = new List<ChipRead>();
            SortType sortType = parent.GetSortType();
            PeopleType peopleType = parent.GetPeopleType();
            string location = parent.GetLocation();
            string readerName = parent.GetReader();
            reads.AddRange(database.GetChipReads(theEvent.Identifier));
            chipReads.Clear();
            chipReads.AddRange(reads);
            HashSet<string> readerNames = [];
            foreach (ChipRead read in chipReads)
            {
                readerNames.Add(read.Box);
            }
            parent.SetReaders(["All Readers", .. readerNames], true);
            string search = parent.GetSearchValue();
            bool manualOnly = onlyManualBox.IsChecked == true;
            bool ignoredOnly = onlyIgnoreBox.IsChecked == true;
            SortWorker(reads, sortType, peopleType, search, manualOnly, location, ignoredOnly, readerName);
            updateListView.SelectedItems.Clear();
            updateListView.ItemsSource = reads;
            updateListView.Items.Refresh();
        }

        internal void SafemodeUpdateView()
        {
            theEvent = database.GetCurrentEvent();
            if (theEvent == null){
                return;
            }
            List<ChipRead> reads = new List<ChipRead>();
            SortType sortType = parent.GetSortType();
            PeopleType peopleType = parent.GetPeopleType();
            reads.AddRange(database.GetChipReadsSafemode(theEvent.Identifier));
            chipReads.Clear();
            chipReads.AddRange(reads);
            string search = parent.GetSearchValue();
            string location = parent.GetLocation();
            string readerName = parent.GetReader();
            bool manualOnly = onlyManualBox.IsChecked == true;
            bool ignoredOnly = onlyIgnoreBox.IsChecked == true;
            SortWorker(reads, sortType, peopleType, search, manualOnly, location, ignoredOnly, readerName);
            updateListView.SelectedItems.Clear();
            updateListView.ItemsSource = reads;
            updateListView.Items.Refresh();
        }

        public void Closing() { }

        public void UpdateDatabase() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public void EditSelected() { }

        private void SortWorker(
            List<ChipRead> reads,
            SortType sortType,
            PeopleType peopleType,
            string search,
            bool manualOnly,
            string location,
            bool ignoredOnly,
            string reader
            )
        {
            if (peopleType == PeopleType.UNKNOWN)
            {
                reads.RemoveAll(read => read.Name.Length > 0);
            }
            reads.RemoveAll(read => read.IsNotMatch(search));
            if (manualOnly)
            {
                reads.RemoveAll(read => read.Type == Constants.Timing.CHIPREAD_TYPE_CHIP);
            }
            if (ignoredOnly)
            {
                reads.RemoveAll(read =>
                    read.Status != Constants.Timing.CHIPREAD_STATUS_DNF_IGNORE
                    && read.Status != Constants.Timing.CHIPREAD_STATUS_DNS_IGNORE
                    && read.Status != Constants.Timing.CHIPREAD_STATUS_IGNORE
                    );
            }
            if (location != null && location.Length > 0 && !location.Equals("All Locations", StringComparison.OrdinalIgnoreCase))
            {
                reads.RemoveAll(read => !read.LocationName.Equals(location, StringComparison.OrdinalIgnoreCase));
            }
            if (reader != null && reader.Length > 0 && !reader.Equals("All Readers", StringComparison.OrdinalIgnoreCase))
            {
                reads.RemoveAll(read => !read.Box.Equals(reader, StringComparison.OrdinalIgnoreCase));
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

        public async void Show(PeopleType peopleType)
        {
            List<ChipRead> reads = [.. chipReads];
            string search = parent.GetSearchValue();
            string location = parent.GetLocation();
            string readerName = parent.GetReader();
            SortType sortType = parent.GetSortType();
            bool manualOnly = onlyManualBox.IsChecked == true;
            bool ignoredOnly = onlyIgnoreBox.IsChecked == true;
            await Task.Run(() =>
            {
                SortWorker(reads, sortType, peopleType, search, manualOnly, location, ignoredOnly, readerName);
            });
            updateListView.SelectedItems.Clear();
            updateListView.ItemsSource = reads;
            updateListView.Items.Refresh();
            updateListView.SelectedIndex = updateListView.Items.Count - 1;
            updateListView.ScrollIntoView(updateListView.SelectedItem);
        }

        public async void SortBy(SortType sortType)
        {
            List<ChipRead> reads = [.. chipReads];
            string search = parent.GetSearchValue();
            string location = parent.GetLocation();
            string readerName = parent.GetReader();
            PeopleType peopleType = parent.GetPeopleType();
            bool manualOnly = onlyManualBox.IsChecked == true;
            bool ignoredOnly = onlyIgnoreBox.IsChecked == true;
            await Task.Run(() =>
            {
                SortWorker(reads, sortType, peopleType, search, manualOnly, location, ignoredOnly, readerName);
            });
            updateListView.SelectedItems.Clear();
            updateListView.ItemsSource = reads;
            updateListView.Items.Refresh();
            updateListView.SelectedIndex = updateListView.Items.Count - 1;
            updateListView.ScrollIntoView(updateListView.SelectedItem);
        }

        public async void Location(string location)
        {
            List<ChipRead> reads = [.. chipReads];
            PeopleType peopleType = parent.GetPeopleType();
            SortType sortType = parent.GetSortType();
            string search = parent.GetSearchValue();
            string readerName = parent.GetReader();
            bool manualOnly = onlyManualBox.IsChecked == true;
            bool ignoredOnly = onlyIgnoreBox.IsChecked == true;
            await Task.Run(() =>
            {
                SortWorker(reads, sortType, peopleType, search, manualOnly, location, ignoredOnly, readerName);
            });
            updateListView.SelectedItems.Clear();
            updateListView.ItemsSource = reads;
            updateListView.Items.Refresh();
            updateListView.SelectedIndex = updateListView.Items.Count - 1;
            updateListView.ScrollIntoView(updateListView.SelectedItem);
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
                    if (parent is TimingPage)
                    {
                        PrivateUpdateView();
                    }
                    else if (parent is MinTimingPage)
                    {
                        SafemodeUpdateView();
                    }
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
            if (parent is TimingPage)
            {
                PrivateUpdateView();
            }
            else if (parent is MinTimingPage)
            {
                SafemodeUpdateView();
            }
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
            if (parent is TimingPage)
            {
                PrivateUpdateView();
            }
            else if (parent is MinTimingPage)
            {
                SafemodeUpdateView();
            }
            parent.NotifyTimingWorker();
        }

        private void OnlyManualBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.TimingRawReadsPage", "Manual entries only box checked status changed.");
            List<ChipRead> reads = [.. chipReads];
            string search = parent.GetSearchValue();
            string location = parent.GetLocation();
            string readerName = parent.GetReader();
            SortType sortType = parent.GetSortType();
            PeopleType peopleType = parent.GetPeopleType();
            bool manualOnly = onlyManualBox.IsChecked == true;
            bool ignoredOnly = onlyIgnoreBox.IsChecked == true;
            SortWorker(reads, sortType, peopleType, search, manualOnly, location, ignoredOnly, readerName);
            updateListView.SelectedItems.Clear();
            updateListView.ItemsSource = reads;
            updateListView.Items.Refresh();
            updateListView.SelectedIndex = updateListView.Items.Count - 1;
            updateListView.ScrollIntoView(updateListView.SelectedItem);
        }

        private void UpdateListView_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            labelsViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

        private void OnlyIgnoreBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.TimingRawReadsPage", "Manual entries only box checked status changed.");
            List<ChipRead> reads = [.. chipReads];
            string search = parent.GetSearchValue();
            string location = parent.GetLocation();
            string readerName = parent.GetReader();
            SortType sortType = parent.GetSortType();
            PeopleType peopleType = parent.GetPeopleType();
            bool manualOnly = onlyManualBox.IsChecked == true;
            bool ignoredOnly = onlyIgnoreBox.IsChecked == true;
            SortWorker(reads, sortType, peopleType, search, manualOnly, location, ignoredOnly, readerName);
            updateListView.SelectedItems.Clear();
            updateListView.ItemsSource = reads;
            updateListView.Items.Refresh();
            updateListView.SelectedIndex = updateListView.Items.Count - 1;
            updateListView.ScrollIntoView(updateListView.SelectedItem);
        }

        public void Reader(string reader)
        {
            List<ChipRead> reads = [.. chipReads];
            string search = parent.GetSearchValue();
            string location = parent.GetLocation();
            string readerName = parent.GetReader();
            SortType sortType = parent.GetSortType();
            PeopleType peopleType = parent.GetPeopleType();
            bool manualOnly = onlyManualBox.IsChecked == true;
            bool ignoredOnly = onlyIgnoreBox.IsChecked == true;
            SortWorker(reads, sortType, peopleType, search, manualOnly, location, ignoredOnly, readerName);
            updateListView.SelectedItems.Clear();
            updateListView.ItemsSource = reads;
            updateListView.Items.Refresh();
            updateListView.SelectedIndex = updateListView.Items.Count - 1;
            updateListView.ScrollIntoView(updateListView.SelectedItem);
        }
    }
}
