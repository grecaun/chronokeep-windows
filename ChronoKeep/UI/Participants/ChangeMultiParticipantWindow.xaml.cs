using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.Timing.API;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.Participants
{
    /// <summary>
    /// Interaction logic for ChangeMultiParticipantWindow.xaml
    /// </summary>
    public partial class ChangeMultiParticipantWindow : FluentWindow
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
            foreach (Distance div in database.GetDistances(theEvent.Identifier))
            {
                DistanceBox.Items.Add(new ComboBoxItem()
                {
                    Content = div.Name,
                    Uid = div.Identifier.ToString()
                });
            }
            DistanceBox.SelectedIndex = 0;
            DistanceBox.Focus();
        }

        private void Change_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Participants.ChangeMultiParticipantWindow", "Change clicked.");
            int distanceId = Convert.ToInt32(((ComboBoxItem)DistanceBox.SelectedItem).Uid);
            foreach (Participant part in toChange)
            {
                part.EventSpecific.DistanceIdentifier = distanceId;
            }
            database.UpdateParticipants(toChange);
            database.ResetTimingResultsEvent(theEvent.Identifier);
            window.NotifyTimingWorker();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Participants.ChangeMultiParticipantWindow", "Cancel clicked.");
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
        }
    }
}
