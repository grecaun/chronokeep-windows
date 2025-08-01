﻿using Chronokeep.Interfaces;
using Chronokeep.Network.API;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronokeepRemote;
using Chronokeep.Timing.Remote;
using Chronokeep.UI.UIObjects;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using Wpf.Ui.Controls;
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
            loadingPanel.Visibility = Visibility.Collapsed;
            apiListView.Visibility = Visibility.Visible;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Log.D("UI.API.RemoteReaders", "Window is closed.");
            theOne = null;
            window.WindowFinalize(this);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
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

        internal class APIExpander : Wpf.Ui.Controls.ListViewItem
        {
            private Wpf.Ui.Controls.ListView readerListView;

            public APIExpander(APIObject api, List<RemoteReader> readers, Dictionary<(int, string), RemoteReader> savedReaders, IDBInterface database, IMainWindow mainWindow)
            {
                Expander expander = new()
                {
                    Header = api.Nickname,
                    IsExpanded = true,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Width = 1050,
                    Margin = new Thickness(5),
                };
                this.Content = expander;
                Style style = (Style)FindResource("NoFocusListViewItem");
                readerListView = new()
                {
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    ItemContainerStyle = style,
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

        internal class ReaderListItem : Wpf.Ui.Controls.ListViewItem
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
                locations.Insert(0, new TimingLocation(Constants.Timing.LOCATION_ANNOUNCER, theEvent.Identifier, "Announcer", 0, 0));
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
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                this.Content = thePanel;
                autoFetch = new()
                {
                    Height = 35,
                    Width = 55,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsChecked = savedReaders.ContainsKey((reader.APIIDentifier, reader.Name)),
                    Margin = new Thickness(5),
                };
                thePanel.Children.Add(autoFetch);
                nameBlock = new()
                {
                    Text = reader.Name,
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 80,
                    Margin = new Thickness(5),
                };
                thePanel.Children.Add(nameBlock);
                locationBox = new()
                {
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5),
                    Height = 35,
                    Width = 150,
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
                    Margin = new Thickness(5)
                };
                thePanel.Children.Add(startDatePicker);
                startTimeBox = new()
                {
                    Text = "00:00:00",
                    Mask = "00:00:00",
                    Height = 35,
                    Width = 80,
                    VerticalAlignment = VerticalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(5)
                };
                thePanel.Children.Add(startTimeBox);
                endDatePicker = new()
                {
                    Text = dateStr,
                    Height = 35,
                    Width = 200,
                    Margin = new Thickness(5)
                };
                thePanel.Children.Add(endDatePicker);
                endTimeBox = new()
                {
                    Text = "23:59:59",
                    Mask = "00:00:00",
                    Height = 35,
                    Width = 80,
                    VerticalAlignment = VerticalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(5)
                };
                thePanel.Children.Add(endTimeBox);
                Button rewind = new()
                {
                    Icon = new SymbolIcon() { Symbol = SymbolRegular.Rewind24 },
                    Height = 35,
                    Width = 35,
                    Margin = new Thickness(5),
                };
                rewind.Click += new RoutedEventHandler(async (sender, e) =>
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
                        (var reads, var note) = await api.GetReads(this.reader, startDate, endDate);
                        this.database.AddChipReads(reads);
                        mainWindow.UpdateTimingFromController();
                        DialogBox.Show("Rewind complete.");
                    }
                    catch (APIException ex)
                    {
                        DialogBox.Show(ex.Message);
                        return;
                    }
                });
                thePanel.Children.Add(rewind);
                Button delete = new()
                {
                    Icon = new SymbolIcon() { Symbol = SymbolRegular.Delete24 },
                    Height = 35,
                    Width = 35,
                    Margin = new Thickness(5),
                };
                delete.Click += new RoutedEventHandler((sender, e) =>
                {
                    Log.D("UI.API.RemoteReadersWindow.ReaderListItem", "Delete clicked.");
                    DialogBox.Show(
                        "Warning!\n\nThis will delete every read uploaded to the remote api. That data cannot be recoverred once deleted.",
                        "Delete",
                        "Cancel",
                        async () =>
                        {
                            Log.D("UI.API.RemoteReadersWindow.ReaderListItem", "User requests deletion.");
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
                                long count = await api.DeleteReads(this.reader, startDate, endDate);
                                mainWindow.UpdateTimingFromController();
                                DialogBox.Show(string.Format("Successfully deleted\n\n{0}\n\nreads.", count));
                            }
                            catch (APIException ex)
                            {
                                DialogBox.Show(ex.Message);
                                return;
                            }
                        });
                });
                thePanel.Children.Add(delete);
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
