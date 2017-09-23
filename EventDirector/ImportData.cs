using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EventDirector
{
    public class ImportData
    {
        public String FileName { get; private set; }
        public string[] Headers { get; private set; }
        public List<String[]> Data { get; private set; }
        Regex regex = new Regex("[^\\\\]*\\.");


        public ImportData(string[] headers, string filename)
        {
            FileName = regex.Match(filename).Value.TrimEnd('.');
            Log.D(FileName + " is the filename.");
            string[] newheaders = new string[headers.Length + 1];
            Array.Copy(headers, 0, newheaders, 1, headers.Length);
#if DEBUG
            StringBuilder sb = new StringBuilder("Headers are");
            foreach (string s in newheaders)
            {
                sb.Append(" '" + s + "'");
            }
            Log.D(sb.ToString());
#endif
            Data = new List<String[]>();
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
                Log.E("Wrong count! It's burning! AHHHHHHH! " + Headers.Length + " - " + newdata.Length);
            }
            Data.Add(newdata);
#if DEBUG
            StringBuilder sb = new StringBuilder("Data input is");
            foreach (string s in newdata)
            {
                sb.Append(" '" + s + "'");
            }
            Log.D(sb.ToString());
#endif
        }

        public string[] GetDivisionNames(int index)
        {
            HashSet<String> values = new HashSet<string>();
            foreach (string[] line in Data)
            {
                values.Add(line[index].ToLower());
            }
            string[] output = new string[values.Count];
            values.CopyTo(output);
            return output;
        }
    }
}
