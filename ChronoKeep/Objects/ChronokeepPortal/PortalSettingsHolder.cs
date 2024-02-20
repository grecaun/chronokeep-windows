using System;
using System.Collections.Generic;

namespace Chronokeep.Objects.ChronokeepPortal
{
    internal class PortalSettingsHolder
    {
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

        public enum ChangeType
        {
            SETTINGS,
            READERS,
            APIS,
            ANTENNAS
        }

        public class ReaderAntennas
        {
            public string ReaderName { get; set; }
            public int[] Antennas { get; set; }
        }

        public string Name { get; set; }
        public int SightingPeriod { get; set; }
        public int ReadWindow { get; set; }
        public ChipTypeEnum ChipType { get; set; }
        public bool PlaySound { get; set; }
        public double Volume { get; set; }
        public List<PortalReader> Readers { get; set; }
        public List<PortalAPI> APIs { get; set; }
        public PortalStatus AutoUpload { get; set; } = PortalStatus.NOTSET;
        public VoiceType Voice { get; set; }
        public ReaderAntennas Antennas { get; set; }
        public HashSet<ChangeType> Changes { get; set; } = new HashSet<ChangeType>();
        public string PortalVersion { get; set; }
    }
}
