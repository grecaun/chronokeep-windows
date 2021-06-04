﻿using ChronoKeep.IO;
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

namespace ChronoKeep.UI.Timing.Import
{
    /// <summary>
    /// Interaction logic for ImportLogPage1.xaml
    /// </summary>
    public partial class ImportLogPage1 : Page
    {
        LogImporter importer;
        ImportLogWindow parent;

        public ImportLogPage1(ImportLogWindow parent, LogImporter importer, List<TimingLocation> locations)
        {
            InitializeComponent();
            this.parent = parent;
            this.importer = importer;
            TypeHolder.Items.Clear();
            ComboBoxItem current, selected = null, custom = null;
            foreach (LogImporter.Type type in Enum.GetValues(typeof(LogImporter.Type)))
            {
                current = new ComboBoxItem()
                {
                    Content = type.ToString(),
                    Uid = type.ToString()
                };
                TypeHolder.Items.Add(current);
                if (type == importer.type)
                {
                    selected = current;
                }
                if (type == LogImporter.Type.CUSTOM)
                {
                    custom = current;
                }
            }
            if (selected == null)
            {
                selected = custom;
            }
            TypeHolder.SelectedItem = selected;
            UpdateLocations(locations);
        }

        public void UpdateLocations(List<TimingLocation> locations)
        {
            Log.D("Updating locations in import log page 1.");
            int locationId = -12;
            if (LocationHolder.SelectedItem != null)
            {
                locationId = Convert.ToInt32(((ComboBoxItem)LocationHolder.SelectedItem).Uid);
            }
            LocationHolder.Items.Clear();
            ComboBoxItem current, selected = null;
            foreach (TimingLocation loc in locations)
            {
                current = new ComboBoxItem()
                {
                    Content = loc.Name,
                    Uid = loc.Identifier.ToString()
                };
                LocationHolder.Items.Add(current);
                if (locationId == loc.Identifier)
                {
                    selected = current;
                }
            }
            if (selected != null)
            {
                LocationHolder.SelectedItem = selected;
            }
            else
            {
                LocationHolder.SelectedIndex = 0;
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Next Button Clicked.");
            int locationId = Convert.ToInt32(((ComboBoxItem)LocationHolder.SelectedItem).Uid);
            Log.D("Location ID is: " + locationId + " name of: " + ((ComboBoxItem)LocationHolder.SelectedItem).Content.ToString());
            if (((ComboBoxItem)TypeHolder.SelectedItem).Uid == LogImporter.Type.CUSTOM.ToString())
            {
                parent.Next(locationId);
                return;
            }
            foreach (LogImporter.Type type in Enum.GetValues(typeof(LogImporter.Type)))
            {
                if (((ComboBoxItem)TypeHolder.SelectedItem).Uid == type.ToString()) {
                    parent.Import(type, locationId, 0, 0);
                    return;
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Cancel Button Clicked.");
            parent.Cancel();
        }

        private void TypeHolder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.D("Type changed.");
            if (((ComboBoxItem)TypeHolder.SelectedItem).Uid == LogImporter.Type.CUSTOM.ToString()) {
                NextButton.Content = "Next";
            }
            else
            {
                NextButton.Content = "Import";
            }
        }
    }
}
