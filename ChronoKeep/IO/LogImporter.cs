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
        private static readonly Regex chronokeep = new Regex("\"status\",\"chip_number\",\"seconds\",\"milliseconds\",\"time_seconds\",\"time_milliseconds\",\"antenna\",\"reader\",\"box\",\"log_index\",\"rssi\",\"is_rewind\",\"reader_time\",\"start_time\",\"read_bib\",\"type\"");

        public Type type = Type.CUSTOM;

        public LogImporter(string filePath) : base(filePath) { }

        public void FindType()
        {
            string headerLine = file.ReadLine();
            Log.D("IO.LogImporter", "HeaderLine: " + headerLine);
            if (rfid.IsMatch(headerLine))
            {
                Log.D("IO.LogImporter", "Found a match! RFID");
                type = Type.RFID;
            }
            if (ipico.IsMatch(headerLine))
            {
                Log.D("IO.LogImporter", "Found a match! Ipico");
                type = Type.IPICO;
            }
            if (chronokeep.IsMatch(headerLine))
            {
                Log.D("IO.LogImporter", "Found a match! Chronokeep");
                type = Type.CHRONOKEEP;
            }
            ProcessFirstLine(headerLine);
        }

        public enum Type
        {  RFID, IPICO, CHRONOKEEP, CUSTOM }
    }
}
