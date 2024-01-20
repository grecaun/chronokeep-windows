using Chronokeep.Objects;
using Chronokeep.UI.UIObjects;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for APIPage1.xaml
    /// </summary>
    public partial class APIPage1
    {
        APIWindow window;
        IDBInterface database;
        Dictionary<string, APIObject> apiDict;

        public APIPage1(APIWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;

            AppSetting last_api = database.GetAppSetting(Constants.Settings.LAST_USED_API_ID);
            List<APIObject> apis = database.GetAllAPI();
            apis.RemoveAll(x => !Constants.APIConstants.API_RESULTS[x.Type]);
            apiDict = new Dictionary<string, APIObject>();
            int api_id = -1;
            if (last_api != null)
            {
                try
                {
                    api_id = Convert.ToInt32(last_api.Value);
                }
                catch
                {
                    api_id = -1;
                }
            }
            int ix = 0;
            int count = 0;
            foreach (APIObject api in apis)
            {
                apiDict[api.Identifier.ToString()] = api;
                APIBox.Items.Add(new ComboBoxItem
                {
                    Content = api.Nickname,
                    Uid = api.Identifier.ToString()
                });
                if (api_id > 0 && api_id == api.Identifier)
                {
                    ix = count;
                }
                count++;
            }
            APIBox.SelectedIndex = ix;
            Event theEvent = database.GetCurrentEvent();
            // Check if we've actually got a linked event, then unlink it.
            if (theEvent != null && theEvent.API_ID != Constants.APIConstants.NULL_ID && theEvent.API_Event_ID != Constants.APIConstants.NULL_EVENT_ID)
            {
                unlinkButton.Visibility = Visibility.Visible;
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            window.GotoPage2(apiDict[((ComboBoxItem)APIBox.SelectedItem).Uid]);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            window.Close();
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
            }
            else
            {
                DialogBox.Show("Unable to Link Event");
            }
            window.Close();
        }
    }
}
