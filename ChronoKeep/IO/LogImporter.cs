using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChronoKeep.IO
{
    public class LogImporter : CSVImporter
    {
        private static readonly Regex rfid = new Regex("^\\d,[0-9A-Fa-f]+,\\d,\"(\\d{4}-\\d{2}-\\d{2} )?\\d{1,2}:\\d{2}:\\d{2}\\.\\d{3}\"$|" + // RFID Timing style?
                                "^[0-9A-Fa-f]+\\t(\\d{4}-\\d{2}-\\d{2} )?\\d{1,2}:\\d{2}:\\d{2}\\.\\d{3}$");            // RFID Server style?
        private static readonly Regex ipico = new Regex(@"aa[0-9a-fA-F]{34,36}");

        public Type type = Type.CUSTOM;

        public LogImporter(string filePath) : base(filePath) { }

        public void FindType()
        {
            string headerLine = file.ReadLine();
            if (rfid.IsMatch(headerLine))
            {
                Log.D("Found a match! RFID");
                type = Type.RFID;
            }
            if (ipico.IsMatch(headerLine))
            {
                Log.D("Found a match! Ipico");
                type = Type.IPICO;
            }
            ProcessFirstLine(headerLine);
        }

        public enum Type
        {  RFID, IPICO, CUSTOM }
    }
}
