using System;
using System.Diagnostics;
using System.IO;

namespace Chronokeep.Helpers
{
#if DEBUG
    class Log
    {
        public static void D(string ns, string msg)
        {
            Debug.WriteLine(string.Format("{0} LOGOUTPUT - {1} - {2} - {3}", DateTime.Now.ToString("hh:mm:ss.fff"), "d", ns, msg));
        }
        public static void F(string ns, string msg)
        {
            Debug.WriteLine(string.Format("{0} LOGOUTPUT - {1} - {2} - {3}", DateTime.Now.ToString("hh:mm:ss.fff"), "f", ns, msg));
        }
        public static void E(string ns, string msg)
        {
            Debug.WriteLine(string.Format("{0} LOGOUTPUT - {1} - {2} - {3}", DateTime.Now.ToString("hh:mm:ss.fff"), "e", ns, msg));
            File.AppendAllText(Globals.ErrorLogPath, string.Format("{0}: {1,-20} - {2}\n", DateTime.Now.ToString("hh:mm:ss.fff"), ns[..(ns.Length > 20 ? 20 : ns.Length)], msg));
        }
    }
#endif
}
