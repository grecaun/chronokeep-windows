using ChronoKeep.Interfaces;
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.UI.IO
{
    class ExcelExporter : IDataExporter
    {
        string[] headers = { };
        List<object[]> data;

        public void ExportData(string Path)
        {
            Application excel = Utils.GetExcelApp();
            Workbook wBook = excel.Workbooks.Add("");
            Worksheet wSheet = wBook.ActiveSheet;
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
                    outData[i, j] = localData[i][j];
                }
            }
            Range startCell = wSheet.Cells[1, 1];
            Range endCell = wSheet.Cells[localData.Count, data[0].Length];
            Range writeRange = wSheet.get_Range(startCell, endCell);
            writeRange.Value2 = outData;
            writeRange.EntireColumn.AutoFit();
            excel.DisplayAlerts = false;
            wBook.SaveAs(Path, XlFileFormat.xlWorkbookDefault, Type.Missing, Type.Missing, false, false, XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            excel.DisplayAlerts = true;
            wBook.Close();
            excel.ScreenUpdating = true;
            while (Marshal.ReleaseComObject(wSheet) > 0) ;
            wSheet = null;
            while (Marshal.ReleaseComObject(wBook) > 0) ;
            wBook = null;
            Utils.QuitExcel();
        }

        public Utils.FileType FileType()
        {
            return Utils.FileType.EXCEL;
        }

        public void SetData(string[] headers, List<object[]> data)
        {
            this.headers = headers;
            this.data = data;
            Log.D("Headers " + this.headers);
            Log.D("Data " + this.data);
        }
    }
}
