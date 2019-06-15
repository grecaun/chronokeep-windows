using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector.Database
{
    class InvalidDatabaseVersion : System.Exception
    {
        public int FoundVersion { get; set; } = -1;
        public int MaxVersion { get; set; } = -1;
        public InvalidDatabaseVersion() : base() { }
        public InvalidDatabaseVersion(int foundVersion, int maxVersion) : base()
        {
            FoundVersion = foundVersion;
            MaxVersion = maxVersion;
        }
    }
}
