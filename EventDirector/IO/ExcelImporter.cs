using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector
{
    class ExcelImporter : IDataImporter
    {
        public ImportData Data { get; private set; }
        string FilePath;
        Workbook workBook;
        Worksheet currentSheet;
        public List<string> SheetNames { get; private set; }
        public int NumSheets { get; private set; }

        public ExcelImporter(String filename)
        {
            Log.D("Creating importer object.");
            FilePath = filename;
            try
            {
                Log.D("Opening workbook.");
                workBook = Utils.GetExcelApp().Workbooks.Open(FilePath, ReadOnly: true);
                NumSheets = workBook.Sheets.Count;
                if (NumSheets > 0)
                {
                    currentSheet = (Worksheet)workBook.Sheets[1];
                }
                SheetNames = new List<string>();
                for (int i = 1; i <= NumSheets; i++)
                {
                    string name = ((Worksheet)workBook.Sheets[i]).Name;
                    SheetNames.Add(name);
                    Log.D("Sheet name is " + name);
                }
            }
            catch (Exception excep)
            {
                Log.E("Something went wrong when trying to open workseet.");
                Log.E(excep.StackTrace);
            }
        }

        public void ChangeSheet(int ix)
        {
            try
            {
                if (ix <= NumSheets && ix > 0)
                {
                    currentSheet = (Worksheet)workBook.Sheets[ix];
                }
            }
            catch
            {
                Log.E("Hmm, can't get that worksheet. Sorry.");
            }
        }

        public void FetchHeaders()
        {
            Log.D("Getting headers from excel file.");
            try
            {
                Range excelRange = currentSheet.UsedRange;
                Log.D("Used range set.");
                object[,] valueArray = (object[,])excelRange.get_Value(XlRangeValueDataType.xlRangeValueDefault);
                Log.D("Value array populated.");
                int numHeaders = valueArray.GetUpperBound(1);
                int numDataRows = valueArray.GetUpperBound(0);
                Log.D("Rows " + numDataRows + " Columns " + numHeaders);
                string[] headers = new string[numHeaders];
                for (int i=0; i<numHeaders; i++)
                {
                    headers[i] = valueArray[1, i + 1] == null ? "" : valueArray[1, i + 1].ToString();
                }
                Data = new ImportData(headers, FilePath, ImportData.FileType.EXCEL);
            }
            catch (Exception excep)
            {
                Log.E("Something went wrong when trying to get headers.");
                Log.E(excep.StackTrace);
                string[] headers = new string[0];
                Data = new ImportData(headers, FilePath, ImportData.FileType.EXCEL);
            }
        }

        public void FetchData()
        {
            Log.D("Getting data from excel file.");
            try
            {
                Range excelRange = currentSheet.UsedRange;
                object[,] valueArray = (object[,])excelRange.get_Value(XlRangeValueDataType.xlRangeValueDefault);
                int numHeaders = valueArray.GetUpperBound(1);
                int numDataRows = valueArray.GetUpperBound(0);
                Log.D("Rows " + numDataRows + " Columns " + numHeaders);
                for (int row = 2; row <= numDataRows; row++)
                {
                    string[] dataLine = new string[numHeaders];
                    for (int column=0; column < numHeaders; column++)
                    {
                        dataLine[column] = valueArray[row, column + 1] == null ? "" : valueArray[row, column + 1].ToString();
                    }
                    Data.AddData(dataLine);
                }
                Finish();
            }
            catch (Exception excep)
            {
                Log.E("Couldn't get data.");
                Log.E(excep.StackTrace);
            }
        }

        public void Finish()
        {
            Log.D("Closing file.");
            try
            {
                if (currentSheet != null)
                {
                    while (Marshal.ReleaseComObject(currentSheet) > 0) ;
                    currentSheet = null;
                }
                if (workBook != null)
                {
                    workBook.Close(0);
                    while (Marshal.ReleaseComObject(workBook) > 0) ;
                    workBook = null;
                }
                Utils.QuitExcel();
            }
            catch
            {
                Log.D("Something went wrong when trying to close excel file.");
            }
        }
    }
}
