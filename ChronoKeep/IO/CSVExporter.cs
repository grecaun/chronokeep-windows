using Chronokeep.Helpers;
using Chronokeep.Interfaces.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Chronokeep.IO
{
    class CSVExporter(string format) : IDataExporter
    {
        readonly string format = format;
        string[] headers = [];
        List<object[]> data = [];

        public void ExportData(string Path)
        {
            using var outFile = File.Create(Path);
            using var outWriter = new StreamWriter(outFile);
            outWriter.WriteLine(string.Format(format, headers));
            foreach (object[] line in data)
            {
                outWriter.WriteLine(string.Format(format, [.. line.Select(x => x != null ? x.ToString() : "")]));
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
