using Chronokeep.Interfaces;
using Chronokeep.UI.UIObjects;
using System;
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

        private ChangeEventWindow(IWindowCallback window, IDBInterface database)
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
            if (searchBox.Text.Length > 0)
            {
                Log.D("UI.ChangeEventWindow", "searchBox.Text " + searchBox.Text);
                events.RemoveAll(x => !x.Name.Contains(searchBox.Text, StringComparison.OrdinalIgnoreCase));
            }
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
                database.SetCurrentEvent(one.Identifier);
                window.WindowFinalize(this);
            }
            else
            {
                Log.D("UI.ChangeEventWindow", "No event selected.");
                DialogBox.Show("No event selected.");
                return;
            }
            this.Close();
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.ChangeEventWindow", "Delete button clicked.");
            Event one = (Event)eventList.SelectedItem;
            if (one != null)
            {
                Log.D("UI.ChangeEventWindow", "Selected event has ID of " + one.Identifier);
                database.RemoveEvent(one.Identifier);
                UpdateEventBox();
            }
            else
            {
                Log.D("UI.ChangeEventWindow", "No event selected.");
                DialogBox.Show("No event selected.");
            }
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
                database.SetCurrentEvent(one.Identifier);
                window.WindowFinalize(this);
            }
            else
            {
                Log.D("UI.ChangeEventWindow", "No event selected.");
                DialogBox.Show("No event selected.");
                return;
            }
            this.Close();
        }

        private void searchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateEventBox();
        }
    }
}
