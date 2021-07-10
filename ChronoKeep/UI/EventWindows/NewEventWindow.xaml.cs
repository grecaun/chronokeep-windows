using ChronoKeep.Interfaces;
using ChronoKeep.Objects;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChronoKeep
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class NewEventWindow : Window
    {
        IDBInterface database = null;
        IWindowCallback window = null;

        public NewEventWindow(IMainWindow mainWindow)
        {
            InitializeComponent();
            datePicker.SelectedDate = DateTime.Today;
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
            return new NewEventWindow(window, database);
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
            long dateVal = DateTime.Now.Date.Ticks;
            if (datePicker.SelectedDate != null)
            {
                dateVal = datePicker.SelectedDate.Value.Date.Ticks;
            }
            Log.D("Name given for event: '" + nameString + "' Date Given: " + dateVal + " Date Value: " + dateVal);
            if (nameString == "")
            {
                MessageBox.Show("Please input a value in the name box.");
                return;
            }
            else
            {
                int oldEventId = Convert.ToInt32(((ComboBoxItem)oldEvent.SelectedItem).Uid);
                Event newEvent = new Event(nameString, dateVal, shirtOptionalVal, shirtPrice, yearString);
                database.AddEvent(newEvent);
                newEvent.Identifier = database.GetEventID(newEvent);
                // Copy all values from old event.
                if (oldEventId > 0)
                {
                    // Copy old event values.
                    Event oldEvent = database.GetEvent(oldEventId);
                    newEvent.EventType = oldEvent.EventType;
                    newEvent.StartWindow = oldEvent.StartWindow;
                    newEvent.FinishIgnoreWithin = oldEvent.FinishIgnoreWithin;
                    newEvent.FinishMaxOccurrences = oldEvent.FinishMaxOccurrences;
                    newEvent.CommonAgeGroups = oldEvent.CommonAgeGroups;
                    newEvent.CommonStartFinish = oldEvent.CommonStartFinish;
                    newEvent.DivisionSpecificSegments = oldEvent.DivisionSpecificSegments;
                    newEvent.AllowEarlyStart = oldEvent.AllowEarlyStart;
                    newEvent.RankByGun = oldEvent.RankByGun;
                    // Update database with current values.
                    database.UpdateEvent(newEvent);
                    // Get divisions from old event
                    List<Distance> divisions = database.GetDistances(oldEventId);
                    List<Distance> newDivs = new List<Distance>();
                    // DivDict translates a division name into the old division identifier.
                    Dictionary<string, int> DivDict = new Dictionary<string, int>();
                    // DivTranslationDict holds a new division id and translates it from the old division with the same name.
                    Dictionary<int, int> DivTranslationDict = new Dictionary<int, int>();
                    foreach (Distance d in divisions)
                    {
                        DivDict[d.Name] = d.Identifier;
                        d.Identifier = Constants.Timing.DIVISION_DUMMYIDENTIFIER;
                        d.EventIdentifier = newEvent.Identifier;
                        newDivs.Add(d);
                    }
                    // Update database with new divisions.
                    database.AddDistances(newDivs);
                    // Retrieve newly added divisions.
                    newDivs = database.GetDistances(newEvent.Identifier);
                    foreach (Distance newD in newDivs)
                    {
                        // Set up a translation dictionary.
                        DivTranslationDict[DivDict[newD.Name]] = newD.Identifier;
                    }
                    // Get locations from old event.
                    List<TimingLocation> locations = database.GetTimingLocations(oldEventId);
                    List<TimingLocation> newLocations = new List<TimingLocation>();
                    foreach (TimingLocation loc in locations)
                    {
                        loc.EventIdentifier = newEvent.Identifier;
                        newLocations.Add(loc);
                    }
                    // Update database with new locations
                    database.AddTimingLocations(newLocations);
                    // Get old segments from the database.
                    List<Segment> segments = database.GetSegments(oldEventId);
                    List<Segment> newSegments = new List<Segment>();
                    foreach (Segment s in segments)
                    {
                        s.EventId = newEvent.Identifier;
                        if (newEvent.DivisionSpecificSegments && DivTranslationDict.ContainsKey(s.DivisionId))
                        {
                            s.DivisionId = DivTranslationDict[s.DivisionId];
                        }
                        newSegments.Add(s);
                    }
                    // Update database with new segments.
                    database.AddSegments(newSegments);
                    // Get age groups from database.
                    List<AgeGroup> ageGroups = database.GetAgeGroups(oldEventId);
                    List<AgeGroup> newAgeGroups = new List<AgeGroup>();
                    foreach (AgeGroup ag in ageGroups)
                    {
                        ag.EventId = newEvent.Identifier;
                        if (!newEvent.CommonAgeGroups && DivTranslationDict.ContainsKey(ag.DivisionId))
                        {
                            ag.DivisionId = DivTranslationDict[ag.DivisionId];
                        }
                        newAgeGroups.Add(ag);
                    }
                    // Update database with new age groups.
                    database.AddAgeGroups(newAgeGroups);
                }
                else
                {
                    database.AddDistance(new Distance("Default Division", newEvent.Identifier, 0));
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
            if (window != null) window.WindowFinalize(this);
        }
    }
}
