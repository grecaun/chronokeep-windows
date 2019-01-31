using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class Log
    {
        public static void D(String msg)
        {
#if DEBUG
            Console.WriteLine(DateTime.Now.ToLongTimeString() + " LOGOUTPUT - d - " + msg);
#endif
        }

        public static void F(String msg)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString() + " LOGOUTPUT - f - " + msg);
        }

        public static void E(String msg)
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString() + " LOGOUTPUT - e - " + msg);
        }

        public static void WriteFile(String path, String[] msgs)
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
