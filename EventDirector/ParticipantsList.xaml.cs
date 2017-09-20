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
        public ParticipantsList(IDBInterface database)
        {
            InitializeComponent();
            this.database = database;
            UpdateEventsBox();
            UpdateParticipantsView();
        }

        public void UpdateEventsBox()
        {
            ArrayList events = database.GetEvents();
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
            switch (eventIx)
            {
                case 0:
                    Log.D("Get everything.");
                    break;
                default:
                    Log.D("Figure out what ID to use and get a specific event.");
                    break;
            }
        }
    }
}
