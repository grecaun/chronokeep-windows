using Chronokeep.Interfaces;
using ClosedXML.Excel;
using System.Collections.Generic;

namespace Chronokeep.UI.IO
{
    class ExcelExporter : IDataExporter
    {
        string[] headers = { };
        List<object[]> data;

        public void ExportData(string Path)
        {
            using XLWorkbook workbook = new XLWorkbook();
            IXLWorksheet worksheet = workbook.Worksheets.Add();
            List<object[]> localData = new List<object[]>
            {
                headers
            };
            foreach (object[] line in data)
            {
                localData.Add(line);
            }
            for (int i = 0; i < localData.Count; i++)
            {
                for (int j = 0; j < localData[0].Length; j++)
                {
                    worksheet.Cell(i + 1, j + 1).Style.NumberFormat.Format = "@";
                    worksheet.Cell(i+1, j+1).Value = localData[i][j].ToString();
                }
            }
            workbook.SaveAs(Path);
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
