using Chronokeep.Interfaces;
using System.Collections.Generic;
using System.Windows;
using Wpf.Ui.Controls;
using static Chronokeep.Helpers.Globals;

namespace Chronokeep.UI.Timing.Notifications
{
    /// <summary>
    /// Interaction logic for ReaderNotificationWindow.xaml
    /// </summary>
    public partial class ReaderNotificationWindow : FluentWindow
    {
        private IWindowCallback window;

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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.Notifications.ReaderNotificationWindow", "Done button clicked.");
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            window.WindowFinalize(this);
        }
    }
}
