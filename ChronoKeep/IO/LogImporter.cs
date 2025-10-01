﻿using Chronokeep.Helpers;
using System.Text.RegularExpressions;

namespace Chronokeep.IO
{
    public partial class LogImporter : CSVImporter
    {
        [GeneratedRegex("^\\d,[0-9A-Fa-f]+,\\d,\"(\\d{4}-\\d{2}-\\d{2} )?\\d{1,2}:\\d{2}:\\d{2}\\.\\d{3}\"$|" + // RFID Timing style?
                                "^[0-9A-Fa-f]+\\t(\\d{4}-\\d{2}-\\d{2} )?\\d{1,2}:\\d{2}:\\d{2}\\.\\d{3}$")]    // RFID Server style?    
        private static partial Regex Rfid();
        [GeneratedRegex(@"aa[0-9a-fA-F]{34,36}")]
        private static partial Regex Ipico();
        [GeneratedRegex("[\"]?status[\"]?,[\"]?chip_number[\"]?,[\"]?seconds[\"]?,[\"]?milliseconds[\"]?,[\"]?time_seconds[\"]?,[\"]?time_milliseconds[\"]?,[\"]?antenna[\"]?,[\"]?reader[\"]?,[\"]?box[\"]?,[\"]?log_index[\"]?,[\"]?rssi[\"]?,[\"]?is_rewind[\"]?,[\"]?reader_time[\"]?,[\"]?start_time[\"]?,[\"]?read_bib[\"]?,[\"]?type[\"]?")]
        private static partial Regex Chronokeep();

        public Type type = Type.CUSTOM;

        public LogImporter(string filePath) : base(filePath) { }

        public void FindType()
        {
            string headerLine = file.ReadLine();
            Log.D("IO.LogImporter", "HeaderLine: " + headerLine);
            if (Rfid().IsMatch(headerLine))
            {
                Log.D("IO.LogImporter", "Found a match! RFID");
                type = Type.RFID;
            }
            if (Ipico().IsMatch(headerLine))
            {
                Log.D("IO.LogImporter", "Found a match! Ipico");
                type = Type.IPICO;
            }
            if (Chronokeep().IsMatch(headerLine))
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
