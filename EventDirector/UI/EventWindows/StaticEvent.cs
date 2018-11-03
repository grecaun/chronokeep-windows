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
        public static Window tagToolWindow = null;
        public static Window tagAssignmentWindow = null;
        public static Window timingWindow = null;
        public static Window announceWindow = null;
        public static Window nextYearWindow = null;
        public static Window kioskWindow = null;

        public static bool AreToolWindowsOpen()
        {
            return (tagToolWindow != null || tagAssignmentWindow != null | timingWindow != null || announceWindow != null || nextYearWindow != null || kioskWindow != null);
        }
    }
}
