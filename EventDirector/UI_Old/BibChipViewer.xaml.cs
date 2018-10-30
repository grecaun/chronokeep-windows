using System;
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
    /// Interaction logic for BibChipViewer.xaml
    /// </summary>
    public partial class BibChipViewer : Window
    {
        IDBInterface database;
        MainWindow mainWindow;
        bool running = false, handleSelection = true, firsttime = true;
        int oldEventIndex = 0;

        public BibChipViewer(IDBInterface database, MainWindow mWin)
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

        public async void UpdateBibChipView()
        {
            if (eventComboBox.SelectedIndex < 0)
            {
                Log.D("Error, nothing is selected");
                return;
            }
            int eventIx = eventComboBox.SelectedIndex, eventIdentifier = -1;
            if (eventIx != 0)
            {
                eventIdentifier = Convert.ToInt32(((ComboBoxItem)eventComboBox.SelectedItem).Uid);
            }
            List<BibChipAssociation> bibChips = new List<BibChipAssociation>();
            running = true;
            await Task.Run(() =>
            {
                if (eventIx == 0)
                {
                    Log.D("Get everything.");
                    bibChips = database.GetBibChips();
                }
                else
                {
                    Log.D("Get a specific event.");
                    bibChips = database.GetBibChips(eventIdentifier);
                }
                if (bibChips == null)
                {
                    Log.D("Listview isn't there.");
                    return;
                }
            });
            running = false;
            Log.D("Listview found.");
            bibChips.Sort();
            bibchipListView.ItemsSource = bibChips;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            mainWindow.WindowClosed(this);
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
                    UpdateBibChipView();
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
    }
}
