using EventDirector.Interfaces;
using EventDirector.UI.EventWindows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    /// Interaction logic for NextYearSetup.xaml
    /// </summary>
    public partial class NextYearSetup : Window
    {
        MainWindow mainWindow;
        IWindowCallback callback = null;
        IDBInterface database;
        Event oldEvent = null, newEvent;

        public NextYearSetup(MainWindow mainWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.database = database;
            Log.D("Showing first page.");
            NYFrame.Content = new NextYearSetupPage0(this);
        }

        public NextYearSetup(IWindowCallback nextYearCallBack, IDBInterface database, Event oldEvent)
        {
            InitializeComponent();
            this.mainWindow = null;
            this.callback = nextYearCallBack;
            this.database = database;
            Log.D("Showing first page.");
            NYFrame.Content = new NextYearSetupPage0(this);
            this.oldEvent = oldEvent;
        }

        public static NextYearSetup NewWindow(IWindowCallback nextYearCallBack, IDBInterface database, Event oldEvent)
        {
            if (StaticEvent.changeMainEventWindow != null || StaticEvent.nextYearWindow != null)
            {
                return null;
            }
            NextYearSetup output = new NextYearSetup(nextYearCallBack, database, oldEvent);
            StaticEvent.nextYearWindow = output;
            return output;
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
            if (mainWindow != null) mainWindow.NextYearSetupFinalize(oldEvent.Identifier);
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (callback != null) callback.WindowFinalize(this);
            if (mainWindow != null) mainWindow.WindowClosed(this);
            StaticEvent.nextYearWindow = null;
        }
    }
}
