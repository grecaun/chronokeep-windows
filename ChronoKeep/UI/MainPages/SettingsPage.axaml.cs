using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.Interfaces.UI;
using Chronokeep.Objects;
using Chronokeep.UI.Parts;
using NAudio.Wave;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Chronokeep.UI.MainPages;

public partial class SettingsPage : UserControl, IMainPage
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
            Tag = Constants.Readers.SYSTEM_RFID
        });
        DefaultTimingBox.Items.Add(new ComboBoxItem()
        {
            Content = Constants.Readers.SYSTEM_NAMES[Constants.Readers.SYSTEM_CHRONOKEEP_PORTAL],
            Tag = Constants.Readers.SYSTEM_CHRONOKEEP_PORTAL
        });
        DefaultTimingBox.Items.Add(new ComboBoxItem()
        {
            Content = Constants.Readers.SYSTEM_NAMES[Constants.Readers.SYSTEM_IPICO],
            Tag = Constants.Readers.SYSTEM_IPICO
        });
        DefaultTimingBox.Items.Add(new ComboBoxItem()
        {
            Content = Constants.Readers.SYSTEM_NAMES[Constants.Readers.SYSTEM_IPICO_LITE],
            Tag = Constants.Readers.SYSTEM_IPICO_LITE
        });
        SystemTheme = Utils.GetSystemTheme();
        if (SystemTheme != -1)
        {
            ThemeOffset = 0;
            ThemeColorBox.Items.Add(new ComboBoxItem()
            {
                Content = "System",
                Tag = Constants.Settings.THEME_SYSTEM
            });
        }
        ThemeColorBox.Items.Add(new ComboBoxItem()
        {
            Content = "Light",
            Tag = Constants.Settings.THEME_LIGHT
        });
        ThemeColorBox.Items.Add(new ComboBoxItem()
        {
            Content = "Dark",
            Tag = Constants.Settings.THEME_DARK
        });
        UpdateView();
    }

    public void UpdateView()
    {
        AppSetting setting = database.GetAppSetting(Constants.Settings.DEFAULT_TIMING_SYSTEM)!;
        DefaultTimingBox.SelectedIndex = setting.Value switch
        {
            Constants.Readers.SYSTEM_CHRONOKEEP_PORTAL => 1,
            Constants.Readers.SYSTEM_IPICO => 2,
            Constants.Readers.SYSTEM_IPICO_LITE => 3,
            _ => 0,
        };
        CompanyNameBox.Text = database.GetAppSetting(Constants.Settings.COMPANY_NAME)!.Value;
        ContactEmailBox.Text = database.GetAppSetting(Constants.Settings.CONTACT_EMAIL)!.Value;
        DefaultExportDirBox.Text = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR)!.Value;
        UpdatePage.IsChecked = database.GetAppSetting(Constants.Settings.UPDATE_ON_PAGE_CHANGE)!.Value == Constants.Settings.SETTING_TRUE;
        ExitNoPrompt.IsChecked = database.GetAppSetting(Constants.Settings.EXIT_NO_PROMPT)!.Value == Constants.Settings.SETTING_TRUE;
        CheckUpdates.IsChecked = database.GetAppSetting(Constants.Settings.CHECK_UPDATES)!.Value == Constants.Settings.SETTING_TRUE;
        AutoChangelog.IsChecked = database.GetAppSetting(Constants.Settings.AUTO_SHOW_CHANGELOG)!.Value == Constants.Settings.SETTING_TRUE;
        AppSetting themeSetting = database.GetAppSetting(Constants.Settings.CURRENT_THEME)!;
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
        if (int.TryParse(database.GetAppSetting(Constants.Settings.UPLOAD_INTERVAL)!.Value, out int uploadInt) && uploadInt > 0 && uploadInt < 60)
        {
            uploadSlider.Value = uploadInt;
            uploadBlock.Text = uploadInt.ToString();
        }
        if (int.TryParse(database.GetAppSetting(Constants.Settings.DOWNLOAD_INTERVAL)!.Value, out int downloadInt) && downloadInt > 0 && downloadInt < 60)
        {
            downloadSlider.Value = downloadInt;
            downloadBlock.Text = downloadInt.ToString();
        }
        if (int.TryParse(database.GetAppSetting(Constants.Settings.ANNOUNCER_WINDOW)!.Value, out int announcerWindow) && announcerWindow >= 15 && announcerWindow <= 180)
        {
            announcerSlider.Value = announcerWindow;
            announcerBlock.Text = announcerWindow.ToString();
        }
        if (int.TryParse(database.GetAppSetting(Constants.Settings.ALARM_SOUND)!.Value, out int alarm))
        {
            AlarmSoundBox.SelectedIndex = alarm;
        }
        RegistrationServerNameBox.Text = database.GetAppSetting(Constants.Settings.SERVER_NAME)!.Value;
        TwilioAccountSIDBox.Text = database.GetAppSetting(Constants.Settings.TWILIO_ACCOUNT_SID)!.Value;
        TwilioAuthTokenBox.Text = database.GetAppSetting(Constants.Settings.TWILIO_AUTH_TOKEN)!.Value;
        TwilioPhoneNumberBox.Text = database.GetAppSetting(Constants.Settings.TWILIO_PHONE_NUMBER)!.Value;
        MailgunFromNameBox.Text = database.GetAppSetting(Constants.Settings.MAILGUN_FROM_NAME)!.Value;
        MailgunFromEmailBox.Text = database.GetAppSetting(Constants.Settings.MAILGUN_FROM_EMAIL)!.Value;
        MailgunAPIKeyBox.Text = database.GetAppSetting(Constants.Settings.MAILGUN_API_KEY)!.Value;
        MailgunAPIURLBox.Text = database.GetAppSetting(Constants.Settings.MAILGUN_API_URL)!.Value;
        UniqueProgramID.Text = database.GetAppSetting(Constants.Settings.PROGRAM_UNIQUE_MODIFIER)!.Value;
    }

    private void SaveSettings()
    {
        Log.D("UI.MainPages.SettingsPage", "Saving.");
        database.SetAppSetting(Constants.Settings.COMPANY_NAME, CompanyNameBox.Text!.Trim());
        database.SetAppSetting(Constants.Settings.CONTACT_EMAIL, ContactEmailBox.Text!.Trim());
        database.SetAppSetting(Constants.Settings.DEFAULT_TIMING_SYSTEM, (string)((ComboBoxItem)DefaultTimingBox.SelectedItem!).Tag!);
        database.SetAppSetting(Constants.Settings.CURRENT_THEME, (string)((ComboBoxItem)ThemeColorBox.SelectedItem!).Tag!);
        database.SetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR, DefaultExportDirBox.Text!.Trim());
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
        database.SetAppSetting(Constants.Settings.ALARM_SOUND, AlarmSoundBox.SelectedIndex.ToString());
        database.SetAppSetting(Constants.Settings.SERVER_NAME, RegistrationServerNameBox.Text!.Trim());

        Constants.GlobalVars.SetTwilioCredentials(TwilioAccountSIDBox.Text!.Trim(), TwilioAuthTokenBox.Text!.Trim(), TwilioPhoneNumberBox.Text!.Trim());
        database.SetAppSetting(Constants.Settings.TWILIO_ACCOUNT_SID, Constants.GlobalVars.TwilioCredentials.AccountSID);
        database.SetAppSetting(Constants.Settings.TWILIO_AUTH_TOKEN, Constants.GlobalVars.TwilioCredentials.AuthToken);
        database.SetAppSetting(Constants.Settings.TWILIO_PHONE_NUMBER, Constants.GlobalVars.TwilioCredentials.PhoneNumber);

        database.SetAppSetting(Constants.Settings.MAILGUN_FROM_NAME, MailgunFromNameBox.Text!.Trim());
        database.SetAppSetting(Constants.Settings.MAILGUN_FROM_EMAIL, MailgunFromEmailBox.Text!.Trim());
        database.SetAppSetting(Constants.Settings.MAILGUN_API_KEY, MailgunAPIKeyBox.Text!.Trim());
        database.SetAppSetting(Constants.Settings.MAILGUN_API_URL, MailgunAPIURLBox.Text!.Trim());
    }

    public static void UpdateDatabase() { }

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

    private async void ResetDB_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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

    private async void RebuildDB_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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

    private void Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs? e)
    {
        Log.D("UI.MainPages.SettingsPage", "Save button clicked.");
        SaveSettings();
        UpdateView();
    }

    private void ThemeColorBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ThemeColorBox.SelectedItem is ComboBoxItem selectedItem)
        {
            database.SetAppSetting(Constants.Settings.CURRENT_THEME, (string)((ComboBoxItem)ThemeColorBox.SelectedItem!).Tag!);
            string theme = selectedItem.Tag != null ? (string)selectedItem.Tag : "light";
            mWindow.UpdateTheme(theme);
        }
    }

    private async void ChangeExport_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.SettingsPage", "Change export directory button clicked.");
        var topLevel = TopLevel.GetTopLevel((Window)mWindow);
        if (topLevel != null)
        {
            IStorageFolder? oldFold;
            try
            {
                oldFold = await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri(DefaultExportDirBox.Text ?? ""));
            }
            catch
            {
                oldFold = null;
            }
            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Default Export Directory",
                SuggestedStartLocation = oldFold,
            });
            if (folder.Count > 0)
            {
                DefaultExportDirBox.Text = folder[0].Path.ToString();
            }
        }
    }

    private void UploadSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (uploadSlider != null && uploadBlock != null)
        {
            uploadBlock.Text = uploadSlider.Value.ToString();
        }
    }

    private void DownloadSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (downloadSlider != null && downloadBlock != null)
        {
            downloadBlock.Text = downloadSlider.Value.ToString();
        }
    }

    private void AnnouncerSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (announcerSlider != null && announcerBlock != null)
        {
            announcerBlock.Text = announcerSlider.Value.ToString();
        }
    }

    private void PlayBtn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.D("UI.MainPages.SettingsPage", "Play alarm sound clicked.");
        try
        {
            AudioPlaybackEngine.PlaySound(AlarmSoundBox.SelectedIndex);
        }
        catch (ArgumentException) { }
        catch (Exception ex)
        {
            DialogBox.Show("Error trying to play sound. " + ex.Message + ex.GetType());
        }
    }

    private void RegenerateUniqueProgramIDButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        string randomMod = Constants.Settings.AlphaNum().Replace(Guid.NewGuid().ToString("N"), "").ToUpper()[0..3];
        database.SetAppSetting(Constants.Settings.PROGRAM_UNIQUE_MODIFIER, randomMod);
        UpdateView();
    }
}