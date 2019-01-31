using EventDirector.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EventDirector.UI
{
    /// <summary>
    /// Interaction logic for ChangeEventWindow.xaml
    /// </summary>
    public partial class ChangeEventWindow : Window
    {
        private IWindowCallback window;
        private IDBInterface database;

        public ChangeEventWindow(IWindowCallback window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            UpdateEventBox();
        }

        public static ChangeEventWindow NewWindow(IWindowCallback window, IDBInterface database)
        {
            return new ChangeEventWindow(window, database);
        }

        internal async void UpdateEventBox()
        {
            List<Event> events = null;
            await Task.Run(() =>
            {
                events = database.GetEvents();
            });
            eventList.ItemsSource = events;
            if (events.Count < 1)
            {
                changeButton.IsEnabled = false;
            }
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Change Button Clicked.");
            Event one = (Event)eventList.SelectedItem;
            if (one != null)
            {
                Log.D("Selected event has ID of " + one.Identifier);
                database.SetAppSetting(Constants.Settings.CURRENT_EVENT, one.Identifier.ToString());
            }
            else
            {
                Log.D("No event selected.");
            }
            this.Close();

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Cancel button clicked.");
            this.Close();
        }

        private void EventList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Log.D("EventList size has changed.");
            ListView listView = sender as ListView;
            GridView gView = listView.View as GridView;

            var workingWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth-2;
            gView.Columns[0].Width = workingWidth * 0.5;
            gView.Columns[1].Width = workingWidth * 0.2;
            gView.Columns[2].Width = workingWidth * 0.3;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            window.WindowFinalize(this);
        }
    }
}
