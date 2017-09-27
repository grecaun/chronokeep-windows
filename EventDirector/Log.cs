using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    }
}
