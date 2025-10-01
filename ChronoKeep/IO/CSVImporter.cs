using Chronokeep.Helpers;
using Chronokeep.IO;
using System.IO;
using System.Text.RegularExpressions;

namespace Chronokeep
{
    public class CSVImporter : IDataImporter
    {
        Regex regex = new Regex("\"[^\"]*\",|[^,]*,|[^,]*$");

        public ImportData Data { get; private set; }
        protected readonly string FilePath;
        protected StreamReader file;

        public CSVImporter(string filePath)
        {
            Log.D("IO.CSVImporter", "Opening file.");
            file = new StreamReader(filePath);
            FilePath = filePath;
        }

        public void FetchHeaders()
        {
            Log.D("IO.CSVImporter", "Getting headers from file.");
            ProcessFirstLine(file.ReadLine());
        }

        protected void ProcessFirstLine(string line)
        {
            MatchCollection matches = regex.Matches(line);
            string[] headers = new string[matches.Count];
            int counter = 0;
            foreach (Match m in matches)
            {
                headers[counter++] = m.Value.Replace('"', ' ').TrimEnd(',').Trim();
            }
            Data = new ImportData(headers, FilePath, ImportData.FileType.CSV);
        }

        public void FetchData()
        {
            Log.D("IO.CSVImporter", "Getting data from file.");
            string line;
            while ((line = file.ReadLine()) != null)
            {
                MatchCollection matches = regex.Matches(line);
                string[] dataLine = new string[matches.Count];
                int counter = 0;
                string match;
                foreach (Match m in matches)
                {
                    match = m.Value.Replace('"', ' ').Trim().TrimEnd(',');
                    dataLine[counter++] = match;
                }
                Data.AddData(dataLine);
            }
            Finish();
        }

        public void Finish()
        {
            if (file != null)
            {
                try
                {
                    Log.D("IO.CSVImporter", "Closing file.");
                    file.Close();
                    file = null;
                }
                catch
                {
                    Log.D("IO.CSVImporter", "Already closed.");
                }
            }
        }
    }
}