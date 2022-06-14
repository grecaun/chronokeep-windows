using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Chronokeep
{
    internal class HeaderListBoxItem : ListBoxItem
    {
        public Label HeaderLabel { get; private set; }
        public ComboBox HeaderBox { get; private set; }
        public int Index { get; private set; }

        public HeaderListBoxItem(string s, int ix)
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

        public LogListBoxItem(string s, int ix, string[] human_fields, int selectedIx)
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

        public BibChipHeaderListBoxItem(string s, int ix)
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
            if (string.Equals(s, "bib", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }
            else if (string.Equals(s, "chip", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }
            return 0;
        }
    }

    internal class DistanceListBoxItemAlternate : ListBoxItem
    {
        public Label DistanceName { get; private set; }
        public ComboBox Distances { get; private set; }

        public DistanceListBoxItemAlternate(string name, List<Distance> distances)
        {
            this.IsTabStop = false;
            Grid theGrid = new Grid();
            this.Content = theGrid;
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            DistanceName = new Label
            {
                Content = name
            };
            theGrid.Children.Add(DistanceName);
            Grid.SetColumn(DistanceName, 0);
            Distances = new ComboBox();
            Distances.Items.Add(new ComboBoxItem()
            {
                Content = "Auto",
                Uid = "-1"
            });
            foreach (Distance d in distances)
            {
                Distances.Items.Add(new ComboBoxItem()
                {
                    Content = d.Name,
                    Uid = d.Identifier.ToString()
                });
            }
            Distances.SelectedIndex = 0;
            theGrid.Children.Add(Distances);
            Grid.SetColumn(Distances, 1);
        }

        public string NameFromFile()
        {
            return DistanceName.Content.ToString().Trim();
        }

        public int DistanceId()
        {
            int output = -1;
            int.TryParse(((ComboBoxItem)Distances.SelectedItem).Uid, out output);
            return output;
        }
    }

    internal class MultipleEntryListBoxItem : ListBoxItem
    {
        public CheckBox Keep { get; private set; }
        public Label Existing { get; private set; }
        public Label Bib { get; private set; }
        public Label Distance { get; private set; }
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
            Distance = new Label
            {
                Content = person.Distance
            };
            theGrid.Children.Add(Distance);
            Grid.SetColumn(Distance, 3);
            PartName = new Label
            {
                Content = string.Format("{0} {1}", person.FirstName, person.LastName)
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

    internal class DistanceListBoxItem : ListBoxItem
    {
        public Label DistanceName { get; private set; }
        public TextBox DistanceCost { get; private set; }

        public DistanceListBoxItem(string name, int cost)
        {
            this.IsTabStop = false;
            Grid theGrid = new Grid();
            this.Content = theGrid;
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            DistanceName = new Label
            {
                Content = name
            };
            theGrid.Children.Add(DistanceName);
            Grid.SetColumn(DistanceName, 0);
            DistanceCost = new TextBox
            {
                Text = string.Format("{0}.{1:D2}", cost / 100, cost % 100)
            };
            theGrid.Children.Add(DistanceCost);
            Grid.SetColumn(DistanceCost, 1);
            DistanceCost.GotFocus += new RoutedEventHandler(SelectAll);
        }

        public void SelectAll(object sender, RoutedEventArgs e)
        {
            DistanceCost.SelectAll();
        }

        public int Cost()
        {
            int output = 70;
            string[] priceVals = DistanceCost.Text.Split('.');
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
            return DistanceName.Content.ToString().Trim();
        }
    }

    internal class DistanceListBoxItem2 : ListBoxItem
    {
        public TextBox DistanceName { get; private set; }
        public TextBox DistanceCost { get; private set; }

        public DistanceListBoxItem2(string name, int cost)
        {
            this.IsTabStop = false;
            Grid theGrid = new Grid();
            this.Content = theGrid;
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            DistanceName = new TextBox
            {
                Text = name
            };
            theGrid.Children.Add(DistanceName);
            Grid.SetColumn(DistanceName, 0);
            DistanceCost = new TextBox
            {
                Text = string.Format("{0}.{1:D2}", cost / 100, cost % 100)
            };
            theGrid.Children.Add(DistanceCost);
            Grid.SetColumn(DistanceCost, 1);
            DistanceCost.GotFocus += new RoutedEventHandler(SelectAll);
        }

        public void SelectAll(object sender, RoutedEventArgs e)
        {
            DistanceCost.SelectAll();
        }

        public int Cost()
        {
            int output = 70;
            string[] priceVals = DistanceCost.Text.Split('.');
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
            return DistanceName.Text.ToString().Trim();
        }
    }
}
