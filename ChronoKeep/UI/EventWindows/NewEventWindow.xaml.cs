using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace Chronokeep
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class NewEventWindow : FluentWindow
    {
        IDBInterface database = null;
        IWindowCallback window = null;

        public NewEventWindow(IMainWindow mainWindow)
        {
            InitializeComponent();
            datePicker.SelectedDate = DateTime.Today;
            CopyLabel.Visibility = Visibility.Collapsed;
            oldEvent.Visibility = Visibility.Collapsed;
            this.MinWidth = 350;
            this.MinHeight = 350;
            this.Width = 350;
            this.Height = 310;
        }

        public NewEventWindow(IWindowCallback window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            this.MinWidth = 350;
            this.MinHeight = 350;
            this.Width = 350;
            this.Height = 310;
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
            long dateVal = DateTime.Now.Date.Ticks;
            if (datePicker.SelectedDate != null)
            {
                dateVal = datePicker.SelectedDate.Value.Date.Ticks;
            }
            Log.D("NewEventWindow", "Name given for event: '" + nameString + "' Date Given: " + dateVal + " Date Value: " + dateVal);
            if (nameString == "")
            {
                DialogBox.Show("Please input a value in the name box.");
                return;
            }
            else
            {
                int oldEventId = Convert.ToInt32(((ComboBoxItem)oldEvent.SelectedItem).Uid);
                Event newEvent = new Event(nameString, dateVal, yearString);
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
                    newEvent.DistanceSpecificSegments = oldEvent.DistanceSpecificSegments;
                    newEvent.RankByGun = oldEvent.RankByGun;
                    // Update database with current values.
                    database.UpdateEvent(newEvent);
                    // Get distances from old event
                    List<Distance> distances = database.GetDistances(oldEventId);
                    List<Distance> newDistances = new List<Distance>();
                    // DistanceDict translates a distance name into the old distance identifier.
                    Dictionary<string, int> DistanceDict = new Dictionary<string, int>();
                    // DistanceTranslationDict holds a new distance id and translates it from the old distance with the same name.
                    Dictionary<int, int> DistanceTranslationDict = new Dictionary<int, int>();
                    foreach (Distance d in distances)
                    {
                        DistanceDict[d.Name] = d.Identifier;
                        d.Identifier = Constants.Timing.DISTANCE_DUMMYIDENTIFIER;
                        d.EventIdentifier = newEvent.Identifier;
                        newDistances.Add(d);
                    }
                    // Update database with new distances.
                    database.AddDistances(newDistances);
                    // Retrieve newly added distances.
                    newDistances = database.GetDistances(newEvent.Identifier);
                    foreach (Distance newD in newDistances)
                    {
                        // Set up a translation dictionary.
                        DistanceTranslationDict[DistanceDict[newD.Name]] = newD.Identifier;
                    }
                    // Translate linked distance id's.
                    foreach (Distance newD in newDistances)
                    {
                        if (Constants.Timing.DISTANCE_NO_LINKED_ID != newD.LinkedDistance)
                        {
                            if (DistanceTranslationDict.ContainsKey(newD.LinkedDistance))
                            {
                                newD.LinkedDistance = DistanceTranslationDict[newD.LinkedDistance];
                            }
                            else
                            {
                                newD.LinkedDistance = Constants.Timing.DISTANCE_NO_LINKED_ID;
                            }
                            database.UpdateDistance(newD);
                        }
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
                        if (newEvent.DistanceSpecificSegments && DistanceTranslationDict.ContainsKey(s.DistanceId))
                        {
                            s.DistanceId = DistanceTranslationDict[s.DistanceId];
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
                        if (!newEvent.CommonAgeGroups && DistanceTranslationDict.ContainsKey(ag.DistanceId))
                        {
                            ag.DistanceId = DistanceTranslationDict[ag.DistanceId];
                        }
                        newAgeGroups.Add(ag);
                    }
                    // Update database with new age groups.
                    database.AddAgeGroups(newAgeGroups);
                }
                else
                {
                    database.AddDistance(new Distance("Default Distance", newEvent.Identifier));
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
