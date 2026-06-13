using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Chronokeep.Constants;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;
using Chronokeep.UI.Timing.Windows;
using Chronokeep.UI.Util;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Chronokeep.UI.Parts;

public partial class ReaderPart : UserControl
{
    readonly ITimingPage parent;
    private List<TimingLocation> locations;
    public TimingSystem reader;

    public RewindWindow? rewind = null;

    [GeneratedRegex("^([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])$")]
    private static partial Regex IPPattern();
    [GeneratedRegex("[^0-9.]")]
    private static partial Regex AllowedChars();
    [GeneratedRegex("[^0-9]")]
    private static partial Regex AllowedNums();

    public ReaderPart(ITimingPage parent, TimingSystem sys, List<TimingLocation> locations)
    {
        this.parent = parent;
        this.locations = locations;
        InitializeComponent();
        reader = sys;
        ComboBoxItem? current, selected = null;
        foreach (string SYSTEM_IDVAL in Readers.SYSTEM_NAMES.Keys)
        {
            current = new ComboBoxItem()
            {
                Content = Readers.SYSTEM_NAMES[SYSTEM_IDVAL],
                Tag = SYSTEM_IDVAL
            };
            if (SYSTEM_IDVAL == reader.Type)
            {
                selected = current;
            }
            ReaderType.Items.Add(current);
        }
        if (selected != null)
        {
            ReaderType.SelectedItem = selected;
        }
        else
        {
            ReaderType.SelectedIndex = 0;
        }
        ReaderIP.Text = reader.IPAddress;
        ReaderPort.Text = reader.Port.ToString();
        selected = null;
        foreach (TimingLocation loc in this.locations)
        {
            current = new ComboBoxItem()
            {
                Content = loc.Name,
                Tag = loc.Identifier.ToString()
            };
            if (reader.LocationID == loc.Identifier)
            {
                selected = current;
            }
            ReaderLocation.Items.Add(current);
        }
        if (selected != null)
        {
            ReaderLocation.SelectedItem = selected;
        }
        else
        {
            ReaderLocation.SelectedIndex = 0;
        }
        if (reader.Saved())
        {
            RemoveButton.IsVisible = true;
        }
        else
        {
            RemoveButton.IsVisible = false;
        }
        UpdateStatus();
    }

    public void UpdateLocations(List<TimingLocation> locations)
    {
        this.locations = locations;
        int selectedLocation = Convert.ToInt32(((ComboBoxItem)ReaderLocation.SelectedItem!).Tag!);
        ReaderLocation.Items.Clear();
        ComboBoxItem? current, selected = null;
        foreach (TimingLocation loc in this.locations)
        {
            current = new()
            {
                Content = loc.Name,
                Tag = loc.Identifier.ToString()
            };
            if (selectedLocation == loc.Identifier)
            {
                selected = current;
            }
            ReaderLocation.Items.Add(current);
        }
        if (selected != null)
        {
            ReaderLocation.SelectedItem = selected;
        }
        else
        {
            ReaderLocation.SelectedIndex = 0;
        }
    }

    public void UpdateStatus()
    {
        if (reader.Status == SYSTEM_STATUS.CONNECTED)
        {
            SetConnected();
        }
        else if (reader.Status == SYSTEM_STATUS.DISCONNECTED)
        {
            SetDisconnected();
        }
        else
        {
            SetWorking();
        }
        ChangeReadingStatus(reader.SystemStatus);
    }

    public void UpdateReader()
    {
        // Check if IP is a valid IP address
        if (!IPPattern().IsMatch(ReaderIP.Text!.Trim()))
        {
            reader.IPAddress = "";
        }
        else
        {
            reader.IPAddress = ReaderIP.Text.Trim();
        }
        // Check if Port is valid.
        _ = int.TryParse(ReaderPort.Text!.Trim(), out int portNo);
        if (portNo > 65535)
        {
            portNo = -1;
        }
        reader.Port = portNo;
        reader.LocationID = Convert.ToInt32(((ComboBoxItem)ReaderLocation.SelectedItem!).Tag!);
        reader.LocationName = ((ComboBoxItem)ReaderLocation.SelectedItem).Content!.ToString()!;
    }

