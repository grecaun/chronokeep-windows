namespace Chronokeep.Objects.ChronokeepPortal
{
    internal class PortalNotification
    {
        public const string UPS_DISCONNECTED = "UPS_DISCONNECTED";
        public const string UPS_CONNECTED = "UPS_CONNECTED";
        public const string UPS_ON_BATTERY = "UPS_ON_BATTERY";
        public const string UPS_LOW_BATTERY = "UPS_LOW_BATTERY";
        public const string UPS_ONLINE = "UPS_ONLINE";
        public const string SHUTTING_DOWN = "SHUTTING_DOWN";
        public const string RESTARTING = "RESTARTING";
        public const string HIGH_TEMP = "HIGH_TEMP";
        public const string MAX_TEMP = "MAX_TEMP";

        // TODO - add information for all portal error messages
        public static string GetRemoteNotificationMessage(string reader, string Type) {
            switch (Type)
            {
                case UPS_DISCONNECTED:
                    return string.Format("Notification from '{0}'\nUPS has been disconnected.", reader);
                case UPS_CONNECTED:
                    return string.Format("Notification from '{0}'\nUPS connection has been re-established.", reader);
                case UPS_ON_BATTERY:
                    return string.Format("Notification from '{0}'\nUPS is working from battery power.\nCheck to ensure the UPS is plugged in and the power source is working.", reader);
                case UPS_LOW_BATTERY:
                    return string.Format("Notification from '{0}'\nUPS battery is low. Shutdown imminent.", reader);
                case UPS_ONLINE:
                    return string.Format("Notification from '{0}'\nUPS is back on line power and no longer running off the UPS battery.", reader);
                case SHUTTING_DOWN:
                    return string.Format("Notification from '{0}'\nShutting down.", reader);
                case RESTARTING:
                    return string.Format("Notification from '{0}'\nRestarting.", reader);
                case HIGH_TEMP:
                    return string.Format("Notification from '{0}'\nTemperature is high.", reader);
                case MAX_TEMP:
                    return string.Format("Notification from '{0}'\nTemperature is very high. Throttling wil most likely occur.", reader);
                default:
                    return string.Format("Notification from '{0}'\nUnknown notification. {1}", reader, Type);
            }
        }

        public static string GetRemoteNotificationMessage(string Type)
        {
            switch (Type)
            {
                case UPS_DISCONNECTED:
                    return string.Format("UPS has been disconnected.");
                case UPS_CONNECTED:
                    return string.Format("UPS connection has been re-established.");
                case UPS_ON_BATTERY:
                    return string.Format("UPS is working from battery power.\nCheck to ensure the UPS is plugged in and the power source is working.");
                case UPS_LOW_BATTERY:
                    return string.Format("UPS battery is low. Shutdown imminent.");
                case UPS_ONLINE:
                    return string.Format("UPS is back on line power and no longer running off the UPS battery.");
                case SHUTTING_DOWN:
                    return string.Format("Shutting down.");
                case RESTARTING:
                    return string.Format("Restarting.");
                case HIGH_TEMP:
                    return string.Format("Temperature is high.");
                case MAX_TEMP:
                    return string.Format("Temperature is very high. Throttling wil most likely occur.");
                default:
                    return string.Format("Unknown notification. {1}", Type);
            }
        }
    }
}
