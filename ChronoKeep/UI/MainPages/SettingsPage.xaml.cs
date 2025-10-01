using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Chronokeep.UI.UIObjects;
using Chronokeep.Helpers;
using System.Media;
using System.Text.RegularExpressions;
using Chronokeep.Database;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;

namespace Chronokeep.UI.MainPages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : IMainPage
    {
        private readonly IMainWindow mWindow;
        private readonly IDBInterface database;

        private readonly int SystemTheme = -1;
        private readonly int ThemeOffset = -1;

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
            AutoChangelog.IsChecked = database.GetAppSetting(Constants.Settings.AUTO_SHOW_CHANGELOG).Value == Constants.Settings.SETTING_TRUE;
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
            if (int.TryParse(database.GetAppSetting(Constants.Settings.UPLOAD_INTERVAL).Value, out int uploadInt) && uploadInt > 0 && uploadInt < 60) {
                uploadSlider.Value = uploadInt;
                uploadBlock.Text = uploadInt.ToString();
            }
            if (int.TryParse(database.GetAppSetting(Constants.Settings.DOWNLOAD_INTERVAL).Value, out int downloadInt) && downloadInt > 0 && downloadInt < 60)
            {
                downloadSlider.Value = downloadInt;
                downloadBlock.Text = downloadInt.ToString();
            }
            if (int.TryParse(database.GetAppSetting(Constants.Settings.ANNOUNCER_WINDOW).Value, out int announcerWindow) && announcerWindow >= 15 && announcerWindow <= 180)
            {
                announcerSlider.Value = announcerWindow;
                announcerBlock.Text = announcerWindow.ToString();
            }
            int alarm = 1;
            if (int.TryParse(database.GetAppSetting(Constants.Settings.ALARM_SOUND).Value, out alarm))
            {
                AlarmSoundBox.SelectedIndex = alarm - 1;
            }
            RegistrationServerNameBox.Text = database.GetAppSetting(Constants.Settings.SERVER_NAME).Value;
            TwilioAccountSIDBox.Text = database.GetAppSetting(Constants.Settings.TWILIO_ACCOUNT_SID).Value;
            TwilioAuthTokenBox.Text = database.GetAppSetting(Constants.Settings.TWILIO_AUTH_TOKEN).Value;
            TwilioPhoneNumberBox.Text = database.GetAppSetting(Constants.Settings.TWILIO_PHONE_NUMBER).Value;
            MailgunFromNameBox.Text = database.GetAppSetting(Constants.Settings.MAILGUN_FROM_NAME).Value;
            MailgunFromEmailBox.Text = database.GetAppSetting(Constants.Settings.MAILGUN_FROM_EMAIL).Value;
            MailgunAPIKeyBox.Text = database.GetAppSetting(Constants.Settings.MAILGUN_API_KEY).Value;
            MailgunAPIURLBox.Text = database.GetAppSetting(Constants.Settings.MAILGUN_API_URL).Value;
            UniqueProgramID.Text = database.GetAppSetting(Constants.Settings.PROGRAM_UNIQUE_MODIFIER).Value;
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
            database.SetAppSetting(Constants.Settings.AUTO_SHOW_CHANGELOG, AutoChangelog.IsChecked == true ? Constants.Settings.SETTING_TRUE : Constants.Settings.SETTING_FALSE);
            database.SetAppSetting(Constants.Settings.UPLOAD_INTERVAL, Convert.ToInt32(uploadSlider.Value).ToString());
            Globals.UploadInterval = Convert.ToInt32(uploadSlider.Value);
            database.SetAppSetting(Constants.Settings.DOWNLOAD_INTERVAL, Convert.ToInt32(downloadSlider.Value).ToString());
            Globals.DownloadInterval = Convert.ToInt32(downloadSlider.Value);
            database.SetAppSetting(Constants.Settings.ANNOUNCER_WINDOW, Convert.ToInt32(announcerSlider.Value).ToString());
            Globals.AnnouncerWindow = Convert.ToInt32(announcerSlider.Value);
            database.SetAppSetting(Constants.Settings.ALARM_SOUND, ((ComboBoxItem)AlarmSoundBox.SelectedItem).Uid);
            database.SetAppSetting(Constants.Settings.SERVER_NAME, RegistrationServerNameBox.Text.Trim());

            Constants.GlobalVars.SetTwilioCredentials(TwilioAccountSIDBox.Text.Trim(), TwilioAuthTokenBox.Text.Trim(), TwilioPhoneNumberBox.Text.Trim());
            database.SetAppSetting(Constants.Settings.TWILIO_ACCOUNT_SID, Constants.GlobalVars.TwilioCredentials.AccountSID);
            database.SetAppSetting(Constants.Settings.TWILIO_AUTH_TOKEN, Constants.GlobalVars.TwilioCredentials.AuthToken);
            database.SetAppSetting(Constants.Settings.TWILIO_PHONE_NUMBER, Constants.GlobalVars.TwilioCredentials.PhoneNumber);

            database.SetAppSetting(Constants.Settings.MAILGUN_FROM_NAME, MailgunFromNameBox.Text.Trim());
            database.SetAppSetting(Constants.Settings.MAILGUN_FROM_EMAIL, MailgunFromEmailBox.Text.Trim());
            database.SetAppSetting(Constants.Settings.MAILGUN_API_KEY, MailgunAPIKeyBox.Text.Trim());
            database.SetAppSetting(Constants.Settings.MAILGUN_API_URL, MailgunAPIURLBox.Text.Trim());
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
                Wpf.Ui.Appearance.ApplicationTheme theme = Wpf.Ui.Appearance.ApplicationTheme.Light;
                bool system = selectedItem.Uid == Constants.Settings.THEME_SYSTEM;
                if ((selectedItem.Uid == Constants.Settings.THEME_SYSTEM && SystemTheme == 0) || selectedItem.Uid == Constants.Settings.THEME_DARK)
                {
                    theme = Wpf.Ui.Appearance.ApplicationTheme.Dark;
                }
                mWindow.UpdateTheme(theme, system);
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
                case 5:
                    soundFile += "\\Sounds\\emily-runner-here.wav";
                    break;
                case 6:
                    soundFile += "\\Sounds\\emily-runner-arrived.wav";
                    break;
                case 7:
                    soundFile += "\\Sounds\\emily-alert-runner-here.wav";
                    break;
                case 8:
                    soundFile += "\\Sounds\\michael-runner-here.wav";
                    break;
                case 9:
                    soundFile += "\\Sounds\\michael-runner-arrived.wav";
                    break;
                case 10:
                    soundFile += "\\Sounds\\michael-alert-runner-here.wav";
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

        private void uploadSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (uploadSlider != null && uploadBlock != null)
            {
                uploadBlock.Text = uploadSlider.Value.ToString();
            }
        }

        private void downloadSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (downloadSlider != null && downloadBlock != null)
            {
                downloadBlock.Text = downloadSlider.Value.ToString();
            }
        }

        private void announcerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (announcerSlider != null && announcerBlock != null)
            {
                announcerBlock.Text = announcerSlider.Value.ToString();
            }
        }

        private void RegenerateUniqueProgramIDButton_Click(object sender, RoutedEventArgs e)
        {
            string randomMod = Constants.Settings.AlphaNum().Replace(Guid.NewGuid().ToString("N"), "").ToUpper()[0..3];
            database.SetAppSetting(Constants.Settings.PROGRAM_UNIQUE_MODIFIER, randomMod);
            UpdateView();
        }
    }
}
