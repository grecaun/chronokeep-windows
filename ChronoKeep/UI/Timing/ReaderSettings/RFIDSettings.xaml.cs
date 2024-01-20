using Chronokeep.Objects.RFID;
using Chronokeep.Timing.Interfaces;
using Chronokeep.UI.UIObjects;
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
using System.Windows.Shapes;
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
            this.reader = reader;
        }

        public void UpdateView(RFIDSettingsHolder settings)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Updating View.");
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
            if (settings.GatingPeriod >= 0 && settings.GatingPeriod < 21)
            {
                gatingSlider.Value = settings.GatingPeriod;
                gatingDisplay.Text = settings.GatingPeriod.ToString();
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
        }

        private void timeZoneSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Time zone changed.");
            timeZoneDisplay.Text = timeZoneSlider.Value.ToString();
        }

        private void saveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Save button clicked.");
            try
            {
                reader.SetUltraId(Convert.ToInt32(Math.Floor(idSlider.Value)));
                byte byteVal = 0x00;
                switch (chipBox.SelectedIndex)
                {
                    case 0:     // Decimal
                        byteVal = 0x00;
                        break;
                    case 1:     // Hexadecimal
                        byteVal = 0x01;
                        break;
                }
                reader.SetChipOutputType(byteVal);
                byteVal = 0x00;
                switch (gatingModeBox.SelectedIndex)
                {
                    case 0:     // Per reader
                        byteVal = 0x00;
                        break;
                    case 1:     // Per box
                        byteVal = 0x01;
                        break;
                    case 2:     // First time seen
                        byteVal = 0x02;
                        break;
                }
                reader.SetGatingMode(byteVal);
                reader.SetGatingInterval(Convert.ToInt32(Math.Floor(gatingSlider.Value)));
                byteVal = 0x00;
                switch (whenBeepBox.SelectedIndex)
                {
                    case 0:     // always
                        byteVal = 0x00;
                        break;
                    case 1:     // when first seen
                        byteVal = 0x01;
                        break;
                }
                reader.SetWhenToBeep(byteVal);
                byteVal = 0x00;
                switch (volumeBox.SelectedIndex)
                {
                    case 0:     // off
                        byteVal = 0x00;
                        break;
                    case 1:     // soft
                        byteVal = 0x01;
                        break;
                    case 2:     // loud
                        byteVal = 0x02;
                        break;
                }
                reader.SetBeeperVolume(byteVal);
                reader.SetTimeZone(Convert.ToInt32(Math.Floor(timeZoneSlider.Value)));
                byteVal = 0x00;
                if (setGPSSwitch.IsChecked == true)
                {
                    byteVal = 0x01;
                }
                reader.SetAutoGPSTime(byteVal);
            }
            catch (Exception ex)
            {
                Log.E("UI.Timing.ReaderSettings.RFIDSettings", "Error saving settings. " + ex.Message);
                DialogBox.Show("Error saving settings.");
                return;
            }
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Close button clicked.");
            this.Close();
        }

        private void gatingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "Gating changed.");
            gatingDisplay.Text = gatingSlider.Value.ToString();
        }

        private void idSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Log.D("UI.Timing.ReaderSettings.RFIDSettings", "ID changed.");
            idDisplay.Text = idSlider.Value.ToString();
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

        private void UiWindow_Closed(object sender, EventArgs e)
        {
            reader.SettingsWindowFinalize();
        }
    }
}
