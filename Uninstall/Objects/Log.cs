using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uninstall.Objects
{
    class Log
    {
        public static void D(string ns, string msg)
        {
#if DEBUG
            Trace.WriteLine(string.Format("{0} LOGOUTPUT - {1} - {2} - {3}", DateTime.Now.ToString("hh:mm:ss.fff"), "d", ns, msg));
#endif
        }

        public static void F(string ns, string msg)
        {
            Trace.WriteLine(string.Format("{0} LOGOUTPUT - {1} - {2} - {3}", DateTime.Now.ToString("hh:mm:ss.fff"), "f", ns, msg));
        }

        public static void E(string ns, string msg)
        {
            Trace.WriteLine(string.Format("{0} LOGOUTPUT - {1} - {2} - {3}", DateTime.Now.ToString("hh:mm:ss.fff"), "e", ns, msg));
        }

        public static void WriteFile(string path, string[] msgs)
        {
            using (var outWriter = File.AppendText(path))
            {
                foreach (string msg in msgs)
                {
                    outWriter.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + msg);
                }
            }
        }
    }
}
