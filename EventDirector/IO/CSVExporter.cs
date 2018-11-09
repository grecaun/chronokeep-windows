using EventDirector.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector.UI.IO
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
                    outWriter.WriteLine(String.Format(format, headers));
                    foreach (object[] line in data)
                    {
                        outWriter.WriteLine(String.Format(format, line.Select(x => x.ToString()).ToArray()));
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
