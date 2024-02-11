using Chronokeep.Objects.RFID;
using Chronokeep.Timing.Interfaces;
using Chronokeep.UI.UIObjects;
using System;
using System.Windows;
using Wpf.Ui.Controls;

namespace Chronokeep.UI.Timing.ReaderSettings
{
    /// <summary>
    /// Interaction logic for RFIDSettings.xaml
    /// </summary>
    public partial class RFIDSettings : UiWindow
    {
        private RFIDUltraInterface reader = null;

        public RFIDSettings(RFIDUltraInterface reader)
        {
            InitializeComponent();
            this.MinWidth = 100;
            this.MinHeight = 100;
            this.reader = reader;
            reader.GetStatus();
            reader.QuerySettings();
        }

        public void UpdateView(RFIDSettingsHolder settings)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Updating View.");
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                if (settings.UltraID > 0 && settings.UltraID < 256)
                {
                    idSlider.Value = settings.UltraID;
                    idDisplay.Text = settings.UltraID.ToString();
                }
                switch (settings.ChipType)
                {
                    case RFIDSettingsHolder.ChipTypeEnum.DEC:
                        chipBox.SelectedIndex = 0;
                        break;
                    case RFIDSettingsHolder.ChipTypeEnum.HEX:
                        chipBox.SelectedIndex = 1;
                        break;
                }
                switch (settings.GatingMode)
                {
                    case RFIDSettingsHolder.GatingModeEnum.PER_READER:
                        gatingModeBox.SelectedIndex = 0;
                        break;
                    case RFIDSettingsHolder.GatingModeEnum.PER_BOX:
                        gatingModeBox.SelectedIndex = 1;
                        break;
                    case RFIDSettingsHolder.GatingModeEnum.FIRST_TIME_SEEN:
                        gatingModeBox.SelectedIndex = 2;
                        break;
                }
                if (settings.GatingInterval >= 0 && settings.GatingInterval < 21)
                {
                    gatingSlider.Value = settings.GatingInterval;
                    gatingDisplay.Text = settings.GatingInterval.ToString();
                }
                switch (settings.Beep)
                {
                    case RFIDSettingsHolder.BeepEnum.ALWAYS:
                        whenBeepBox.SelectedIndex = 0;
                        break;
                    case RFIDSettingsHolder.BeepEnum.ONLY_FIRST_SEEN:
                        whenBeepBox.SelectedIndex = 1;
                        break;
                }
                switch (settings.BeepVolume)
                {
                    case RFIDSettingsHolder.BeepVolumeEnum.OFF:
                        volumeBox.SelectedIndex = 0;
                        break;
                    case RFIDSettingsHolder.BeepVolumeEnum.SOFT:
                        volumeBox.SelectedIndex = 1;
                        break;
                    case RFIDSettingsHolder.BeepVolumeEnum.LOUD:
                        volumeBox.SelectedIndex = 2;
                        break;
                }
                switch (settings.SetFromGPS)
                {
                    case RFIDSettingsHolder.GPSEnum.SET:
                        setGPSSwitch.IsChecked = true;
                        break;
                    case RFIDSettingsHolder.GPSEnum.DONT_SET:
                        setGPSSwitch.IsChecked = false;
                        break;
                }
                if (settings.TimeZone > -24 && settings.TimeZone < 24)
                {
                    timeZoneSlider.Value = settings.TimeZone;
                    timeZoneDisplay.Text = settings.TimeZone.ToString();
                }
                switch (settings.Status)
                {
                    case RFIDSettingsHolder.StatusEnum.STARTED:
                        readingSwitch.IsChecked = true;
                        break;
                    case RFIDSettingsHolder.StatusEnum.STOPPED:
                        readingSwitch.IsChecked = false;
                        break;
                }
                sacrifice.Visibility = Visibility.Collapsed;
                settingsPanel.Visibility = Visibility.Visible;
            }));
        }

        private void timeZoneSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Time zone changed.");
            if (timeZoneDisplay != null && timeZoneSlider != null)
            {
                timeZoneDisplay.Text = timeZoneSlider.Value.ToString();
            }
        }

        /*private void saveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Save button clicked.");
            DialogBox.Show(
                "Saving settings will reboot the reader and forcibly close the connection. Proceed?",
                "Yes",
                "No",
                () =>
            {
                try
                {
                }
                catch (Exception ex)
                {
                    Log.E("UI.Timing.ReaderSettings.RFIDSettings", "Error saving settings. " + ex.Message);
                    DialogBox.Show("Error saving settings.");
                }
                reader.Disconnect();
                reader.CloseSettings();
            });
        }//*/

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Close button clicked.");
            this.Close();
        }

        private void gatingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Gating changed.");
            if (gatingDisplay != null && gatingSlider != null)
            {
                gatingDisplay.Text = gatingSlider.Value.ToString();
            }
        }

        private void idSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "ID changed.");
            if (idDisplay != null && idSlider != null)
            {
                idDisplay.Text = idSlider.Value.ToString();
            }
        }

        public void CloseWindow()
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "CloseWindow.");
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                Close();
            }));
        }

        private void readingSwitch_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.ChronokeepSettings", "Reading clicked.");
            if (readingSwitch.IsChecked == true)
            {
                // switch just switched on
                reader.StartReading();
            }
            else
            {
                // switch just switch off
                reader.StopReading();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            reader.SettingsWindowFinalize();
        }

        private void saveID_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Save ID button clicked.");
            DialogBox.Show(
                "Saving ID will reboot the reader and forcibly close the connection. Proceed?",
                "Yes",
                "No",
                () =>
                {
                    reader.SetUltraId(Convert.ToInt32(Math.Floor(idSlider.Value)));
                });
        }

        private void saveChip_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Save Chip button clicked.");
            char byteVal = (char)0x00;
            switch (chipBox.SelectedIndex)
            {
                case 0:     // Decimal
                    byteVal = (char)0x00;
                    break;
                case 1:     // Hexadecimal
                    byteVal = (char)0x01;
                    break;
            }
            reader.SetChipOutputType(byteVal);
        }

        private void saveGatingMode_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Save Gating Mode button clicked.");
            char byteVal = (char)0x00;
            switch (gatingModeBox.SelectedIndex)
            {
                case 0:     // Per reader
                    byteVal = (char)0x00;
                    break;
                case 1:     // Per box
                    byteVal = (char)0x01;
                    break;
                case 2:     // First time seen
                    byteVal = (char)0x02;
                    break;
            }
            reader.SetGatingMode(byteVal);
        }

        private void saveGatingInterval_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Save Gating Interval button clicked.");
            reader.SetGatingInterval(Convert.ToInt32(Math.Floor(gatingSlider.Value)));
        }

        private void saveWhenBeep_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Save When to Beep button clicked.");
            char byteVal = (char)0x00;
            switch (whenBeepBox.SelectedIndex)
            {
                case 0:     // always
                    byteVal = (char)0x00;
                    break;
                case 1:     // when first seen
                    byteVal = (char)0x01;
                    break;
            }
            reader.SetWhenToBeep(byteVal);
        }

        private void saveVolume_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Save Volume button clicked.");
            char byteVal = '0';
            switch (volumeBox.SelectedIndex)
            {
                case 0:     // off
                    byteVal = (char)0x00;
                    break;
                case 1:     // soft
                    byteVal = (char)0x01;
                    break;
                case 2:     // loud
                    byteVal = (char)0x02;
                    break;
            }
            reader.SetBeeperVolume(byteVal);
        }

        private void saveTimezone_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Save Timezone button clicked.");
            reader.SetTimeZone(Convert.ToInt32(Math.Floor(timeZoneSlider.Value)));
        }

        private void setGPSSwitch_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Set Time Via GPS button clicked.");
            char byteVal = (char)0x00;
            if (setGPSSwitch.IsChecked == true)
            {
                byteVal = (char)0x01;
            }
            reader.SetAutoGPSTime(byteVal);
        }
    }
}
