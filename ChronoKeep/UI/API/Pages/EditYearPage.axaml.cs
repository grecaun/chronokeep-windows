using Avalonia.Controls;
using Chronokeep.Helpers;
using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronoKeepAPI;
using Chronokeep.UI.API.Windows;
using Chronokeep.UI.Parts;
using System;

namespace Chronokeep.UI.API;

public partial class EditYearPage : UserControl
{
    private readonly EditAPIWindow window;

    private readonly APIObject api;
    private readonly string slug;
    private readonly string year;

    private EventYearResponse? response;

    public EditYearPage(EditAPIWindow window, APIObject api, string slug, string year)
    {
        InitializeComponent();
        this.window = window;

        this.api = api;
        this.slug = slug;
        this.year = year;

        GetEventYears();
    }

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
        dateBox.Text = DateTime.Parse(response.EventYear.DateTime).ToString("MM/dd/yyyy");
        if (response.Event.Type == Constants.APIConstants.CHRONOKEEP_EVENT_TYPE_BACKYARD_ULTRA)
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
        rankBox.SelectedIndex = response.EventYear.RankingType.Equals("chip", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        LiveBox.IsChecked = response.EventYear.Live;
        DaysAllowedText.Text = response.EventYear.DaysAllowed.ToString();
        DaysAllowedSlider.Value = response.EventYear.DaysAllowed;
        yearPanel.IsVisible = true;
        holdingLabel.IsVisible = false;
        SaveButton.IsEnabled = true;
    }

    private void DaysAllowed_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (DaysAllowedSlider != null && DaysAllowedText != null)
        {
            DaysAllowedText.Text = DaysAllowedSlider.Value.ToString();
        }
    }

    private async void Done_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            await APIHandlers.UpdateEventYear(api, slug, new APIEventYear
            {
                Year = yearBox.Text!,
                DateTime = Convert.ToDateTime(dateBox.Text!.Replace('_', '0')).ToString("yyyy/MM/dd HH:mm:ss zzz"),
                Live = LiveBox.IsChecked == true,
                DaysAllowed = Convert.ToInt32(DaysAllowedSlider.Value),
                RankingType = ((string)((ComboBoxItem)rankBox.SelectedItem!).Tag!).Equals("Chip", StringComparison.OrdinalIgnoreCase) ? "chip" : "gun",
            });
            window.Close();
        }
        catch (APIException ex)
        {
            DialogBox.Show(ex.Message);
            return;
        }
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        window.Close();
    }
}