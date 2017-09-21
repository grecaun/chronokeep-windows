using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class ImportData
    {
        public string[] Headers { get; private set; }
        public ArrayList Data { get; private set; }
        
        public ImportData(string[] headers)
        {
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
