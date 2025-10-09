using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronoKeepAPI;
using Chronokeep.UI.UIObjects;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for EditEventPage.xaml
    /// </summary>
    public partial class EditEventPage
    {
        private readonly EditAPIWindow window;

        private readonly APIObject api;
        private readonly string slug;

        private GetEventResponse apiEvent;

        private async void GetEvent()
        {
            try
            {
                apiEvent = await APIHandlers.GetEvent(api, slug);
            }
            catch (APIException ex)
            {
                DialogBox.Show(ex.Message);
                window.Close();
                return;
            }
            nameBox.Text = apiEvent.Event.Name;
            slugBox.Text = apiEvent.Event.Slug;
            contactBox.Text = apiEvent.Event.ContactEmail;
            websiteBox.Text = apiEvent.Event.Website;
            imageBox.Text = apiEvent.Event.Image;
            restrictBox.IsChecked = apiEvent.Event.AccessRestricted == true;
            ComboBoxItem type = null;
            foreach (ComboBoxItem item in typeBox.Items)
            {

                if (item.Content.ToString().Equals("Distance", StringComparison.OrdinalIgnoreCase)
                    && apiEvent.Event.Type.Equals(Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_DISTANCE, StringComparison.OrdinalIgnoreCase))
                {
                    type = item;
                }
                else if (item.Content.ToString().Equals("Time", StringComparison.OrdinalIgnoreCase)
                    && apiEvent.Event.Type.Equals(Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_TIME, StringComparison.OrdinalIgnoreCase))
                {
                    type = item;
                }
                else if (item.Content.ToString().Equals("Backyard Ultra", StringComparison.OrdinalIgnoreCase)
                    && apiEvent.Event.Type.Equals(Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_BACKYARD_ULTRA, StringComparison.OrdinalIgnoreCase))
                {
                    type = item;
                }
            }
            if (type != null)
            {
                typeBox.SelectedItem = type;
            }
            else
            {
                typeBox.SelectedIndex = 0;
            }
            eventPanel.Visibility = Visibility.Visible;
            holdingLabel.Visibility = Visibility.Collapsed;
            SaveButton.IsEnabled = true;
        }

        public EditEventPage(EditAPIWindow window, APIObject api, string slug)
        {
            InitializeComponent();
            this.window = window;
            this.api = api;
            this.slug = slug;
            GetEvent();
        }

        private async void Done_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string type = Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_UNKNOWN;
                if (((ComboBoxItem)typeBox.SelectedItem).Content.ToString().Equals("Distance", StringComparison.OrdinalIgnoreCase))
                {
                    type = Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_DISTANCE;
                }
                else if (((ComboBoxItem)typeBox.SelectedItem).Content.ToString().Equals("Time", StringComparison.OrdinalIgnoreCase))
                {
                    type = Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_TIME;
                }
                else if (((ComboBoxItem)typeBox.SelectedItem).Content.ToString().Equals("Backyard Ultra", StringComparison.OrdinalIgnoreCase))
                {
                    type = Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_BACKYARD_ULTRA;
                }

                ModifyEventResponse addResponse = await APIHandlers.UpdateEvent(api, new APIEvent
                {
                    Name = nameBox.Text,
                    CertificateName = certNameBox.Text,
                    Slug = slugBox.Text,
                    Website = websiteBox.Text,
                    Image = imageBox.Text,
                    ContactEmail = contactBox.Text,
                    AccessRestricted = restrictBox.IsChecked == true,
                    Type = type
                });
                window.Close();
            }
            catch (APIException ex)
            {
                DialogBox.Show(ex.Message);
                return;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            window.Close();
        }
    }
}
