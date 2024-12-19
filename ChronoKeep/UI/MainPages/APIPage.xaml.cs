using Chronokeep.Interfaces;
using Chronokeep.Objects;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chronokeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for APIPage.xaml
    /// </summary>
    public partial class APIPage : IMainPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private List<APIObject> resultsAPI;

        public APIPage(IMainWindow mWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mWindow;
            this.database = database;
            UpdateView();
        }

        public void Keyboard_Ctrl_A()
        {
            Log.D("UI.MainPages.APIPage", "Ctrl + A Passed to this page.");
            Add_Click(null, null);
        }

        public void Keyboard_Ctrl_S()
        {
            Log.D("UI.MainPages.APIPage", "Ctrl + S Passed to this page.");
            UpdateResultsAPI();
            UpdateView();
        }

        public void Keyboard_Ctrl_Z()
        {
            UpdateView();
        }

        public void UpdateView()
        {
            APIBox.Items.Clear();
            resultsAPI = database.GetAllAPI();
            foreach (APIObject api in resultsAPI)
            {
                APIBox.Items.Add(new AnAPI(this, api));
            }
        }

        public void Closing()
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateResultsAPI();
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.APIPage", "Add api clicked.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateResultsAPI();
            }
            database.AddAPI(new APIObject());
            UpdateView();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.APIPage", "Update clicked.");
            UpdateResultsAPI();
            UpdateView();
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.APIPage", "Revert clicked.");
            UpdateView();
        }

        public void UpdateResultsAPI()
        {
            foreach (AnAPI listDiv in APIBox.Items)
            {
                listDiv.UpdateResultsAPI();
                database.UpdateAPI(listDiv.theAPI);
            }
        }

        public void RemoveAPI(APIObject api)
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE)
            {
                UpdateResultsAPI();
            }
            database.RemoveAPI(api.Identifier);
            UpdateView();
        }

        public void UpdateResultsAPI(APIObject api)
        {
            database.UpdateAPI(api);
            UpdateView();
        }

        private void APIBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Utils.GetScrollViewer(sender as DependencyObject) is ScrollViewer scrollViewer)
            {
                if (e.Delta < 0)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + 35);
                }
                else if (e.Delta > 0)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - 35);
                }
            }
        }
        private class AnAPI : ListBoxItem
        {
            public TextBox APINickname { get; private set; }
            public ComboBox APIType { get; private set; }
            public TextBox APIURL { get; private set; }
            public TextBox APIToken { get; private set; }
            public TextBox APIWebURL { get; private set; }
            public Button Remove { get; private set; }

            readonly APIPage page;
            public APIObject theAPI;

            public AnAPI(APIPage page, APIObject api)
            {
                theAPI = api;
                this.page = page;
                Grid thePanel = new Grid()
                {
                    MaxHeight = 100
                };
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(150) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(45) });
                this.Content = thePanel;
                this.IsTabStop = false;

                StackPanel nickPanel = new StackPanel();
                nickPanel.Children.Add(new Label
                {
                    Content = "Nickname",
                    FontSize = 15,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                });
                APINickname = new TextBox
                {
                    FontSize = 15,
                    Height = 45,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = api.Nickname
                };
                nickPanel.Children.Add(APINickname);
                thePanel.Children.Add(nickPanel);
                Grid.SetColumn(nickPanel, 0);


                StackPanel typePanel = new StackPanel();
                typePanel.Children.Add(new Label
                {
                    Content = "API Type",
                    FontSize = 15,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                });
                APIType = new ComboBox()
                {
                    FontSize = 14,
                    Height = 45,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center
                };
                foreach (string uid in Constants.APIConstants.API_TYPE_NAMES.Keys)
                {
                    APIType.Items.Add(new ComboBoxItem()
                    {
                        Content = Constants.APIConstants.API_TYPE_NAMES[uid],
                        Uid = uid,
                        IsSelected = theAPI.Type.Equals(uid),
                    });
                }
                typePanel.Children.Add(APIType);
                thePanel.Children.Add(typePanel);
                Grid.SetColumn(typePanel, 1);

                StackPanel urlPanel = new StackPanel();
                urlPanel.Children.Add(new Label
                {
                    Content = "API URL",
                    FontSize = 15,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                });
                APIURL = new TextBox
                {
                    FontSize = 15,
                    Height = 45,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = api.URL,
                    IsEnabled = false
                };
                urlPanel.Children.Add(APIURL);
                thePanel.Children.Add(urlPanel);
                Grid.SetColumn(urlPanel, 2);

                APIURL.IsEnabled = Constants.APIConstants.API_SELF_HOSTED[theAPI.Type];
                APIType.SelectionChanged += new SelectionChangedEventHandler(this.APIType_SelectionChanged);

                StackPanel tokenPanel = new StackPanel();
                tokenPanel.Children.Add(new Label
                {
                    Content = "API Key",
                    FontSize = 15,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                });
                APIToken = new TextBox
                {
                    FontSize = 15,
                    Height = 45,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = api.AuthToken
                };
                tokenPanel.Children.Add(APIToken);
                thePanel.Children.Add(tokenPanel);
                Grid.SetColumn(tokenPanel, 3);

                StackPanel webURLPanel = new StackPanel();
                webURLPanel.Children.Add(new Label
                {
                    Content = "API Web URL",
                    FontSize = 15,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                });
                APIWebURL = new TextBox
                {
                    FontSize = 15,
                    Height = 45,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = api.WebURL
                };
                webURLPanel.Children.Add(APIWebURL);
                thePanel.Children.Add(webURLPanel);
                Grid.SetColumn(webURLPanel, 4);

                Remove = new Button()
                {
                    Content = "x",
                    Margin = new Thickness(0, 5, 0, 9),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Height = 38
                };
                Remove.Click += new RoutedEventHandler(this.Remove_Click);
                thePanel.Children.Add(Remove);
                Grid.SetColumn(Remove, 5);
            }

            private void Remove_Click(object sender, RoutedEventArgs e)
            {
                Log.D("UI.MainPages.APIPage", "Removing api.");
                this.page.RemoveAPI(theAPI);
            }

            public void UpdateResultsAPI()
            {
                Log.D("UI.MainPages.APIPage", "Updating api.");
                theAPI.Nickname = APINickname.Text;
                theAPI.URL = APIURL.Text;
                if (!theAPI.URL.EndsWith("/"))
                {
                    theAPI.URL = theAPI.URL + "/";
                }
                theAPI.AuthToken = APIToken.Text;
                theAPI.Type = ((ComboBoxItem)APIType.SelectedItem).Uid;
                theAPI.WebURL = APIWebURL.Text;
                if (theAPI.WebURL.Length > 0 && !theAPI.WebURL.EndsWith("/"))
                {
                    theAPI.WebURL = theAPI.WebURL + "/";
                }
            }

            private void SelectAll(object sender, RoutedEventArgs e)
            {
                TextBox src = (TextBox)e.OriginalSource;
                src.SelectAll();
            }

            private void APIType_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                Log.D("UI.MainPages.APIPage", "Changing API Type!");
                // Ensure we've got something selected, and then change the URL if they've selected Chronokeep.
                if (APIType.SelectedItem != null)
                {
                    string type = ((ComboBoxItem)APIType.SelectedItem).Uid;
                    if (!Constants.APIConstants.API_SELF_HOSTED[type])
                    {
                        theAPI.URL = Constants.APIConstants.API_URL[type];
                        APIURL.Text = theAPI.URL;
                        APIURL.IsEnabled = false;
                    }
                    else
                    {
                        APIURL.IsEnabled = true;
                    }
                    if (Constants.APIConstants.API_RESULTS[type])
                    {
                        APIWebURL.Text = theAPI.WebURL;
                        APIWebURL.IsEnabled = true;
                    }
                    else
                    {
                        APIWebURL.Text = "";
                        APIWebURL.IsEnabled = false;
                    }
                }
            }
        }

        private void DoneBtn_Click(object sender, RoutedEventArgs e)
        {
            mWindow.SwitchPage(new DashboardPage(mWindow, database));
        }
    }
}
