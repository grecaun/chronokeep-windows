﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EventDirector
{
    public class CSVImporter
    {
        public ImportData Data { get; private set; }
        StreamReader file;
        string FilePath;
        Regex regex = new Regex("\".*\",|[^,]*,|[^,]*$");

        public CSVImporter(string filePath)
        {
            Log.D("Opening file.");
            file = new StreamReader(filePath);
            FilePath = filePath;
        }

        public void FetchHeaders()
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
            Data = new ImportData(headers, FilePath);
        }

        public void FetchData()
        {
            Log.D("Getting data from file.");
            string line;
            while ((line = file.ReadLine()) != null)
            {
                MatchCollection matches = regex.Matches(line);
                if (Data.GetNumHeaders() != matches.Count)
                {
                    Log.E("Wrong count! It's burning! AHHHHHHH! " + Data.GetNumHeaders() + " - " + matches.Count);
                }
                string[] dataLine = new string[matches.Count];
                int counter = 0;
                string match;
                foreach (Match m in matches)
                {
                    match = m.Value.Replace('"', ' ').Trim().TrimEnd(',');
                    dataLine[counter++] = match;
                }
                Data.AddData(dataLine);
            }
        }
    }
}