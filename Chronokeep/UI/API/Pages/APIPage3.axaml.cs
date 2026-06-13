using Avalonia.Controls;
using Chronokeep.Helpers;
using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronoKeepAPI;
using Chronokeep.UI.API.Windows;
using Chronokeep.UI.Util;
using System;

namespace Chronokeep.UI.API;

public partial class APIPage3 : UserControl
{
    private readonly APIWindow window;
    private readonly APIObject api;
    private readonly Event theEvent;
    private readonly string slug;

    private GetEventYearsResponse? years;

    public APIPage3(APIWindow window, APIObject api, Event theEvent, string slug)
    {
        InitializeComponent();
        this.window = window;
        this.api = api;
        this.theEvent = theEvent;
        this.slug = slug;
        GetEventYears();
    }

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
            Tag = "NEW"
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
                    Tag = y.Year
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
            newPanel.IsVisible = true;
        }
        else
        {
            newPanel.IsVisible = false;
        }
        yearBox.Text = theEvent.YearCode;
        dateBox.Text = DateTime.Parse(theEvent.Date).ToString("MM/dd/yyyy");
        if (theEvent != null && theEvent.EventType == Constants.Timing.EVENT_TYPE_BACKYARD_ULTRA)
        {
            rankBox.Items.Add(new ComboBoxItem
            {
                Content = "Elapsed",
                Tag = "Clock"
            });
            rankBox.Items.Add(new ComboBoxItem
            {
                Content = "Cumulative",
                Tag = "Chip"
            });
        }
        else
        {
            rankBox.Items.Add(new ComboBoxItem
            {
                Content = "Clock",
                Tag = "Clock"
            });
            rankBox.Items.Add(new ComboBoxItem
            {
                Content = "Chip",
                Tag = "Chip"
            });
        }
        rankBox.SelectedIndex = theEvent!.RankByGun ? 0 : 1;
        yearPanel.IsVisible = true;
        holdingLabel.IsVisible = false;
    }

    private void YearBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if ((string)((ComboBoxItem)yearCopyBox.SelectedItem!).Tag! == "NEW")
        {
            newPanel.IsVisible = true;
        }
        else
        {
            newPanel.IsVisible = false;
        }
    }

    private void DaysAllowed_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (DaysAllowedSlider != null && DaysAllowedText != null)
        {
            DaysAllowedText.Text = DaysAllowedSlider.Value.ToString();
        }
    }

    private async void Next_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.API.APIPage3", "DateTime: " + Convert.ToDateTime(dateBox.Text).ToString("yyyy/MM/dd HH:mm:ss"));
        string year = (string)((ComboBoxItem)yearCopyBox.SelectedItem!).Tag!;
        if (year == "NEW")
        {
            try
            {
                EventYearResponse addResponse = await APIHandlers.AddEventYear(api, slug, new APIEventYear
                {
                    Year = yearBox.Text!,
                    DateTime = Convert.ToDateTime(dateBox.Text).ToString("yyyy/MM/dd HH:mm:ss zzz"),
                    Live = LiveBox.IsChecked == true,
                    DaysAllowed = Convert.ToInt32(DaysAllowedSlider.Value),
                    RankingType = ((string)((ComboBoxItem)rankBox.SelectedItem!).Tag!).Equals("Chip", StringComparison.OrdinalIgnoreCase) ? "chip" : "gun",
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

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        window.Close();
    }
}