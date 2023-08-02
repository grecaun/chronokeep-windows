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
using Wpf.Ui.Controls;

namespace Chronokeep.UI.Announcer
{
    /// <summary>
    /// Interaction logic for AnnouncerWindow.xaml
    /// </summary>
    public partial class AnnouncerWindow : UiWindow
    {
        IMainWindow window = null;
        AnnouncerWorker announcerWorker = null;
        Thread announcerThread = null;
        IDBInterface database = null;

        Event theEvent = null;

        public AnnouncerWindow(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            announcerWorker = AnnouncerWorker.NewAnnouncer(window, database);
            announcerThread = new Thread(new ThreadStart(announcerWorker.Run));
            announcerThread.Start();
            UpdateView();
            UpdateTiming();
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

        public void UpdateTiming()
        {
            if (!window.AnnouncerConnected())
            {
                // Get our list of results to display.
                List<TimeResult> results = database.GetTimingResults(theEvent.Identifier);
                // Ensure results are sorted.
                results.Sort(TimeResult.CompareBySystemTime);
                results.RemoveAll((x) => TimeResult.IsNotFinish(x) || x.IsDNF());
                results.Reverse();
                // Remove old entries.
                AnnouncerBox.Items.Clear();
                // Add header item.
                AnnouncerBox.Items.Add(new AResultHeaderItem());
                // Add all results to the display.
                int bg = 1;
                foreach (TimeResult res in results)
                {
                    AnnouncerBox.Items.Add(new AResultItem(res, theEvent, bg));
                    bg = Math.Abs(bg - 1);
                }
            }
        }

        public void UpdateView()
        {
            // Check if we've got an announcer reader connected.
            if (window.AnnouncerConnected())
            {
                // Get our list of people to display. Remove anything older than 45 seconds.
                List<AnnouncerParticipant> participants = AnnouncerWorker.GetList();
                participants.Sort((x1, x2) => x1.CompareTo(x2));
                DateTime cutoff = DateTime.Now.AddSeconds(Constants.Timing.ANNOUNCER_DISPLAY_WINDOW);
                AnnouncerBox.Items.Clear();
                AnnouncerBox.Items.Add(new AHeaderItem());
                int bg = 1;
                foreach (AnnouncerParticipant part in participants)
                {
                    // If 0 then they're equal, if greater then 0 then now came before the when.
                    // Only display participants that have shown up within the SecondGap window.
                    if (DateTime.Compare(cutoff, part.When) <= 0)
                    {
                        // Display in order they came in in the last 45 seconds.
                        AnnouncerBox.Items.Add(new AnAnnouncerItem(part, theEvent, bg));
                        // Display the last person in first.
                        //AnnouncerBox.Items.Insert(1, new AnAnnouncerItem(part, theEvent));
                        bg = Math.Abs(bg - 1);
                    }
                }
            }
        }

        private class AResultItem : ListBoxItem
        {
            public AResultItem(TimeResult res, Event theEvent, int bg)
            {
                // Time - Distance - Name - City - Age - Gender
                StackPanel mainPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10, 10, 10, 10)
                };
                // Distance
                mainPanel.Children.Add(new TextBlock()
                {
                    Text = res.DistanceName,
                    FontSize = 16,
                    Width = 150,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Place
                mainPanel.Children.Add(new TextBlock()
                {
                    Text = res.PlaceStr,
                    FontSize = 16,
                    Width = 60,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Chip Time
                mainPanel.Children.Add(new TextBlock()
                {
                    Text = res.ChipTime,
                    FontSize = 20,
                    Width = 150,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Name
                mainPanel.Children.Add(new TextBlock()
                {
                    Text = res.ParticipantName,
                    FontSize = 20,
                    MinWidth = 250,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Age && Gender
                mainPanel.Children.Add(new TextBlock()
                {
                    Text = string.Format("{0} {1}", res.Age(theEvent.Date), res.PrettyGender),
                    FontSize = 20,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                this.Content = mainPanel;
                byte alpha = (byte)(120 * bg);
                this.Background = new SolidColorBrush(Color.FromArgb(alpha, 230, 230, 230));
            }
        }
        private class AResultHeaderItem : ListBoxItem
        {
            public AResultHeaderItem()
            {
                // Distance - Place - Chip Time - Name - City - Age - Gender
                StackPanel mainPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10, 10, 10, 10)
                };
                // Distance
                mainPanel.Children.Add(new TextBlock()
                {
                    Text = "Distance",
                    FontSize = 16,
                    Width = 150,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Place
                mainPanel.Children.Add(new TextBlock()
                {
                    Text = "Pl",
                    FontSize = 16,
                    Width = 60,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Chip Time
                mainPanel.Children.Add(new TextBlock()
                {
                    Text = "Time",
                    FontSize = 20,
                    Width = 150,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Name
                mainPanel.Children.Add(new TextBlock()
                {
                    Text = "Name",
                    FontSize = 20,
                    Width = 250,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Age && Gender
                mainPanel.Children.Add(new TextBlock()
                {
                    Text = "Age G",
                    FontSize = 20,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                this.Content = mainPanel;
            }
        }

        private class AnAnnouncerItem : ListBoxItem
        {
            public AnAnnouncerItem(AnnouncerParticipant part, Event theEvent, int bg)
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
                namePanel.Children.Add(new TextBlock()
                {
                    Text = part.When.ToString("HH:mm:ss"),
                    FontSize = 16,
                    Width = 80,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Distance
                namePanel.Children.Add(new TextBlock()
                {
                    Text = part.Person.Distance,
                    FontSize = 16,
                    Width = 150,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Bib
                namePanel.Children.Add(new TextBlock()
                {
                    Text = part.Person.Bib.ToString(),
                    FontSize = 20,
                    Width = 80,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Name
                namePanel.Children.Add(new TextBlock()
                {
                    Text = string.Format("{0} {1}", part.Person.FirstName, part.Person.LastName),
                    FontSize = 20,
                    MinWidth = 250,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                namePanel.Children.Add(new TextBlock()
                {
                    Text = string.Format("{0}, {1}", part.Person.City, part.Person.State),
                    FontSize = 20,
                    MinWidth = 250,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Age && Gender
                namePanel.Children.Add(new TextBlock()
                {
                    Text = string.Format("{0} {1}", part.Person.Age(theEvent.Date), part.Person.Gender),
                    FontSize = 20,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                mainPanel.Children.Add(namePanel);
                // Comments
                if (part.Person.Comments.Length > 0)
                {
                    mainPanel.Children.Add(new TextBlock()
                    {
                        Text = part.Person.Comments,
                        FontSize = 20,
                        Margin = new Thickness(5, 10, 5, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }
                this.Content = mainPanel;
                byte alpha = (byte)(120 * bg);
                this.Background = new SolidColorBrush(Color.FromArgb(alpha, 230, 230, 230));
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
                namePanel.Children.Add(new TextBlock()
                {
                    Text = "When",
                    FontSize = 16,
                    Width = 80,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Distance
                namePanel.Children.Add(new TextBlock()
                {
                    Text = "Distance",
                    FontSize = 16,
                    Width = 150,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Bib
                namePanel.Children.Add(new TextBlock()
                {
                    Text = "Bib",
                    FontSize = 20,
                    Width = 80,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Name
                namePanel.Children.Add(new TextBlock()
                {
                    Text = "Name",
                    FontSize = 20,
                    Width = 250,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                namePanel.Children.Add(new TextBlock()
                {
                    Text = "City, State",
                    FontSize = 20,
                    Width = 250,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                // Age && Gender
                namePanel.Children.Add(new TextBlock()
                {
                    Text = "Age G",
                    FontSize = 20,
                    Margin = new Thickness(0, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                mainPanel.Children.Add(namePanel);
                this.Content = mainPanel;
            }
        }

        private void AnnouncerBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = ScrollBox;
            if (e.Delta < 0)
            {
                if (scv.VerticalOffset - e.Delta <= scv.ExtentHeight - scv.ViewportHeight)
                {
                    scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
                }
                else
                {
                    scv.ScrollToBottom();
                }
            }
            else
            {
                if (scv.VerticalOffset - e.Delta > 0)
                {
                    scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
                }
                else
                {
                    scv.ScrollToTop();
                }
            }
        }
    }
}
