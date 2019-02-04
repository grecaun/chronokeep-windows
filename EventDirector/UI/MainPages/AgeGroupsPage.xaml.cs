using EventDirector.Interfaces;
using EventDirector.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for AgeGroupsPage.xaml
    /// </summary>
    public partial class AgeGroupsPage : Page, IMainPage
    {
        private IMainWindow mWindow;
        private IDBInterface database;
        private Event theEvent;

        private bool touched = false;

        public AgeGroupsPage(IMainWindow mWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mWindow;
            this.database = database;
            UpdateView();
        }

        private void Divisions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.D("Division changed.");
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            UpdateAgeGroupsList();
        }

        public void UpdateView()
        {
            theEvent = database.GetCurrentEvent();
            if (theEvent.CommonAgeGroups == 1)
            {
                DivisionRow.Height = new GridLength(0);
                UpdateAgeGroupsList();
            }
            else
            {
                DivisionRow.Height = new GridLength(55);
                UpdateDivisionsBox();
            }
        }

        private void UpdateDivisionsBox()
        {
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            Divisions.Items.Clear();
            List<Division> divisions = database.GetDivisions(theEvent.Identifier);
            divisions.Sort();
            foreach (Division d in divisions)
            {
                Divisions.Items.Add(new ComboBoxItem()
                {
                    Content = d.Name,
                    Uid = d.Identifier.ToString()
                });
            }
            Divisions.SelectedIndex = 0;
        }

        private void UpdateAgeGroupsList()
        {
            if (theEvent == null || theEvent.Identifier < 0)
            {
                return;
            }
            AgeGroupsBox.Items.Clear();
            AgeGroupsBox.Items.Add(new ALabel());
            List<AgeGroup> ageGroups = database.GetAgeGroups(theEvent.Identifier);
            ageGroups.Sort();
            foreach (AgeGroup group in ageGroups)
            {
                AgeGroupsBox.Items.Add(new AAgeGroup(this, group));
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Adding group.");
            int divId = Constants.Timing.COMMON_AGEGROUPS_DIVISIONID;
            if (theEvent.CommonAgeGroups != 1)
            {
                divId = Convert.ToInt32(((ComboBoxItem)Divisions.SelectedItem).Uid);
            }
            AgeGroupsBox.Items.Add(new AAgeGroup(this, new AgeGroup(theEvent.Identifier, divId, 0, 0)));
        }

        private void AddDefault_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Add default age groups button clicked.");
            int divId = Constants.Timing.COMMON_AGEGROUPS_DIVISIONID;
            if (theEvent.CommonAgeGroups != 1)
            {
                divId = Convert.ToInt32(((ComboBoxItem)Divisions.SelectedItem).Uid);
            }
            database.RemoveAgeGroups(theEvent.Identifier, divId);
            int increment;
            switch (DefaultGroupsBox.SelectedIndex)
            {
                case 0:
                    increment = 10;
                    break;
                case 1:
                    increment = 5;
                    break;
                default:
                    increment = 99;
                    break;
            }
            if (DefaultGroupsBox.SelectedIndex != 2)
            {
                for (int i = 0; i < 100; i += increment)
                {
                    database.AddAgeGroup(new AgeGroup(theEvent.Identifier, divId, i, i + increment - 1));
                }
            }
            else
            {
                database.AddAgeGroup(new AgeGroup(theEvent.Identifier, divId, 0, 39));
                database.AddAgeGroup(new AgeGroup(theEvent.Identifier, divId, 40, 59));
                database.AddAgeGroup(new AgeGroup(theEvent.Identifier, divId, 60, 99));
            }
            touched = true;
            UpdateAgeGroupsList();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Update age groups button clicked.");
            List<AgeGroup> ageGroups = new List<AgeGroup>();
            List<AgeGroup> toAdd = new List<AgeGroup>();
            foreach (ListBoxItem aAge in AgeGroupsBox.Items)
            {
                if (aAge is AAgeGroup)
                {
                    ageGroups.Add(((AAgeGroup)aAge).GetAgeGroup());
                }
            }
            ageGroups.Sort();
            bool conflict = false;
            AgeGroup previous = null;
            foreach (AgeGroup current in ageGroups)
            {
                if (previous != null)
                {
                    if (previous.EndAge >= current.StartAge)
                    {
                        conflict = true;
                        break;
                    }
                    else if (previous.EndAge != current.StartAge - 1)
                    {
                        toAdd.Add(new AgeGroup(current.EventId, current.DivisionId, previous.EndAge + 1, current.StartAge - 1));
                    }
                }
                else if (current.StartAge > 1)
                {
                    toAdd.Add(new AgeGroup(current.EventId, current.DivisionId, 0, current.StartAge - 1));
                }
                previous = current;
            }
            if (conflict)
            {
                MessageBox.Show("There is a conflict in the age groups. Unable to save.");
                return;
            }
            ageGroups.AddRange(toAdd);
            int divId = Constants.Timing.COMMON_AGEGROUPS_DIVISIONID;
            if (theEvent.CommonAgeGroups != 1)
            {
                divId = Convert.ToInt32(((ComboBoxItem)Divisions.SelectedItem).Uid);
            }
            database.RemoveAgeGroups(theEvent.Identifier, divId);
            foreach (AgeGroup age in ageGroups)
            {
                database.AddAgeGroup(age);
            }
            touched = true;
            UpdateAgeGroupsList();
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            UpdateAgeGroupsList();
        }

        private void RemoveAgeGroup(AAgeGroup group)
        {
            Log.D("Removing Age Group from view.");
            AgeGroupsBox.Items.Remove(group);
        }

        public void UpdateDatabase()
        {
            Update_Click(null, null);
        }

        public void Keyboard_Ctrl_A()
        {
            Add_Click(null, null);
        }

        public void Keyboard_Ctrl_S()
        {
            UpdateDatabase();
            UpdateAgeGroupsList();
        }

        public void Keyboard_Ctrl_Z()
        {
            UpdateAgeGroupsList();
        }

        public void Closing()
        {
            if (database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE)
            {
                UpdateDatabase();
            }
            if (touched)
            {
                database.ResetTimingResultsPlacements(theEvent.Identifier);
                mWindow.NotifyRecalculateAgeGroups();
                mWindow.NotifyTimingWorker();
            }
        }

        private class ALabel : ListBoxItem
        {
            public ALabel()
            {
                Grid theGrid = new Grid()
                {
                    MaxWidth = 400
                };
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                Label l = new Label()
                {
                    Content = "Start Age",
                    FontSize = 16,
                    Margin = new Thickness(10, 10, 10, 10),
                    HorizontalContentAlignment = HorizontalAlignment.Center
                };
                theGrid.Children.Add(l);
                Grid.SetColumn(l, 0);
                l = new Label()
                {
                    Content = "End Age",
                    FontSize = 16,
                    Margin = new Thickness(10, 10, 10, 10),
                    HorizontalContentAlignment = HorizontalAlignment.Center
                };
                theGrid.Children.Add(l);
                Grid.SetColumn(l, 1);
                this.Content = theGrid;
                this.IsTabStop = false;
            }
        }

        private class AAgeGroup : ListBoxItem
        {
            public TextBox StartAge { get; private set; }
            public TextBox EndAge { get; private set; }
            public Button Remove { get; private set; }
            public Button Update { get; private set; }

            private AgeGroupsPage page;
            public AgeGroup MyGroup { get; private set; }

            private readonly Regex allowedChars = new Regex("[^0-9]+");

            public AAgeGroup(AgeGroupsPage page, AgeGroup group)
            {
                this.page = page;
                this.MyGroup = group;
                Grid theGrid = new Grid()
                {
                    MaxWidth = 400
                };
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                StartAge = new TextBox()
                {
                    Text = group.StartAge.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(10, 10, 10, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                StartAge.GotFocus += new RoutedEventHandler(this.SelectAll);
                StartAge.PreviewTextInput += new TextCompositionEventHandler(this.NumberValidation);
                theGrid.Children.Add(StartAge);
                Grid.SetColumn(StartAge, 0);
                EndAge = new TextBox()
                {
                    Text = group.EndAge.ToString(),
                    FontSize = 16,
                    Margin = new Thickness(10, 10, 10, 10),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                EndAge.GotFocus += new RoutedEventHandler(this.SelectAll);
                EndAge.PreviewTextInput += new TextCompositionEventHandler(this.NumberValidation);
                theGrid.Children.Add(EndAge);
                Grid.SetColumn(EndAge, 1);
                Remove = new Button()
                {
                    Content = "Remove",
                    FontSize = 16,
                    Height = 35,
                    Margin = new Thickness(10, 10, 10, 10)
                };
                Remove.Click += new RoutedEventHandler(this.Remove_Click);
                theGrid.Children.Add(Remove);
                Grid.SetColumn(Remove, 2);
                this.Content = theGrid;
                this.IsTabStop = false;
            }

            private void NumberValidation(object sender, TextCompositionEventArgs e)
            {
                e.Handled = allowedChars.IsMatch(e.Text);
            }

            private void Remove_Click(object sender, RoutedEventArgs e)
            {
                Log.D("Removing.");
                page.RemoveAgeGroup(this);
            }

            public AgeGroup GetAgeGroup()
            {
                int start = MyGroup.StartAge, end = MyGroup.EndAge;
                int.TryParse(StartAge.Text, out start);
                int.TryParse(EndAge.Text, out end);
                MyGroup.StartAge = start;
                MyGroup.EndAge = end;
                return MyGroup;
            }

            private void SelectAll(object sender, RoutedEventArgs e)
            {
                TextBox src = (TextBox)e.OriginalSource;
                src.SelectAll();
            }
        }
    }
}
