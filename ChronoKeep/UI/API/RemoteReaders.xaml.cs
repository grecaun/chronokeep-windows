using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.UI.UIObjects;
using System.Collections.Generic;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for RemoteReaders.xaml
    /// </summary>
    public partial class RemoteReaders : UiWindow
    {
        IMainWindow window;
        IDBInterface database;
        Event theEvent;

        List<APIObject> remoteAPIs;

        public RemoteReaders(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                DialogBox.Show("Unable to get event information.");
                this.Close();
                return;
            }
            remoteAPIs = database.GetAllAPI();
            remoteAPIs.RemoveAll( x => x.Type != Constants.APIConstants.CHRONOKEEP_REMOTE && x.Type != Constants.APIConstants.CHRONOKEEP_REMOTE_SELF );
            // fetch all readers from all remote apis
            // display all readers
        }

        private void UiWindow_Closed(object sender, System.EventArgs e)
        {
            Log.D("UI.API.RemoteReaders", "Window is closed.");
            window.WindowFinalize(this);
        }
    }
}
