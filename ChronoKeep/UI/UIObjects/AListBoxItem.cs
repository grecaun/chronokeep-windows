using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChronoKeep
{
    internal class HeaderListBoxItem : ListBoxItem
    {
        public Label HeaderLabel { get; private set; }
        public ComboBox HeaderBox { get; private set; }
        public int Index { get; private set; }

        public HeaderListBoxItem(String s, int ix)
        {
            this.IsTabStop = false;
            Index = ix;
            Grid theGrid = new Grid();
            this.Content = theGrid;
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            HeaderLabel = new Label
            {
                Content = s
            };
            theGrid.Children.Add(HeaderLabel);
            Grid.SetColumn(HeaderLabel, 0);
            HeaderBox = new ComboBox
            {
                ItemsSource = ImportFileWindow.human_fields,
                SelectedIndex = ImportFileWindow.GetHeaderBoxIndex(s.ToLower().Trim()),
            };
            theGrid.Children.Add(HeaderBox);
            Grid.SetColumn(HeaderBox, 1);
        }
    }

    internal class LogListBoxItem : ListBoxItem
    {
        public Label HeaderLabel { get; private set; }
        public ComboBox HeaderBox { get; private set; }
        public int Index { get; private set; }

        public LogListBoxItem(String s, int ix, string[] human_fields, int selectedIx)
        {
            this.IsTabStop = false;
            Index = ix;
            Grid theGrid = new Grid();
            this.Content = theGrid;
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(65) });
            HeaderLabel = new Label
            {
                Content = s
            };
            theGrid.Children.Add(HeaderLabel);
            Grid.SetColumn(HeaderLabel, 0);
            HeaderBox = new ComboBox
            {
                ItemsSource = human_fields,
                SelectedIndex = selectedIx,
            };
            theGrid.Children.Add(HeaderBox);
            Grid.SetColumn(HeaderBox, 1);
        }
    }

    internal class BibChipHeaderListBoxItem : ListBoxItem
    {
        public Label HeaderLabel { get; private set; }
        public ComboBox HeaderBox { get; private set; }
        public int Index { get; private set; }
        public string[] human_fields = {
            "",
            "Bib",
            "Chip"
        };

        public BibChipHeaderListBoxItem(String s, int ix)
        {
            this.IsTabStop = false;
            Index = ix;
            Grid theGrid = new Grid();
            this.Content = theGrid;
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            HeaderLabel = new Label
            {
                Content = s
            };
            theGrid.Children.Add(HeaderLabel);
            Grid.SetColumn(HeaderLabel, 0);
            HeaderBox = new ComboBox
            {
                ItemsSource = human_fields,
                SelectedIndex = GetHeaderBoxIndex(s.ToLower().Trim()),
            };
            theGrid.Children.Add(HeaderBox);
            Grid.SetColumn(HeaderBox, 1);
        }

        internal int GetHeaderBoxIndex(string s) {
            if (String.Equals(s, "bib", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }
            else if (String.Equals(s, "chip", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }
            return 0;
        }
    }

    internal class DivisionListBoxItemAlternate : ListBoxItem
    {
        public Label DivisionName { get; private set; }
        public ComboBox Divisions { get; private set; }

        public DivisionListBoxItemAlternate(String name, List<Division> divisions)
        {
            this.IsTabStop = false;
            Grid theGrid = new Grid();
            this.Content = theGrid;
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            DivisionName = new Label
            {
                Content = name
            };
            theGrid.Children.Add(DivisionName);
            Grid.SetColumn(DivisionName, 0);
            Divisions = new ComboBox();
            Divisions.Items.Add(new ComboBoxItem()
            {
                Content = "Auto",
                Uid = "-1"
            });
            foreach (Division d in divisions)
            {
                Divisions.Items.Add(new ComboBoxItem()
                {
                    Content = d.Name,
                    Uid = d.Identifier.ToString()
                });
            }
            Divisions.SelectedIndex = 0;
            theGrid.Children.Add(Divisions);
            Grid.SetColumn(Divisions, 1);
        }

        public string NameFromFile()
        {
            return DivisionName.Content.ToString().Trim();
        }

        public int DivisionId()
        {
            int output = -1;
            int.TryParse(((ComboBoxItem)Divisions.SelectedItem).Uid, out output);
            return output;
        }
    }

    internal class MultipleEntryListBoxItem : ListBoxItem
    {
        public CheckBox Keep { get; private set; }
        public Label Existing { get; private set; }
        public Label Bib { get; private set; }
        public Label Division { get; private set; }
        public Label PartName { get; private set; }
        public Label Age { get; private set; }
        public Label Sex { get; private set; }
        public Participant Part { get; private set; }

        public MultipleEntryListBoxItem(Participant person, Event theEvent)
        {
            this.Part = person;
            this.IsTabStop = false;
            Grid theGrid = new Grid();
            this.Content = theGrid;
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(20) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(20) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) });
            Keep = new CheckBox();
            theGrid.Children.Add(Keep);
            Grid.SetColumn(Keep, 0);
            Existing = new Label
            {
                Content = (person.Identifier == Constants.Timing.PARTICIPANT_DUMMYIDENTIFIER ? "" : "X")
            };
            theGrid.Children.Add(Existing);
            Grid.SetColumn(Existing, 1);
            Bib = new Label
            {
                Content = person.Bib.ToString()
            };
            theGrid.Children.Add(Bib);
            Grid.SetColumn(Bib, 2);
            Division = new Label
            {
                Content = person.Division
            };
            theGrid.Children.Add(Division);
            Grid.SetColumn(Division, 3);
            PartName = new Label
            {
                Content = String.Format("{0} {1}", person.FirstName, person.LastName)
            };
            theGrid.Children.Add(PartName);
            Grid.SetColumn(PartName, 4);
            Sex = new Label
            {
                Content = person.Gender
            };
            theGrid.Children.Add(Sex);
            Grid.SetColumn(Sex, 5);
            Age = new Label
            {
                Content = person.GetAge(theEvent.Date)
            };
            theGrid.Children.Add(Age);
            Grid.SetColumn(Age, 6);
        }
    }

    internal class DivisionListBoxItem : ListBoxItem
    {
        public Label DivisionName { get; private set; }
        public TextBox DivisionCost { get; private set; }

        public DivisionListBoxItem(String name, int cost)
        {
            this.IsTabStop = false;
            Grid theGrid = new Grid();
            this.Content = theGrid;
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            DivisionName = new Label
            {
                Content = name
            };
            theGrid.Children.Add(DivisionName);
            Grid.SetColumn(DivisionName, 0);
            DivisionCost = new TextBox
            {
                Text = String.Format("{0}.{1:D2}", cost / 100, cost % 100)
            };
            theGrid.Children.Add(DivisionCost);
            Grid.SetColumn(DivisionCost, 1);
            DivisionCost.GotFocus += new RoutedEventHandler(SelectAll);
        }

        public void SelectAll(object sender, RoutedEventArgs e)
        {
            DivisionCost.SelectAll();
        }

        public int Cost()
        {
            int output = 70;
            string[] priceVals = DivisionCost.Text.Split('.');
            if (priceVals.Length > 0)
            {
                int.TryParse(priceVals[0], out output);
            }
            output = output * 100;
            int cents = 0;
            if (priceVals.Length > 1)
            {
                int.TryParse(priceVals[1], out cents);
            }
            while (cents > 100)
            {
                cents = cents / 100;
            }
            return output + cents;
        }

        public string DivName()
        {
            return DivisionName.Content.ToString().Trim();
        }
    }

    internal class DivisionListBoxItem2 : ListBoxItem
    {
        public TextBox DivisionName { get; private set; }
        public TextBox DivisionCost { get; private set; }

        public DivisionListBoxItem2(String name, int cost)
        {
            this.IsTabStop = false;
            Grid theGrid = new Grid();
            this.Content = theGrid;
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            DivisionName = new TextBox
            {
                Text = name
            };
            theGrid.Children.Add(DivisionName);
            Grid.SetColumn(DivisionName, 0);
            DivisionCost = new TextBox
            {
                Text = String.Format("{0}.{1:D2}", cost / 100, cost % 100)
            };
            theGrid.Children.Add(DivisionCost);
            Grid.SetColumn(DivisionCost, 1);
            DivisionCost.GotFocus += new RoutedEventHandler(SelectAll);
        }

        public void SelectAll(object sender, RoutedEventArgs e)
        {
            DivisionCost.SelectAll();
        }

        public int Cost()
        {
            int output = 70;
            string[] priceVals = DivisionCost.Text.Split('.');
            if (priceVals.Length > 0)
            {
                int.TryParse(priceVals[0], out output);
            }
            output = output * 100;
            int cents = 0;
            if (priceVals.Length > 1)
            {
                int.TryParse(priceVals[1], out cents);
            }
            while (cents > 100)
            {
                cents = cents / 100;
            }
            return output + cents;
        }

        public string DivName()
        {
            return DivisionName.Text.ToString().Trim();
        }
    }
}
