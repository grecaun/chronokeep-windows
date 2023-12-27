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
using Chronokeep.UI.UIObjects;
using Chronokeep.Helpers;
using System.Media;
using System.Resources;

namespace Chronokeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : IMainPage
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
                Content = Constants.Readers.SYSTEM_NAMES[Constants.Readers.SYSTEM_RFID],
                Uid = Constants.Readers.SYSTEM_RFID
            });
            DefaultTimingBox.Items.Add(new ComboBoxItem()
            {
                Content = Constants.Readers.SYSTEM_NAMES[Constants.Readers.SYSTEM_CHRONOKEEP_PORTAL],
                Uid = Constants.Readers.SYSTEM_CHRONOKEEP_PORTAL
            });
            DefaultTimingBox.Items.Add(new ComboBoxItem()
            {
                Content = Constants.Readers.SYSTEM_NAMES[Constants.Readers.SYSTEM_IPICO],
                Uid = Constants.Readers.SYSTEM_IPICO
            });
            DefaultTimingBox.Items.Add(new ComboBoxItem()
            {
                Content = Constants.Readers.SYSTEM_NAMES[Constants.Readers.SYSTEM_IPICO_LITE],
                Uid = Constants.Readers.SYSTEM_IPICO_LITE
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
            for (int i=1; i<=60; i++)
            {
                UploadIntervalBox.Items.Add(new ComboBoxItem()
                {
                    Content = i.ToString(),
                    Uid = i.ToString(),
                });
            }
            for (int i=15; i<=180; i+=5)
            {
                AnnouncerWindowBox.Items.Add(new ComboBoxItem()
                {
                    Content = i.ToString(),
                    Uid = i.ToString(),
                });
            }
            UpdateView();
        }

        public void UpdateView()
        {
            AppSetting setting = database.GetAppSetting(Constants.Settings.DEFAULT_TIMING_SYSTEM);
            switch (setting.Value)
            {
                case Constants.Readers.SYSTEM_CHRONOKEEP_PORTAL:
                    DefaultTimingBox.SelectedIndex = 1;
                    break;
                case Constants.Readers.SYSTEM_IPICO:
                    DefaultTimingBox.SelectedIndex = 2;
                    break;
                case Constants.Readers.SYSTEM_IPICO_LITE:
                    DefaultTimingBox.SelectedIndex = 3;
                    break;
                default:
                    DefaultTimingBox.SelectedIndex = 0;
                    break;
            }
            CompanyNameBox.Text = database.GetAppSetting(Constants.Settings.COMPANY_NAME).Value;
            ContactEmailBox.Text = database.GetAppSetting(Constants.Settings.CONTACT_EMAIL).Value;
            DefaultExportDirBox.Text = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).Value;
            UpdatePage.IsChecked = database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE).Value == Constants.Settings.SETTING_TRUE;
            ExitNoPrompt.IsChecked = database.GetAppSetting(Constants.Settings.EXIT_NO_PROMPT).Value == Constants.Settings.SETTING_TRUE;
            CheckUpdates.IsChecked = database.GetAppSetting(Constants.Settings.CHECK_UPDATES).Value == Constants.Settings.SETTING_TRUE;
            AppSetting themeSetting = database.GetAppSetting(Constants.Settings.CURRENT_THEME);
            Log.D("UI.MainPages.SettingsPage", "Current theme set to " + themeSetting.Value + " Theme Offset is " + ThemeOffset);
            if (themeSetting.Value == Constants.Settings.THEME_SYSTEM)
            {
                Log.D("UI.MainPages.SettingsPage", "Setting selected theme to System.");
                ThemeColorBox.SelectedIndex = 0;
            }
            else if (themeSetting.Value == Constants.Settings.THEME_LIGHT)
            {
                Log.D("UI.MainPages.SettingsPage", "Setting selected theme to Light. " + (ThemeOffset + 1));
                ThemeColorBox.SelectedIndex = ThemeOffset + 1;
            }
            else
            {
                Log.D("UI.MainPages.SettingsPage", "Setting selected theme to Dark. " + (ThemeOffset + 2));
                ThemeColorBox.SelectedIndex = ThemeOffset + 2;
            }
            int uploadInt;
            if (int.TryParse(database.GetAppSetting(Constants.Settings.UPLOAD_INTERVAL).Value, out uploadInt) && uploadInt > 0 && uploadInt < 60) {
                UploadIntervalBox.SelectedIndex = uploadInt - 1;
            }
            int announcerWindow;
            if (int.TryParse(database.GetAppSetting(Constants.Settings.ANNOUNCER_WINDOW).Value, out announcerWindow) && announcerWindow >= 15 && announcerWindow <= 180)
            {
                AnnouncerWindowBox.SelectedIndex = (announcerWindow - 15) / 5;
            }
            int alarm = 1;
            if (int.TryParse(database.GetAppSetting(Constants.Settings.ALARM_SOUND).Value, out alarm))
            {
                AlarmSoundBox.SelectedIndex = alarm - 1;
            }
        }

        private async void ResetDB_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.SettingsPage", "Reset button clicked.");
            bool YesClicked = false;
            DialogBox.Show(
                "This deletes all of the data stored in the database.  You cannot recover any of the data in the database after this step.\n\nAre you sure you wish to continue?",
                "Yes",
                "No",
                () =>
                {
                    YesClicked = true;
                });
            if (YesClicked)
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
            bool YesClicked = false;
            DialogBox.Show(
                "This deletes all of the tables and values in the database, then rebuilds all of the tables.  You cannot recover any of the data in the database after this step.\n\nAre you sure you wish to continue?",
                "Yes",
                "No",
                () =>
                {
                    YesClicked = true;
                });
            if (YesClicked)
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
            SaveSettings();
            UpdateView();
        }

        private void SaveSettings()
        {
            Log.D("UI.MainPages.SettingsPage", "Saving.");
            database.SetAppSetting(Constants.Settings.COMPANY_NAME, CompanyNameBox.Text.Trim());
            database.SetAppSetting(Constants.Settings.CONTACT_EMAIL, ContactEmailBox.Text.Trim());
            database.SetAppSetting(Constants.Settings.DEFAULT_TIMING_SYSTEM, ((ComboBoxItem)DefaultTimingBox.SelectedItem).Uid);
            database.SetAppSetting(Constants.Settings.CURRENT_THEME, ((ComboBoxItem)ThemeColorBox.SelectedItem).Uid);
            database.SetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR, DefaultExportDirBox.Text.Trim());
            database.SetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE, UpdatePage.IsChecked == true ? Constants.Settings.SETTING_TRUE : Constants.Settings.SETTING_FALSE);
            database.SetAppSetting(Constants.Settings.EXIT_NO_PROMPT, ExitNoPrompt.IsChecked == true ? Constants.Settings.SETTING_TRUE : Constants.Settings.SETTING_FALSE);
            database.SetAppSetting(Constants.Settings.CHECK_UPDATES, CheckUpdates.IsChecked == true ? Constants.Settings.SETTING_TRUE : Constants.Settings.SETTING_FALSE);
            database.SetAppSetting(Constants.Settings.UPLOAD_INTERVAL, ((ComboBoxItem)UploadIntervalBox.SelectedItem).Uid);
            database.SetAppSetting(Constants.Settings.ALARM_SOUND, ((ComboBoxItem)AlarmSoundBox.SelectedItem).Uid);
            if (!int.TryParse(((ComboBoxItem)UploadIntervalBox.SelectedItem).Uid, out Globals.UploadInterval))
            {
                DialogBox.Show("Something went wrong trying to update the upload interval.");
            }
            database.SetAppSetting(Constants.Settings.ANNOUNCER_WINDOW, ((ComboBoxItem)AnnouncerWindowBox.SelectedItem).Uid);
            if (!int.TryParse(((ComboBoxItem)AnnouncerWindowBox.SelectedItem).Uid, out Globals.AnnouncerWindow))
            {
                DialogBox.Show("Something went wrong trying to update the announcer window.");
            }
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
            Log.D("UI.MainPages.SettingsPage", "Closing page.");
            if (UpdatePage.IsChecked == true)
            {
                SaveSettings();
            }
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
                Wpf.Ui.Appearance.Theme.Apply(theme, Wpf.Ui.Appearance.BackgroundType.Mica, true, true);
            }
        }

        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.MainPages.SettingsPage", "Play alarm sound clicked.");
            string soundFile = Environment.CurrentDirectory;
            switch (AlarmSoundBox.SelectedIndex)
            {
                case 0:
                    soundFile += "\\Sounds\\alert-1.wav";
                    break;
                case 1:
                    soundFile += "\\Sounds\\alert-2.wav";
                    break;
                case 2:
                    soundFile += "\\Sounds\\alert-3.wav";
                    break;
                case 3:
                    soundFile += "\\Sounds\\alert-4.wav";
                    break;
                case 4:
                    soundFile += "\\Sounds\\alert-5.wav";
                    break;
                default:
                    DialogBox.Show("Sound not selected.");
                    return;
            }
            Log.D("UI.MainPages.SettingsPage", "Path we're trying to play: " + soundFile);
            try
            {
                new SoundPlayer(soundFile).Play();
            }
            catch (Exception ex)
            {
                DialogBox.Show("Error trying to play sound. " + ex.Message);
            }
        }
    }
}
