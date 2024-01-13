using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Objects.ChronokeepPortal
{
    internal class AllPortalSettings
    {
        public enum ChipTypeEnum
        {
            DEC,
            HEX
        }

        public string Name { get; set; }
        public int SightingPeriod { get; set; }
        public int ReadWindow { get; set; }
        public ChipTypeEnum ChipType { get; set; }
        public bool PlaySound { get; set; }
        public double Volume { get; set; }
        public List<PortalReader> Readers { get; set; }
        public List<PortalAPI> APIs { get; set; }
    }
}
