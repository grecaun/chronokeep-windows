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
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Chronokeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page, IMainPage
    {
        IMainWindow mWindow;
        IDBInterface database;

        private int SystemTheme = -1;
        private int ThemeOffset = -1;

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
            SystemTheme = Utils.GetSystemTheme();
            if (SystemTheme != -1)
            {
                ThemeOffset = 0;
                ThemeColorBox.Items.Add(new ComboBoxItem()
                {
                    Content = "System",
                    Uid = Constants.Settings.THEME_SYSTEM
                });
            }
            ThemeColorBox.Items.Add(new ComboBoxItem()
            {
                Content = "Light",
                Uid = Constants.Settings.THEME_LIGHT
            });
            ThemeColorBox.Items.Add(new ComboBoxItem()
            {
                Content = "Dark",
                Uid = Constants.Settings.THEME_DARK
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
            AppSetting themeSetting = database.GetAppSetting(Constants.Settings.CURRENT_THEME);
            Log.D("UI.MainPages.SettingsPage", "Current theme set to " + themeSetting.value + " Theme Offset is " + ThemeOffset);
            if (themeSetting.value == Constants.Settings.THEME_SYSTEM)
            {
                Log.D("UI.MainPages.SettingsPage", "Setting selected theme to System.");
                ThemeColorBox.SelectedIndex = 0;
            }
            else if (themeSetting.value == Constants.Settings.THEME_LIGHT)
            {
                Log.D("UI.MainPages.SettingsPage", "Setting selected theme to Light. " + (ThemeOffset + 1));
                ThemeColorBox.SelectedIndex = ThemeOffset + 1;
            }
            else
            {
                Log.D("UI.MainPages.SettingsPage", "Setting selected theme to Dark. " + (ThemeOffset + 2));
                ThemeColorBox.SelectedIndex = ThemeOffset + 2;
            }
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
            database.SetAppSetting(Constants.Settings.CURRENT_THEME, ((ComboBoxItem)ThemeColorBox.SelectedItem).Uid);
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private void ThemeColorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selectedItem = ThemeColorBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                database.SetAppSetting(Constants.Settings.CURRENT_THEME, ((ComboBoxItem)ThemeColorBox.SelectedItem).Uid);
                var theme = Wpf.Ui.Appearance.ThemeType.Light;
                if ((selectedItem.Uid == Constants.Settings.THEME_SYSTEM && SystemTheme == 0) || selectedItem.Uid == Constants.Settings.THEME_DARK)
                {
                    theme = Wpf.Ui.Appearance.ThemeType.Dark;
                }
                Wpf.Ui.Appearance.Theme.Apply(theme, Wpf.Ui.Appearance.BackgroundType.Mica, false);
            }
        }
    }
}
