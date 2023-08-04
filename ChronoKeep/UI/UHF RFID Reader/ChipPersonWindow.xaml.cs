using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace Chronokeep
{
    /// <summary>
    /// Interaction logic for ChipPersonWindow.xaml
    /// </summary>
    public partial class ChipPersonWindow : UiWindow
    {
        private ChipReaderWindow readerWindow;
        private string eventDate;
        readonly object _locker = new object();

        public ChipPersonWindow(ChipReaderWindow reader, string eventDate)
        {
            this.readerWindow = reader;
            this.eventDate = eventDate;
            InitializeComponent();
        }

        public async void UpdateInfo(Participant person)
        {
            await Task.Run(() =>
            {
                lock (_locker)
                {
                    Monitor.Pulse(_locker);
                }
                Thread.Sleep(100);
            });
            if (person != null)
            {
                Bib.Text = "Bib: " + person.EventSpecific.Bib;
                Chip.Text = "Chip: " + person.EventSpecific.Chip;
                First.Text = "First: " + person.FirstName;
                Last.Text = "Last: " + person.LastName;
                Age.Text = "Age: " + person.Age(eventDate);
                Gender.Text = "Gender: " + person.Gender;
                Distance.Text = "Distance: " + person.EventSpecific.DistanceName;
                Unknown.Text = "";
            }
            else
            {
                Bib.Text = "";
                Chip.Text = "";
                First.Text = "";
                Last.Text = "";
                Age.Text = "";
                Gender.Text = "";
                Distance.Text = "";
                Unknown.Text = "Information not found.";
            }
            await Task.Run(() =>
            {
                lock (_locker)
                {
                    Monitor.Wait(_locker, 5000);
                }
            });
            Bib.Text = "";
            Chip.Text = "";
            First.Text = "";
            Last.Text = "";
            Age.Text = "";
            Gender.Text = "";
            Distance.Text = "";
            Unknown.Text = "";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            readerWindow.PersonWindowClosing();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
