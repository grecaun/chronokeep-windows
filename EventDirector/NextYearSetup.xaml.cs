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
        IDBInterface database;
        int oldEventId;
        int shirtOptional = 1;

        public NextYearSetup(MainWindow mainWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.database = database;
            Log.D("Showing first page.");
            NYFrame.Content = new NextYearSetupPage1(this, database);
        }

        public void GotoPage2(int eventId, string eventName, int shirtOptional)
        {
            this.oldEventId = eventId;
            this.shirtOptional = shirtOptional;
            NYFrame.Content = new NextYearSetupPage2(this, eventName);
        }

        public void Finish(string newEventName, long date)
        {
            // Add new event for next year
            Event newEvent = new Event(newEventName, date);
            database.AddEvent(newEvent);
            newEvent = database.GetEvent(database.GetEventID(newEvent));
            // Get old event.
            Event oldEvent = database.GetEvent(oldEventId);
            // Add divisions to new event. Same as last year. User can edit these later.
            List<Division> divs = database.GetDivisions(oldEvent.Identifier);
            foreach (Division d in divs)
            {
                database.AddDivision(new Division(d.Name, newEvent.Identifier, d.Cost));
            }
            // Update old event with new information.
            oldEvent.NextYear = newEvent.Identifier;
            oldEvent.ShirtOptional = shirtOptional;
            database.UpdateEvent(oldEvent);
            mainWindow.UpdateEventBox();
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWindow.WindowClosed(this);
        }
    }
}
