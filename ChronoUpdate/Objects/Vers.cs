using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoUpdate.Objects
{
    public class Vers
    {
        public int major;
        public int minor;
        public int patch;
        public string arch;

        public Vers()
        {
            major = 0;
            minor = 0;
            patch = 0;
            arch = "";
        }

        public bool Equal(Vers other)
        {
            return this.major == other.major && this.minor == other.minor && this.patch == other.patch && this.arch == other.arch;
        }

        public override string ToString()
        {
            return $"v{major}.{minor}.{patch}-{arch}";
        }
    }
}
