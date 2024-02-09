using Chronokeep.Objects.ChronokeepPortal;
using Chronokeep.Timing.Interfaces;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace Chronokeep.UI.Timing.ReaderSettings
{
    /// <summary>
    /// Interaction logic for ChronokeepSettings.xaml
    /// </summary>
    public partial class ChronokeepSettings : UiWindow
    {
        private ChronokeepInterface reader = null;
        private IDBInterface database = null;

        private bool saving = false;

        private Dictionary<long, ReaderListItem> readerDict = new Dictionary<long, ReaderListItem>();
        private Dictionary<long, APIListItem> apiDict = new Dictionary<long, APIListItem>();

        internal ChronokeepSettings(ChronokeepInterface reader, IDBInterface database)
        {
            InitializeComponent();
            this.MinWidth = 100;
            this.MinHeight = 100;
            this.reader = reader;
            this.database = database;
            reader.SendGetSettings();
        }

        private void uploadParticipantsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Upload participants button clicked.");
            Event theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            try
            {
                List<Participant> participants = database.GetParticipants(theEvent.Identifier);
                List<PortalParticipant> uploadParticipants = new List<PortalParticipant>();
                foreach (Participant participant in participants)
                {
                    uploadParticipants.Add(new PortalParticipant
                    {
                        Bib = participant.Bib,
                        First = participant.FirstName,
                        Last = participant.LastName,
                        Age = participant.GetAge(theEvent.Date),
                        Gender = participant.Gender,
                        AgeGroup = participant.EventSpecific.AgeGroupName,
                        Distance = participant.Distance,
                        Chip = participant.Chip,
                        Anonymous = participant.Anonymous,
                    });
                }
                reader.SendUploadParticipants(uploadParticipants);
                DialogBox.Show("Participants successfully uploaded.");
            }
            catch (Exception ex)
            {
                Log.E("UI.Timing.ReaderSettings.ChronokeepSettings", string.Format("something went wrong trying to upload participants: " + ex.Message));
                DialogBox.Show("Something went wrong uploading participants.");
            }
        }

        private void removeParticipantsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Remove participants button clicked.");
            DialogBox.Show(
                "This is not reversable, are you sure you want to do this?",
                "Yes",
                "No",
                () =>
                {
                    reader.SendRemoveParticipants();
                }
                );
        }

        private void stopServerButton_Click(object sender, RoutedEventArgs e)
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

        private void shutdownServerButton_Click(object sender, RoutedEventArgs e)
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

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Save button clicked.");
            saving = true;
            int window = 0;
            string[] split = sightingPeriodBox.Text.Split(':');
            if (split.Length > 0)
            {
                switch (split.Length)
                {
                    case 1:
                        int.TryParse(split[0], out window);
                        break;
                    case 2:
                        if (split[1].Length == 2)
                        {
                            int minutes, seconds;
                            if (int.TryParse(split[0], out minutes) &&
                                int.TryParse(split[1], out seconds)) {
                                window = seconds + (minutes * 60);
                            }
                        }
                        break;
                    case 3:
                        if (split[1].Length == 2 && split[2].Length == 2)
                        {
                            int hours, minutes, seconds;
                            if (int.TryParse(split[0], out hours) &&
                                int.TryParse(split[1], out minutes) &&
                                int.TryParse(split[2], out seconds))
                            {
                                window = seconds + (minutes * 60) + (hours * 3600);
                            }
                        }
                        break;
                }
            }
            try
            {
                PortalSettingsHolder sett = new PortalSettingsHolder
                {
                    Name = nameBox.Text.Trim(),
                    SightingPeriod = window,
                    ReadWindow = int.Parse(readWindowBox.Text.Trim()),
                    ChipType = chipTypeBox.SelectedIndex == 0 ? PortalSettingsHolder.ChipTypeEnum.DEC
                        : PortalSettingsHolder.ChipTypeEnum.HEX,
                    Volume = volumeSlider.Value / 10,
                    PlaySound = soundBox.IsChecked == true,
                    Voice = voiceBox.SelectedIndex == 0 ? PortalSettingsHolder.VoiceType.EMILY
                        : voiceBox.SelectedIndex == 1 ? PortalSettingsHolder.VoiceType.MICHAEL
                        : PortalSettingsHolder.VoiceType.CUSTOM,
                };
                reader.SendSetSettings(sett);
            }
            catch (Exception ex)
            {
                Log.E("UI.Timing.ReaderSettings.ChronokeepSettings", "Error saving settings: " + ex.Message);
                DialogBox.Show("Error saving settings.");
            }
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Close button clicked.");
            this.Close();
        }

        private void addAPIButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Add API button clicked.");
            reader.SendSaveApi(new PortalAPI
            {
                Id = -1,
                Nickname = "New API",
                Kind = PortalAPI.API_TYPE_CHRONOKEEP_RESULTS,
                Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                Uri = PortalAPI.API_URI_CHRONOKEEP_RESULTS,
            });
        }

        internal void UpdateView(PortalSettingsHolder allSettings, bool settings, bool readers, bool apis)
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
                if (settings)
                {
                    nameBox.Text = allSettings.Name;
                    if (allSettings.SightingPeriod > 3600)
                    {
                        sightingPeriodBox.Text = Constants.Timing.SecondsToTime(allSettings.SightingPeriod);
                    }
                    else
                    {
                        sightingPeriodBox.Text = string.Format("{0}:{1:D2}", allSettings.SightingPeriod / 60, allSettings.SightingPeriod % 60);
                    }
                    readWindowBox.Text = allSettings.ReadWindow.ToString();
                    chipTypeBox.SelectedIndex = allSettings.ChipType == PortalSettingsHolder.ChipTypeEnum.DEC ? 0 : 1;
                    volumeSlider.Value = allSettings.Volume * 10;
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
                }
                // add readers and apis to views
                if (readers)
                {
                    // keep track of which readers we are already displaying
                    HashSet<long> found = new HashSet<long>();
                    foreach (PortalReader read in allSettings.Readers)
                    {
                        found.Add(read.Id);
                        // update if we know about them
                        if (readerDict.ContainsKey(read.Id))
                        {
                            readerDict[read.Id].UpdateReader(read);
                        }
                        // otherwise add new
                        else
                        {
                            readerDict[read.Id] = new ReaderListItem(read, reader);
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
                if (apis)
                {
                    // keep track of which apis we are already displaying
                    HashSet<long> found = new HashSet<long>();
                    foreach (PortalAPI api in allSettings.APIs)
                    {
                        found.Add(api.Id);
                        // update if we know about them
                        if (apiDict.ContainsKey(api.Id))
                        {
                            apiDict[api.Id].UpdateAPI(api);
                        }
                        else
                        {
                            apiDict[api.Id] = new APIListItem(api, reader);
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

        private void UiWindow_Closed(object sender, EventArgs e)
        {
            reader.SettingsWindowFinalize();
        }

        private void ReaderExpander_Changed(object sender, RoutedEventArgs e)
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

        private void APIExpander_Changed(object sender, RoutedEventArgs e)
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

        private void addReaderButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Adding new reader.");
            reader.SendSaveReader(new PortalReader()
            {
                Id = -1,
                Name = "New Reader",
                Kind = PortalReader.READER_KIND_ZEBRA,
                IPAddress = "192.168.1.0",
                Port = uint.Parse(PortalReader.READER_DEFAULT_PORT_ZEBRA),
                AutoConnect = true,
            });
        }

        private void manualResultsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Manually uploading results.");
            reader.SendManualResultsUpload();
        }

        private void autoResultsSwitch_Checked(object sender, RoutedEventArgs e)
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

        private class ReaderListItem : ListViewItem
        {
            private PortalReader reader;
            private ChronokeepInterface readerInterface;

            private const string IPPattern = "^([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\\.([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])$";
            private const string allowedChars = "[^0-9.]";
            private const string allowedNums = "[^0-9]";

            private System.Windows.Controls.TextBox nameBox;
            private ComboBox kindBox;
            private System.Windows.Controls.TextBox ipBox;
            private System.Windows.Controls.TextBox portBox;
            private ToggleSwitch autoConnectSwitch;
            private ToggleSwitch connectedSwitch;
            private ToggleSwitch readingSwitch;
            private Button saveReaderButton;
            private Button removeReaderButton;

            public ReaderListItem(PortalReader reader, ChronokeepInterface readerInterface)
            {
                this.reader = reader;
                this.readerInterface = readerInterface;
                StackPanel thePanel = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                this.Content = thePanel;
                StackPanel subPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                thePanel.Children.Add(subPanel);
                nameBox = new System.Windows.Controls.TextBox()
                {
                    Text = reader.Name,
                    Width = 190,
                    Margin = new Thickness(5)
                };
                subPanel.Children.Add(nameBox);
                kindBox = new ComboBox()
                {
                    Width = 120,
                    Margin = new Thickness(5),
                };
                kindBox.Items.Add(new ComboBoxItem()
                {
                    Content = "Zebra"
                });
                /*kindBox.Items.Add(new ComboBoxItem()
                {
                    Content = "Impinj"
                });
                kindBox.Items.Add(new ComboBoxItem()
                {
                    Content = "RFID"
                });//*/
                kindBox.SelectedIndex = reader.Kind.Equals(PortalReader.READER_KIND_ZEBRA) ? 0
                    //: reader.Kind.Equals(PortalReader.READER_KIND_IMPINJ) ? 1 
                    //: reader.Kind.Equals(PortalReader.READER_KIND_RFID) ? 2 
                    : -1;
                kindBox.SelectionChanged += new SelectionChangedEventHandler(this.UpdateReaderPort);
                subPanel.Children.Add(kindBox);
                subPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                thePanel.Children.Add(subPanel);
                ipBox = new System.Windows.Controls.TextBox()
                {
                    Text = reader.IPAddress,
                    Width = 120,
                    Margin = new Thickness(5),
                };
                ipBox.PreviewTextInput += new TextCompositionEventHandler(this.IPValidation);
                subPanel.Children.Add(ipBox);
                portBox = new System.Windows.Controls.TextBox()
                {
                    Text = reader.Port.ToString(),
                    Width = 60,
                    Margin = new Thickness(5),
                };
                portBox.PreviewTextInput += new TextCompositionEventHandler(this.NumberValidation);
                subPanel.Children.Add(portBox);
                autoConnectSwitch = new ToggleSwitch()
                {
                    IsChecked = reader.AutoConnect,
                    Content = "Auto Connect",
                    Width = 120,
                    Margin = new Thickness(5),
                    FontSize = 10,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                subPanel.Children.Add(autoConnectSwitch);
                subPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                thePanel.Children.Add(subPanel);
                connectedSwitch = new ToggleSwitch()
                {
                    IsChecked = reader.Connected,
                    Content = "Connected",
                    Width = 115,
                    Margin = new Thickness(5),
                    FontSize = 10,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                connectedSwitch.Click += new RoutedEventHandler(this.ConnectReader);
                subPanel.Children.Add(connectedSwitch);
                readingSwitch = new ToggleSwitch()
                {
                    IsChecked = reader.Reading,
                    IsEnabled = reader.Connected,
                    Content = "Started",
                    Width = 95,
                    Margin = new Thickness(5),
                    FontSize = 10,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                readingSwitch.Click += new RoutedEventHandler(this.StartReader);
                subPanel.Children.Add(readingSwitch);
                saveReaderButton = new Button()
                {
                    Icon = Wpf.Ui.Common.SymbolRegular.Save20,
                    Margin = new Thickness(5),
                    Width = 40,
                };
                saveReaderButton.Click += new RoutedEventHandler(this.SaveReader);
                subPanel.Children.Add(saveReaderButton);
                removeReaderButton = new Button()
                {
                    Icon = Wpf.Ui.Common.SymbolRegular.Delete20,
                    Margin = new Thickness(5),
                    Width = 40,
                };
                removeReaderButton.Click += new RoutedEventHandler(this.DeleteReader);
                subPanel.Children.Add(removeReaderButton);
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
                readingSwitch.IsChecked = reader.Reading && reader.Connected;
                connectedSwitch.IsEnabled = true;
                readingSwitch.IsEnabled = reader.Connected;
            }

            private void UpdateReaderPort(object sender, RoutedEventArgs e)
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

            private void ConnectReader(object sender, RoutedEventArgs e)
            {
                Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Connecting/disconnecting reader " + reader.Id);
                if (reader.Connected)
                {
                    readerInterface.SendDisconnectReader(reader);
                }
                else
                {
                    readerInterface.SendConnectReader(reader);
                }
                connectedSwitch.IsEnabled = false;
                readingSwitch.IsEnabled = false;
            }

            private void StartReader(object sender, RoutedEventArgs e)
            {
                Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Stopping/starting reader " + reader.Id);
                if (reader.Reading)
                {
                    readerInterface.SendStopReader(reader);
                }
                else
                {
                    readerInterface.SendStartReader(reader);
                }
                connectedSwitch.IsEnabled = false;
                readingSwitch.IsEnabled = false;

            }

            private void SaveReader(object sender, RoutedEventArgs e)
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
                if (!Regex.IsMatch(ipBox.Text.Trim(), IPPattern))
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

            private void DeleteReader(object sender, RoutedEventArgs e)
            {
                Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Deleting reader " + reader.Id);
                readerInterface.SendRemoveReader(reader);
            }

            private void IPValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = Regex.IsMatch(e.Text, allowedChars);
            }

            private void NumberValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = Regex.IsMatch(e.Text, allowedNums);
            }
        }

        private class APIListItem : ListViewItem
        {
            private PortalAPI api = null;
            private ChronokeepInterface reader = null;

            private System.Windows.Controls.TextBox nameBox;
            private ComboBox kindBox;
            private System.Windows.Controls.TextBox tokenBox;
            private System.Windows.Controls.TextBox uriBox;

            private Button saveAPIButton;
            private Button removeAPIButton;

            public APIListItem(PortalAPI api, ChronokeepInterface reader)
            {
                this.api = api;
                this.reader = reader;
                StackPanel thePanel = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                this.Content = thePanel;
                StackPanel subPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                thePanel.Children.Add(subPanel);
                nameBox = new System.Windows.Controls.TextBox()
                {
                    Text = api.Nickname,
                    Width = 170,
                    Margin = new Thickness(5)
                };
                subPanel.Children.Add(nameBox);
                kindBox = new ComboBox()
                {
                    Width = 140,
                    Margin = new Thickness(5),
                };
                kindBox.Items.Add(new ComboBoxItem()
                {
                    Content = "Remote",
                    Uid = PortalAPI.API_TYPE_CHRONOKEEP_REMOTE
                });
                kindBox.Items.Add(new ComboBoxItem()
                {
                    Content = "Remote Self",
                    Uid = PortalAPI.API_TYPE_CHRONOKEEP_REMOTE_SELF
                });
                kindBox.Items.Add(new ComboBoxItem()
                {
                    Content = "Results",
                    Uid = PortalAPI.API_TYPE_CHRONOKEEP_RESULTS
                });
                kindBox.Items.Add(new ComboBoxItem()
                {
                    Content = "Results Self",
                    Uid = PortalAPI.API_TYPE_CHRONOKEEP_RESULTS_SELF
                });
                switch (api.Kind)
                {
                    case PortalAPI.API_TYPE_CHRONOKEEP_REMOTE:
                        kindBox.SelectedIndex = 0;
                        break;
                    case PortalAPI.API_TYPE_CHRONOKEEP_REMOTE_SELF:
                        kindBox.SelectedIndex = 1;
                        break;
                    case PortalAPI.API_TYPE_CHRONOKEEP_RESULTS:
                        kindBox.SelectedIndex = 2;
                        break;
                    case PortalAPI.API_TYPE_CHRONOKEEP_RESULTS_SELF:
                        kindBox.SelectedIndex = 3;
                        break;
                    default:
                        kindBox.SelectedIndex = 0;
                        break;
                }
                kindBox.SelectionChanged += new SelectionChangedEventHandler(this.UpdateURI);
                subPanel.Children.Add(kindBox);
                subPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                thePanel.Children.Add(subPanel);
                tokenBox = new System.Windows.Controls.TextBox()
                {
                    Text = api.Token,
                    Width = 320,
                    Margin = new Thickness(5),
                };
                subPanel.Children.Add(tokenBox);
                subPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                thePanel.Children.Add(subPanel);
                uriBox = new System.Windows.Controls.TextBox()
                {
                    Text = api.Uri,
                    Width = 220,
                    Margin = new Thickness(5),
                };
                subPanel.Children.Add(uriBox);
                saveAPIButton = new Button()
                {
                    Icon = Wpf.Ui.Common.SymbolRegular.Save20,
                    Margin = new Thickness(5),
                    Width = 40,
                    Height = 35,
                };
                saveAPIButton.Click += new RoutedEventHandler(this.SaveAPI);
                subPanel.Children.Add(saveAPIButton);
                removeAPIButton = new Button()
                {
                    Icon = Wpf.Ui.Common.SymbolRegular.Delete20,
                    Margin = new Thickness(5),
                    Width = 40,
                    Height = 35,
                };
                removeAPIButton.Click += new RoutedEventHandler(this.DeleteAPI);
                subPanel.Children.Add(removeAPIButton);
                PrivateUpdateURI();
            }

            public void UpdateAPI(PortalAPI api)
            {
                this.api = api;
                nameBox.Text = api.Nickname;
                switch (api.Kind)
                {
                    case PortalAPI.API_TYPE_CHRONOKEEP_REMOTE:
                        kindBox.SelectedIndex = 0;
                        break;
                    case PortalAPI.API_TYPE_CHRONOKEEP_REMOTE_SELF:
                        kindBox.SelectedIndex = 1;
                        break;
                    case PortalAPI.API_TYPE_CHRONOKEEP_RESULTS:
                        kindBox.SelectedIndex = 2;
                        break;
                    case PortalAPI.API_TYPE_CHRONOKEEP_RESULTS_SELF:
                        kindBox.SelectedIndex = 3;
                        break;
                    default:
                        kindBox.SelectedIndex = 0;
                        break;
                }
                tokenBox.Text = api.Token;
                uriBox.Text = api.Uri;
                PrivateUpdateURI();
            }

            public void PrivateUpdateURI()
            {
                switch (((ComboBoxItem)kindBox.SelectedItem).Uid)
                {
                    case PortalAPI.API_TYPE_CHRONOKEEP_REMOTE:
                        uriBox.Visibility = Visibility.Collapsed;
                        uriBox.Text = PortalAPI.API_URI_CHRONOKEEP_REMOTE;
                        break;
                    case PortalAPI.API_TYPE_CHRONOKEEP_RESULTS:
                        uriBox.Visibility = Visibility.Collapsed;
                        uriBox.Text = PortalAPI.API_URI_CHRONOKEEP_RESULTS;
                        break;
                    case PortalAPI.API_TYPE_CHRONOKEEP_REMOTE_SELF:
                    case PortalAPI.API_TYPE_CHRONOKEEP_RESULTS_SELF:
                    default:
                        uriBox.Visibility = Visibility.Visible;
                        uriBox.Text = api.Uri;
                        break;
                }
            }

            private void UpdateURI(object sender, SelectionChangedEventArgs e)
            {
                Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Selected type changed.");
                PrivateUpdateURI();
            }

            private void SaveAPI(object sender, RoutedEventArgs e)
            {
                Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Saving api " + api.Id);
                api.Nickname = nameBox.Text.Trim();
                api.Token = tokenBox.Text.Trim();
                api.Uri = uriBox.Text.Trim();
                api.Kind = ((ComboBoxItem)kindBox.SelectedItem).Uid;
                reader.SendSaveApi(api);
            }

            private void DeleteAPI(object sender, RoutedEventArgs e)
            {
                Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Deleting api " + api.Id);
                reader.SendDeleteApi(api);
            }
        }
    }
}
