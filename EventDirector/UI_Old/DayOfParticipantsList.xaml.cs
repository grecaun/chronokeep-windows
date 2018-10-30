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
    public partial class DayOfParticipantsListWindow : Window
    {
        IDBInterface database;
        MainWindow mainWindow;
        bool running = false, handleSelection = true, firsttime = true;
        int oldEventIndex = 0;

        public DayOfParticipantsListWindow(IDBInterface database, MainWindow mWin)
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
            dofEventComboBox.Items.Clear();
            ComboBoxItem boxItem = new ComboBoxItem
            {
                Content = "All",
                Uid = "-1"
            };
            dofEventComboBox.Items.Add(boxItem);
            foreach (Event e in events)
            {
                boxItem = new ComboBoxItem
                {
                    Content = e.Name,
                    Uid = e.Identifier.ToString()
                };
                dofEventComboBox.Items.Add(boxItem);
            }
            dofEventComboBox.SelectedIndex = 0;
        }

        public async void UpdateParticipantsView()
        {
            if (dofEventComboBox.SelectedIndex < 0)
            {
                Log.D("Ruh-roh, somehow nothing is selected.");
                return;
            }
            int eventIx = dofEventComboBox.SelectedIndex, eventIdentifier = -1;
            if (eventIx != 0)
            {
                eventIdentifier = Convert.ToInt32(((ComboBoxItem)dofEventComboBox.SelectedItem).Uid);
            }
            List<DayOfParticipant> participants = new List<DayOfParticipant>();
            running = true;
            await Task.Run(() =>
            {
                if (eventIx == 0)
                {
                    Log.D("Get everything.");
                    participants = database.GetDayOfParticipants();
                }
                else
                {
                    Log.D("Figure out what ID to use and get a specific event.");
                    participants = database.GetDayOfParticipants(eventIdentifier);
                }
                if (participantsListView == null)
                {
                    Log.D("Participants Listview isn't there.");
                    return;
                }
            });
            running = false;
            Log.D("Participants listview found!");
            participants.Sort();
            participantsListView.ItemsSource = participants;
        }

        private void PrintParticipant(object sender, RoutedEventArgs e)
        {
            if (participantsListView.SelectedIndex >= 0)
            {
                Log.D("Printing actual participant");
                DayOfParticipant part = (DayOfParticipant)participantsListView.SelectedItem;
                List<Division> divs = database.GetDivisions(part.EventIdentifier);
                foreach (Division d in divs)
                {
                    if (d.Identifier == part.DivisionIdentifier)
                    {
                        Printerface.PrintDayOfShowDialog(part, d);
                        break;
                    }
                }
            }
            else
            {
                Log.D("No participant found.");
            }
        }

        private void EventComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.D("Event selected.");
            if (handleSelection == true)
            {
                if (running == false)
                {
                    Log.D("No update running currently.");
                    oldEventIndex = dofEventComboBox.SelectedIndex;
                    UpdateParticipantsView();
                }
                else if (firsttime != true)
                {
                    Log.D("Update running.");
                    MessageBox.Show("Currently getting information. Please wait.");
                    handleSelection = false;
                    dofEventComboBox.SelectedIndex = oldEventIndex;
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
            mainWindow.DoPartListClosed();
        }
    }
}
