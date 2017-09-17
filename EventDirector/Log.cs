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
            Debug.WriteLine("LOGOUTPUT - d - "+msg);
#endif
        }

        public static void F(String msg)
        {
            Debug.WriteLine("LOGOUTPUT - f - "+msg);
        }

        public static void E(String msg)
        {
            Debug.WriteLine("LOGOUTPUT - e - "+msg);
        }
    }
}
