using Chronokeep.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.UI.IO
{
    class CSVExporter : IDataExporter
    {
        string format = "";
        string[] headers = { };
        List<object[]> data;

        public CSVExporter(string format)
        {
            this.format = format;
        }

        public void ExportData(string Path)
        {
            using (var outFile = File.Create(Path))
            {
                using (var outWriter = new StreamWriter(outFile))
                {
                    outWriter.WriteLine(string.Format(format, headers));
                    foreach (object[] line in data)
                    {
                        outWriter.WriteLine(string.Format(format, line.Select(x => x != null ? x.ToString() : "").ToArray()));
                    }
                }
            }
        }

        public Utils.FileType FileType()
        {
            return Utils.FileType.CSV;
        }

        public void SetData(string[] headers, List<object[]> data)
        {
            this.headers = headers;
            this.data = data;
        }
    }
}
