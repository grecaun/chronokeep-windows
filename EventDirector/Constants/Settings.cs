using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector.Constants
{
    class Settings
    {
        public static readonly string PROGRAM_DIR            = "EventDirector";

        public static readonly string DEFAULT_EXPORT_DIR    = "SETTING_DEFAULT_EXPORT_DIR";
        public static readonly string AUTO_READ_LOG         = "SETTING_AUTO_READ_LOG";
        public static readonly string AUTO_READ_LOG_DIR     = "SETTING_AUTO_READ_LOG_DIR";
        public static readonly string DEFAULT_TIMING_SYSTEM = "SETTING_DEFAULT_TIMING_SYSTEM";
        public static readonly string DEFAULT_WAIVER        = "SETTING_DEFAULT_WAIVER";
        public static readonly string SHOW_PRICES           = "SETTING_SHOW_PRICES";
        public static readonly string CURRENT_EVENT         = "SETTING_CURRENT_EVENT";
        public static readonly string COMPANY_NAME          = "SETTING_COMPANY_NAME";
        public static readonly string SETTING_LAST_TIMING   = "SETTING_LAST_USED_TIMING";

        public static readonly string NULL_EVENT_ID     = "-1";

        public static readonly string TIMING_RFID       = "RFID";
        public static readonly string TIMING_IPICO      = "IPICO";
        public static readonly string TIMING_MANUAL     = "MANUAL";
        public static readonly string TIMING_LAST_USED  = "LAST_USED";

        public static readonly string WAIVER_YEAR       = "[YEAR]";
        public static readonly string WAIVER_EVENT      = "[EVENT]";
        public static readonly string WAIVER_COMPANY    = "[COMPANY]";
        public static readonly string EXAMPLE_WAIVER    = "[YEAR] PARTICIPANT'S LIABILITY RELEASE & HOLD HARMLESS AGREEMENT \n\n" +
            "By my signature and/or acceptance below, I hereby enter into this binding Liability Release & Hold Harmless Agreement " +
            "(hereinafter referred to as the 'Agreement'). I understand and acknowledge: (a) that the [EVENT] (hereinafter " +
            "referred to as the 'Race') is potentially hazardous; (b) that the Race involves grueling physical activity in racing over " +
            "rugged natural terrain and presents a significant challenge and risk of injury to myself and others; (c) that I am familiar " +
            "with the risks involved; and (d) that I should not enter and participate unless I am medically able and properly trained. I " +
            "realize participation in the Race is allowed in consideration of payment of the entry fee and my entering into this Agreement, " +
            "and I acknowledge that such participation is adequate consideration for the rights herein released and obligations undertaken. " +
            "\n\nTherefore, for myself and my heirs and executors, I hereby: (a) assume all risks (known and unknown) associated with or " +
            "arising out of my participation in this Race, including but not limited to, falls, drowning, contact with objects, spectators, " +
            "and other participants, occurrences associated with weather, conditions of water and the terrain, and conditions of traffic, " +
            "roads and trails; (b) waive any and all specific notice of the existence of these and any other such hazards or conditions; (c) " +
            "assume full and complete responsibility for any injury or accident arising out of or associated with my participation in this " +
            "Race; and (d) RELEASE [COMPANY], [EVENT], its directors, officers, employees, volunteers, or any agent or representative of them" +
            " (collectively referred to as the 'Sponsoring Organization'), and INDEMNIFY and HOLD HARMLESS the Sponsoring Organization from any " +
            "and all liability, cause of action, claims, demands, costs or debts of any kind or nature, associated with or arising from or out " +
            "of my participation in this Race, whether such claims are based on negligence or otherwise and whether brought by myself or by any" +
            " third persons (collectively referred to as 'Claims'). \n\nIf Racer is under 18 years of age, the parent/guardian's acceptance" +
            " of this Agreement constitutes parent/guardian's RELEASE and promise to INDEMNIFY and HOLD HARMLESS the Sponsoring Organization for" +
            " parent/guardian's Claims. Additionally, parent/guardian agrees to INDEMNIFY, DEFEND, and HOLD HARMLESS Sponsoring Organization for" +
            " Claims by Racer against Sponsoring Organization. \n\nI grant permission to all of the foregoing to use any photographs, " +
            "motion pictures, records, or any other record of this event for any legitimate purpose and I understand that my name will be listed" +
            " in the results posted in media publications including but not limited online and newspapers.  \n\nI HAVE READ, UNDERSTOOD," +
            " AND ACCEPTED THE CONDITIONS OF THE LIABILITY RELEASE PRINTED ABOVE.";

        public static void SetupSettings(IDBInterface database)
        {
            if (database.GetAppSetting(DEFAULT_EXPORT_DIR) == null)
            {
                String dirPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), PROGRAM_DIR, "Exports");
                database.SetAppSetting(DEFAULT_EXPORT_DIR, dirPath);
            }
            if (database.GetAppSetting(DEFAULT_TIMING_SYSTEM) == null)
            {
                database.SetAppSetting(DEFAULT_TIMING_SYSTEM, TIMING_LAST_USED);
            }
            if (database.GetAppSetting(DEFAULT_WAIVER) == null)
            {
                database.SetAppSetting(DEFAULT_WAIVER, EXAMPLE_WAIVER);
            }
            if (database.GetAppSetting(CURRENT_EVENT) == null)
            {
                database.SetAppSetting(CURRENT_EVENT, NULL_EVENT_ID);
            }
            if (database.GetAppSetting(COMPANY_NAME) == null)
            {
                database.SetAppSetting(COMPANY_NAME, WAIVER_COMPANY);
            }
        }
    }
}
