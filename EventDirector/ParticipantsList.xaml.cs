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
    public partial class ParticipantsList : Window
    {
        IDBInterface database;
        MainWindow mainWindow;

        public ParticipantsList(IDBInterface database, MainWindow mWin)
        {
            this.mainWindow = mWin;
            this.database = database;
            InitializeComponent();
            UpdateEventsBox();
            UpdateParticipantsView();
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

        public void UpdateParticipantsView()
        {
            if (eventComboBox.SelectedIndex < 0)
            {
                Log.D("Ruh-roh, somehow nothing is selected.");
                return;
            }
            int eventIx = eventComboBox.SelectedIndex;
            List<Participant> participants;
            if (eventIx == 0)
            {
                Log.D("Get everything.");
                participants = database.GetParticipants();
            }
            else
            {
                Log.D("Figure out what ID to use and get a specific event.");
                int eventIdentifier = Convert.ToInt32(((ComboBoxItem)eventComboBox.SelectedItem).Uid);
                participants = database.GetParticipants(eventIdentifier);
            }
            if (participantsListView == null)
            {
                Log.D("Participants Listview isn't there.");
                return;
            }
            Log.D("Participants listview found!");
            participantsListView.Items.Clear();
            foreach (Participant p in participants)
            {
                participantsListView.Items.Add(p);
            }
        }

        private void EventComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateParticipantsView();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            mainWindow.PartListClosed();
        }
    }
}