    private void SetConnected()
    {
        ReaderType.IsEnabled = false;
        ReaderIP.IsEnabled = false;
        ReaderPort.IsEnabled = false;
        ReaderLocation.IsEnabled = false;
        RemoveButton.IsEnabled = false;
        RemoveButton.Opacity = 0.2;
        if (reader.Type.Equals(Readers.SYSTEM_IPICO_LITE, StringComparison.OrdinalIgnoreCase))
        {
            RewindButton.IsEnabled = false;
            ClockButton.IsEnabled = false;
            SettingsButton.IsEnabled = false;
            RewindButton.Opacity = 0.2;
            ClockButton.Opacity = 0.2;
            SettingsButton.Opacity = 0.2;
        }
        else
        {
            RewindButton.IsEnabled = true;
            ClockButton.IsEnabled = true;
            RewindButton.Opacity = 1.0;
            ClockButton.Opacity = 1.0;
            if (reader.SystemInterface!.SettingsEditable())
            {
                SettingsButton.IsEnabled = true;
                ReaderButton.IsEnabled = true;
                SettingsButton.Opacity = 1.0;
                ReaderButton.Opacity = 1.0;
            }
            else
            {
                SettingsButton.IsEnabled = false;
                ReaderButton.IsEnabled = false;
                SettingsButton.Opacity = 0.2;
                ReaderButton.Opacity = 0.2;
            }
        }
        ConnectButton.IsEnabled = true;
        ConnectButton.Opacity = 1.0;
        Application.Current!.Resources.TryGetResource("stop_regular", null, out object? icon);
        ConnectButton.Content = new PathIcon()
        {
            Data = (StreamGeometry?)icon,
        };
        ConnectButton.Tag = "disconnect";
    }

    private void SetDisconnected()
    {
        ReaderType.IsEnabled = true;
        ReaderIP.IsEnabled = true;
        ReaderPort.IsEnabled = Readers.SYSTEM_CHRONOKEEP_PORTAL != reader.Type;
        ReaderLocation.IsEnabled = true;
        // Set Remove and Connect buttons to enabled
        RemoveButton.IsEnabled = true;
        ConnectButton.IsEnabled = true;
        RemoveButton.Opacity = 1.0;
        ConnectButton.Opacity = 1.0;
        // Set Clock and Rewind Buttons to disabled
        ClockButton.IsEnabled = false;
        RewindButton.IsEnabled = false;
        SettingsButton.IsEnabled = false;
        ReaderButton.IsEnabled = false;
        ClockButton.Opacity = 0.2;
        RewindButton.Opacity = 0.2;
        SettingsButton.Opacity = 0.2;
        ReaderButton.Opacity = 0.2;
        Application.Current!.Resources.TryGetResource("play_regular", null, out object? icon);
        ConnectButton.Content = new PathIcon()
        {
            Data = (StreamGeometry?)icon,
        };
        ConnectButton.Tag = "connect";
    }

    private void SetWorking()
    {
        ReaderType.IsEnabled = false;
        ReaderIP.IsEnabled = false;
        ReaderPort.IsEnabled = false;
        ReaderLocation.IsEnabled = false;
        ClockButton.IsEnabled = false;
        RewindButton.IsEnabled = false;
        ConnectButton.IsEnabled = false;
        RemoveButton.IsEnabled = false;
        SettingsButton.IsEnabled = false;
        ReaderButton.IsEnabled = false;
        ClockButton.Opacity = 0.2;
        RewindButton.Opacity = 0.2;
        ConnectButton.Opacity = 0.2;
        RemoveButton.Opacity = 0.2;
        SettingsButton.Opacity = 0.2;
        ReaderButton.Opacity = 0.2;
        ConnectButton.Tag = "working";
    }

    private void ChangeReadingStatus(string status)
    {
        if (status == TimingSystem.READING_STATUS_STOPPED)
        {
            ReaderButton.Foreground = new SolidColorBrush(Colors.Red);
        }
        else if (status == TimingSystem.READING_STATUS_READING)
        {
            ReaderButton.Foreground = new SolidColorBrush(Colors.LimeGreen);
        }
        else if (status == TimingSystem.READING_STATUS_PARTIAL)
        {
            ReaderButton.Foreground = new SolidColorBrush(Colors.Violet);
        }
        else
        {
            ReaderButton.Foreground = null;
        }
    }

