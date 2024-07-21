using Chronokeep.Objects.ChronoKeepAPI;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Database.SQLite
{
    internal class SmsSubscriptions
    {
        public static List<APISmsSubscription> GetSmsSubscriptions(int eventId, SQLiteConnection connection)
        {
            return null;
        }

        public static void AddSmsSubscriptions(int eventId, List<APISmsSubscription> subscriptions, SQLiteConnection connection)
        {

        }

        public static void DeleteSmsSubscriptions(int eventId, SQLiteConnection connection)
        {

        }
    }
}
