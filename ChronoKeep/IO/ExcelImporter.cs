using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;

namespace Chronokeep
{
    class ExcelImporter : IDataImporter
    {
        public ImportData Data { get; private set; }
        string FilePath;

        ExcelPackage package;
        ExcelWorksheet worksheet;
        ExcelWorkbook workbook;

        public List<string> SheetNames { get; private set; }
        public int NumSheets { get; private set; }

        public ExcelImporter(string filename)
        {
            Log.D("IO.ExcelImporter", "Creating importer object.");
            FilePath = filename;
            try
            {
                Log.D("IO.ExcelImporter", "Opening workbook.");
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                package = new ExcelPackage(new FileInfo(filename));
                workbook = package.Workbook;
                NumSheets = workbook.Worksheets.Count;
                if (NumSheets > 0)
                {
                    worksheet = workbook.Worksheets[0];
                }
                SheetNames = new List<string>();
                for (int i = 0; i < NumSheets; i++)
                {
                    string name = workbook.Worksheets[i].Name;
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
                if (ix < NumSheets && ix >= 0)
                {
                    worksheet = workbook.Worksheets[ix];
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
                object[,] valueArray = worksheet.Cells.GetValue<object[,]>();
                Log.D("IO.ExcelImporter", "Value array populated.");
                int numHeaders = valueArray.GetUpperBound(1);
                int numDataRows = valueArray.GetUpperBound(0);
                Log.D("IO.ExcelImporter", "Rows " + numDataRows + " Columns " + numHeaders);
                string[] headers = new string[numHeaders];
                for (int i=0; i<numHeaders; i++)
                {
                    headers[i] = valueArray[0, i] == null ? "" : valueArray[0, i].ToString();
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
                object[,] valueArray = worksheet.Cells.GetValue<object[,]>();
                int numHeaders = valueArray.GetUpperBound(1);
                int numDataRows = valueArray.GetUpperBound(0);
                Log.D("IO.ExcelImporter", "Rows " + numDataRows + " Columns " + numHeaders);
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
                    worksheet.Dispose();
                    worksheet = null;
                }
                if (workbook != null)
                {
                    workbook.Dispose();
                    workbook = null;
                }
                if (package != null)
                {
                    package.Dispose();
                    package = null;
                }
            }
            catch
            {
                Log.D("IO.ExcelImporter", "Something went wrong when trying to close excel file.");
            }
        }
    }
}
