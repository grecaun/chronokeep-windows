using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace EventDirector.UI.EventWindows
{
    public class StaticEvent
    {
        public static Window changeMainEventWindow = null;
        public static Window chipReaderWindow = null;
        public static Window chipAssigmentWindow = null;
        public static Window timingWindow = null;
        public static Window announceWindow = null;
        public static Window nextYearWindow = null;
        public static Window kioskWindow = null;
        public static Window participantWindow = null;
        public static Window manualEntryWindow = null;
        public static Window rawReadsWindow = null;

        public static bool AreToolWindowsOpen()
        {
            return (chipReaderWindow != null || chipAssigmentWindow != null | timingWindow != null
                || announceWindow != null || nextYearWindow != null || kioskWindow != null 
                || participantWindow != null || manualEntryWindow != null || rawReadsWindow != null);
        }
    }
}
