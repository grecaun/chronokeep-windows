using Microsoft.Office.Interop.Excel;
using System.Threading.Tasks;

namespace EventDirector
{
    class Utils
    {
        public static readonly Application excelApp = new Application();

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
    }
}
