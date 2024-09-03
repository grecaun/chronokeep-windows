using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.API;
using Chronokeep.UI.UIObjects;
using System.Windows;
using System.Windows.Controls;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for APIPage2.xaml
    /// </summary>
    public partial class APIPage2
    {
        APIWindow window;
        IDBInterface database;
        APIObject api;
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
            events.Events.Sort((a, b) => b.CompareTo(a));
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
            slugBox.Text = theEvent.Name.Replace(' ', '-').Replace("'","").ToLower();
            contactBox.Text = database.GetAppSetting(Constants.Settings.CONTACT_EMAIL).Value;
            eventPanel.Visibility = Visibility.Visible;
            holdingLabel.Visibility = Visibility.Collapsed;
        }

        public APIPage2(APIWindow window, IDBInterface database, APIObject api, Event theEvent)
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
            if (EventBox == null || EventBox.SelectedItem == null)
            {
                window.Close();
                return;
            }
            string slug = ((ComboBoxItem)EventBox.SelectedItem).Uid;
            if (slug == "NEW")
            {
                try
                {
                    string type = Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_UNKNOWN;
                    if (Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA == theEvent.EventType)
                    {
                        type = Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_BACKYARD_ULTRA;
                    }
                    else if (Constants.Timing.EVENT_TYPE_TIME == theEvent.EventType)
                    {
                        type = Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_TIME;
                    }
                    else if (Constants.Timing.EVENT_TYPE_DISTANCE == theEvent.EventType)
                    {
                        type = Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_DISTANCE;
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
