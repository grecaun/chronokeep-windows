﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EventDirector
{
    internal class HListBoxItem : ListBoxItem
    {
        public Label HeaderLabel { get; private set; }
        public ComboBox HeaderBox { get; private set; }
        public int Index { get; private set; }

        public HListBoxItem(String s, int ix)
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

    internal class H2ListBoxItem : ListBoxItem
    {
        public Label HeaderLabel { get; private set; }
        public ComboBox HeaderBox { get; private set; }
        public int Index { get; private set; }
        public string[] human_fields = {
            "",
            "Bib",
            "Chip"
        };

        public H2ListBoxItem(String s, int ix)
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

    internal class DListBoxItemAlt : ListBoxItem
    {
        public Label DivisionName { get; private set; }
        public ComboBox Divisions { get; private set; }

        public DListBoxItemAlt(String name, List<Division> divisions)
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

    internal class DListBoxItem : ListBoxItem
    {
        public Label DivisionName { get; private set; }
        public TextBox DivisionCost { get; private set; }

        public DListBoxItem(String name, int cost)
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

    internal class D2ListBoxItem : ListBoxItem
    {
        public TextBox DivisionName { get; private set; }
        public TextBox DivisionCost { get; private set; }

        public D2ListBoxItem(String name, int cost)
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