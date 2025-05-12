using Chronokeep.Database.SQLite;
using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.UI.UIObjects;
using System.Collections.Generic;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for APIWindow.xaml
    /// </summary>
    public partial class EditAPIWindow : FluentWindow
    {
        IMainWindow window = null;
        IDBInterface database;
        Event theEvent;

        // Variables relating to information we're collecting.
        APIObject api;
        string slug, year;

        public EditAPIWindow(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
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
            EditAPIFrame.Content = new EditAPIPage1(this);
        }

        public static EditAPIWindow NewWindow(IMainWindow window, IDBInterface database)
        {
            return new EditAPIWindow(window, database);
        }

        public void GotoEditEvent()
        {
            EditAPIFrame.Content = new EditEventPage(this, database);
        }

        public void GotoEditYear()
        {
            EditAPIFrame.Content = new EditYearPage(this, database);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
        }
    }
}
