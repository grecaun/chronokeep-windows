using Chronokeep.Helpers;
using System.Collections.Generic;

namespace Chronokeep.Interfaces.IO
{
    interface IDataExporter
    {
        Utils.FileType FileType();
        void SetData(string[] headers, List<object[]> data);
        void ExportData(string Path);
    }
}
