using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Constants
{
    class Settings
    {
        public static readonly string PROGRAM_DIR           = "Chronokeep";

        public static readonly string SERVER_NAME           = "SETTING_SERVER_NAME";
        public static readonly string DATABASE_VERSION      = "DATABASE_VERSION";

        public static readonly string DEFAULT_EXPORT_DIR    = "SETTING_DEFAULT_EXPORT_DIR";
        public static readonly string DEFAULT_TIMING_SYSTEM = "SETTING_DEFAULT_TIMING_SYSTEM";
        public static readonly string CURRENT_EVENT         = "SETTING_CURRENT_EVENT";
        public static readonly string COMPANY_NAME          = "SETTING_COMPANY_NAME";
        public static readonly string CONTACT_EMAIL         = "SETTING_CONTACT_EMAIL";
        public static readonly string UPDATE_ON_PAGE_CHANGE = "SETTING_UPDATE_PAGE_CHANGE";
        public static readonly string EXIT_NO_PROMPT        = "EXIT_NO_PROMPT";
        public static readonly string DEFAULT_CHIP_TYPE     = "DEFAULT_CHIP_TYPE";
        public static readonly string LAST_USED_API_ID      = "SETTING_LAST_USED_API_ID";
        public static readonly string CHECK_UPDATES         = "SETTING_CHECK_UPDATES";
        public static readonly string CURRENT_THEME         = "SETTING_THEME";

        public static readonly string NULL_EVENT_ID     = "-1";

        public static readonly string SETTING_TRUE      = "TRUE";
        public static readonly string SETTING_FALSE     = "FALSE";

        public static readonly string TIMING_RFID       = "RFID";
        public static readonly string TIMING_IPICO      = "IPICO";
        public static readonly string TIMING_IPICO_LITE = "IPICO_LITE";

        public static readonly string THEME_SYSTEM      = "THEME_SYSTEM";
        public static readonly string THEME_DARK        = "THEME_DARK";
        public static readonly string THEME_LIGHT       = "THEME_LIGHT";

        public static readonly string CHIP_TYPE_DEC     = "DEC";
        public static readonly string CHIP_TYPE_HEX     = "HEX";

        public static readonly string WAIVER_YEAR       = "[YEAR]";
        public static readonly string WAIVER_EVENT      = "[EVENT]";
        public static readonly string WAIVER_COMPANY    = "[COMPANY]";

        public static void SetupSettings(IDBInterface database)
        {
            if (database.GetAppSetting(DEFAULT_EXPORT_DIR) == null)
            {
                string dirPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), PROGRAM_DIR, "Exports");
                database.SetAppSetting(DEFAULT_EXPORT_DIR, dirPath);
            }
            if (database.GetAppSetting(DEFAULT_TIMING_SYSTEM) == null)
            {
                database.SetAppSetting(DEFAULT_TIMING_SYSTEM, TIMING_RFID);
            }
            if (database.GetAppSetting(CURRENT_EVENT) == null)
            {
                database.SetAppSetting(CURRENT_EVENT, NULL_EVENT_ID);
            }
            if (database.GetAppSetting(COMPANY_NAME) == null)
            {
                database.SetAppSetting(COMPANY_NAME, "");
            }
            if (database.GetAppSetting(UPDATE_ON_PAGE_CHANGE) == null)
            {
                database.SetAppSetting(UPDATE_ON_PAGE_CHANGE, SETTING_TRUE);
            }
            if (database.GetAppSetting(EXIT_NO_PROMPT) == null)
            {
                database.SetAppSetting(EXIT_NO_PROMPT, SETTING_FALSE);
            }
            if (database.GetAppSetting(DEFAULT_CHIP_TYPE) == null)
            {
                database.SetAppSetting(DEFAULT_CHIP_TYPE, CHIP_TYPE_DEC);
            }
            if (database.GetAppSetting(CONTACT_EMAIL) == null)
            {
                database.SetAppSetting(CONTACT_EMAIL, "");
            }
            if (database.GetAppSetting(CHECK_UPDATES) == null)
            {
                database.SetAppSetting(CHECK_UPDATES, SETTING_FALSE);
            }
            if (database.GetAppSetting(CURRENT_THEME) == null)
            {
                if (Utils.GetSystemTheme() != -1)
                {
                    database.SetAppSetting(CURRENT_THEME, THEME_SYSTEM);
                }
                else
                {
                    database.SetAppSetting(CURRENT_THEME, THEME_LIGHT);
                }
            }
        }
    }
}
