using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using static Chronokeep.Helpers.Globals;

namespace Chronokeep.UI.Timing.Notifications;

public partial class ReaderNotificationWindow : Window
{
    private readonly IWindowCallback window;

    private ReaderNotificationWindow(IWindowCallback window)
    {
        InitializeComponent();
        this.window = window;
        UpdateNotificatonsBox();
    }

    public static ReaderNotificationWindow NewWindow(IWindowCallback window)
    {
        return new ReaderNotificationWindow(window);
    }

    internal void UpdateNotificatonsBox()
    {
        List<ReaderMessage> messages = GetReaderMessages();
        messages.Sort();
        notificationsList.ItemsSource = messages;
        foreach (ReaderMessage msg in messages)
        {
            msg.Notified = true;
        }
        UpdateReaderMessages(messages);
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        window.WindowFinalize(this);
    }

    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.Notifications.ReaderNotificationWindow", "Done button clicked.");
        this.Close();
    }
}