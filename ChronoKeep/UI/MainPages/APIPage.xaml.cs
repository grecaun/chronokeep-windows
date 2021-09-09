using ChronoKeep.Interfaces;
using ChronoKeep.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChronoKeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for APIPage.xaml
    /// </summary>
    public partial class APIPage : IMainPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private List<ResultsAPI> resultsAPI;

        public APIPage(IMainWindow mWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mWindow;
            this.database = database;
            UpdateView();
        }

        public void Keyboard_Ctrl_A()
        {
            Log.D("Ctrl + A Passed to this page.");
            Add_Click(null, null);
        }

        public void Keyboard_Ctrl_S()
        {
            Log.D("Ctrl + S Passed to this page.");
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
            resultsAPI = database.GetAllResultsAPI();
            foreach (ResultsAPI api in resultsAPI)
            {
                APIBox.Items.Add(new AnAPI(this, api));
            }
        }

        public void Closing()
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateResultsAPI();
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Add api clicked.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateResultsAPI();
            }
            database.AddResultsAPI(new ResultsAPI());
            UpdateView();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Update clicked.");
            UpdateResultsAPI();
            UpdateView();
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Revert clicked.");
            UpdateView();
        }

        public void UpdateResultsAPI()
        {
            foreach (AnAPI listDiv in APIBox.Items)
            {
                listDiv.UpdateResultsAPI();
                database.UpdateResultsAPI(listDiv.theAPI);
            }
        }

        public void RemoveAPI(ResultsAPI api)
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateResultsAPI();
            }
            database.RemoveResultsAPI(api.Identifier);
            UpdateView();
        }

        public void UpdateResultsAPI(ResultsAPI api)
        {
            database.UpdateResultsAPI(api);
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
            public Button Remove { get; private set; }

            readonly APIPage page;
            public ResultsAPI theAPI;

            public AnAPI(APIPage page, ResultsAPI api)
            {
                theAPI = api;
                this.page = page;
                Grid thePanel = new Grid()
                {
                    MaxHeight = 100
                };
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(150) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Star) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Star) });
                thePanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(110) });
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
                    Height = 25,
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
                    Height = 25,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center
                };
                APIType.Items.Add(new ComboBoxItem()
                {
                    Content = Constants.ResultsAPI.API_TYPE_NAMES[Constants.ResultsAPI.CHRONOKEEP],
                    Uid = Constants.ResultsAPI.CHRONOKEEP
                });
                APIType.Items.Add(new ComboBoxItem()
                {
                    Content = Constants.ResultsAPI.API_TYPE_NAMES[Constants.ResultsAPI.CHRONOKEEP_SELF],
                    Uid = Constants.ResultsAPI.CHRONOKEEP_SELF
                });
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
                    Height = 25,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = api.URL,
                    IsEnabled = false
                };
                urlPanel.Children.Add(APIURL);
                thePanel.Children.Add(urlPanel);
                Grid.SetColumn(urlPanel, 2);

                int ix = 0;
                if (theAPI.Type == Constants.ResultsAPI.CHRONOKEEP_SELF)
                {
                    ix = 1;
                    APIURL.IsEnabled = true;
                }
                else
                {
                    APIURL.IsEnabled = false;
                }
                APIType.SelectedIndex = ix;
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
                    Height = 25,
                    Margin = new Thickness(10, 5, 0, 5),
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = api.AuthToken
                };
                tokenPanel.Children.Add(APIToken);
                thePanel.Children.Add(tokenPanel);
                Grid.SetColumn(tokenPanel, 3);

                Remove = new Button()
                {
                    Content = "Remove",
                    FontSize = 14,
                    Width = 95,
                    Height = 40,
                    Margin = new Thickness(0, 5, 0, 5),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Remove.Click += new RoutedEventHandler(this.Remove_Click);
                thePanel.Children.Add(Remove);
                Grid.SetColumn(Remove, 4);
            }

            private void Remove_Click(object sender, RoutedEventArgs e)
            {
                Log.D("Removing api.");
                this.page.RemoveAPI(theAPI);
            }

            public void UpdateResultsAPI()
            {
                Log.D("Updating api.");
                theAPI.Nickname = APINickname.Text;
                theAPI.URL = APIURL.Text;
                theAPI.AuthToken = APIToken.Text;
                theAPI.Type = ((ComboBoxItem)APIType.SelectedItem).Uid;
            }

            private void SelectAll(object sender, RoutedEventArgs e)
            {
                TextBox src = (TextBox)e.OriginalSource;
                src.SelectAll();
            }

            private void APIType_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                Log.D("Changing API Type!");
                // Ensure we've got something selected, and then change the URL if they've selected Chronokeep.
                if (APIType.SelectedItem != null)
                {
                    if (((ComboBoxItem)APIType.SelectedItem).Uid == Constants.ResultsAPI.CHRONOKEEP) {
                        theAPI.URL = Constants.ResultsAPI.CHRONOKEEP_URL;
                        APIURL.Text = theAPI.URL;
                        APIURL.IsEnabled = false;
                    } else
                    {
                        APIURL.IsEnabled = true;
                    }
                }
            }
        }
    }
}
