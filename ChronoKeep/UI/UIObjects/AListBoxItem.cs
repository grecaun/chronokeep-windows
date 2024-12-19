using Chronokeep.Objects;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Chronokeep
{
    internal class HeaderListBoxItem : ListBoxItem
    {
        public TextBlock HeaderLabel { get; private set; }
        public ComboBox HeaderBox { get; private set; }
        public int Index { get; private set; }

        public HeaderListBoxItem(string s, int ix)
        {
            this.IsTabStop = false;
            Index = ix;
            Grid theGrid = new Grid();
            this.Content = theGrid;
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(150) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            HeaderLabel = new TextBlock
            {
                Text = s,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 18,
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
        public TextBlock HeaderLabel { get; private set; }
        public ComboBox HeaderBox { get; private set; }
        public int Index { get; private set; }

        public LogListBoxItem(string s, int ix, string[] human_fields, int selectedIx)
        {
            this.IsTabStop = false;
            Index = ix;
            Grid theGrid = new Grid();
            this.Content = theGrid;
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            HeaderLabel = new TextBlock
            {
                Text = s,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 18,
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
        public TextBlock HeaderLabel { get; private set; }
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
            HeaderLabel = new TextBlock
            {
                Text = s,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 18,
                Margin = new Thickness(5, 0, 0, 0),
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
        public TextBlock DistanceName { get; private set; }
        public ComboBox Distances { get; private set; }

        public DistanceListBoxItemAlternate(string name, List<Distance> distances)
        {
            this.IsTabStop = false;
            Grid theGrid = new Grid();
            this.Content = theGrid;
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            DistanceName = new TextBlock
            {
                Text = name,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 18,
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
            return DistanceName.Text.ToString().Trim();
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
        public Wpf.Ui.Controls.ToggleSwitch Keep { get; private set; }
        public TextBlock Existing { get; private set; }
        public TextBlock Bib { get; private set; }
        public TextBlock Distance { get; private set; }
        public TextBlock PartName { get; private set; }
        public TextBlock Age { get; private set; }
        public TextBlock Sex { get; private set; }
        public Participant Part { get; private set; }

        public MultipleEntryListBoxItem(Participant person, Event theEvent)
        {
            this.Part = person;
            this.IsTabStop = false;
            Grid theGrid = new Grid();
            this.Content = theGrid;
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) });
            theGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) });
#pragma warning disable CA1416 // Validate platform compatibility
            Keep = new();
#pragma warning restore CA1416 // Validate platform compatibility
            theGrid.Children.Add(Keep);
            Grid.SetColumn(Keep, 0);
            Existing = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = (person.Identifier == Constants.Timing.PARTICIPANT_DUMMYIDENTIFIER ? "" : "X")
            };
            theGrid.Children.Add(Existing);
            Grid.SetColumn(Existing, 1);
            Bib = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = person.Bib.ToString()
            };
            theGrid.Children.Add(Bib);
            Grid.SetColumn(Bib, 2);
            Distance = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = person.Distance
            };
            theGrid.Children.Add(Distance);
            Grid.SetColumn(Distance, 3);
            PartName = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = string.Format("{0} {1}", person.FirstName, person.LastName)
            };
            theGrid.Children.Add(PartName);
            Grid.SetColumn(PartName, 4);
            Sex = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = person.Gender
            };
            theGrid.Children.Add(Sex);
            Grid.SetColumn(Sex, 5);
            Age = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = person.Age(theEvent.Date)
            };
            theGrid.Children.Add(Age);
            Grid.SetColumn(Age, 6);
        }
    }
}
