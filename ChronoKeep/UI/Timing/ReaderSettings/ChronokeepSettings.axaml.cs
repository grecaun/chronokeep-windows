using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Objects.ChronokeepPortal;
using Chronokeep.Timing.Interfaces;
using Chronokeep.UI.Parts;
using static Chronokeep.UI.API.RemoteReadersWindow;
using Button = Wpf.Ui.Controls.Button;

namespace Chronokeep.UI.Timing.ReaderSettings;

public partial class ChronokeepSettings : Window
{
    private readonly ChronokeepInterface reader = null;
    private readonly IDBInterface database = null;

    private bool saving = false;

    private Dictionary<long, ReaderListItem> readerDict = [];
    private Dictionary<long, APIListItem> apiDict = [];

    internal ChronokeepSettings(ChronokeepInterface reader, IDBInterface database)
    {
        InitializeComponent();
        this.MinWidth = 100;
        this.MinHeight = 100;
        this.reader = reader;
        this.database = database;
        reader.SendGetSettings();
    }

    internal void UpdateView(PortalSettingsHolder allSettings)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "UpdateView.");
        Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
        {
            if (saving)
            {
                this.Close();
            }
            sacrifice.Visibility = Visibility.Collapsed;
            settingsPanel.Visibility = Visibility.Visible;
            if (allSettings.Changes.Contains(PortalSettingsHolder.ChangeType.SETTINGS))
            {
                if (allSettings.PortalVersion != null && allSettings.PortalVersion.Length > 0)
                {
                    titleBar.Title = string.Format("v{0}", allSettings.PortalVersion.Trim());
                }

                nameBox.Text = allSettings.Name;
                readWindowBox.Text = allSettings.ReadWindow.ToString();
                chipTypeBox.SelectedIndex = allSettings.ChipType == PortalSettingsHolder.ChipTypeEnum.DEC ? 0 : 1;
                volumeSlider.Value = allSettings.Volume * 10;
                uploadSlider.Value = allSettings.UploadInterval;
                soundBox.IsChecked = allSettings.PlaySound;
                switch (allSettings.Voice)
                {
                    case PortalSettingsHolder.VoiceType.EMILY:
                        voiceBox.SelectedIndex = 0;
                        break;
                    case PortalSettingsHolder.VoiceType.MICHAEL:
                        voiceBox.SelectedIndex = 1;
                        break;
                    case PortalSettingsHolder.VoiceType.CUSTOM:
                        voiceBox.SelectedIndex = 2;
                        break;
                }
                ntfyUrlBox.Text = allSettings.NtfyURL;
                ntfyTopicBox.Text = allSettings.NtfyTopic;
                ntfyUserBox.Text = allSettings.NtfyUser;
                ntfyPassBox.Text = allSettings.NtfyPass;
                enableNTFYSwitch.IsChecked = allSettings.EnableNTFY;
                if (allSettings.ScreenType == Constants.Readers.CHRONOKEEP_SCREEN_ADAFRUIT)
                {
                    screenPanel.Visibility = Visibility.Visible;
                    screenBox.SelectedIndex = 0;
                }
                else if (allSettings.ScreenType == Constants.Readers.CHRONOKEEP_SCREEN_PCF8574T)
                {
                    screenPanel.Visibility = Visibility.Visible;
                    screenBox.SelectedIndex = 1;
                }
                else
                {
                    screenPanel.Visibility = Visibility.Collapsed;
                    screenBox.SelectedIndex = -1;
                }
            }
            // add readers and apis to views
            if (allSettings.Changes.Contains(PortalSettingsHolder.ChangeType.READERS))
            {
                // keep track of which readers we are already displaying
                HashSet<long> found = [];
                foreach (PortalReader read in allSettings.Readers)
                {
                    found.Add(read.Id);
                    // update if we know about them
                    if (readerDict.TryGetValue(read.Id, out ReaderListItem oReaderItem))
                    {
                        oReaderItem.UpdateReader(read);
                    }
                    // otherwise add new
                    else
                    {
                        readerDict[read.Id] = new(read, reader);
                    }
                }
                var newDictionary = readerDict.Where(pair => found.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
                readerDict = newDictionary;
                readerListView.Items.Clear();
                foreach (ReaderListItem item in readerDict.Values)
                {
                    readerListView.Items.Add(item);
                }
            }
            if (allSettings.Changes.Contains(PortalSettingsHolder.ChangeType.APIS))
            {
                // keep track of which apis we are already displaying
                HashSet<long> found = [];
                foreach (PortalAPI api in allSettings.APIs)
                {
                    found.Add(api.Id);
                    // update if we know about them
                    if (apiDict.TryGetValue(api.Id, out APIListItem oAPIItem))
                    {
                        oAPIItem.UpdateAPI(api);
                    }
                    else
                    {
                        apiDict[api.Id] = new(api, reader);
                    }
                }
                var newDictionary = apiDict.Where(pair => found.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
                apiDict = newDictionary;
                apiListView.Items.Clear();
                foreach (APIListItem item in apiDict.Values)
                {
                    apiListView.Items.Add(item);
                }
            }
            if (allSettings.Changes.Contains(PortalSettingsHolder.ChangeType.ANTENNAS))
            {
                Dictionary<string, ReaderListItem> readerNameDict = [];
                foreach (ReaderListItem reader in readerDict.Values)
                {
                    if (reader.GetReaderName().Equals(allSettings.Antennas.ReaderName, StringComparison.OrdinalIgnoreCase))
                    {
                        reader.UpdateAntennas(allSettings.Antennas.Antennas);
                        break;
                    }
                }

            }
            switch (allSettings.AutoUpload)
            {
                case PortalStatus.RUNNING:
                    autoResultsSwitch.IsEnabled = true;
                    autoResultsSwitch.IsChecked = true;
                    break;
                case PortalStatus.UNKNOWN:
                case PortalStatus.STOPPED:
                    autoResultsSwitch.IsEnabled = true;
                    autoResultsSwitch.IsChecked = false;
                    break;
                case PortalStatus.STOPPING:
                    autoResultsSwitch.IsEnabled = false;
                    autoResultsSwitch.IsChecked = true;
                    break;
            }
        }));
    }

    public void CloseWindow()
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "CloseWindow.");
        Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
        {
            Close();
        }));
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        reader.SettingsWindowFinalize();
    }

    private void VolumeSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (volumeSlider != null && volumeBlock != null)
        {
            volumeBlock.Text = volumeSlider.Value.ToString();
        }
    }

    private void ReaderExpander_Changed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Reader expander expanding/contracting.");
        if (readerExpander.IsExpanded)
        {
            addReaderButton.Visibility = Visibility.Visible;
        }
        else
        {
            addReaderButton.Visibility = Visibility.Collapsed;
        }
    }

    private void AddReaderButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Adding new reader.");
        reader.SendSaveReader(new()
        {
            Id = -1,
            Name = "New Reader",
            Kind = PortalReader.READER_KIND_ZEBRA,
            IPAddress = "192.168.1.0",
            Port = uint.Parse(PortalReader.READER_DEFAULT_PORT_ZEBRA),
            AutoConnect = true,
        });
    }

    private void APIExpander_Changed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "API expander expanding/contracting.");
        if (apiExpander.IsExpanded)
        {
            addAPIButton.Visibility = Visibility.Visible;
        }
        else
        {
            addAPIButton.Visibility = Visibility.Collapsed;
        }
    }

    private void AddAPIButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Add API button clicked.");
        reader.SendSaveApi(new()
        {
            Id = -1,
            Nickname = "New API",
            Kind = PortalAPI.API_TYPE_CHRONOKEEP_REMOTE,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            Uri = PortalAPI.API_URI_CHRONOKEEP_REMOTE,
        });
    }

    private void UploadSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (uploadSlider != null && uploadBlock != null)
        {
            uploadBlock.Text = uploadSlider.Value.ToString();
        }
    }

    private void ManualResultsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Manually uploading results.");
        reader.SendManualResultsUpload();
    }

    private void DeleteReadsButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "User requests deletion of reader chip reads.");
        DialogBox.Show("This will delete all of the chip reads from the reader.  This action is not reversible. Continue?", "Yes", "No", () =>
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Clearing chip reads from reader.");
            reader.SendDeleteAllReads();
        });
    }

    private void UpdateServerButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Update button clicked.");
        DialogBox.Show(
            "This will update the portal software. Do you want to proceed?",
            "Yes",
            "No",
            () =>
            {
                // send update command
                reader.SendUpdate();
                this.Close();
            }
            );
    }

    private void RestartServerButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Restart button clicked.");
        DialogBox.Show(
            "This will restart the portal software. Do you want to proceed?",
            "Yes",
            "No",
            () =>
            {
                // send restart command
                reader.SendRestart();
                this.Close();
            }
            );
    }

    private void StopServerButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Stop button clicked.");
        DialogBox.Show(
            "This will stop the portal software. Do you want to proceed?",
            "Yes",
            "No",
            () =>
            {
                // send stop command
                reader.SendQuit();
                this.Close();
            }
            );
    }

    private void ShutdownServerButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Shutdown button clicked.");
        DialogBox.Show(
            "This will shutdown the entire computer the portal software is running on. Do you want to proceed?",
            "Yes",
            "No",
            () =>
            {
                // send shutdown command
                reader.SendShutdown();
                this.Close();
            }
            );
    }

    private void SaveButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Save button clicked.");
        saving = true;
        try
        {
            PortalSettingsHolder sett = new()
            {
                Name = nameBox.Text.Trim(),
                ReadWindow = int.Parse(readWindowBox.Text.Trim()),
                ChipType = chipTypeBox.SelectedIndex == 0 ? PortalSettingsHolder.ChipTypeEnum.DEC
                    : PortalSettingsHolder.ChipTypeEnum.HEX,
                Volume = volumeSlider.Value / 10,
                UploadInterval = (int)uploadSlider.Value,
                PlaySound = soundBox.IsChecked == true,
                Voice = voiceBox.SelectedIndex == 0 ? PortalSettingsHolder.VoiceType.EMILY
                    : voiceBox.SelectedIndex == 1 ? PortalSettingsHolder.VoiceType.MICHAEL
                    : PortalSettingsHolder.VoiceType.CUSTOM,
                NtfyURL = ntfyUrlBox.Text.Trim(),
                NtfyTopic = ntfyTopicBox.Text.Trim(),
                NtfyUser = ntfyUserBox.Text.Trim(),
                NtfyPass = ntfyPassBox.Text.Trim(),
                EnableNTFY = enableNTFYSwitch.IsChecked == true,
                ScreenType = screenBox.SelectedItem != null ? ((ComboBoxItem)screenBox.SelectedItem).Uid : ""
            };
            reader.SendSetSettings(sett);
        }
        catch (Exception ex)
        {
            Log.E("UI.Timing.ReaderSettings.ChronokeepSettings", "Error saving settings: " + ex.Message);
            DialogBox.Show("Error saving settings.");
        }
    }

    private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Close button clicked.");
        this.Close();
    }

    private void AutoResultsSwitch_Checked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Auto upload switched.");
        if (autoResultsSwitch.IsEnabled == false)
        {
            return;
        }
        if (autoResultsSwitch.IsChecked == false)
        {
            reader.SendAutoUploadResults(Objects.ChronokeepPortal.Requests.AutoUploadQuery.STOP);
        }
        else
        {
            reader.SendAutoUploadResults(Objects.ChronokeepPortal.Requests.AutoUploadQuery.START);
        }
    }
}