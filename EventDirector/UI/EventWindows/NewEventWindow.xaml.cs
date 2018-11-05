using EventDirector.Interfaces;
using EventDirector.UI.EventWindows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class NewEventWindow : Window
    {
        IMainWindow mainWindow = null;
        Event theEvent = null;
        IDBInterface database = null;
        IWindowCallback window = null;

        public NewEventWindow(IMainWindow mainWindow)
        {
            InitializeComponent();
            datePicker.SelectedDate = DateTime.Today;
            this.mainWindow = mainWindow;
            CopyLabel.Visibility = Visibility.Collapsed;
            oldEvent.Visibility = Visibility.Collapsed;
            this.Height = 310;
        }

        public NewEventWindow(MainWindow mW, Event e)
        {
            InitializeComponent();
            nameBox.Text = e.Name;
            yearCodeBox.Text = e.YearCode;
            datePicker.SelectedDate = DateTime.Parse(e.Date);
            this.mainWindow = mW;
            this.theEvent = e;
            shirtOptional.IsChecked = e.ShirtOptional == 1 ? true : false;
            shirtPriceBox.Text = String.Format("{0}.{1:D2}", e.ShirtPrice / 100, e.ShirtPrice % 100);
            CopyLabel.Visibility = Visibility.Collapsed;
            oldEvent.Visibility = Visibility.Collapsed;
            this.Height = 310;
        }

        public NewEventWindow(IWindowCallback window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            oldEvent.Items.Clear();
            oldEvent.Items.Add(new ComboBoxItem
            {
                Content = "None",
                Uid = "-1"
            });
            List<Event> events = database.GetEvents();
            events.Sort();
            foreach (Event e in events)
            {
                oldEvent.Items.Add(new ComboBoxItem
                {
                    Content = (e.YearCode + " " + e.Name).Trim(),
                    Uid = e.Identifier.ToString()
                });
            }
            oldEvent.SelectedIndex = 0;
        }

        public static NewEventWindow NewWindow(IWindowCallback window, IDBInterface database)
        {
            if (StaticEvent.changeMainEventWindow != null)
            {
                return null;
            }
            if (StaticEvent.AreToolWindowsOpen())
            {
                return null;
            }
            NewEventWindow output = new NewEventWindow(window, database);
            StaticEvent.changeMainEventWindow = output;
            return output;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            Submit();
        }

        private void Submit()
        {
            string nameString = nameBox.Text.Trim();
            string yearString = yearCodeBox.Text.Trim();
            int shirtPrice = -1, shirtOptionalVal = shirtOptional.IsChecked == true ? 1 : 0;
            string[] parts = shirtPriceBox.Text.Split('.');
            shirtPrice = 20;
            if (parts.Length > 0)
            {
                int.TryParse(parts[0].Trim(), out shirtPrice);
            }
            shirtPrice = shirtPrice * 100;
            int cents = 0;
            if (parts.Length > 1)
            {
                int.TryParse(parts[1].Trim(), out cents);
            }
            while (cents > 100)
            {
                cents = cents / 100;
            }
            shirtPrice += cents;
            long dateVal = datePicker.SelectedDate.Value.Date.Ticks;
            Log.D("Name given for event: '" + nameString + "' Date Given: " + datePicker.SelectedDate.Value.Date.ToShortDateString() + " Date Value: " + dateVal);
            if (nameString == "")
            {
                MessageBox.Show("Please input a value in the name box.");
                return;
            }
            if (mainWindow != null)
            {
                if (theEvent == null)
                {
                    mainWindow.AddEvent(nameString, dateVal, shirtOptionalVal, shirtPrice);
                }
                else
                {
                    mainWindow.UpdateEvent(theEvent.Identifier, nameString, dateVal, theEvent.NextYear, shirtOptionalVal, shirtPrice);
                }
            }
            else
            {
                int oldEventId = Convert.ToInt32(((ComboBoxItem)oldEvent.SelectedItem).Uid);
                Event newEvent = new Event(nameString, dateVal, shirtOptionalVal, shirtPrice, yearString);
                database.AddEvent(newEvent);
                newEvent.Identifier = database.GetEventID(newEvent);
                if (oldEventId > 0)
                {
                    List<Division> divisions = database.GetDivisions(oldEventId);
                    foreach (Division d in divisions)
                    {
                        d.EventIdentifier = newEvent.Identifier;
                        database.AddDivision(d);
                    }
                    List<TimingLocation> locations = database.GetTimingLocations(oldEventId);
                    foreach (TimingLocation loc in locations)
                    {
                        loc.EventIdentifier = newEvent.Identifier;
                        database.AddTimingLocation(loc);
                    }
                    List<Segment> segments = database.GetSegments(oldEventId);
                    foreach (Segment s in segments)
                    {
                        s.EventId = newEvent.Identifier;
                        database.AddSegment(s);
                    }
                }
                else
                {
                    database.AddDivision(new Division("Default Division", newEvent.Identifier, 7000));
                }
                database.SetAppSetting(Constants.Settings.CURRENT_EVENT, newEvent.Identifier.ToString());
                window.WindowFinalize(this);
            }
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Keyboard_Up(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Submit();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mainWindow != null) mainWindow.WindowClosed(this);
            if (window != null) window.WindowFinalize(this);
            StaticEvent.changeMainEventWindow = null;
        }
    }
}
