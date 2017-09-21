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
        public ArrayList Data { get; private set; }
        Regex regex = new Regex("[^\\\\]*\\.");


        public ImportData(string[] headers, string filename)
        {
            FileName = regex.Match(filename).Value.TrimEnd('.');
            Log.D(FileName + " is the filename.");
            StringBuilder sb = new StringBuilder("Headers are");
            foreach (string s in headers)
            {
                sb.Append(" '" + s + "'");
            }
            Log.D(sb.ToString());
            Data = new ArrayList();
            Headers = headers;
        }

        public int GetNumHeaders()
        {
            return Headers.Length;
        }

        public void AddData(string[] data)
        {
            Data.Add(data);
            StringBuilder sb = new StringBuilder("Data input is");
            foreach (string s in data)
            {
                sb.Append(" '" + s + "'");
            }
            Log.D(sb.ToString());
        }
    }
}
