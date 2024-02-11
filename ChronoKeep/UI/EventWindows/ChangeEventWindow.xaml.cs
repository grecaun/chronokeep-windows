using Chronokeep.Interfaces;
using Chronokeep.UI.UIObjects;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui.Controls;

namespace Chronokeep.UI
{
    /// <summary>
    /// Interaction logic for ChangeEventWindow.xaml
    /// </summary>
    public partial class ChangeEventWindow : FluentWindow
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
            events.Sort();
            eventList.ItemsSource = events;
            if (events.Count < 1)
            {
                changeButton.IsEnabled = false;
            }
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.ChangeEventWindow", "Change Button Clicked.");
            Event one = (Event)eventList.SelectedItem;
            if (one != null)
            {
                Log.D("UI.ChangeEventWindow", "Selected event has ID of " + one.Identifier);
                database.SetAppSetting(Constants.Settings.CURRENT_EVENT, one.Identifier.ToString());
            }
            else
            {
                Log.D("UI.ChangeEventWindow", "No event selected.");
                DialogBox.Show("No event selected.");
                return;
            }
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.ChangeEventWindow", "Cancel button clicked.");
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            window.WindowFinalize(this);
        }

        private void eventList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Log.D("UI.ChangeEventWindow", "Double Click detected.");
            Event one = (Event)eventList.SelectedItem;
            if (one != null)
            {
                Log.D("UI.ChangeEventWindow", "Selected event has ID of " + one.Identifier);
                database.SetAppSetting(Constants.Settings.CURRENT_EVENT, one.Identifier.ToString());
            }
            else
            {
                Log.D("UI.ChangeEventWindow", "No event selected.");
                DialogBox.Show("No event selected.");
                return;
            }
            this.Close();
        }
    }
}
