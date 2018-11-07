using EventDirector.Interfaces;
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
        }

        private void Modify_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Modify clicked.");
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Remove clicked.");
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
