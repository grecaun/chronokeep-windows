using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.API;
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
        EditAPIWindow window;

        APIObject api;
        string slug;

        GetEventResponse apiEvent;

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
                if (((ComboBoxItem)typeBox.SelectedItem).Content.ToString().Equals("Backyard Ultra", StringComparison.OrdinalIgnoreCase))
                {
                    type = Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_BACKYARD_ULTRA;
                }
                else if (((ComboBoxItem)typeBox.SelectedItem).Content.ToString().Equals("Time", StringComparison.OrdinalIgnoreCase))
                {
                    type = Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_TIME;
                }
                else if (((ComboBoxItem)typeBox.SelectedItem).Content.ToString().Equals("Distance", StringComparison.OrdinalIgnoreCase))
                {
                    type = Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_DISTANCE;
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
