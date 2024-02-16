using Chronokeep.Interfaces;
using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronokeepRemote;
using Chronokeep.Timing.Remote;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;
using Xceed.Wpf.Toolkit;
using Button = Wpf.Ui.Controls.Button;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for RemoteReaders.xaml
    /// </summary>
    public partial class RemoteReadersWindow : FluentWindow
    {
        private static RemoteReadersWindow theOne = null;

        IMainWindow window;
        IDBInterface database;
        Event theEvent;

        List<APIObject> remoteAPIs;

        public static RemoteReadersWindow CreateWindow(IMainWindow window, IDBInterface database)
        {
            if (theOne == null)
            {
                theOne = new RemoteReadersWindow(window, database);
            }
            return theOne;
        }

        private RemoteReadersWindow(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            this.MinWidth = 10;
            this.MinHeight = 10;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                DialogBox.Show("Unable to get event information.");
                this.Close();
                return;
            }
            remoteAPIs = database.GetAllAPI();
            remoteAPIs.RemoveAll( x => x.Type != Constants.APIConstants.CHRONOKEEP_REMOTE && x.Type != Constants.APIConstants.CHRONOKEEP_REMOTE_SELF );
            GetReaders();
        }

        private async void GetReaders()
        {
            try
            {
                Dictionary<(int, string), RemoteReader> savedReaders = new();
                foreach (RemoteReader reader in database.GetRemoteReaders(theEvent.Identifier))
                {
                    savedReaders[(reader.APIIDentifier, reader.Name)] = reader;
                }
                // fetch all readers from the remote apis
                foreach (APIObject api in remoteAPIs)
                {
                    var readers = await api.GetReaders();
                    apiListView.Items.Add(new APIExpander(api, readers, savedReaders, database, window));
                }
            }
            catch (APIException ex)
            {
                DialogBox.Show(ex.Message);
                Close();
                return;
            }
            loadingPanel.Visibility = System.Windows.Visibility.Collapsed;
            apiListView.Visibility = System.Windows.Visibility.Visible;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Log.D("UI.API.RemoteReaders", "Window is closed.");
            theOne = null;
            window.WindowFinalize(this);
        }

        private void Close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Log.D("UI.API.RemoteReaders", "Close button clicked.");
            List<RemoteReader> readersToSave = new();
            List<RemoteReader> otherReaders = new();
            foreach (APIExpander item in apiListView.Items)
            {
                var downDict = item.GetAutoDownloadDictionary();
                foreach (RemoteReader reader in downDict.Keys)
                {
                    if (downDict[reader])
                    {
                        readersToSave.Add(reader);
                    }
                    else
                    {
                        otherReaders.Add(reader);
                    }
                }
            }
            List<RemoteReader> deleteReaders = new();
            HashSet<(int, string)> readerNames = new();
            foreach (RemoteReader reader in database.GetRemoteReaders(theEvent.Identifier))
            {
                readerNames.Add((reader.APIIDentifier, reader.Name));
            }
            foreach (RemoteReader reader in otherReaders)
            {
                if (readerNames.Contains((reader.APIIDentifier, reader.Name)))
                {
                    deleteReaders.Add(reader);
                }
            }
            database.DeleteRemoteReaders(theEvent.Identifier, deleteReaders);
            database.AddRemoteReaders(theEvent.Identifier, readersToSave);
            // notify mainwindow to update/start remote reader thread
            RemoteReadersNotifier.GetRemoteReadersNotifier().Notify();
            Close();
        }

        internal class APIExpander : ListViewItem
        {
            private ListView readerListView;

            public APIExpander(APIObject api, List<RemoteReader> readers, Dictionary<(int, string), RemoteReader> savedReaders, IDBInterface database, IMainWindow mainWindow)
            {
                Expander expander = new()
                {
                    Header = api.Nickname,
                    IsExpanded = true,
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
                    Width = 1050,
                    Margin = new System.Windows.Thickness(5),
                };
                this.Content = expander;
                readerListView = new()
                {
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
                };
                expander.Content = readerListView;
                foreach (RemoteReader reader in readers)
                {
                    reader.APIIDentifier = api.Identifier;
                    if (savedReaders.ContainsKey((reader.APIIDentifier, reader.Name)))
                    {
                        reader.LocationID = savedReaders[(reader.APIIDentifier, reader.Name)].LocationID;
                    }
                    readerListView.Items.Add(new ReaderListItem(reader, api, savedReaders, database, mainWindow));
                }
            }

            public Dictionary<RemoteReader, bool> GetAutoDownloadDictionary()
            {
                var output = new Dictionary<RemoteReader, bool>();
                foreach (ReaderListItem item in readerListView.Items)
                {
                    output[item.GetUpdatedReader()] = item.AutoDownloadReads();
                }
                return output;
            }
        }

        internal class ReaderListItem : ListViewItem
        {
            private RemoteReader reader;
            private APIObject api;
            private IDBInterface database;
            private IMainWindow mainWindow;

            ToggleSwitch autoFetch;
            Wpf.Ui.Controls.TextBlock nameBlock;
            ComboBox locationBox;
            DatePicker startDatePicker;
            DatePicker endDatePicker;
            MaskedTextBox startTimeBox;
            MaskedTextBox endTimeBox;

            public ReaderListItem(
                RemoteReader reader,
                APIObject api,
                Dictionary<(int, string), RemoteReader> savedReaders,
                IDBInterface database,
                IMainWindow mainWindow
                )
            {
                this.reader = reader;
                this.api = api;
                this.database = database;
                this.mainWindow = mainWindow;

                string dateStr = DateTime.Now.ToString("MM/dd/yyyy");
                var theEvent = database.GetCurrentEvent();
                if (theEvent == null || theEvent.Identifier < 1)
                {
                    return;
                }
                this.reader.EventID = theEvent.Identifier;
                List<TimingLocation> locations = database.GetTimingLocations(theEvent.Identifier);
                if (!theEvent.CommonStartFinish)
                {
                    locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                    locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_START, theEvent.Identifier, "Start", 0, theEvent.StartWindow));
                }
                else
                {
                    locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_FINISH, theEvent.Identifier, "Start/Finish", theEvent.FinishMaxOccurrences, theEvent.FinishIgnoreWithin));
                }
                StackPanel thePanel = new()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                };
                this.Content = thePanel;
                autoFetch = new()
                {
                    Height = 35,
                    Width = 55,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    IsChecked = savedReaders.ContainsKey((reader.APIIDentifier, reader.Name)),
                    Margin = new System.Windows.Thickness(5),
                };
                thePanel.Children.Add(autoFetch);
                nameBlock = new()
                {
                    Text = reader.Name,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Width = 100,
                    Margin = new System.Windows.Thickness(5),
                };
                thePanel.Children.Add(nameBlock);
                locationBox = new()
                {
                    VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
                    Margin = new System.Windows.Thickness(5),
                    Height = 35,
                    Width = 170,
                };
                foreach (TimingLocation loc in locations)
                {
                    locationBox.Items.Add(new ComboBoxItem()
                    {
                        Content = loc.Name,
                        Uid = loc.Identifier.ToString(),
                        IsSelected = reader.LocationID == loc.Identifier,
                    });
                }
                if (locationBox.SelectedItem == null)
                {
                    locationBox.SelectedIndex = 0;
                }
                thePanel.Children.Add(locationBox);
                startDatePicker = new()
                {
                    Text = dateStr,
                    Height = 35,
                    Width = 200,
                    Margin = new System.Windows.Thickness(5)
                };
                thePanel.Children.Add(startDatePicker);
                startTimeBox = new()
                {
                    Text = "00:00:00",
                    Mask = "00:00:00",
                    Height = 35,
                    Width = 80,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
                    TextAlignment = System.Windows.TextAlignment.Center,
                    Margin = new System.Windows.Thickness(5)
                };
                thePanel.Children.Add(startTimeBox);
                endDatePicker = new()
                {
                    Text = dateStr,
                    Height = 35,
                    Width = 200,
                    Margin = new System.Windows.Thickness(5)
                };
                thePanel.Children.Add(endDatePicker);
                endTimeBox = new()
                {
                    Text = "23:59:59",
                    Mask = "00:00:00",
                    Height = 35,
                    Width = 80,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
                    TextAlignment = System.Windows.TextAlignment.Center,
                    Margin = new System.Windows.Thickness(5)
                };
                thePanel.Children.Add(endTimeBox);
                Button rewind = new()
                {
                    Icon = new SymbolIcon() { Symbol = SymbolRegular.Rewind24 },
                    Height = 35,
                    Width = 35,
                    Margin = new System.Windows.Thickness(5),
                };
                rewind.Click += new System.Windows.RoutedEventHandler(async (sender, e) =>
                {
                    Log.D("UI.API.RemoteReadersWindow.ReaderListItem", "Rewind clicked.");
                    if (!DateTime.TryParse(string.Format("{0} {1}", startDatePicker.Text, startTimeBox.Text.Replace('_', '0')), out DateTime startDate))
                    {
                        startDate = DateTime.Now;
                    }
                    if (!DateTime.TryParse(string.Format("{0} {1}", endDatePicker.Text, endTimeBox.Text.Replace('_', '0')), out DateTime endDate))
                    {
                        endDate = DateTime.Now;
                    }
                    try
                    {
                        var theEvent = database.GetCurrentEvent();
                        if (theEvent == null || theEvent.Identifier < 1)
                        {
                            return;
                        }
                        this.reader.EventID = theEvent.Identifier;
                        if (locationBox.SelectedItem == null)
                        {
                            this.reader.LocationID = Constants.Timing.LOCATION_FINISH;
                        }
                        else
                        {
                            this.reader.LocationID = Convert.ToInt32(((ComboBoxItem)locationBox.SelectedItem).Uid);
                        }
                        var reads = await api.GetReads(this.reader, startDate, endDate);
                        this.database.AddChipReads(reads);
                        mainWindow.UpdateTimingFromController();
                    }
                    catch (APIException ex)
                    {
                        DialogBox.Show(ex.Message);
                        return;
                    }
                });
                thePanel.Children.Add(rewind);
            }

            public RemoteReader GetUpdatedReader()
            {
                var output = new RemoteReader();
                output.Name = reader.Name;
                output.EventID = reader.EventID;
                output.APIIDentifier = api.Identifier;
                if (locationBox.SelectedItem != null && int.TryParse(((ComboBoxItem)locationBox.SelectedItem).Uid, out var locId))
                {
                    output.LocationID = locId;
                }
                else
                {
                    output.LocationID = Constants.Timing.LOCATION_FINISH;
                }
                return output;
            }

            public bool AutoDownloadReads()
            {
                return autoFetch.IsChecked == true;
            }
        }
    }
}
