using EventDirector.Interfaces;
using EventDirector.UI.Participants;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EventDirector.UI.MainPages
{
    /// <summary>
    /// Interaction logic for ParticipantsPage.xaml
    /// </summary>
    public partial class ParticipantsPage : Page, IMainPage
    {
        private INewMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;

        public ParticipantsPage(INewMainWindow mainWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mainWindow;
            this.database = database;
            UpdateDivisionsBox();
            Update();
            UpdateImportOptions();
        }

        public void Update()
        {
            Log.D("Updating Participants Page.");
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            List<Participant> participants;
            int divisionId = -1;
            try
            {
                divisionId = Convert.ToInt32(((ComboBoxItem)DivisionBox.SelectedItem).Uid);
            }
            catch
            {
                divisionId = -1;
            }
            if (divisionId == -1)
            {
                participants = database.GetParticipants(theEvent.Identifier);
            }
            else
            {
                participants = database.GetParticipants(theEvent.Identifier, divisionId);
            }
            participants.Sort();
            ParticipantsList.ItemsSource = participants;
            ParticipantsList.SelectedItems.Clear();
        }

        public void UpdateDivisionsBox()
        {
            theEvent = database.GetCurrentEvent();
            DivisionBox.Items.Clear();
            DivisionBox.Items.Add(new ComboBoxItem()
            {
                Content = "All",
                Uid = "-1"
            });
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            List<Division> divisions = database.GetDivisions(theEvent.Identifier);
            divisions.Sort();
            foreach (Division d in divisions)
            {
                DivisionBox.Items.Add(new ComboBoxItem()
                {
                    Content = d.Name,
                    Uid = d.Identifier.ToString()
                });
            }
            DivisionBox.SelectedIndex = 0;
        }

        private void UpdateImportOptions()
        {
            if (mWindow.ExcelEnabled())
            {
                Log.D("Excel is allowed.");
                ImportExcel.Visibility = Visibility.Visible;
            }
            else
            {
                Log.D("Excel is not allowed.");
                ImportExcel.Visibility = Visibility.Collapsed;
            }
        }

        private void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Import Excel clicked.");
        }

        private void ImportCSV_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Import CSV clicked.");
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Add clicked.");
            ModifyParticipantWindow addParticipant = ModifyParticipantWindow.NewWindow(mWindow, database);
            if (addParticipant != null)
            {
                mWindow.AddWindow(addParticipant);
                addParticipant.Show();
            }
        }

        private void Modify_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Modify clicked.");
            IList selected = ParticipantsList.SelectedItems;
            Log.D(selected.Count + " participants selected.");
            if (selected.Count > 1)
            {
                MessageBox.Show("You may only modify a single participant at a time.");
                return;
            }
            Participant part = null;
            foreach (Participant p in selected)
            {
                part = p;
            }
            if (part == null) return;
            ModifyParticipantWindow modifyParticipant = ModifyParticipantWindow.NewWindow(mWindow, database, part);
            if (modifyParticipant != null)
            {
                mWindow.AddWindow(modifyParticipant);
                modifyParticipant.Show();
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Remove clicked.");
            IList selected = ParticipantsList.SelectedItems;
            List<Participant> parts = new List<Participant>();
            foreach (Participant p in selected)
            {
                parts.Add(p);
            }
            database.RemoveEntries(parts);
            Update();
        }

        private void ParticipantsList_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void DivisionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Update();
        }
    }
}
