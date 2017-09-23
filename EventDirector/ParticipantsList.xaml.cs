using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for ParticipantsList.xaml
    /// </summary>
    public partial class ParticipantsListWindow : Window
    {
        IDBInterface database;
        MainWindow mainWindow;
        bool running = false, handleSelection = true, firsttime = true;
        int oldEventIndex = 0;

        public ParticipantsListWindow(IDBInterface database, MainWindow mWin)
        {
            this.mainWindow = mWin;
            this.database = database;
            InitializeComponent();
            UpdateEventsBox();
            firsttime = false;
        }

        public void UpdateEventsBox()
        {
            List<Event> events = database.GetEvents();
            eventComboBox.Items.Clear();
            ComboBoxItem boxItem = new ComboBoxItem
            {
                Content = "All",
                Uid = "-1"
            };
            eventComboBox.Items.Add(boxItem);
            foreach (Event e in events)
            {
                boxItem = new ComboBoxItem
                {
                    Content = e.Name,
                    Uid = e.Identifier.ToString()
                };
                eventComboBox.Items.Add(boxItem);
            }
            eventComboBox.SelectedIndex = 0;
        }

        public async void UpdateParticipantsView()
        {
            if (eventComboBox.SelectedIndex < 0)
            {
                Log.D("Ruh-roh, somehow nothing is selected.");
                return;
            }
            int eventIx = eventComboBox.SelectedIndex, eventIdentifier = -1;
            if (eventIx != 0)
            {
                eventIdentifier = Convert.ToInt32(((ComboBoxItem)eventComboBox.SelectedItem).Uid);
            }
            List<Participant> participants = new List<Participant>();
            running = true;
            await Task.Run(() =>
            {
                if (eventIx == 0)
                {
                    Log.D("Get everything.");
                    participants = database.GetParticipants();
                }
                else
                {
                    Log.D("Figure out what ID to use and get a specific event.");
                    participants = database.GetParticipants(eventIdentifier);
                }
                if (participantsListView == null)
                {
                    Log.D("Participants Listview isn't there.");
                    return;
                }
            });
            running = false;
            Log.D("Participants listview found!");
            participantsListView.Items.Clear();
            foreach (Participant p in participants)
            {
                participantsListView.Items.Add(p);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void EventComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.D("Event selected.");
            if (handleSelection == true)
            {
                if (running == false)
                {
                    Log.D("No update running currently.");
                    oldEventIndex = eventComboBox.SelectedIndex;
                    UpdateParticipantsView();
                }
                else if (firsttime != true)
                {
                    Log.D("Update running.");
                    MessageBox.Show("Currently getting information. Please wait.");
                    handleSelection = false;
                    eventComboBox.SelectedIndex = oldEventIndex;
                }
            }
            else
            {
                Log.D("We've changed the index ourself. Go figure.");
                handleSelection = true;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            mainWindow.PartListClosed();
        }
    }
}
