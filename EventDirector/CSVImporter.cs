using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EventDirector
{
    class CSVImporter
    {
        private ImportData data;
        StreamReader file;
        Regex regex = new Regex("\".*\",|[^,]*,|[^,]*$");

        public CSVImporter(string filePath)
        {
            Log.D("Opening file.");
            file = new StreamReader(filePath);
        }

        public void GetHeaders()
        {
            Log.D("Getting headers from file.");
            string headerLine = file.ReadLine();
            MatchCollection matches = regex.Matches(headerLine);
            string[] headers = new string[matches.Count];
            int counter = 0;
            foreach (Match m in matches)
            {
                headers[counter++] = m.Value.Replace('"',' ').Trim();
            }
            data = new ImportData(headers);
        }

        public void GetData()
        {
            Log.D("Getting data from file.");
            string line;
            while ((line = file.ReadLine()) != null)
            {
                MatchCollection matches = regex.Matches(line);
                if (data.GetNumHeaders() != matches.Count)
                {
                    Log.E("Wrong count! It's burning! AHHHHHHH! " + data.GetNumHeaders() + " - " + matches.Count);
                }
                string[] dataLine = new string[matches.Count];
                int counter = 0;
                string match;
                foreach (Match m in matches)
                {
                    match = m.Value.Replace('"', ' ').Trim().TrimEnd(',');
                    dataLine[counter++] = match;
                }
                data.AddData(dataLine);
            }
        }
    }
}