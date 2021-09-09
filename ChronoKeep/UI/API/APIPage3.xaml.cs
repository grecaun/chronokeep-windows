using ChronoKeep.Network.API;
using ChronoKeep.Objects;
using ChronoKeep.Objects.API;
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

namespace ChronoKeep.UI.API
{
    /// <summary>
    /// Interaction logic for APIPage3.xaml
    /// </summary>
    public partial class APIPage3 : Page
    {
        APIWindow window;
        ResultsAPI api;
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
                MessageBox.Show(ex.Message);
                window.Close();
                return;
            }
            YearBox.Items.Add(new ComboBoxItem
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
                    YearBox.Items.Add(new ComboBoxItem
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
            YearBox.SelectedIndex = ix;
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
            holdingLabel.Visibility = Visibility.Hidden;
        }

        public APIPage3(APIWindow window, ResultsAPI api, Event theEvent, string slug)
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
            if (((ComboBoxItem)YearBox.SelectedItem).Uid == "NEW")
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
            string year = ((ComboBoxItem)YearBox.SelectedItem).Uid;
            if (year == "NEW")
            {
                try
                {
                    EventYearResponse addResponse = await APIHandlers.AddEventYear(api, slug, new APIEventYear
                    {
                        Year = yearBox.Text,
                        DateTime = Convert.ToDateTime(dateBox.Text).ToString("yyyy/MM/dd HH:mm:ss")
                    });
                    year = addResponse.EventYear.Year;
                }
                catch (APIException ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
            }
            window.Finish(year);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            window.Close();
        }
    }
}
