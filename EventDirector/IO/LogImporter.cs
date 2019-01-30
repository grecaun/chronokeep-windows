using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EventDirector.IO
{
    public class LogImporter : CSVImporter
    {
        Regex regex = new Regex("^\\d,[0-9A-Fa-f]+,\\d,\"(\\d{4}-\\d{2}-\\d{2} )?\\d{1,2}:\\d{2}:\\d{2}\\.\\d{3}\"$|" + // RFID Timing style?
                                "^[0-9A-Fa-f]+\\t(\\d{4}-\\d{2}-\\d{2} )?\\d{1,2}:\\d{2}:\\d{2}\\.\\d{3}$");            // RFID Server style?

        public Type type = Type.CUSTOM;

        public LogImporter(string filePath) : base(filePath) { }

        public void FindType()
        {
            string headerLine = file.ReadLine();
            if (regex.IsMatch(headerLine))
            {
                Log.D("Found a match! RFID");
                type = Type.RFID;
            }
            ProcessFirstLine(headerLine);
        }

        public enum Type
        {  RFID, IPICO, CUSTOM }
    }
}
