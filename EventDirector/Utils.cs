using Microsoft.Office.Interop.Excel;
using System.Threading.Tasks;

namespace EventDirector
{
    public class Utils
    {
        public static Application excelApp;

        public static string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] charArray = s.ToCharArray();
            charArray[0] = char.ToUpper(charArray[0]);
            return new string(charArray);
        }

        public static bool ExcelEnabled()
        {
            try
            {
                excelApp = new Application();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public enum FileType { CSV, EXCEL }
    }
}
