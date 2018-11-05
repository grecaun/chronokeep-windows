using EventDirector.Interfaces;
using EventDirector.Objects;
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

namespace EventDirector.UI.MainPages
{
    /// <summary>
    /// Interaction logic for BibAssignmentPage.xaml
    /// </summary>
    public partial class BibAssignmentPage : Page
    {
        private INewMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;
        int count = 0;

        public BibAssignmentPage(INewMainWindow mWindow, IDBInterface database)
        {
            InitializeComponent();
            InitializeComponent();
            this.mWindow = mWindow;
            this.database = database;
            this.theEvent = database.GetCurrentEvent();
            Update();
            UpdateImportOptions();
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

        private void Update()
        {

        }

        private void UpdateImportOptions()
        {

        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {

        }

        public void AddBibRange(int start, int end)
        {

        }

        public void AddBib(int number)
        {

        }

        private class ABibGroup : ListBoxItem
        {
            public TextBox SingleBib { get; private set; }
            public TextBox RangeStartBib { get; private set; }
            public TextBox RangeEndBib { get; private set; }
            public TextBox GroupNumber { get; private set; }
            public TextBox GroupName { get; private set; }
            public Button AddSingle { get; private set; }
            public Button AddRange { get; private set; }
            public Button Remove { get; private set; }
            public Button Save { get; private set; }

            ListBox parent;
            BibAssignmentPage page;
            BibGroup myGroup;

            public ABibGroup(ListBox parent, BibAssignmentPage page, BibGroup bg, int number, int basebib)
            {
                this.page = page;
                this.parent = parent;
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
                    FontSize = 16,
                    Margin = new Thickness(10,10,10,10)
                };
                GroupName.GotFocus += new RoutedEventHandler(this.SelectAll);
                theGrid.Children.Add(GroupName);
                Grid.SetColumn(GroupName, 0);
                Grid.SetRow(GroupName, 0);

                // number + save + remove buttons
                DockPanel numberDP = new DockPanel();
                GroupNumber = new TextBox()
                {
                    Text = bg.Number.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(10,10,10,10),
                    Width = 50
                };
                GroupNumber.KeyDown += new KeyEventHandler(this.KeyPressHandler);
                GroupNumber.GotFocus += new RoutedEventHandler(this.SelectAll);
                numberDP.Children.Add(GroupNumber);
                Save = new Button()
                {
                    Content = "Save",
                    FontSize = 16,
                    Height = 35,
                    Width = 60,
                    Margin = new Thickness(5, 5, 5, 5)
                };
                Save.Click += new RoutedEventHandler(this.Save_Click);
                numberDP.Children.Add(Save);
                Remove = new Button()
                {
                    Content = "Remove",
                    FontSize = 16,
                    Height = 35,
                    Width = 60,
                    Margin = new Thickness(5, 5, 5, 5)
                };
                Remove.Click += new RoutedEventHandler(this.Remove_Click);
                numberDP.Children.Add(Remove);
                theGrid.Children.Add(numberDP);
                Grid.SetColumn(numberDP, 1);
                Grid.SetRow(numberDP, 0);

                // If we're the catchall, don't let them edit things.
                if (bg.Number == -1)
                {
                    GroupName.IsEnabled = false;
                    GroupNumber.Visibility = Visibility.Collapsed;
                    Save.Visibility = Visibility.Collapsed;
                    Remove.Visibility = Visibility.Collapsed;
                }
                else
                {
                    GroupName.IsEnabled = true;
                    GroupNumber.Visibility = Visibility.Visible;
                    Save.Visibility = Visibility.Visible;
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
                Grid.SetColumn(RangeEndBib, 1);
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
            }

            private void AddRange_Click(object senter, EventArgs e)
            {

            }

            private void Remove_Click(object sender, EventArgs e)
            {
                Log.D("Removing an item.");
                try
                {
                    parent.Items.Remove(this);
                }
                catch { }
            }

            private void Save_Click(object sender, EventArgs e)
            {

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
    }
}
