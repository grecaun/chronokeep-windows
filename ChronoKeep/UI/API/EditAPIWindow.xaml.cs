using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for EditAPIWindow.xaml
    /// </summary>
    public partial class EditAPIWindow : FluentWindow
    {
        private readonly IMainWindow window = null;
        private readonly Event theEvent;

        // Variables relating to information we're collecting.
        private readonly APIObject api;
        private readonly string slug, year;

        public EditAPIWindow(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.MinHeight = 100;
            this.MinWidth = 300;
            this.Width = 330;
            theEvent = database.GetCurrentEvent();
            // Get API to upload.
            if (theEvent == null || theEvent.Identifier < 1 || theEvent.API_ID < 0 || theEvent.API_Event_ID.Length < 1)
            {
                Log.E("UI.API.APIWindow", "event not found or no apis set up");
                EditAPIFrame.Content = new EditAPIErrorPage(this, true);
                return;
            }
            api = database.GetAPI(theEvent.API_ID);
            string[] event_ids = theEvent.API_Event_ID.Split(',');
            if (event_ids.Length != 2)
            {
                return;
            }
            slug = event_ids[0];
            year = event_ids[1];
            EditAPIFrame.Content = new EditAPIPage1(this, database);
        }

        public void NetworkUpdateResults()
        {
            window.NetworkUpdateResults();
        }

        public static EditAPIWindow NewWindow(IMainWindow window, IDBInterface database)
        {
            return new EditAPIWindow(window, database);
        }

        public void GotoEditEvent()
        {
            EditAPIFrame.Content = new EditEventPage(this, api, slug);
        }

        public void GotoEditYear()
        {
            EditAPIFrame.Content = new EditYearPage(this, api, slug, year);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
        }
    }
}
