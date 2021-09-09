using ChronoKeep.Interfaces;
using ChronoKeep.Objects;
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

namespace ChronoKeep.UI.API
{
    /// <summary>
    /// Interaction logic for APIWindow.xaml
    /// </summary>
    public partial class APIWindow : Window
    {
        IWindowCallback window = null;
        IDBInterface database;
        Event theEvent;

        // Variables relating to information we're collecting.
        ResultsAPI api;
        string slug, year;


        public APIWindow(IWindowCallback window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            List<ResultsAPI> apis = database.GetAllResultsAPI();
            if (theEvent == null || theEvent.Identifier < 1 || apis.Count < 1)
            {
                Log.E("UI.API.APIWindow", "event not found or no apis set up");
                APIFrame.Content = new APIErrorPage(this, apis.Count < 1);
            } else
            {
                APIFrame.Content = new APIPage1(this, database);
            }
        }

        public static APIWindow NewWindow(IWindowCallback window, IDBInterface database)
        {
            return new APIWindow(window, database);
        }

        public void GotoPage2(ResultsAPI api)
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
            } else
            {
                MessageBox.Show("One or more values retrieved is invalid.");
            }
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (window != null) window.WindowFinalize(this);
        }
    }
}
