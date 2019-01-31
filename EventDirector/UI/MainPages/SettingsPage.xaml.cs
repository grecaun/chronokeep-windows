using EventDirector.Interfaces;
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
using Microsoft.WindowsAPICodePack.Dialogs;

namespace EventDirector.UI.MainPages
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
                Content = "Last Used",
                Uid = Constants.Settings.TIMING_LAST_USED
            });
            DefaultTimingBox.Items.Add(new ComboBoxItem()
            {
                Content = "Manual",
                Uid = Constants.Settings.TIMING_MANUAL
            });
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
            if (setting.value == Constants.Settings.TIMING_MANUAL)
            {
                DefaultTimingBox.SelectedIndex = 1;
            }
            else if (setting.value == Constants.Settings.TIMING_RFID)
            {
                DefaultTimingBox.SelectedIndex = 2;
            }
            else if (setting.value == Constants.Settings.TIMING_IPICO)
            {
                DefaultTimingBox.SelectedIndex = 3;
            }
            else
            {
                DefaultTimingBox.SelectedIndex = 0;
            }
            CompanyNameBox.Text = database.GetAppSetting(Constants.Settings.COMPANY_NAME).value;
            DefaultExportDirBox.Text = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value;
            DefaultWaiverBox.Text = database.GetAppSetting(Constants.Settings.DEFAULT_WAIVER).value;
            UpdatePage.IsChecked = database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).value == Constants.Settings.SETTING_TRUE;
            ExitNoPrompt.IsChecked = database.GetAppSetting(Constants.Settings.EXIT_NO_PROMPT).value == Constants.Settings.SETTING_TRUE;
            mWindow.NonUIUpdate();
        }

        private async void ResetDB_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Reset button clicked.");
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
            }
        }

        private async void RebuildDB_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Rebuild button clicked.");
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
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Save button clicked.");
            database.SetAppSetting(Constants.Settings.COMPANY_NAME, CompanyNameBox.Text.Trim());
            database.SetAppSetting(Constants.Settings.DEFAULT_TIMING_SYSTEM, ((ComboBoxItem)DefaultTimingBox.SelectedItem).Uid);
            database.SetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR, DefaultExportDirBox.Text.Trim());
            database.SetAppSetting(Constants.Settings.DEFAULT_WAIVER, DefaultWaiverBox.Text);
            database.SetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE, UpdatePage.IsChecked == true ? Constants.Settings.SETTING_TRUE : Constants.Settings.SETTING_FALSE);
            database.SetAppSetting(Constants.Settings.EXIT_NO_PROMPT, ExitNoPrompt.IsChecked == true ? Constants.Settings.SETTING_TRUE : Constants.Settings.SETTING_FALSE);
            UpdateView();
        }

        private void ChangeExport_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Change export directory button clicked.");
            try
            {
                using (var dialog = new CommonOpenFileDialog())
                {
                    dialog.Title = "Export Directory";
                    dialog.IsFolderPicker = true;
                    dialog.InitialDirectory = DefaultExportDirBox.Text;

                    dialog.AddToMostRecentlyUsedList = false;
                    dialog.AllowNonFileSystemItems = false;
                    dialog.DefaultDirectory = DefaultExportDirBox.Text;
                    dialog.EnsureFileExists = true;
                    dialog.EnsurePathExists = true;
                    dialog.EnsureValidNames = true;
                    dialog.Multiselect = false;
                    dialog.ShowPlacesList = true;

                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        DefaultExportDirBox.Text = dialog.FileName;
                    }
                }
            }
            catch
            {
                Log.E("Something went wrong with the dialog.");
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
