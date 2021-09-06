using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep
{
    class Log
    {
        public static void D(string msg)
        {
#if DEBUG
            Console.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff") + " LOGOUTPUT - d - " + msg);
#endif
        }

        public static void F(string msg)
        {
            Console.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff") + " LOGOUTPUT - f - " + msg);
        }

        public static void E(string msg)
        {
            Console.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff") + " LOGOUTPUT - e - " + msg);
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
