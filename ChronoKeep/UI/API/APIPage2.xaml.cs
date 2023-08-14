using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.API;
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
    /// Interaction logic for APIPage2.xaml
    /// </summary>
    public partial class APIPage2
    {
        APIWindow window;
        IDBInterface database;
        ResultsAPI api;
        Event theEvent;

        GetEventsResponse events;

        private async void GetEvents()
        {
            try
            {
                events = await APIHandlers.GetEvents(api);
            }
            catch (APIException ex)
            {
                DialogBox.Show(ex.Message);
                window.Close();
                return;
            }
            EventBox.Items.Add(new ComboBoxItem
            {
                Content = "New Event",
                Uid = "NEW"
            });
            int ix = 0;
            int count = 1;
            Log.D("UI.API.APIPage2", "Adding events to combo box.");
            if (events.Events != null)
            {
                foreach (APIEvent ev in events.Events)
                {
                    EventBox.Items.Add(new ComboBoxItem
                    {
                        Content = ev.Name,
                        Uid = ev.Slug
                    });
                    if (theEvent.Name == ev.Name)
                    {
                        ix = count;
                    }
                    count++;
                }
            }
            EventBox.SelectedIndex = ix;
            if (ix == 0)
            {
                newPanel.Visibility = Visibility.Visible;
            }
            else
            {
                newPanel.Visibility = Visibility.Collapsed;
            }
            nameBox.Text = theEvent.Name;
            slugBox.Text = theEvent.Name.Replace(' ', '-').ToLower();
            contactBox.Text = database.GetAppSetting(Constants.Settings.CONTACT_EMAIL).value;
            eventPanel.Visibility = Visibility.Visible;
            holdingLabel.Visibility = Visibility.Hidden;
        }

        public APIPage2(APIWindow window, IDBInterface database, ResultsAPI api, Event theEvent)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            this.api = api;
            this.theEvent = theEvent;

            GetEvents();
        }

        private void EventBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBoxItem)EventBox.SelectedItem).Uid == "NEW")
            {
                newPanel.Visibility = Visibility.Visible;
            }
            else
            {
                newPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            string slug = ((ComboBoxItem)EventBox.SelectedItem).Uid;
            if (slug == "NEW")
            {
                try
                {
                    string type = "";
                    switch (theEvent.EventType)
                    {
                        case Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA:
                            type = Constants.ResultsAPI.CHRONOKEEP_EVENT_TYPE_BACKYARD_ULTRA;
                            break;
                        case Constants.Timing.EVENT_TYPE_TIME:
                            type = Constants.ResultsAPI.CHRONOKEEP_EVENT_TYPE_TIME;
                            break;
                        case Constants.Timing.EVENT_TYPE_DISTANCE:
                            type = Constants.ResultsAPI.CHRONOKEEP_EVENT_TYPE_DISTANCE;
                            break;
                        default:
                            type = Constants.ResultsAPI.CHRONOKEEP_EVENT_TYPE_UNKNOWN;
                            break;

                    }

                    ModifyEventResponse addResponse = await APIHandlers.AddEvent(api, new APIEvent
                    {
                        Name = nameBox.Text,
                        Slug = slugBox.Text,
                        Website = "",
                        Image = "",
                        ContactEmail = contactBox.Text,
                        AccessRestricted = (bool)restrictBox.IsChecked ? true : false,
                        Type = type
                    });
                    slug = addResponse.Event.Slug;
                }
                catch (APIException ex)
                {
                    DialogBox.Show(ex.Message);
                    return;
                }
            }
            window.GotoPage3(slug);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            window.Close();
        }
    }
}
