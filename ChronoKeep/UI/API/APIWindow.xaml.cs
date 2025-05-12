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
    public partial class APIWindow : FluentWindow
    {
        IMainWindow window = null;
        IDBInterface database;
        Event theEvent;

        // Variables relating to information we're collecting.
        APIObject api;
        string slug, year;


        public APIWindow(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            this.MinHeight = 100;
            this.MinWidth = 300;
            this.Width = 330;
            theEvent = database.GetCurrentEvent();
            List<APIObject> apis = database.GetAllAPI();
            apis.RemoveAll(x => !Constants.APIConstants.API_RESULTS[x.Type]);
            if (theEvent == null || theEvent.Identifier < 1 || apis.Count < 1)
            {
                Log.E("UI.API.APIWindow", "event not found or no apis set up");
                APIFrame.Content = new APIErrorPage(this, apis.Count < 1);
            } else
            {
                APIFrame.Content = new APIPage1(this, database);
            }
        }

        public static APIWindow NewWindow(IMainWindow window, IDBInterface database)
        {
            return new APIWindow(window, database);
        }

        public void GotoPage2(APIObject api)
        {
            this.api = api;
            database.SetAppSetting(Constants.Settings.LAST_USED_API_ID, api.Identifier.ToString());
            APIFrame.Content = new APIPage2(this, database, api, theEvent);
        }

        public void GotoPage3(string slug)
        {
            this.slug = slug;
            APIFrame.Content = new APIPage3(this, api, theEvent, slug);
        }

        public void Finish(string year)
        {
            this.year = year;
            if (api.Identifier > 0 && this.slug != "" && this.year != "")
            {
                theEvent.API_ID = api.Identifier;
                theEvent.API_Event_ID = this.slug + "," + this.year;
                database.UpdateEvent(theEvent);
                window.NetworkUpdateResults();
            }
            else
            {
                DialogBox.Show("One or more values retrieved is invalid.");
                return;
            }
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
        }
    }
}
