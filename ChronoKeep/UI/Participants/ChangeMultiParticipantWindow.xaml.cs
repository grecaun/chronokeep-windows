using ChronoKeep.Interfaces;
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

namespace ChronoKeep.UI.Participants
{
    /// <summary>
    /// Interaction logic for ChangeMultiParticipantWindow.xaml
    /// </summary>
    public partial class ChangeMultiParticipantWindow : Window
    {
        IMainWindow window;
        IDBInterface database;
        List<Participant> toChange;
        Event theEvent;

        public ChangeMultiParticipantWindow(IMainWindow window, IDBInterface database, List<Participant> toChange)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            this.toChange = toChange;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null) return;
            foreach (Division div in database.GetDivisions(theEvent.Identifier))
            {
                DivisionBox.Items.Add(new ComboBoxItem()
                {
                    Content = div.Name,
                    Uid = div.Identifier.ToString()
                });
            }
            DivisionBox.SelectedIndex = 0;
            DivisionBox.Focus();
            if (theEvent.AllowEarlyStart)
            {
                EarlyStartHolder.Visibility = Visibility.Visible;
            }
            else
            {
                EarlyStartHolder.Visibility = Visibility.Collapsed;
                EarlyStart.IsChecked = false;
            }
        }

        private void Change_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Change clicked.");
            int divisionId = Convert.ToInt32(((ComboBoxItem)DivisionBox.SelectedItem).Uid);
            foreach (Participant part in toChange)
            {
                if (SwitchDivision.IsChecked == true)
                {
                    part.EventSpecific.DivisionIdentifier = divisionId;
                }
                if (EarlyStart.IsChecked == true)
                {
                    part.EventSpecific.EarlyStart = EarlyStartTrue.IsChecked == true ? 1 : 0;
                }
                if (CheckIn.IsChecked == true)
                {
                    part.EventSpecific.CheckedIn = CheckedInTrue.IsChecked == true ? 1 : 0;
                }
            }
            database.UpdateParticipants(toChange);
            database.ResetTimingResultsEvent(theEvent.Identifier);
            window.DatasetChanged();
            window.NotifyTimingWorker();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Cancel clicked.");
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
        }
    }
}
