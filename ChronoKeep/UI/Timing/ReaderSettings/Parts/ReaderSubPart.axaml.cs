using Avalonia.Controls;
using Chronokeep.Helpers;
using Chronokeep.Objects.ChronokeepPortal;
using Chronokeep.Timing.Interfaces;
using Chronokeep.UI.Parts;
using System.Text.RegularExpressions;

namespace Chronokeep.UI.Timing.ReaderSettings.Parts;

public partial class ReaderSubPart : UserControl
{
    private PortalReader reader;
    private readonly ChronokeepInterface readerInterface;

    [GeneratedRegex("^([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])$")]
    private static partial Regex IPPattern();
    [GeneratedRegex("[^0-9.]")]
    private static partial Regex AllowedChars();
    [GeneratedRegex("[^0-9]")]
    private static partial Regex AllowedNums();

    public ReaderSubPart(PortalReader reader, ChronokeepInterface readerInterface)
    {
        InitializeComponent();
        this.reader = reader;
        this.readerInterface = readerInterface;
        nameBox.Text = reader.Name;
        kindBox.SelectedIndex = reader.Kind.Equals(PortalReader.READER_KIND_ZEBRA) ? 0
                    //: reader.Kind.Equals(PortalReader.READER_KIND_IMPINJ) ? 1 
                    //: reader.Kind.Equals(PortalReader.READER_KIND_RFID) ? 2 
                    : -1;
        ipBox.Text = reader.IPAddress;
        portBox.Text = reader.Port.ToString();
        autoConnectSwitch.IsChecked = reader.AutoConnect;
        connectedSwitch.IsChecked = reader.Connected;
        if (reader.Antennas != null)
        {
            for (int ix = 0; ix < reader.Antennas.Length; ix++)
            {
                // TODO -- update border background with correct coloring
                if (reader.Antennas[ix] != Constants.Readers.CHRONOKEEP_ANTENNA_STATUS_NONE)
                {
                    antennaPanel.Children.Add(new Border()
                    {
                        Child = new TextBlock()
                        {
                            Text = (ix + 1).ToString(),
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        },
                        Width = 30,
                        Height = 30,
                        CornerRadius = Avalonia.CornerRadius.Parse("15"),
                    });
                }
            }
        }
    }

    public string GetReaderName()
    {
        return reader.Name;
    }

    public void UpdateAntennas(int[] antennas)
    {
        reader.Antennas = antennas;
        antennaPanel.Children.Clear();
        for (int ix = 0; ix < reader.Antennas.Length; ix++)
        {
            if (reader.Antennas[ix] != Constants.Readers.CHRONOKEEP_ANTENNA_STATUS_NONE)
            {
                antennaPanel.Children.Add(new Border()
                {
                    Child = new TextBlock()
                    {
                        Text = (ix + 1).ToString(),
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    },
                    Width = 30,
                    Height = 30,
                    CornerRadius = Avalonia.CornerRadius.Parse("15"),
                });
            }
        }
    }

    public void UpdateReader(PortalReader reader)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Updating reader " + reader.Id);
        this.reader = reader;
        this.nameBox.Text = reader.Name;
        switch (reader.Kind)
        {
            case PortalReader.READER_KIND_ZEBRA:
                this.kindBox.SelectedIndex = 0;
                break;
            /*case PortalReader.READER_KIND_IMPINJ:
                this.kindBox.SelectedIndex = 1;
                break;
            case PortalReader.READER_KIND_RFID:
                this.kindBox.SelectedIndex = 2;
                break;//*/
            default:
                this.kindBox.SelectedIndex = -1;
                break;
        }
        this.ipBox.Text = reader.IPAddress;
        this.portBox.Text = reader.Port.ToString();
        autoConnectSwitch.IsChecked = reader.AutoConnect;
        connectedSwitch.IsChecked = reader.Connected;
        connectedSwitch.IsEnabled = true;
        antennaPanel.Children.Clear();
        for (int ix = 0; ix < reader.Antennas.Length; ix++)
        {
            if (this.reader.Antennas[ix] != Constants.Readers.CHRONOKEEP_ANTENNA_STATUS_NONE)
            {
                antennaPanel.Children.Add(new Border()
                {
                    Child = new TextBlock()
                    {
                        Text = (ix + 1).ToString(),
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    },
                    Width = 30,
                    Height = 30,
                    CornerRadius = Avalonia.CornerRadius.Parse("15"),
                });
            }
        }
    }

    private void ConnectReader(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Connecting/disconnecting reader " + reader.Id);
        if (reader.Connected)
        {
            readerInterface.SendStopReader(reader);
        }
        else
        {
            readerInterface.SendStartReader(reader);
        }
        connectedSwitch.IsEnabled = false;
    }

    private void KindBox_ValueChanged(object? sender, SelectionChangedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Changing port for reader " + reader.Id);
        switch (kindBox.SelectedIndex)
        {
            case 0:
                portBox.Text = PortalReader.READER_DEFAULT_PORT_ZEBRA;
                break;
            /*case 1:
                portBox.Text = PortalReader.READER_DEFAULT_PORT_IMPINJ;
                break;
            case 2:
                portBox.Text = PortalReader.READER_DEFAULT_PORT_RFID;
                break;//*/
            default:
                portBox.Text = "";
                return;
        }
    }

    private void IPValidation(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
        e.Handled = AllowedChars().IsMatch(e.Text);
    }

    private void NumberValidation(object? sender, Avalonia.Input.TextInputEventArgs e)
    {
        e.Handled = AllowedNums().IsMatch(e.Text);
    }

    private void DeleteReader(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Deleting reader " + reader.Id);
        readerInterface.SendRemoveReader(reader);
    }

    private void SaveReader(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Saving reader " + reader.Id);
        reader.Name = nameBox.Text.Trim();
        switch (kindBox.SelectedIndex)
        {
            case 0:
                reader.Kind = PortalReader.READER_KIND_ZEBRA;
                break;
            /*case 1:
                reader.Kind = PortalReader.READER_KIND_IMPINJ;
                break;
            case 2:
                reader.Kind = PortalReader.READER_KIND_RFID;
                break;//*/
            default:
                DialogBox.Show("Unknown kind specified. Unable to save.");
                return;
        }
        if (!IPPattern().IsMatch(ipBox.Text.Trim()))
        {
            reader.IPAddress = "";
        }
        else
        {
            reader.IPAddress = ipBox.Text.Trim();
        }
        uint portNo = 0;
        uint.TryParse(portBox.Text.Trim(), out portNo);
        if (portNo > 65535)
        {
            portNo = 0;
        }
        reader.Port = portNo;
        reader.AutoConnect = autoConnectSwitch.IsChecked == true;

        readerInterface.SendSaveReader(reader);
    }
}