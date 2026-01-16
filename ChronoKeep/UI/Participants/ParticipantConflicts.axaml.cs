using Chronokeep.Interfaces.UI;

namespace Chronokeep.UI.Participants;

public partial class ParticipantConflicts : Window
{
    private readonly IMainWindow window;

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

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        window?.WindowFinalize(this);
    }

    private void ParticipantsList_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        labelsViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
    }
}