using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using Xceed.Wpf.Toolkit;
using Button = Wpf.Ui.Controls.Button;

namespace Chronokeep.UI.Timing
{
    /// <summary>
    /// Interaction logic for RemoteReaders.xaml
    /// </summary>
    public partial class ClockControl : FluentWindow
    {
        internal class ClockListItem : Wpf.Ui.Controls.ListViewItem
        {
            private Chronoclock clock;
            private readonly Event theEvent;

            private readonly Wpf.Ui.Controls.TextBox nameBlock;
            private readonly Wpf.Ui.Controls.TextBox urlBlock;
            private readonly ToggleSwitch enabledSwitch;
            private readonly Wpf.Ui.Controls.TextBlock lockedLabel;
            private readonly ToggleSwitch lockedSwitch;
            private readonly ComboBox brightnessBox;
            private readonly DatePicker countDatePicker;
            private readonly MaskedTextBox countTimeBox;

            public ClockListItem(Chronoclock clock, ClockControl parent, Event theEvent)
            {
                this.clock = clock;
                string dateStr = DateTime.Now.ToString("MM/dd/yyyy");

                StackPanel panelTwo = new()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                mainPanel.Children.Add(panelTwo);
                countDatePicker = new()
                {
                    Text = "",
                    Height = 35,
                    Width = 230,
                    Margin = new Thickness(5),
                    IsEnabled = false
                };
                panelTwo.Children.Add(countDatePicker);
                countTimeBox = new()
                {
                    Text = "",
                    Mask = "00:00:00",
                    Height = 35,
                    Width = 80,
                    VerticalAlignment = VerticalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(5),
                    IsEnabled = false
                };
                panelTwo.Children.Add(countTimeBox);
                Button start = new()
                {
                    Content = "▶",
                    Height = 35,
                    Margin = new Thickness(5),
                };
                start.Click += new RoutedEventHandler(async (sender, e) =>
                {
                    Log.D("UI.Timing.ClockControl.ClockListItem", "Start clicked.");
                    clock = GetUpdatedClock();
                    DateTime countDate;
                    if (countDatePicker.Text.Length < 1)
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
                        if (!DateTime.TryParse(string.Format("{0} {1}", countDatePicker.Text, countTimeBox.Text.Replace('_', '0')), out countDate))
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
                });
                panelTwo.Children.Add(start);
                Button stop = new()
                {
                    Content = "🔳",
                    Height = 35,
                    Margin = new Thickness(5),
                };
                stop.Click += new RoutedEventHandler(async (sender, e) =>
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
                });
                panelTwo.Children.Add(stop);
                Button getTime = new()
                {
                    Content = "🕒",
                    Height = 35,
                    Margin = new Thickness(5),
                };
                getTime.Click += new RoutedEventHandler(async (sender, e) =>
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
                });
                panelTwo.Children.Add(getTime);
                Button setTime = new()
                {
                    Content = "🔧",
                    Height = 35,
                    Margin = new Thickness(5),
                };
                setTime.Click += new RoutedEventHandler(async (sender, e) =>
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
                });
                panelTwo.Children.Add(setTime);
                Button refresh = new()
                {
                    Content = "🔃",
                    Height = 35,
                    Margin = new Thickness(5),
                };
                refresh.Click += new RoutedEventHandler(async (sender, e) =>
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
                });
                panelTwo.Children.Add(refresh);
                if (clock.URL != null && clock.URL.Length > 0)
                {
                    GetConfig();
                }
                this.theEvent = theEvent;
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
                lockedSwitch.IsChecked = info.LockCountUpDown;
                if (info.LockCountUpDown)
                {
                    lockedLabel.Text = "🔒";
                }
                else
                {
                    lockedLabel.Text = "🔓";
                }
                if (info.CountUpDownTimestamp > 0)
                {
                    DateTime countupdown = Constants.Timing.UTCToLocalDate(info.CountUpDownTimestamp, 0);
                    countDatePicker.Text = countupdown.ToString("MM/dd/yyyy");
                    ChangeCountTimeBox(countupdown.ToString("HH:mm:ss"));
                }
                else if (theEvent.StartSeconds > 0 || theEvent.StartMilliseconds > 0)
                {
                    countDatePicker.Text = theEvent.LongDate;
                    ChangeCountTimeBox(Constants.Timing.SecondsToTime(theEvent.StartMilliseconds >= 500 ? theEvent.StartSeconds + 1 : theEvent.StartSeconds));
                    Log.D("UI.Timing.ClockControl.ClockListItem", string.Format("Time should be set to: {0}", Constants.Timing.SecondsToTime(theEvent.StartSeconds)));
                }
                else
                {
                    countDatePicker.Text = "";
                    ChangeCountTimeBox("");
                }
                EnableConfig();
            }

            public void ChangeCountTimeBox(string time)
            {
                countTimeBox.IsEnabled = true;
                countTimeBox.Text = time;
                countTimeBox.IsEnabled = false;
            }

            public async void LockedChanged(object sender, RoutedEventArgs args)
            {
                Log.D("UI.Timing.ClockControl.ClockListItem", "LockedChanged");
                clock = GetUpdatedClock();
                if (lockedSwitch.IsEnabled == true)
                {
                    DisableConfig();
                    try
                    {
                        CountUpDownTimestampResponse resp = await clock.SetLockCountUpDown(lockedSwitch.IsChecked == true);
                        UpdateInformation(resp);
                    }
                    catch (APIException ex)
                    {
                        DialogBox.Show(ex.Message);
                    }
                }
            }

            public async void BrightnessChanged(object sender, RoutedEventArgs args)
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

            public void EnableConfig()
            {
                brightnessBox.IsEnabled = true;
                lockedSwitch.IsEnabled = true;
                countDatePicker.IsEnabled = true;
                countTimeBox.IsEnabled = true;
            }

            public void DisableConfig()
            {
                brightnessBox.IsEnabled = false;
                lockedSwitch.IsEnabled = false;
                countDatePicker.IsEnabled = false;
                countTimeBox.IsEnabled = false;
            }

            public Chronoclock GetUpdatedClock()
            {
                Chronoclock output = new()
                {
                    Identifier = clock.Identifier,
                    Name = nameBlock.Text,
                    Enabled = enabledSwitch.IsChecked == true,
                    URL = urlBlock.Text,
                };
                return output;
            }
        }
    }
}
