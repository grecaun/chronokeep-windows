using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;

namespace Chronokeep
{
    class ExcelImporter : IDataImporter
    {
        public ImportData Data { get; private set; }
        string FilePath;

        XLWorkbook workbook;
        IXLWorksheet worksheet;

        public List<string> SheetNames { get; private set; }
        public int NumSheets { get; private set; }

        public ExcelImporter(string filename)
        {
            Log.D("IO.ExcelImporter", "Creating importer object.");
            FilePath = filename;
            try
            {
                Log.D("IO.ExcelImporter", "Opening workbook.");
                workbook = new XLWorkbook(filename);
                NumSheets = workbook.Worksheets.Count;
                if (NumSheets > 0)
                {
                    worksheet = workbook.Worksheets.Worksheet(1);
                }
                SheetNames = new List<string>();
                for (int i = 1; i <= NumSheets; i++)
                {
                    string name = workbook.Worksheets.Worksheet(i).Name;
                    SheetNames.Add(name);
                    Log.D("IO.ExcelImporter", "Sheet name is " + name);
                }
            }
            catch (Exception excep)
            {
                Log.E("IO.ExcelImporter", $"Something went wrong when trying to open workseet. {excep.StackTrace}");
            }
        }

        public void ChangeSheet(int ix)
        {
            try
            {
                if (ix <= NumSheets && ix >= 0)
                {
                    worksheet = workbook.Worksheets.Worksheet(ix);
                }
            }
            catch
            {
                Log.E("IO.ExcelImporter", "Hmm, can't get that worksheet. Sorry.");
            }
        }

        public void FetchHeaders()
        {
            Log.D("IO.ExcelImporter", "Getting headers from excel file.");
            try
            {
                Log.D("IO.ExcelImporter", "Used range set.");
                var rows = worksheet.RowsUsed();
                int numHeaders = 0;
                int numDataRows = 0;
                foreach (var row in rows)
                {
                    int tmp = 0;
                    foreach (var col in row.CellsUsed())
                    {
                        tmp++;
                    }
                    numHeaders = numHeaders > tmp ? numHeaders : tmp;
                    numDataRows++;
                }
                Log.D("IO.ExcelImporter", "Value array populated. Rows " + numDataRows + " Columns " + numHeaders);
                string[] headers = new string[numHeaders];
                for (int i=1; i<=numHeaders; i++)
                {
                    headers[i-1] = worksheet.Cell(1, i).Value == null ? "" : worksheet.Cell(1,i).Value.ToString();
                }
                Data = new ImportData(headers, FilePath, ImportData.FileType.EXCEL);
            }
            catch (Exception excep)
            {
                Log.E("IO.ExcelImporter", $"Something went wrong when trying to get headers. {excep.StackTrace}");
                string[] headers = new string[0];
                Data = new ImportData(headers, FilePath, ImportData.FileType.EXCEL);
            }
        }

        public void FetchData()
        {
            Log.D("IO.ExcelImporter", "Getting data from excel file.");
            try
            {
                var rows = worksheet.RowsUsed();
                int numHeaders = 0;
                int numDataRows = 0;
                foreach (var row in rows)
                {
                    int tmp = 0;
                    foreach (var col in row.CellsUsed())
                    {
                        tmp++;
                    }
                    numHeaders = numHeaders > tmp ? numHeaders : tmp;
                    numDataRows++;
                }
                object[,] valueArray = new object[numDataRows, numHeaders];
                for (int i=1; i<= numDataRows; i++)
                {
                    for (int j=1; j<=numHeaders; j++)
                    {
                        valueArray[i-1, j-1] = worksheet.Cell(i, j).Value;
                    }
                }
                Log.D("IO.ExcelImporter", "Value array populated. Rows " + numDataRows + " Columns " + numHeaders);
                for (int row = 1; row < numDataRows; row++)
                {
                    string[] dataLine = new string[numHeaders];
                    for (int column=0; column < numHeaders; column++)
                    {
                        dataLine[column] = valueArray[row, column] == null ? "" : valueArray[row, column].ToString();
                    }
                    Data.AddData(dataLine);
                }
                Finish();
            }
            catch (Exception excep)
            {
                Log.E("IO.ExcelImporter", $"Couldn't get data. {excep.StackTrace}");
            }
        }

        public void Finish()
        {
            Log.D("IO.ExcelImporter", "Closing file.");
            try
            {
                if (worksheet != null)
                {
                    worksheet = null;
                }
                if (workbook != null)
                {
                    workbook.Dispose();
                    workbook = null;
                }
            }
            catch
            {
                Log.D("IO.ExcelImporter", "Something went wrong when trying to close excel file.");
            }
        }
    }
}
