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
    /// Interaction logic for APIPage3.xaml
    /// </summary>
    public partial class APIPage3
    {
        APIWindow window;
        APIObject api;
        Event theEvent;
        string slug;

        GetEventYearsResponse years;

        private async void GetEventYears()
        {
            try
            {
                years = await APIHandlers.GetEventYears(api, slug);
            }
            catch (APIException ex)
            {
                DialogBox.Show(ex.Message);
                window.Close();
                return;
            }
            yearCopyBox.Items.Add(new ComboBoxItem
            {
                Content = "New Year",
                Uid = "NEW"
            });
            int ix = 0;
            int count = 1;
            if (years.EventYears != null)
            {
                foreach (APIEventYear y in years.EventYears)
                {
                    yearCopyBox.Items.Add(new ComboBoxItem
                    {
                        Content = y.Year,
                        Uid = y.Year
                    });
                    if (theEvent.YearCode == y.Year)
                    {
                        ix = count;
                    }
                    count++;
                }
            }
            yearCopyBox.SelectedIndex = ix;
            if (ix == 0)
            {
                newPanel.Visibility = Visibility.Visible;
            }
            else
            {
                newPanel.Visibility = Visibility.Collapsed;
            }
            yearBox.Text = theEvent.YearCode;
            dateBox.Text = theEvent.Date;
            yearPanel.Visibility = Visibility.Visible;
            holdingLabel.Visibility = Visibility.Collapsed;
        }

        public APIPage3(APIWindow window, APIObject api, Event theEvent, string slug)
        {
            InitializeComponent();
            this.window = window;
            this.api = api;
            this.theEvent = theEvent;
            this.slug = slug;

            GetEventYears();
        }

        private void YearBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBoxItem)yearCopyBox.SelectedItem).Uid == "NEW")
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
            Log.D("UI.API.APIPage3", "DateTime: " + Convert.ToDateTime(dateBox.Text).ToString("yyyy/MM/dd HH:mm:ss"));
            string year = ((ComboBoxItem)yearCopyBox.SelectedItem).Uid;
            if (year == "NEW")
            {
                try
                {
                    EventYearResponse addResponse = await APIHandlers.AddEventYear(api, slug, new APIEventYear
                    {
                        Year = yearBox.Text,
                        DateTime = Convert.ToDateTime(dateBox.Text).ToString("yyyy/MM/dd HH:mm:ss zzz"),
                        Live = LiveBox.IsChecked == true,
                        DaysAllowed = Convert.ToInt32(DaysAllowedSlider.Value),
                    });
                    year = addResponse.EventYear.Year;
                }
                catch (APIException ex)
                {
                    DialogBox.Show(ex.Message);
                    return;
                }
            }
            window.Finish(year);
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
