using Chronokeep.Interfaces;
using OfficeOpenXml;
using System.Collections.Generic;

namespace Chronokeep.UI.IO
{
    class ExcelExporter : IDataExporter
    {
        string[] headers = { };
        List<object[]> data;

        public void ExportData(string Path)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using ExcelPackage package = new ExcelPackage(Path);
            using ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
            worksheet.Name = "";
            List<object[]> localData = new List<object[]>
            {
                headers
            };
            foreach (object[] line in data)
            {
                localData.Add(line);
            }
            object[,] outData = new object[localData.Count, localData[0].Length];
            for (int i = 0; i < localData.Count; i++)
            {
                for (int j = 0; j < localData[0].Length; j++)
                {
                    worksheet.Cells[i, j].Value = localData[i][j].ToString();
                    worksheet.Cells[i, j].Style.Numberformat.Format = "@";
                }
            }
            package.Save(Path);
        }

        public Utils.FileType FileType()
        {
            return Utils.FileType.EXCEL;
        }

        public void SetData(string[] headers, List<object[]> data)
        {
            this.headers = headers;
            this.data = data;
            Log.D("IO.ExcelExporter", $"Headers {this.headers} Data {this.data}");
        }
    }
}
