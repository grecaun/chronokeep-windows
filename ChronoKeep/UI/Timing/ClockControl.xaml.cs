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
        private static ClockControl theOne = null;

        private readonly IMainWindow window;
        private readonly IDBInterface database;

        private readonly Dictionary<int, Chronoclock> ClockDict = [];

        public static ClockControl CreateWindow(IMainWindow window, IDBInterface database)
        {
            theOne ??= new(window, database);
            return theOne;
        }

        private ClockControl(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            this.MinWidth = 10;
            this.MinHeight = 10;
            List<Chronoclock> clocks = database.GetClocks();
            foreach (Chronoclock clock in clocks)
            {
                ClockDict[clock.Identifier] = clock;
            }
            UpdateView();
        }

        private void RemoveClock(Chronoclock clock)
        {
            database.RemoveClocks([clock]);
            ClockDict.Remove(clock.Identifier);
            UpdateView();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Log.D("UI.Timing.ClockControl", "Window is closed.");
            theOne = null;
            foreach (ClockListItem clItem in clockListView.Items)
            {
                Chronoclock clock = clItem.GetUpdatedClock();
                database.UpdateClock(clock);
            }
            window.WindowFinalize(this);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ClockControl", "Close button clicked.");
            Close();
        }

        private void UpdateView()
        {
            Log.D("UI.Timing.ClockControl", "UpdateView");
            foreach (ClockListItem clItem in clockListView.Items)
            {
                Chronoclock clock = clItem.GetUpdatedClock();
                ClockDict[clock.Identifier] = clock;
            }
            clockListView.Items.Clear();
            foreach (Chronoclock clock in ClockDict.Values)
            {
                clockListView.Items.Add(new ClockListItem(clock, this, database.GetCurrentEvent()));
            }
        }

        private void UpdateTime(string time)
        {
            TimeLabel.Text = string.Format("Clock time is {0}", time);
            TimeLabel.Visibility = Visibility.Visible;
            CurrentTimeLabel.Text = string.Format("System time is {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            CurrentTimeLabel.Visibility = Visibility.Visible;
        }

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
                StackPanel mainPanel = new()
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                this.Content = mainPanel;
                StackPanel panelOne = new()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                mainPanel.Children.Add(panelOne);
                enabledSwitch = new()
                {
                    Height = 35,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsChecked = clock.Enabled,
                    Margin = new Thickness(5),
                };
                panelOne.Children.Add(enabledSwitch);
                nameBlock = new()
                {
                    Text = clock.Name,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 120,
                    Margin = new Thickness(5),
                };
                panelOne.Children.Add(nameBlock);
                urlBlock = new()
                {
                    Text = clock.URL,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 200,
                    Margin = new Thickness(5),
                };
                panelOne.Children.Add(urlBlock);
                lockedLabel = new()
                {
                    Text = "🔒",
                    VerticalAlignment = VerticalAlignment.Center,
                };
                panelOne.Children.Add(lockedLabel);
                lockedSwitch = new()
                {
                    Height = 35,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsChecked = false,
                    Margin = new Thickness(5),
                    IsEnabled = false
                };
                lockedSwitch.Checked += new RoutedEventHandler(this.LockedChanged);
                lockedSwitch.Unchecked += new RoutedEventHandler(this.LockedChanged);
                panelOne.Children.Add(lockedSwitch);
                panelOne.Children.Add(new Wpf.Ui.Controls.TextBlock()
                {
                    Text = "💡",
                    VerticalAlignment = VerticalAlignment.Center,
                });
                brightnessBox = new()
                {
                    Height = 35,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5),
                    IsEnabled = false
                };
                for (int i = 1; i <= 15; i++)
                {
                    brightnessBox.Items.Add(i.ToString());
                }
                brightnessBox.SelectionChanged += new SelectionChangedEventHandler(this.BrightnessChanged);
                panelOne.Children.Add(brightnessBox);
                Button delete = new()
                {
                    Content = "🗑",
                    Height = 35,
                    Margin = new Thickness(5),
                };
                delete.Click += new RoutedEventHandler((sender, e) =>
                {
                    Log.D("UI.Timing.ClockControl.ClockListItem", "Delete clicked.");
                    parent.RemoveClock(clock);
                });
                panelOne.Children.Add(delete);
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

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Chronoclock newClock = new()
            {
                Name = "New Clock",
                URL = "chronoclock.local",
                Enabled = false,
            };
            newClock.Identifier = database.AddClock(newClock);
            if (newClock.Identifier >= 0)
            {
                ClockDict[newClock.Identifier] = newClock;
            }
            UpdateView();
        }
    }
}
