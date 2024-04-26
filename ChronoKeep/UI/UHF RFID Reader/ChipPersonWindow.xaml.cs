using Chronokeep.Objects;
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
    public partial class ChipPersonWindow : FluentWindow
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
                Chip.Text = "Chip: " + person.Chip;
                PersonName.Text = string.Format("{0} {1}", person.FirstName, person.LastName);
                AgeGender.Text = string.Format("{0} {1}", person.Age(eventDate), person.Gender);
                Distance.Text = "" + person.EventSpecific.DistanceName;
                Unknown.Text = "";
                Unknown.Visibility = Visibility.Collapsed;
                InfoHolder.Visibility = Visibility.Visible;
            }
            else
            {
                Bib.Text = "";
                Chip.Text = "";
                PersonName.Text = "";
                AgeGender.Text = "";
                Distance.Text = "";
                Unknown.Text = "Information not found.";
                Unknown.Visibility = Visibility.Visible;
                InfoHolder.Visibility = Visibility.Collapsed;
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
            PersonName.Text = "";
            AgeGender.Text = "";
            Distance.Text = "";
            Unknown.Text = "";
            Unknown.Visibility = Visibility.Collapsed;
            InfoHolder.Visibility = Visibility.Collapsed;
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
