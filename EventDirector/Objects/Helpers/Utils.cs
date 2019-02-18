using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Application = Microsoft.Office.Interop.Excel.Application;

namespace EventDirector
{
    public class Utils
    {
        private static Application ExcelApp;

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
                ExcelApp = new Application();
                ExcelApp.Quit();
                while (Marshal.ReleaseComObject(ExcelApp) > 0);
                ExcelApp = null;
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static Application GetExcelApp()
        {
            if (ExcelApp == null)
            {
                try
                {
                    ExcelApp = new Application();
                }
                catch
                {
                    return null;
                }
            }
            return ExcelApp;
        }

        public static void QuitExcel()
        {
            if (ExcelApp != null)
            {
                ExcelApp.Quit();
                while (Marshal.ReleaseComObject(ExcelApp) > 0) ;
                ExcelApp = null;
            }
        }

        public static DependencyObject GetScrollViewer(DependencyObject o)
        {
            if (o is ScrollViewer)
                return o;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);
                var result = GetScrollViewer(child);
                if (result == null)
                    continue;
                else
                    return result;
            }
            return null;
        }

        public enum FileType { CSV, EXCEL }
    }
}
