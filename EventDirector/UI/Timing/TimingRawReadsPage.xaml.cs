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
        }

        public async void UpdateView()
        {
            List<ChipRead> reads = new List<ChipRead>();
            await Task.Run(() =>
            {
                reads.AddRange(database.GetChipReads(theEvent.Identifier));
                reads.Sort();
            });
            updateListView.SelectedItems.Clear();
            updateListView.ItemsSource = reads;
        }

        public void Closing() { }

        public void UpdateDatabase() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        public void Search(string value) { }

        public void EditSelected() { }

        private void UpdateListView_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateView();
        }
    }
}
