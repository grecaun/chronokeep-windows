using Chronokeep.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace Chronokeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page, IMainPage
    {
        IMainWindow mWindow;
        IDBInterface database;

        public SettingsPage(IMainWindow mainWindow, IDBInterface database)
        {
            InitializeComponent();
            this.mWindow = mainWindow;
            this.database = database;
            DefaultTimingBox.Items.Clear();
            DefaultTimingBox.Items.Add(new ComboBoxItem()
            {
                Content = "RFID",
                Uid = Constants.Settings.TIMING_RFID
            });
            DefaultTimingBox.Items.Add(new ComboBoxItem()
            {
                Content = "Ipico",
                Uid = Constants.Settings.TIMING_IPICO
            });
            UpdateView();
        }

        public void UpdateView()
        {
            AppSetting setting = database.GetAppSetting(Constants.Settings.DEFAULT_TIMING_SYSTEM);
            if (setting.value == Constants.Settings.TIMING_IPICO)
            {
                DefaultTimingBox.SelectedIndex = 1;
            }
            else
            {
                DefaultTimingBox.SelectedIndex = 0;
            }
            CompanyNameBox.Text = database.GetAppSetting(Constants.Settings.COMPANY_NAME).value;
            ContactEmailBox.Text = database.GetAppSetting(Constants.Settings.CONTACT_EMAIL).value;
            DefaultExportDirBox.Text = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value;
            UpdatePage.IsChecked = database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE;
            ExitNoPrompt.IsChecked = database.GetAppSetting(Constants.Settings.EXIT_NO_PROMPT).value == Constants.Settings.SETTING_TRUE;
            CheckUpdates.IsChecked = database.GetAppSetting(Constants.Settings.CHECK_UPDATES).value == Constants.Settings.SETTING_TRUE;
        }

        private async void ResetDB_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.SettingsPage", "Reset button clicked.");
            MessageBoxResult result = MessageBox.Show("This deletes all of the data stored in the database.  You cannot recover" +
                " any of the data in the database after this step.\n\nAre you sure you wish to continue?",
                                                        "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                ResetDB.IsEnabled = false;
                await Task.Run(() =>
                {
                    database.ResetDatabase();
                    Constants.Settings.SetupSettings(database);
                });
                UpdateView();
                ResetDB.IsEnabled = true;
                mWindow.UpdateStatus();
            }
        }

        private async void RebuildDB_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.SettingsPage", "Rebuild button clicked.");
            MessageBoxResult result = MessageBox.Show("This deletes all of the tables and values in the database, then rebuilds all of the tables." +
                "  You cannot recover any of the data in the database after this step.\n\nAre you sure you wish to continue?",
                                                        "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                RebuildDB.IsEnabled = false;
                await Task.Run(() =>
                {
                    database.HardResetDatabase();
                    Constants.Settings.SetupSettings(database);
                });
                UpdateView();
                RebuildDB.IsEnabled = true;
                mWindow.UpdateStatus();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.SettingsPage", "Save button clicked.");
            database.SetAppSetting(Constants.Settings.COMPANY_NAME, CompanyNameBox.Text.Trim());
            database.SetAppSetting(Constants.Settings.CONTACT_EMAIL, ContactEmailBox.Text.Trim());
            database.SetAppSetting(Constants.Settings.DEFAULT_TIMING_SYSTEM, ((ComboBoxItem)DefaultTimingBox.SelectedItem).Uid);
            database.SetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR, DefaultExportDirBox.Text.Trim());
            database.SetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE, UpdatePage.IsChecked == true ? Constants.Settings.SETTING_TRUE : Constants.Settings.SETTING_FALSE);
            database.SetAppSetting(Constants.Settings.EXIT_NO_PROMPT, ExitNoPrompt.IsChecked == true ? Constants.Settings.SETTING_TRUE : Constants.Settings.SETTING_FALSE);
            database.SetAppSetting(Constants.Settings.CHECK_UPDATES, CheckUpdates.IsChecked == true ? Constants.Settings.SETTING_TRUE : Constants.Settings.SETTING_FALSE);
            UpdateView();
        }

        private void ChangeExport_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.SettingsPage", "Change export directory button clicked.");
            try
            {
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Export Directory",
                    UseDescriptionForTitle = true,
                    InitialDirectory = DefaultExportDirBox.Text,
                    SelectedPath = DefaultExportDirBox.Text,
                    ShowNewFolderButton = true,
                })
                {

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        DefaultExportDirBox.Text = dialog.SelectedPath;
                    }
                }
            }
            catch
            {
                Log.E("UI.MainPages.SettingsPage", "Something went wrong with the dialog.");
            }
        }

        public void UpdateDatabase() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S()
        {
            Save_Click(null, null);
        }

        public void Keyboard_Ctrl_Z()
        {
            UpdateView();
        }

        public void Closing()
        {
        }
    }
}
