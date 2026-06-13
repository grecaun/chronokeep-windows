namespace Chronokeep.Objects.RFID
{
    public class RFIDSettingsHolder
    {
        public RFIDSettingsHolder()
        {
            UltraID = -1;
            ChipType = ChipTypeEnum.UNKNOWN;
            GatingMode = GatingModeEnum.UNKNOWN;
            GatingInterval = -1;
            Beep = BeepEnum.UNKNOWN;
            BeepVolume = BeepVolumeEnum.UNKNOWN;
            SetFromGPS = GPSEnum.UNKNOWN;
            TimeZone = -25;
            Status = StatusEnum.UNKNOWN;
        }

        public int UltraID { get; set; }
        public ChipTypeEnum ChipType { get; set; }
        public GatingModeEnum GatingMode { get; set; }
        public int GatingInterval { get; set; }
        public BeepEnum Beep { get; set; }
        public BeepVolumeEnum BeepVolume { get; set; }
        public GPSEnum SetFromGPS { get; set; }
        public int TimeZone { get; set; }
        public StatusEnum Status { get; set; }

        public enum ChipTypeEnum
        {
            UNKNOWN,
            DEC,
            HEX
        }

        public enum GatingModeEnum
        {
            UNKNOWN,
            PER_READER,
            PER_BOX,
            FIRST_TIME_SEEN
        }

        public enum BeepEnum
        {
            UNKNOWN,
            ALWAYS,
            ONLY_FIRST_SEEN
        }

        public enum BeepVolumeEnum
        {
            UNKNOWN,
            OFF,
            SOFT,
            LOUD
        }

        public enum GPSEnum
        {
            UNKNOWN,
            SET,
            DONT_SET
        }

        public enum StatusEnum
        {
            UNKNOWN,
            STARTED,
            STOPPED
        }
    }
}
