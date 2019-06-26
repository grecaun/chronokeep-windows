﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.Interfaces
{
    interface IDataExporter
    {
        Utils.FileType FileType();
        void SetData(string[] headers, List<object[]> data);
        void ExportData(string Path);
    }
}