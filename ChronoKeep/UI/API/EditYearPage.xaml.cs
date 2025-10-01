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
    /// Interaction logic for EditYearPage.xaml
    /// </summary>
    public partial class EditYearPage
    {
        private readonly EditAPIWindow window;

        private readonly APIObject api;
        private readonly string slug;
        private readonly string year;

        private EventYearResponse response;

        private async void GetEventYears()
        {
            try
            {
                response = await APIHandlers.GetEventYear(api, slug, year);
            }
            catch (APIException ex)
            {
                DialogBox.Show(ex.Message);
                window.Close();
                return;
            }
            yearBox.Text = response.EventYear.Year;
            dateBox.Text = response.EventYear.DateTime;
            if (response.Event.Type == Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_BACKYARD_ULTRA)
            {
                rankBox.Items.Add(new ComboBoxItem
                {
                    Content = "Elapsed",
                    Uid = "Clock"
                });
                rankBox.Items.Add(new ComboBoxItem
                {
                    Content = "Cumulative",
                    Uid = "Chip"
                });
            }
            else
            {
                rankBox.Items.Add(new ComboBoxItem
                {
                    Content = "Clock",
                    Uid = "Clock"
                });
                rankBox.Items.Add(new ComboBoxItem
                {
                    Content = "Chip",
                    Uid = "Chip"
                });
            }
            rankBox.SelectedIndex = response.EventYear.RankingType.Equals("chip", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            LiveBox.IsChecked = response.EventYear.Live;
            DaysAllowedText.Text = response.EventYear.DaysAllowed.ToString();
            DaysAllowedSlider.Value = response.EventYear.DaysAllowed;
            yearPanel.Visibility = Visibility.Visible;
            holdingLabel.Visibility = Visibility.Collapsed;
            SaveButton.IsEnabled = true;
        }

        public EditYearPage(EditAPIWindow window, APIObject api, string slug, string year)
        {
            InitializeComponent();
            this.window = window;

            this.api = api;
            this.slug = slug;
            this.year = year;

            GetEventYears();
        }

        private async void Done_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await APIHandlers.UpdateEventYear(api, slug, new APIEventYear
                {
                    Year = yearBox.Text,
                    DateTime = Convert.ToDateTime(dateBox.Text).ToString("yyyy/MM/dd HH:mm:ss zzz"),
                    Live = LiveBox.IsChecked == true,
                    DaysAllowed = Convert.ToInt32(DaysAllowedSlider.Value),
                    RankingType = ((ComboBoxItem)rankBox.SelectedItem).Uid.ToString().Equals("Chip", StringComparison.OrdinalIgnoreCase) ? "chip" : "gun",
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

        private void DaysAllowed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DaysAllowedSlider != null && DaysAllowedText != null)
            {
                DaysAllowedText.Text = DaysAllowedSlider.Value.ToString();
            }
        }
    }
}
