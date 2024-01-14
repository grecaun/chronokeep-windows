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

        private Dictionary<long, ReaderListItem> listItemDict = new Dictionary<long, ReaderListItem>();

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
                AllPortalSettings sett = new AllPortalSettings
                {
                    Name = nameBox.Text.Trim(),
                    SightingPeriod = window,
                    ReadWindow = int.Parse(readWindowBox.Text.Trim()),
                    ChipType = chipTypeBox.SelectedIndex == 0 ? AllPortalSettings.ChipTypeEnum.DEC : AllPortalSettings.ChipTypeEnum.HEX,
                    Volume = volumeSlider.Value / 10,
                    PlaySound = soundBox.IsChecked == true,
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

        internal void UpdateView(AllPortalSettings allSettings, bool settings, bool readers, bool apis)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "UpdateView.");
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                if (saving)
                {
                    this.Close();
                }
                settingsPanel.Visibility = Visibility.Visible;
                loadingPanel.Visibility = Visibility.Collapsed;
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
                    chipTypeBox.SelectedIndex = allSettings.ChipType == AllPortalSettings.ChipTypeEnum.DEC ? 0 : 1;
                    volumeSlider.Value = allSettings.Volume * 10;
                    soundBox.IsChecked = allSettings.PlaySound;
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
                        if (listItemDict.ContainsKey(read.Id))
                        {
                            listItemDict[read.Id].UpdateReader(read);
                        }
                        else
                        {
                            listItemDict[read.Id] = new ReaderListItem(read, reader);
                        }
                    }
                    var newDictionary = listItemDict.Where(pair => found.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
                    listItemDict = newDictionary;
                    readerListView.Items.Clear();
                    foreach (ReaderListItem item in listItemDict.Values)
                    {
                        readerListView.Items.Add(item);
                    }
                }
                if (apis)
                {

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
                connectedSwitch.Checked += new RoutedEventHandler(this.ConnectReader);
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
                readingSwitch.Checked += new RoutedEventHandler(this.StartReader);
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
                Log.D("UI.Timing.ReaderSettings.ChronokeepSettings.ReaderListItem", "Updating reader " + reader.Id);
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
                readingSwitch.IsChecked = reader.Reading;
                readingSwitch.IsEnabled = reader.Connected;
            }

            private void UpdateReaderPort(object sender, RoutedEventArgs e)
            {
                Log.D("UI.Timing.ReaderSettings.ChronokeepSettings.ReaderListItem", "Changing port for reader " + reader.Id);
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
                Log.D("UI.Timing.ReaderSettings.ChronokeepSettings.ReaderListItem", "Connecting/disconnecting reader " + reader.Id);
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
                Log.D("UI.Timing.ReaderSettings.ChronokeepSettings.ReaderListItem", "Stopping/starting reader " + reader.Id);
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
                Log.D("UI.Timing.ReaderSettings.ChronokeepSettings.ReaderListItem", "Saving reader " + reader.Id);
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
                Log.D("UI.Timing.ReaderSettings.ChronokeepSettings.ReaderListItem", "Deleting reader " + reader.Id);
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

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings.ReaderListItem", "Expander expanding/contracting.");
            if (readerExpander.IsExpanded)
            {
                addReaderButton.Visibility = Visibility.Visible;
            }
            else
            {
                addReaderButton.Visibility = Visibility.Collapsed;
            }
        }

        private void addReaderButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings.ReaderListItem", "Adding new reader.");
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
    }
}
