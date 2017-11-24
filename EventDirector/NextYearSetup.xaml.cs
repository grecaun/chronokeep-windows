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
        Event oldEvent, newEvent;

        public NextYearSetup(MainWindow mainWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.database = database;
            Log.D("Showing first page.");
            NYFrame.Content = new NextYearSetupPage0(this);
        }

        public void GotoPage1()
        {
            NYFrame.Content = new NextYearSetupPage1(this, database);
        }

        public void GotoPage2(int eventId)
        {
            // Get old event.
            oldEvent = database.GetEvent(eventId);
            NYFrame.Content = new NextYearSetupPage2(this, oldEvent);
        }

        public void GoToPage3(string newEventName, long date, int shirtOptional, int shirtPrice)
        {
            // Add new event for next year
            newEvent = new Event(newEventName, date, shirtOptional, shirtPrice);
            // Add divisions to new event. Same as last year. User can edit these later.
            List<Division> divs = database.GetDivisions(oldEvent.Identifier);
            NYFrame.Content = new NextYearSetupPage3(divs, this);
        }

        public void GoToPage4(List<Division> divs)
        {
            database.AddEvent(newEvent);
            newEvent = database.GetEvent(database.GetEventID(newEvent));
            foreach (Division d in divs)
            {
                database.AddDivision(new Division(d.Name, newEvent.Identifier, d.Cost));
            }
            // Update old event with new information.
            oldEvent.NextYear = newEvent.Identifier;
            database.UpdateEvent(oldEvent);
            mainWindow.NextYearSetupFinalize(oldEvent.Identifier);
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWindow.WindowClosed(this);
        }
    }
}
