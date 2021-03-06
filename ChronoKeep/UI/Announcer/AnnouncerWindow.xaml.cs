using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.Timing.Announcer;
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

namespace Chronokeep.UI.Announcer
{
    /// <summary>
    /// Interaction logic for AnnouncerWindow.xaml
    /// </summary>
    public partial class AnnouncerWindow : Window
    {
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
            Log.D("UI.Announcer.AnnouncerWindow", "Announcer window is closing!");
            if (announcerWorker != null)
            {
                AnnouncerWorker.Shutdown();
            }
            if (window != null)
            {
                window.AnnouncerClosing();
            }
        }

        public void UpdateView()
        {
            // Check if we've got an announcer reader connected.
            if (!window.AnnouncerConnected())
            {
                NoAnnouncer.Height = new GridLength(100);
            }
            else
            {
                NoAnnouncer.Height = new GridLength(0);
            }
            // Get our list of people to display. Remove anything older than 45 seconds.
            List<AnnouncerParticipant> participants = AnnouncerWorker.GetList();
            participants.Sort((x1, x2) => x1.CompareTo(x2));
            DateTime cutoff = DateTime.Now.AddSeconds(Constants.Timing.ANNOUNCER_DISPLAY_WINDOW);
            AnnouncerBox.Items.Clear();
            AnnouncerBox.Items.Add(new AHeaderItem());
            foreach (AnnouncerParticipant part in participants)
            {
                // If 0 then they're equal, if greater then 0 then now came before the when.
                // Only display participants that have shown up within the SecondGap window.
                if (DateTime.Compare(cutoff, part.When) <= 0)
                {
                    // Display in order they came in in the last 45 seconds.
                    AnnouncerBox.Items.Add(new AnAnnouncerItem(part, theEvent));
                    // Display the last person in first.
                    //AnnouncerBox.Items.Insert(1, new AnAnnouncerItem(part, theEvent));
                }
            }
        }

        private class AnAnnouncerItem : ListBoxItem
        {
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
                    FontSize = 16,
                    Width = 80,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Distance
                namePanel.Children.Add(new Label()
                {
                    Content = part.Person.Distance,
                    FontSize = 16,
                    Width = 150,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Bib
                namePanel.Children.Add(new Label()
                {
                    Content = part.Person.Bib,
                    FontSize = 20,
                    Width = 80,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Name
                namePanel.Children.Add(new Label()
                {
                    Content = string.Format("{0} {1}", part.Person.FirstName, part.Person.LastName),
                    FontSize = 20,
                    MinWidth = 250,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                namePanel.Children.Add(new Label()
                {
                    Content = string.Format("{0}, {1}", part.Person.City, part.Person.State),
                    FontSize = 20,
                    MinWidth = 250,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Age && Gender
                namePanel.Children.Add(new Label()
                {
                    Content = string.Format("{0} {1}", part.Person.Age(theEvent.Date), part.Person.Gender),
                    FontSize = 20,
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
                        FontSize = 20,
                        Margin = new Thickness(5, 10, 5, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }
                this.Content = mainPanel;
            }
        }
        private class AHeaderItem : ListBoxItem
        {
            public AHeaderItem()
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
                    Content = "When",
                    FontSize = 16,
                    Width = 80,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Distance
                namePanel.Children.Add(new Label()
                {
                    Content = "Distance",
                    FontSize = 16,
                    Width = 150,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Bib
                namePanel.Children.Add(new Label()
                {
                    Content = "Bib",
                    FontSize = 20,
                    Width = 80,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Name
                namePanel.Children.Add(new Label()
                {
                    Content = "Name",
                    FontSize = 20,
                    Width = 250,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                namePanel.Children.Add(new Label()
                {
                    Content = "City, State",
                    FontSize = 20,
                    Width = 250,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Age && Gender
                namePanel.Children.Add(new Label()
                {
                    Content = "Age G",
                    FontSize = 20,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                mainPanel.Children.Add(namePanel);
                this.Content = mainPanel;
            }
        }
    }
}
