using ChronoKeep.Interfaces;
using ChronoKeep.Objects;
using System;
using System.Collections;
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
    /// Interaction logic for BibAssignmentPage.xaml
    /// </summary>
    public partial class BibAssignmentPage : Page, IMainPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;

        public BibAssignmentPage(IMainWindow mWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mWindow;
            this.database = database;
            this.theEvent = database.GetCurrentEvent();
        }

        private void BibList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Log.D("BibChipList size has changed.");
            ListView listView = sender as ListView;
            GridView gView = listView.View as GridView;

            var workingWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth - 10;
            gView.Columns[0].Width = workingWidth * 0.3;
            gView.Columns[1].Width = workingWidth * 0.7;
        }

        public async void UpdateView()
        {
            GroupsBox.Items.Clear();
            theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            List<BibGroup> bibGroups = database.GetBibGroups(theEvent.Identifier);
            bibGroups.Add(new BibGroup(theEvent.Identifier));
            bibGroups.Sort();
            foreach (BibGroup bg in bibGroups)
            {
                Log.D("Adding BibGroup number " + bg.Number);
                GroupsBox.Items.Add(new ABibGroup(this, bg, database.LargestBib(theEvent.Identifier) + 1));
            }
            List<AvailableBib> availableBibs = new List<AvailableBib>();
            await Task.Run(() =>
            {
                availableBibs = database.GetBibs(theEvent.Identifier);
            });
            availableBibs.Sort();
            bibList.ItemsSource = availableBibs;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Delete clicked.  Attempting to do so.");
            IList selected = bibList.SelectedItems;
            List<AvailableBib> items = new List<AvailableBib>();
            foreach (AvailableBib b in selected)
            {
                items.Add(b);
            }
            database.RemoveBibs(items);
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            UpdateView();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Delete all clicked.");
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete everything? This cannot be undone.",
                                                        "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                List<AvailableBib> list = (List<AvailableBib>)bibList.ItemsSource;
                database.RemoveBibs(list);
                if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
                {
                    UpdateDatabase();
                }
                UpdateView();
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Log.D("New Group clicked.");
            Event theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            List<BibGroup> list = database.GetBibGroups(theEvent.Identifier);
            int groupNum = 1;
            foreach (BibGroup b in list)
            {
                groupNum = b.Number + 1 > groupNum ? b.Number + 1 : groupNum;
            }
            database.AddBibGroup(theEvent.Identifier, new BibGroup()
            {
                Name = "New Group " + groupNum.ToString(),
                Number = groupNum
            });
            UpdateView();
        }

        internal void RemoveGroup(BibGroup group)
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            database.RemoveBibGroup(group);
            UpdateView();
        }

        internal void AddBibRange(int group, int start, int end)
        {
            Event theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            List<int> bibs = new List<int>();
            for (int bib = start; bib <= end; bib++)
            {
                bibs.Add(bib);
            }
            database.AddBibs(theEvent.Identifier, group, bibs);
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            UpdateView();
        }

        internal void AddBib(int group, int bib)
        {
            Event theEvent = database.GetCurrentEvent();
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            database.AddBib(theEvent.Identifier, group, bib);
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            UpdateView();
        }

        public void UpdateDatabase()
        {
            foreach (ABibGroup bg in GroupsBox.Items)
            {
                BibGroup update = bg.UpdateGroup();
                if (update.Number != -1)
                {
                    database.AddBibGroup(theEvent.Identifier, update);
                }
            }
        }

        public void Keyboard_Ctrl_A()
        {
            Add_Click(null, null);
        }

        public void Keyboard_Ctrl_S()
        {
            UpdateDatabase();
            UpdateView();
        }

        public void Keyboard_Ctrl_Z()
        {
            UpdateView();
        }

        private class ABibGroup : ListBoxItem
        {
            public TextBox SingleBib { get; private set; }
            public TextBox RangeStartBib { get; private set; }
            public TextBox RangeEndBib { get; private set; }
            public TextBox GroupName { get; private set; }
            public Button AddSingle { get; private set; }
            public Button AddRange { get; private set; }
            public Button Remove { get; private set; }

            readonly BibAssignmentPage page;
            BibGroup myGroup;

            public ABibGroup(BibAssignmentPage page, BibGroup bg, int basebib)
            {
                this.page = page;
                this.myGroup = bg;
                Grid theGrid = new Grid();
                this.Content = theGrid;
                this.IsTabStop = false;
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Star) });
                theGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(55) });
                theGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

                // Group name
                GroupName = new TextBox()
                {
                    Text = bg.Name,
                    FontSize = 18,
                    Margin = new Thickness(10,10,10,10),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                };
                GroupName.GotFocus += new RoutedEventHandler(this.SelectAll);
                theGrid.Children.Add(GroupName);
                Grid.SetColumn(GroupName, 0);
                Grid.SetRow(GroupName, 0);

                // remove button
                Grid numberGrid = new Grid();
                numberGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                numberGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                // Remove Button
                Remove = new Button()
                {
                    Content = "Remove",
                    FontSize = 16,
                    Height = 35,
                    Margin = new Thickness(5, 5, 5, 5)
                };
                Remove.Click += new RoutedEventHandler(this.Remove_Click);
                numberGrid.Children.Add(Remove);
                Grid.SetColumn(Remove, 1);

                // Add number grid with children to overall grid, then set it in proper areas
                theGrid.Children.Add(numberGrid);
                Grid.SetColumn(numberGrid, 1);
                Grid.SetRow(numberGrid, 0);

                // If we're the catchall, don't let them edit things.
                if (bg.Number == -1)
                {
                    Log.D("BibGroup number is -1");
                    GroupName.IsEnabled = false;
                    Remove.Visibility = Visibility.Collapsed;
                }
                else
                {
                    Log.D("BibGroup number is NOT -1");
                    GroupName.IsEnabled = true;
                    Remove.Visibility = Visibility.Visible;
                }

                // Add single insert
                StackPanel single = new StackPanel();
                single.Children.Add(new Label()
                {
                    Content = "Add Single",
                    FontSize = 20,
                    Margin = new Thickness(10,5,10,0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                });
                DockPanel singleDP = new DockPanel();
                singleDP.Children.Add(new Label()
                {
                    Content = "Bib",
                    Margin = new Thickness(10,0,10,0),
                    FontSize = 16,
                    HorizontalContentAlignment = HorizontalAlignment.Right,
                    Width = 50
                });
                SingleBib = new TextBox()
                {
                    Text = basebib.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(5,5,5,5)
                };
                SingleBib.KeyDown += new KeyEventHandler(this.KeyPressHandler);
                SingleBib.GotFocus += new RoutedEventHandler(this.SelectAll);
                singleDP.Children.Add(SingleBib);
                single.Children.Add(singleDP);
                AddSingle = new Button()
                {
                    Content = "Save Single",
                    MaxWidth = 150,
                    Margin = new Thickness(10,10,10,10),
                    Height = 35,
                    FontSize = 16
                };
                AddSingle.Click += new RoutedEventHandler(this.AddSingle_Click);
                single.Children.Add(AddSingle);
                theGrid.Children.Add(single);
                Grid.SetColumn(single, 0);
                Grid.SetRow(single, 1);

                // Add range insert
                StackPanel range = new StackPanel();
                range.Children.Add(new Label()
                {
                    Content = "Add Range",
                    FontSize = 20,
                    Margin = new Thickness(10,5,10,0),
                    HorizontalContentAlignment = HorizontalAlignment.Center
                });
                Grid bibs = new Grid();
                bibs.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(65) });
                bibs.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                bibs.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                Label bibLabel = new Label()
                {
                    Content = "Bib",
                    FontSize = 16,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                };
                bibs.Children.Add(bibLabel);
                Grid.SetColumn(bibs, 0);
                RangeStartBib = new TextBox()
                {
                    Text = basebib.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(5, 5, 5, 5),
                };
                RangeStartBib.KeyDown += new KeyEventHandler(this.KeyPressHandler);
                RangeStartBib.GotFocus += new RoutedEventHandler(this.SelectAll);
                bibs.Children.Add(RangeStartBib);
                Grid.SetColumn(RangeStartBib, 1);
                RangeEndBib = new TextBox()
                {
                    Text = basebib.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(5, 5, 5, 5),
                };
                RangeEndBib.KeyDown += new KeyEventHandler(this.KeyPressHandler);
                RangeEndBib.GotFocus += new RoutedEventHandler(this.SelectAll);
                bibs.Children.Add(RangeEndBib);
                Grid.SetColumn(RangeEndBib, 2);
                range.Children.Add(bibs);
                AddRange = new Button()
                {
                    Content = "Save Range",
                    MaxWidth = 150,
                    Margin = new Thickness(10, 10, 10, 10),
                    Height = 35,
                    FontSize = 16
                };
                AddRange.Click += new RoutedEventHandler(this.AddRange_Click);
                range.Children.Add(AddRange);
                theGrid.Children.Add(range);
                Grid.SetColumn(range, 1);
                Grid.SetRow(range, 1);
            }

            private void AddSingle_Click(object senter, EventArgs e)
            {
                Log.D("Add single clicked.");
                int bib = -1;
                try
                {
                    bib = int.Parse(SingleBib.Text);
                }
                catch
                {
                    MessageBox.Show("Unable to add bib number.");
                    return;
                }
                this.page.AddBib(this.myGroup.Number, bib);
            }

            private void AddRange_Click(object senter, EventArgs e)
            {
                Log.D("Add range clicked.");
                int startbib = -1, endbib = -1;
                try
                {
                    startbib = int.Parse(RangeStartBib.Text);
                    endbib = int.Parse(RangeEndBib.Text);
                }
                catch
                {
                    MessageBox.Show("Unable to add range of bib numbers.");
                    return;
                }
                if (endbib < startbib)
                {
                    MessageBox.Show("Second box is smaller than the first. Please switch them.");
                    return;
                }
                if (endbib < 0 || startbib < 0)
                {
                    MessageBox.Show("Bib numbers must be greater than zero.");
                    return;
                }
                this.page.AddBibRange(this.myGroup.Number, startbib, endbib);
            }

            private void Remove_Click(object sender, EventArgs e)
            {
                Log.D("Removing an item.");
                this.page.RemoveGroup(this.myGroup);
            }

            internal BibGroup UpdateGroup()
            {
                myGroup.Name = GroupName.Text;
                return myGroup;
            }

            private void SelectAll(object sender, RoutedEventArgs e)
            {
                TextBox src = (TextBox)e.OriginalSource;
                src.SelectAll();
            }

            private void KeyPressHandler(object sender, KeyEventArgs e)
            {
                if (e.Key >= Key.D0 && e.Key <= Key.D9) { }
                else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) { }
                else if (e.Key == Key.Tab) { }
                else
                {
                    e.Handled = true;
                }
            }
        }

        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateDatabase();
        }

        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateView();
        }

        public void Closing()
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateView();
        }
    }
}
