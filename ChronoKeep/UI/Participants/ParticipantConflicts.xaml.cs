using Chronokeep.Interfaces;
using Chronokeep.Objects;
using System.Collections.Generic;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.Participants
{
    /// <summary>
    /// Interaction logic for ParticipantConflicts.xaml
    /// </summary>
    public partial class ParticipantConflicts : FluentWindow
    {
        IMainWindow window;

        public ParticipantConflicts(IMainWindow window, List<Participant> participants)
        {
            InitializeComponent();
            this.window = window;

            ParticipantsList.ItemsSource = participants;
            ParticipantsList.Items.Refresh();
        }
        public static ParticipantConflicts NewWindow(IMainWindow window, List<Participant> participants)
        {
            return new ParticipantConflicts(window, participants);
        }

        private void ParticipantsList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            labelsViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
        }
    }
}
