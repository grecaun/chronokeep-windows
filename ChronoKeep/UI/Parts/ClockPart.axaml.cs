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
    private readonly ClockControl parent;
    private Chronoclock clock;
    private readonly Event theEvent;

    public bool IsLocked { get; private set; }

    public ClockPart(Chronoclock clock, ClockControl parent, Event theEvent)
    {
        InitializeComponent();
        this.clock = clock;
        this.theEvent = theEvent;
        this.parent = parent;
        NameBlock.Text = clock.Name;
        UrlBlock.Text = clock.URL;
        EnabledSwitch.IsChecked = clock.Enabled;
        BrightnessBox.IsEnabled = false;
        CountDatePicker.IsEnabled = false;
        CountTimeBox.IsEnabled = false;
        Start.IsEnabled = false;
        Stop.IsEnabled = false;
        GetTime.IsEnabled = false;
        SetTime.IsEnabled = false;
        if (clock.URL != null && clock.URL.Length > 0)
        {
            GetConfig();
        }
    }

    public void UpdateLockStatus(bool locked)
    {
        IsLocked = locked;
        if (locked)
        {
            Start.IsEnabled = false;
            Stop.IsEnabled = false;
            LockedImage.IsVisible = true;
            UnlockedImage.IsVisible = false;
        }
        else
        {
            Start.IsEnabled = true;
            Stop.IsEnabled = true;
            LockedImage.IsVisible = false;
            UnlockedImage.IsVisible = true;
        }
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
            BrightnessBox.SelectedIndex = (int)(info.Brightness - 1);
        }
        UpdateLockStatus(info.LockCountUpDown);
        if (info.CountUpDownTimestamp > 0)
        {
            DateTime countupdown = Constants.Timing.UTCToLocalDate(info.CountUpDownTimestamp, 0);
            CountDatePicker.Text = countupdown.ToString("MM/dd/yyyy");
            ChangeCountTimeBox(countupdown.ToString("HH:mm:ss"));
        }
        else if (theEvent!.StartSeconds > 0 || theEvent!.StartMilliseconds > 0)
        {
            CountDatePicker.Text = DateTime.Parse(theEvent.Date).ToString("MM/dd/yyyy");
            ChangeCountTimeBox(Constants.Timing.SecondsToTime(theEvent.StartMilliseconds >= 500 ? theEvent.StartSeconds + 1 : theEvent.StartSeconds));
            Log.D("UI.Timing.ClockControl.ClockListItem", string.Format("Time should be set to: {0}", Constants.Timing.SecondsToTime(theEvent.StartSeconds)));
        }
        EnableConfig();
    }

    public void ChangeCountTimeBox(string time)
    {
        CountTimeBox.IsEnabled = true;
        CountTimeBox.Text = time;
        CountTimeBox.IsEnabled = false;
    }

    public void EnableConfig()
    {
        BrightnessBox.IsEnabled = true;
        LockedSwitch.IsEnabled = true;
        CountDatePicker.IsEnabled = true;
        CountTimeBox.IsEnabled = true;
        GetTime.IsEnabled = true;
        SetTime.IsEnabled = true;
        Start.IsEnabled = IsLocked == false;
        Stop.IsEnabled = IsLocked == false;
    }

    public void DisableConfig()
    {
        BrightnessBox.IsEnabled = false;
        LockedSwitch.IsEnabled = false;
        CountDatePicker.IsEnabled = false;
        CountTimeBox.IsEnabled = false;
        GetTime.IsEnabled = false;
        SetTime.IsEnabled = false;
        Start.IsEnabled = false;
        Stop.IsEnabled = false;
    }

    public Chronoclock GetUpdatedClock()
    {
        Chronoclock output = new()
        {
            Identifier = clock.Identifier,
            Name = NameBlock.Text!,
            Enabled = EnabledSwitch.IsChecked == true,
            URL = UrlBlock.Text!,
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
        if (LockedSwitch.IsEnabled == true)
        {
            UpdateLockStatus(!IsLocked);
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

    private async void BrightnessChanged(object sender, SelectionChangedEventArgs e)
    {
        Log.D("UI.Timing.ClockControl.ClockListItem", "BrightnessChanged");
        clock = GetUpdatedClock();
        if (BrightnessBox.IsEnabled == true)
        {
            if (BrightnessBox.SelectedIndex >= 0)
            {
                DisableConfig();
                try
                {
                    CountUpDownTimestampResponse resp = await clock.SetBrightness((uint)(BrightnessBox.SelectedIndex + 1));
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
        if (CountDatePicker.Text == null || CountTimeBox.Text == null)
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
            if (!DateTime.TryParse(string.Format("{0} {1}", CountDatePicker.Text!.Replace('_', '0'), CountTimeBox.Text!.Replace('_', '0')), out countDate))
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

    private async void Stop_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.Timing.ClockControl.ClockListItem", "Stop clicked.");
        clock = GetUpdatedClock();
        try
        {
            CountUpDownTimestampResponse resp = await clock.StopCountUp();
            UpdateInformation(resp);
        }
        catch (APIException ex)
        {
            DialogBox.Show(ex.Message);
            return;
        }
    }

    private async void GetTime_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.Timing.ClockControl.ClockListItem", "Get Time clicked.");
        clock = GetUpdatedClock();
        try
        {
            GetTimeResponse resp = await clock.GetTime();
            parent.UpdateTime(resp.Time);
        }
        catch (APIException ex)
        {
            DialogBox.Show(ex.Message);
            return;
        }
    }

    private async void SetTime_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.Timing.ClockControl.ClockListItem", "Set Time clicked.");
        clock = GetUpdatedClock();
        try
        {
            GetTimeResponse resp = await clock.SetTime(DateTime.Now);
            parent.UpdateTime(resp.Time);
        }
        catch (APIException ex)
        {
            DialogBox.Show(ex.Message);
            return;
        }
    }

    private async void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        Log.D("UI.Timing.ClockControl.ClockListItem", "Refresh clicked.");
        clock = GetUpdatedClock();
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
            DialogBox.Show(ex.Message);
            return;
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        Log.D("UI.Timing.ClockControl.ClockListItem", "Delete clicked.");
        parent.RemoveClock(clock);
    }
}