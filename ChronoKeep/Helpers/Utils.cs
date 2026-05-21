using Avalonia.Platform.Storage;
using Microsoft.Win32;
using System;

namespace Chronokeep.Helpers
{
    public class Utils
    {
        private const string REGISTRY_KEY_NAME = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string APPS_USE_LIGHT_THEME = "AppsUseLightTheme";

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

        public static int GetSystemTheme()
        {
            if (OperatingSystem.IsWindows())
            {
                var registryValue = Registry.GetValue(REGISTRY_KEY_NAME, APPS_USE_LIGHT_THEME, -1);
                if (registryValue != null)
                {
                    return int.Parse(registryValue.ToString()!);
                }
            }
            return -1;
        }

        public enum FileType { CSV, EXCEL }

        public static FilePickerFileType ExcelType { get; } = new("Excel File")
        {
            Patterns = ["*.xlsx", "*.xls", "*.csv"],
        };
    }
}
