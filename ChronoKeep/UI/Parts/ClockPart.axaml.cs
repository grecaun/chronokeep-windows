using Avalonia.Controls;
using Avalonia.Interactivity;
using Chronokeep.Helpers;
using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.UI.Timing.Windows;
using System;

namespace Chronokeep.UI.Parts;

public partial class ClockPart : UserControl
{
    private Chronoclock clock;
    private readonly Event? theEvent;

    public bool IsLocked { get; private set; }
    public bool IsOpen { get => !IsLocked; }

    public ClockPart(Chronoclock clock, ClockControl parent, Event theEvent)
    {
        InitializeComponent();
        this.clock = clock;
        this.theEvent = theEvent;

        string dateStr = DateTime.Now.ToString("MM/dd/yyyy");
    }

    public async void GetConfig()
    {
        try
        {
            GetConfigResponse resp = await clock.GetConfig();
            UpdateInformation(new()
            {
                CountUpDownTimestamp = resp.CountUpDownTimestamp,
                Brightness = resp.Brightness,
                FlipDisplay = resp.FlipDisplay,
                LockCountUpDown = resp.LockCountUpDown,
            });
        }
        catch (APIException ex)
        {
            Log.D("UI.Timing.ClockControl.ClockListItem", "Unable to fetch clock config." + ex.Message);
        }
    }

    public void UpdateInformation(CountUpDownTimestampResponse info)
    {
        clock = GetUpdatedClock();
        if (info.Brightness > 0)
        {
            brightnessBox.SelectedIndex = (int)(info.Brightness - 1);
        }
        IsLocked = info.LockCountUpDown;
        if (info.CountUpDownTimestamp > 0)
        {
            DateTime countupdown = Constants.Timing.UTCToLocalDate(info.CountUpDownTimestamp, 0);
            CountDatePicker.SelectedDate = countupdown;
            ChangeCountTimeBox(countupdown.ToString("HH:mm:ss"));
        }
        else if (theEvent!.StartSeconds > 0 || theEvent!.StartMilliseconds > 0)
        {
            CountDatePicker.SelectedDate = DateTime.Parse(theEvent.Date);
            ChangeCountTimeBox(Constants.Timing.SecondsToTime(theEvent.StartMilliseconds >= 500 ? theEvent.StartSeconds + 1 : theEvent.StartSeconds));
            Log.D("UI.Timing.ClockControl.ClockListItem", string.Format("Time should be set to: {0}", Constants.Timing.SecondsToTime(theEvent.StartSeconds)));
        }
        EnableConfig();
    }

    public void ChangeCountTimeBox(string time)
    {
        countTimeBox.IsEnabled = true;
        countTimeBox.Text = time;
        countTimeBox.IsEnabled = false;
    }

    public void EnableConfig()
    {
        brightnessBox.IsEnabled = true;
        lockedSwitch.IsEnabled = true;
        CountDatePicker.IsEnabled = true;
        countTimeBox.IsEnabled = true;
    }

    public void DisableConfig()
    {
        brightnessBox.IsEnabled = false;
        lockedSwitch.IsEnabled = false;
        CountDatePicker.IsEnabled = false;
        countTimeBox.IsEnabled = false;
    }

    public Chronoclock GetUpdatedClock()
    {
        Chronoclock output = new()
        {
            Identifier = clock.Identifier,
            Name = nameBlock.Text!,
            Enabled = enabledSwitch.IsChecked == true,
            URL = urlBlock.Text!,
        };
        return output;
    }

    private void SelectAll(object sender, RoutedEventArgs e)
    {
        TextBox src = (TextBox)e.Source!;
        src.SelectAll();
    }

    private async void LockedChanged(object sender, RoutedEventArgs e)
    {
        Log.D("UI.Timing.ClockControl.ClockListItem", "LockedChanged");
        clock = GetUpdatedClock();
        if (lockedSwitch.IsEnabled == true)
        {
            IsLocked = !IsLocked;
            DisableConfig();
            try
            {
                CountUpDownTimestampResponse resp = await clock.SetLockCountUpDown(IsLocked);
                UpdateInformation(resp);
            }
            catch (APIException ex)
            {
                DialogBox.Show(ex.Message);
            }
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {

    }

    private async void BrightnessChanged(object sender, SelectionChangedEventArgs e)
    {
        Log.D("UI.Timing.ClockControl.ClockListItem", "BrightnessChanged");
        clock = GetUpdatedClock();
        if (brightnessBox.IsEnabled == true)
        {
            if (brightnessBox.SelectedIndex >= 0)
            {
                DisableConfig();
                try
                {
                    CountUpDownTimestampResponse resp = await clock.SetBrightness((uint)(brightnessBox.SelectedIndex + 1));
                    UpdateInformation(resp);
                }
                catch (APIException ex)
                {
                    DialogBox.Show(ex.Message);
                }
            }
        }
    }

    private async void Start_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.Timing.ClockControl.ClockListItem", "Start clicked.");
        clock = GetUpdatedClock();
        DateTime countDate;
        if (CountDatePicker.SelectedDate.HasValue)
        {
            try
            {
                countDate = DateTime.Now;
                CountUpDownTimestampResponse resp = await clock.SetCountUpDownTime(countDate);
                UpdateInformation(resp);
            }
            catch (APIException ex)
            {
                DialogBox.Show(ex.Message);
                return;
            }
        }
        else
        {
            if (!DateTime.TryParse(string.Format("{0} {1}", CountDatePicker.SelectedDate!.Value, countTimeBox.Text!.Replace('_', '0')), out countDate))
            {
                countDate = DateTime.Now;
            }
            try
            {
                CountUpDownTimestampResponse resp = await clock.SetCountUpDownTime(countDate);
                UpdateInformation(resp);
            }
            catch (APIException ex)
            {
                DialogBox.Show(ex.Message);
                return;
            }
        }
    }
}