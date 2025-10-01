using Chronokeep.Database;
using Chronokeep.Objects;
using Chronokeep.UI.UIObjects;
using System.Windows;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for EditAPIPage1.xaml
    /// </summary>
    public partial class EditAPIPage1
    {
        private readonly EditAPIWindow window;
        private readonly IDBInterface database;

        public EditAPIPage1(EditAPIWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
        }

        private void Edit_Event_Click(object sender, RoutedEventArgs e)
        {
            window.GotoEditEvent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            window.Close();
        }

        private void Edit_Year_Click(object sender, RoutedEventArgs e)
        {
            window.GotoEditYear();
        }

        private void Unlink_Click(object sender, RoutedEventArgs e)
        {
            Event theEvent = database.GetCurrentEvent();
            // Check if we've actually got a linked event, then unlink it.
            if (theEvent != null && theEvent.API_ID != Constants.APIConstants.NULL_ID && theEvent.API_Event_ID != Constants.APIConstants.NULL_EVENT_ID)
            {
                theEvent.API_ID = Constants.APIConstants.NULL_ID;
                theEvent.API_Event_ID = Constants.APIConstants.NULL_EVENT_ID;
                database.UpdateEvent(theEvent);
                window.NetworkUpdateResults();
            }
            else
            {
                DialogBox.Show("Unable to Link Event");
            }
            window.Close();
        }
    }
}
