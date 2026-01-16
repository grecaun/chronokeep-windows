using Avalonia.Controls;
using Chronokeep.Constants;
using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronokeepRemote;
using Chronokeep.UI.MainPages;
using Chronokeep.UI.Timing.ReaderSettings.Parts;
using Chronokeep.UI.Timing.Windows;

namespace Chronokeep.UI.Parts;

public partial class ReaderPart : UserControl
{
    readonly TimingPage parent;
    private List<TimingLocation> locations;
    public TimingSystem reader;

    public RewindWindow rewind = null;

    [GeneratedRegex("^([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])$")]
    private static partial Regex IPPattern();
    [GeneratedRegex("[^0-9.]")]
    private static partial Regex AllowedChars();
    [GeneratedRegex("[^0-9]")]
    private static partial Regex AllowedNums();

    public ReaderPart(TimingPage window, TimingSystem sys, List<TimingLocation> locations)
    {
        parent = window;
        this.locations = locations;
        reader = sys;
        ComboBoxItem current = null, selected = null;
        foreach (string SYSTEM_IDVAL in Readers.SYSTEM_NAMES.Keys)
        {
            current = new ComboBoxItem()
            {
                Content = Readers.SYSTEM_NAMES[SYSTEM_IDVAL],
                Uid = SYSTEM_IDVAL
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
        current = null; selected = null;
        foreach (TimingLocation loc in this.locations)
        {
            current = new ComboBoxItem()
            {
                Content = loc.Name,
                Uid = loc.Identifier.ToString()
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
        int selectedLocation = Convert.ToInt32(((ComboBoxItem)ReaderLocation.SelectedItem).Uid);
        ReaderLocation.Items.Clear();
        ComboBoxItem current, selected = null;
        foreach (TimingLocation loc in this.locations)
        {
            current = new()
            {
                Content = loc.Name,
                Uid = loc.Identifier.ToString()
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
        if (!IPPattern().IsMatch(ReaderIP.Text.Trim()))
        {
            reader.IPAddress = "";
        }
        else
        {
            reader.IPAddress = ReaderIP.Text.Trim();
        }
        // Check if Port is valid.
        int portNo = -1;
        int.TryParse(ReaderPort.Text.Trim(), out portNo);
        if (portNo > 65535)
        {
            portNo = -1;
        }
        reader.Port = portNo;
        reader.LocationID = Convert.ToInt32(((ComboBoxItem)ReaderLocation.SelectedItem).Uid);
        reader.LocationName = ((ComboBoxItem)ReaderLocation.SelectedItem).Content.ToString();
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
            if (reader.SystemInterface.SettingsEditable())
            {
                SettingsButton.IsEnabled = true;
                SettingsButton.Opacity = 1.0;
            }
            else
            {
                SettingsButton.IsEnabled = false;
                SettingsButton.Opacity = 0.2;
            }
        }
        ConnectButton.IsEnabled = true;
        ConnectButton.Opacity = 1.0;
        ConnectButton.Icon = new SymbolIcon() { Symbol = SymbolRegular.Stop24 };
        ConnectButton.Uid = "disconnect";
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
        ClockButton.Opacity = 0.2;
        RewindButton.Opacity = 0.2;
        SettingsButton.Opacity = 0.2;
        ConnectButton.Icon = new SymbolIcon() { Symbol = SymbolRegular.Play24 };
        ConnectButton.Uid = "connect";
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
        ClockButton.Opacity = 0.2;
        RewindButton.Opacity = 0.2;
        ConnectButton.Opacity = 0.2;
        RemoveButton.Opacity = 0.2;
        SettingsButton.Opacity = 0.2;
        ConnectButton.Icon = new SymbolIcon() { Symbol = SymbolRegular.CatchUp24 };
        ConnectButton.Uid = "working";
    }

    private void ChangeReadingStatus(string status)
    {
        if (status == TimingSystem.READING_STATUS_STOPPED)
        {
            SettingsButton.Foreground = new SolidColorBrush(Colors.Red);
        }
        else if (status == TimingSystem.READING_STATUS_READING)
        {
            SettingsButton.Foreground = new SolidColorBrush(Colors.LimeGreen);
        }
        else if (status == TimingSystem.READING_STATUS_PARTIAL)
        {
            SettingsButton.Foreground = new SolidColorBrush(Colors.Violet);
        }
        else
        {
            SettingsButton.SetResourceReference(ForegroundProperty, "TextFillColorPrimaryBrush");
        }
    }

    internal void UpdateSystemType(string type, IDBInterface database)
    {
        reader.UpdateSystemType(type);
        this.ReaderPort.Text = reader.Port.ToString();
    }

    private void ReaderType_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        Log.D("UI.MainPages.TimingPage", "Reader type has changed.");
        string type = ((ComboBoxItem)ReaderType.SelectedItem).Uid;
        Log.D("UI.MainPages.TimingPage", "Updating to type: " + Readers.SYSTEM_NAMES[type]);
        reader.UpdateSystemType(type);
        ReaderPort.Text = reader.Port.ToString();
        ReaderPort.IsEnabled = Readers.SYSTEM_CHRONOKEEP_PORTAL != type;
    }

    private void SelectAll(object? sender, Avalonia.Input.GotFocusEventArgs e)
    {
        TextBox src = (TextBox)e.OriginalSource;
        src.SelectAll();
    }

    private void IPValidation(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
        e.Handled = AllowedChars().IsMatch(e.Text);
    }

    private void NumberValidation(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
        e.Handled = AllowedNums().IsMatch(e.Text);
    }

    private void Rewind(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.TimingPage", "Settings button pressed. IP is " + ReaderIP.Text);
        parent.OpenRewindWindow(reader);
    }

    private void Clock(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.TimingPage", "Clock button pressed. IP is " + ReaderIP.Text);
        parent.OpenTimeWindow(reader);
    }

    private void Settings(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (reader == null || reader.SystemInterface == null)
        {
            return;
        }
        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control
            || (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
        {
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
        if (reader.SystemInterface.SettingsEditable())
        {
            reader.SystemInterface.OpenSettings();
        }
        else
        {
            DialogBox.Show("Settings not yet implemented.");
        }
    }

    private void Connect(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if ("connect" != ConnectButton.Uid)
        {
            Log.D("UI.MainPages.TimingPage", "Disconnect pressed.");
            reader.Status = SYSTEM_STATUS.WORKING;
            parent.DisconnectSystem(reader);
            UpdateStatus();
            reader.SystemInterface.CloseSettings();
            return;
        }
        Log.D("UI.MainPages.TimingPage", "Connect button pressed. IP is " + ReaderIP.Text);
        // Check if IP is a valid IP address
        if (!IPPattern().IsMatch(ReaderIP.Text.Trim()))
        {
            DialogBox.Show("IP address given not valid.");
            return;
        }
        reader.IPAddress = ReaderIP.Text.Trim();
        // Check if Port is valid.
        int portNo = -1;
        int.TryParse(ReaderPort.Text.Trim(), out portNo);
        if (portNo < 0 || portNo > 65535)
        {
            DialogBox.Show("Port given not valid.");
            return;
        }
        reader.Port = portNo;
        reader.LocationID = Convert.ToInt32(((ComboBoxItem)ReaderLocation.SelectedItem).Uid);
        reader.LocationName = ((ComboBoxItem)ReaderLocation.SelectedItem).Content.ToString();
        reader.Status = SYSTEM_STATUS.WORKING;
        parent.ConnectSystem(reader);
        UpdateStatus();
    }

    private void Remove(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.TimingPage", "Remove button for a timing system has been clicked.");
        if (reader.Saved())
        {
            parent.RemoveSystem(reader);
        }
        ;
    }
}