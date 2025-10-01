using Chronokeep.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Chronokeep.IO
{
    public partial class ImportData
    {
        public string FileName { get; private set; }
        public FileType Type { get; private set; }
        public string[] Headers { get; private set; }
        public List<string[]> Data { get; private set; }
        [GeneratedRegex("[^\\\\]*\\.")]
        private static partial Regex DataRegex();


        public ImportData(string[] headers, string filename, FileType type)
        {
            Type = type;
            FileName = DataRegex().Match(filename).Value.TrimEnd('.');
            Log.D("IO.ImportData", FileName + " is the filename.");
            string[] newheaders = new string[headers.Length + 1];
            Array.Copy(headers, 0, newheaders, 1, headers.Length);
#if DEBUG
            StringBuilder sb = new("Headers are");
            foreach (string s in newheaders)
            {
                sb.Append(" '" + s + "'");
            }
            Log.D("IO.ImportData", sb.ToString());
#endif
            Data = [];
            Headers = newheaders;
        }

        public int GetNumHeaders()
        {
            return Headers.Length;
        }

        public void AddData(string[] data)
        {
            string[] newdata = new string[data.Length + 1];
            Array.Copy(data, 0, newdata, 1, data.Length);
            if (Headers.Length != newdata.Length)
            {
                Log.E("IO.ImportData", "Header count wrong on import of data: " + Headers.Length + " - " + newdata.Length);
            }
            Data.Add(newdata);
#if DEBUG
            StringBuilder sb = new("Data input is");
            foreach (string s in newdata)
            {
                sb.Append(" '" + s + "'");
            }
            Log.D("IO.ImportData", sb.ToString());
#endif
        }

        public string[] GetDistanceNames(int index)
        {
            HashSet<string> values = [];
            foreach (string[] line in Data)
            {
                if (line[index] != null && line[index].Length > 0)
                {
                    values.Add(line[index].Trim());
                }
            }
            string[] output = new string[values.Count];
            values.CopyTo(output);
            return output;
        }

        public enum FileType { EXCEL, CSV }
    }
}
