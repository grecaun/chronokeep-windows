using System.Collections.Generic;

namespace Chronokeep.Objects.ChronokeepPortal
{
    internal class PortalSettingsHolder
    {
        internal PortalSettingsHolder()
        {
            AutoUpload = PortalStatus.NOTSET;
        }

        public enum ChipTypeEnum
        {
            DEC,
            HEX
        }

        public enum VoiceType
        {
            EMILY,
            MICHAEL,
            CUSTOM
        }

        public string Name { get; set; }
        public int SightingPeriod { get; set; }
        public int ReadWindow { get; set; }
        public ChipTypeEnum ChipType { get; set; }
        public bool PlaySound { get; set; }
        public double Volume { get; set; }
        public List<PortalReader> Readers { get; set; }
        public List<PortalAPI> APIs { get; set; }
        public PortalStatus AutoUpload { get; set; }
        public VoiceType Voice { get; set; }
    }
}
