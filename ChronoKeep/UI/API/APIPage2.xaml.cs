using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronoKeepAPI;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for APIPage2.xaml
    /// </summary>
    public partial class APIPage2
    {
        private readonly APIWindow window;
        private readonly IDBInterface database;
        private readonly APIObject api;
        private readonly Event theEvent;

        private GetEventsResponse events;

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
            Log.D("UI.API.APIPage2", "Adding events to combo box.");
            events.Events.Sort((a, b) => b.CompareTo(a));
            events.Events ??= [];
            events.Events.Insert(0, new APIEvent
            {
                Name = "New Event"
            });
            List<APIEvent> ev = new(events.Events);
            eventList.ItemsSource = ev;
            APIEvent maybeEvent = ev.Find(x => x.Name.Equals(theEvent.Name, StringComparison.OrdinalIgnoreCase));
            if (maybeEvent != null)
            {
                eventList.SelectedItem = maybeEvent;
                eventList.ScrollIntoView(maybeEvent);
            }
            else
            {
                eventList.SelectedIndex = 0;
                eventList.ScrollIntoView(ev[0]);
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
            if (eventList.SelectedIndex < 1)
            {
                newPanel.Visibility = Visibility.Visible;
            }
            else
            {
                newPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void eventList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Log.D("UI.ChangeEventWindow", "Double Click detected.");
            Next_Click(sender, null);
        }

        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            List<APIEvent> ev = new(events.Events);
            if (searchBox.Text.Trim().Length > 0)
            {
                Log.D("UI.API.APIPage2", $"searchBox.Text {searchBox.Text}");
                ev.RemoveAll(x => 
                    !x.Name.Contains(searchBox.Text, StringComparison.OrdinalIgnoreCase)
                    && !x.Name.Contains("New Event", StringComparison.OrdinalIgnoreCase)
                );
            }
            eventList.ItemsSource = ev;
            APIEvent maybeEvent = ev.Find(x =>x.Name.Equals(theEvent.Name, StringComparison.OrdinalIgnoreCase));
            if (maybeEvent != null)
            {
                eventList.SelectedItem = maybeEvent;
                eventList.ScrollIntoView(maybeEvent);
            }
            else
            {
                eventList.SelectedIndex = 0;
                eventList.ScrollIntoView(ev[0]);
            }
        }

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            if (eventList == null)
            {
                window.Close();
                return;
            }
            string slug;
            if (eventList.SelectedItem == null || ((APIEvent)eventList.SelectedItem).Slug == null || ((APIEvent)eventList.SelectedItem).Slug.Length < 1)
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
                        CertificateName = certNameBox.Text,
                        Slug = slugBox.Text,
                        Website = websiteBox.Text,
                        Image = imageBox.Text,
                        ContactEmail = contactBox.Text,
                        AccessRestricted = (bool)restrictBox.IsChecked,
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
            else
            {
                slug = ((APIEvent)eventList.SelectedItem).Slug;
            }
            window.GotoPage3(slug);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            window.Close();
        }
    }
}
