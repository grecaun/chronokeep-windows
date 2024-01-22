using Chronokeep.Interfaces;
using Chronokeep.Network.API;
using Chronokeep.Network.Remote;
using Chronokeep.Objects;
using Chronokeep.Objects.ChronokeepRemote;
using Chronokeep.UI.UIObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using Xceed.Wpf.Toolkit;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using Button = Wpf.Ui.Controls.Button;

namespace Chronokeep.UI.API
{
    /// <summary>
    /// Interaction logic for RemoteReaders.xaml
    /// </summary>
    public partial class RemoteReadersWindow : UiWindow
    {
        IMainWindow window;
        IDBInterface database;
        Event theEvent;

        List<APIObject> remoteAPIs;
        HashSet<(int, string)> readerNames = new();

        public RemoteReadersWindow(IMainWindow window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                DialogBox.Show("Unable to get event information.");
                this.Close();
                return;
            }
            remoteAPIs = database.GetAllAPI();
            remoteAPIs.RemoveAll( x => x.Type != Constants.APIConstants.CHRONOKEEP_REMOTE && x.Type != Constants.APIConstants.CHRONOKEEP_REMOTE_SELF );
            foreach (RemoteReader reader in database.GetRemoteReaders(theEvent.Identifier))
            {
                readerNames.Add((reader.APIIDentifier, reader.Name));
            }
            GetReaders();
        }

        private async void GetReaders()
        {
            try
            {
                // fetch all readers from the remote apis
                foreach (APIObject api in remoteAPIs)
                {
                    var response = await RemoteHandlers.GetReaders(api);
                    apiListView.Items.Add(new APIExpander(api, response.Readers, readerNames, database));
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

        private void UiWindow_Closed(object sender, EventArgs e)
        {
            Log.D("UI.API.RemoteReaders", "Window is closed.");
            window.WindowFinalize(this);
        }

        private void Close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Log.D("UI.API.RemoteReaders", "Close button clicked.");
            this.Close();
        }

        internal class APIExpander : ListViewItem
        {
            private ListView readerListView;

            public APIExpander(APIObject api, List<RemoteReader> readers, HashSet<(int, string)> readerNames, IDBInterface database)
            {
                Expander expander = new()
                {
                    Header = api.Nickname,
                    IsExpanded = true,
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalContentAlignment = System.Windows.VerticalAlignment.Center,
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
                    readerListView.Items.Add(new ReaderListItem(reader, api, readerNames, database));
                }
            }
        }

        internal class ReaderListItem : ListViewItem
        {
            private RemoteReader reader;
            private APIObject api;
            private IDBInterface database;

            ToggleSwitch autoFetch;
            TextBlock nameBlock;
            DatePicker startDatePicker;
            DatePicker endDatePicker;
            MaskedTextBox startTimeBox;
            MaskedTextBox endTimeBox;

            public ReaderListItem(RemoteReader reader, APIObject api, HashSet<(int, string)> readerNames, IDBInterface database)
            {
                this.reader = reader;
                this.api = api;
                this.database = database;

                string dateStr = DateTime.Now.ToString("MM/dd/yyyy");
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
                    IsChecked = readerNames.Contains((reader.APIIDentifier, reader.Name)),
                    Margin = new System.Windows.Thickness(5),
                };
                thePanel.Children.Add(autoFetch);
                nameBlock = new()
                {
                    Text = reader.Name,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Height = 35,
                    Width = 100,
                    Margin = new System.Windows.Thickness(5),
                };
                thePanel.Children.Add(nameBlock);
                startDatePicker = new()
                {
                    Text = dateStr,
                    Height = 35,
                    Width = 100,
                    Margin = new System.Windows.Thickness(5)
                };
                thePanel.Children.Add(startDatePicker);
                startTimeBox = new()
                {
                    Text = "00:00:00",
                    Mask = "00:00:00",
                    Height = 35,
                    Width = 100,
                    Margin = new System.Windows.Thickness(5)
                };
                thePanel.Children.Add(startTimeBox);
                endDatePicker = new()
                {
                    Text = dateStr,
                    Height = 35,
                    Width = 100,
                    Margin = new System.Windows.Thickness(5)
                };
                thePanel.Children.Add(endDatePicker);
                endTimeBox = new()
                {
                    Text = "23:59:59",
                    Mask = "00:00:00",
                    Height = 35,
                    Width = 100,
                    Margin = new System.Windows.Thickness(5)
                };
                thePanel.Children.Add(endTimeBox);
                Button rewind = new Button()
                {
                    Icon = Wpf.Ui.Common.SymbolRegular.Rewind16,
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
                        var result = await RemoteHandlers.GetReads(
                            this.api,
                            this.reader.Name,
                            Constants.Timing.UnixDateToEpoch(startDate.ToUniversalTime()),
                            Constants.Timing.UnixDateToEpoch(endDate.ToUniversalTime())
                            );
                        List<ChipRead> toUpload = new List<ChipRead>();
                        foreach (RemoteRead read in result.Reads)
                        {
                            toUpload.Add(read.ConvertToChipRead(this.reader.EventID, this.reader.LocationID));
                        }
                        this.database.AddChipReads(toUpload);
                    }
                    catch (APIException ex)
                    {
                        DialogBox.Show(ex.Message);
                        return;
                    }
                });
                thePanel.Children.Add(rewind);
            }
        }
    }
}
