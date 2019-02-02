using EventDirector.Interfaces;
using System.Collections.Generic;
using System.Windows;

namespace EventDirector
{
    /// <summary>
    /// Interaction logic for NextYearSetup.xaml
    /// </summary>
    public partial class NextYearSetup : Window
    {
        IWindowCallback callback = null;
        IDBInterface database;
        Event oldEvent = null, newEvent;

        public NextYearSetup(IWindowCallback nextYearCallBack, IDBInterface database, Event oldEvent)
        {
            InitializeComponent();
            this.callback = nextYearCallBack;
            this.database = database;
            Log.D("Showing first page.");
            NYFrame.Content = new NextYearSetupPage0(this);
            this.oldEvent = oldEvent;
        }

        public static NextYearSetup NewWindow(IWindowCallback nextYearCallBack, IDBInterface database, Event oldEvent)
        {
            return new NextYearSetup(nextYearCallBack, database, oldEvent);
        }

        public void GotoPage1()
        {
            if (oldEvent == null)
            {
                NYFrame.Content = new NextYearSetupPage1(this, database);
            }
            else
            {
                NYFrame.Content = new NextYearSetupPage2(this, oldEvent);
            }
        }

        public void GotoPage2(int eventId)
        {
            // Get old event.
            oldEvent = database.GetEvent(eventId);
            NYFrame.Content = new NextYearSetupPage2(this, oldEvent);
        }

        public void GoToPage3(string newEventName, string yearCode, long date, int shirtOptional, int shirtPrice)
        {
            // Add new event for next year
            newEvent = new Event(newEventName, date, shirtOptional, shirtPrice, yearCode);
            // Add divisions to new event. Same as last year. User can edit these later.
            List<Division> divs = database.GetDivisions(oldEvent.Identifier);
            NYFrame.Content = new NextYearSetupPage3(divs, this);
        }

        public void GoToPage4(List<Division> divs)
        {
            try
            {
                database.AddEvent(newEvent);
            }
            catch
            {
                MessageBox.Show("Unable to create new event.");
                this.Close();
            }
            newEvent = database.GetEvent(database.GetEventID(newEvent));
            foreach (Division d in divs)
            {
                database.AddDivision(new Division(d.Name, newEvent.Identifier, d.Cost));
            }
            List<TimingLocation> locations = database.GetTimingLocations(oldEvent.Identifier);
            foreach (TimingLocation loc in locations)
            {
                loc.EventIdentifier = newEvent.Identifier;
                database.AddTimingLocation(loc);
            }
            List<Segment> segments = database.GetSegments(oldEvent.Identifier);
            foreach (Segment s in segments)
            {
                s.EventId = newEvent.Identifier;
                database.AddSegment(s);
            }
            // Update old event with new information.
            oldEvent.NextYear = newEvent.Identifier;
            database.UpdateEvent(oldEvent);
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (callback != null) callback.WindowFinalize(this);
        }
    }
}
