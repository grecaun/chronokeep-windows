using Chronokeep.Interfaces;
using Chronokeep.Objects.Changelog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using Wpf.Ui.Controls;

namespace Chronokeep.UI
{
    /// <summary>
    /// Interaction logic for ChangeLogWindow.xaml
    /// </summary>
    public partial class ChangelogWindow : FluentWindow
    {
        private IWindowCallback window;
        private IDBInterface database;

        private ChangelogWindow(IWindowCallback window, IDBInterface database)
        {
            InitializeComponent();
            this.window = window;

            string changelogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "changelog");
            string[] changelogFiles = Directory.GetFiles(changelogPath);
            if (changelogFiles.Length < 1)
            {
                Close();
                return;
            }
            List<Entry> changelogEntries = new();
            foreach (string file in changelogFiles)
            {
                string jsonData = File.ReadAllText(file);
                Entry entry = JsonSerializer.Deserialize<Entry>(jsonData);
                changelogEntries.Add(entry);
            }
            changelogEntries.Sort();
            changelogEntries[0].IsExpanded = true;
            logList.ItemsSource = changelogEntries;
            this.database = database;
            AppSetting autoChangelog = database.GetAppSetting(Constants.Settings.AUTO_SHOW_CHANGELOG);
            autoChangelogToggleSwitch.IsChecked = autoChangelog != null && autoChangelog.Value == Constants.Settings.SETTING_TRUE;
        }

        public static ChangelogWindow NewWindow(IWindowCallback window, IDBInterface database)
        {
            return new ChangelogWindow(window, database);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.Notifications.ChangelogWindow", "Done button clicked.");
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            database.SetAppSetting(Constants.Settings.AUTO_SHOW_CHANGELOG, autoChangelogToggleSwitch.IsChecked == true ? Constants.Settings.SETTING_TRUE : Constants.Settings.SETTING_FALSE);
            window.WindowFinalize(this);
        }
    }
}
