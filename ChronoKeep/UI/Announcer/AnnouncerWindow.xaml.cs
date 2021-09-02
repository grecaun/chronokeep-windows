using ChronoKeep.Interfaces;
using ChronoKeep.Objects;
using ChronoKeep.Timing.Announcer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChronoKeep.UI.Announcer
{
    /// <summary>
    /// Interaction logic for AnnouncerWindow.xaml
    /// </summary>
    public partial class AnnouncerWindow : Window
    {
        private readonly int SecondGap = -45;

        IMainWindow window = null;
        AnnouncerWorker announcerWorker = null;
        Thread announcerThread = null;

        Event theEvent = null;

        public AnnouncerWindow(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            theEvent = database.GetCurrentEvent();
            announcerWorker = AnnouncerWorker.NewAnnouncer(window, database);
            announcerThread = new Thread(new ThreadStart(announcerWorker.Run));
            announcerThread.Start();
            UpdateView();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (announcerWorker != null) AnnouncerWorker.Shutdown();
            if (window != null) window.AnnouncerClosing();
        }

        public void UpdateView()
        {
            // Check if we've got an announcer reader connected.
            if (!window.AnnouncerConnected())
            {
                NoAnnouncer.Visibility = Visibility.Visible;
                AnnouncerBox.Visibility = Visibility.Hidden;
            }
            else
            {
                NoAnnouncer.Visibility = Visibility.Hidden;
                AnnouncerBox.Visibility = Visibility.Visible;
            }
            // Get our list of people to display. Remove anything older than 45 seconds.
            List<AnnouncerParticipant> participants = AnnouncerWorker.GetList();
            DateTime now = DateTime.Now.AddSeconds(SecondGap);
            AnnouncerBox.Items.Clear();
            foreach (AnnouncerParticipant part in participants)
            {
                // If 0 then they're equal, if greater then 0 then now came before the when.
                // Only display participants that have shown up within the SecondGap window.
                if (DateTime.Compare(now, part.When) >= 0)
                {
                    // Display in order they came in in the last 45 seconds.
                    AnnouncerBox.Items.Add(new AnAnnouncerItem(part, theEvent));
                    // Display the last person in first.
                    //AnnouncerBox.Items.Insert(0, new AnAnnouncerItem(part, theEvent));
                }
            }
        }

        private class AnAnnouncerItem : ListBoxItem {
            public AnAnnouncerItem(AnnouncerParticipant part, Event theEvent)
            {
                StackPanel mainPanel = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(10, 10, 10, 10)
                };
                // Time - Distance - Name - City - Age - Gender
                StackPanel namePanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                // When the read occurred
                namePanel.Children.Add(new Label()
                {
                    Content = part.When.ToString("HH:mm:ss"),
                    FontSize = 14,
                    Width = 50,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Distance
                namePanel.Children.Add(new Label()
                {
                    Content = part.Person.Distance,
                    FontSize = 16,
                    Width = 50,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Name
                namePanel.Children.Add(new Label()
                {
                    Content = string.Format("{0} {1}", part.Person.FirstName, part.Person.LastName),
                    FontSize = 18,
                    Width = 150,
                    Margin = new Thickness(0,0,5,0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // City if set
                if (part.Person.City.Length > 0)
                {
                    namePanel.Children.Add(new Label()
                    {
                        Content = part.Person.City,
                        FontSize = 18,
                        Width = 150,
                        Margin = new Thickness(0, 0, 5, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }
                // Age && Gender
                namePanel.Children.Add(new Label()
                {
                    Content = string.Format("{0} {1}", part.Person.Age(theEvent.Date), part.Person.Gender),
                    FontSize = 18,
                    Width = 50,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                mainPanel.Children.Add(namePanel);
                // Comments
                if (part.Person.Comments.Length > 0)
                {
                    mainPanel.Children.Add(new Label()
                    {
                        Content = part.Person.Comments,
                        FontSize = 18,
                        Width = 50,
                        Margin = new Thickness(5, 0, 5, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }
                this.Content = mainPanel;
            }
        }
    }
}