    internal void UpdateSystemType(string type)
    {
        reader.UpdateSystemType(type);
        ReaderPort.Text = reader.Port.ToString();
    }

    private void ReaderType_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Log.D("UI.MainPages.TimingPage", "Reader type has changed.");
        string type = (string)((ComboBoxItem)ReaderType.SelectedItem!).Tag!;
        Log.D("UI.MainPages.TimingPage", "Updating to type: " + Readers.SYSTEM_NAMES[type]);
        reader.UpdateSystemType(type);
        ReaderPort.Text = reader.Port.ToString();
        ReaderPort.IsEnabled = Readers.SYSTEM_CHRONOKEEP_PORTAL != type;
    }

    private void SelectAll(object? sender, FocusChangedEventArgs e)
    {
        TextBox src = (TextBox)e.Source!;
        src.SelectAll();
    }

    private void IPValidation(object? sender, TextInputEventArgs e)
    {
        e.Handled = AllowedChars().IsMatch(e.Text!);
    }

    private void NumberValidation(object? sender, TextInputEventArgs e)
    {
        e.Handled = AllowedNums().IsMatch(e.Text!);
    }

    private void Rewind_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.TimingPage", "Settings button pressed. IP is " + ReaderIP.Text);
        parent.OpenRewindWindow(reader);
    }

    private void Clock_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.TimingPage", "Clock button pressed. IP is " + ReaderIP.Text);
        parent.OpenTimeWindow(reader);
    }

    private void Settings_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (reader == null || reader.SystemInterface == null)
        {
            return;
        }
        if (reader.SystemInterface.SettingsEditable())
        {
            reader.SystemInterface.OpenSettings();
        }
        else
        {
            DialogBox.Show("Settings not yet implemented.");
        }
    }

    private void Readers_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (reader == null || reader.SystemInterface == null)
        {
            return;
        }
        if (reader.SystemStatus == TimingSystem.READING_STATUS_READING
            || reader.SystemStatus == TimingSystem.READING_STATUS_PARTIAL)
        {
            reader.SystemInterface.StopReading();
        }
        else if (reader.SystemStatus == TimingSystem.READING_STATUS_STOPPED)
        {
            reader.SystemInterface.StartReading();
        }
        return;
    }

    private void Connect_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if ("connect" != (string)ConnectButton.Tag!)
        {
            Log.D("UI.MainPages.TimingPage", "Disconnect pressed.");
            reader.Status = SYSTEM_STATUS.WORKING;
            parent.DisconnectSystem(reader);
            UpdateStatus();
            reader.SystemInterface!.CloseSettings();
            return;
        }
        Log.D("UI.MainPages.TimingPage", "Connect button pressed. IP is " + ReaderIP.Text);
        // Check if IP is a valid IP address
        if (!IPPattern().IsMatch(ReaderIP.Text!.Trim()))
        {
            DialogBox.Show("IP address given not valid.");
            return;
        }
        reader.IPAddress = ReaderIP.Text.Trim();
        // Check if Port is valid.
        _ = int.TryParse(ReaderPort.Text!.Trim(), out int portNo);
        if (portNo < 0 || portNo > 65535)
        {
            DialogBox.Show("Port given not valid.");
            return;
        }
        reader.Port = portNo;
        reader.LocationID = Convert.ToInt32(((ComboBoxItem)ReaderLocation.SelectedItem!).Tag!);
        reader.LocationName = ((ComboBoxItem)ReaderLocation.SelectedItem).Content!.ToString()!;
        reader.Status = SYSTEM_STATUS.WORKING;
        parent.ConnectSystem(reader);
        UpdateStatus();
    }

    private void Remove_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.TimingPage", "Remove button for a timing system has been clicked.");
        if (reader.Saved())
        {
            parent.RemoveSystem(reader);
        }
        ;
    }
}