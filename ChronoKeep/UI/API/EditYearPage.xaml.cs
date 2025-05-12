using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.API;
using Chronokeep.UI.UIObjects;
using System;
using System.Windows;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for EditYearPage.xaml
    /// </summary>
    public partial class EditYearPage
    {
        EditAPIWindow window;

        APIObject api;
        string slug;
        string year;

        EventYearResponse response;

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
            await APIHandlers.UpdateEventYear(api, slug, new APIEventYear
            {
                Year = yearBox.Text,
                DateTime = Convert.ToDateTime(dateBox.Text).ToString("yyyy/MM/dd HH:mm:ss zzz"),
                Live = LiveBox.IsChecked == true,
                DaysAllowed = Convert.ToInt32(DaysAllowedSlider.Value),
                RankingType = rankBox.SelectedIndex == 0 ? "chip" : "gun",
            });
            window.Close();
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
