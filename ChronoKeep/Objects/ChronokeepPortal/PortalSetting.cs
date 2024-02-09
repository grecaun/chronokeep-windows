using System.Text.Json.Serialization;

namespace Chronokeep.Objects.ChronokeepPortal
{
    public class PortalSetting
    {
        public const string SETTING_PORTAL_NAME = "SETTING_PORTAL_NAME";
        public const string SETTING_SIGHTING_PERIOD = "SETTING_SIGHTING_PERIOD";
        public const string SETTING_READ_WINDOW = "SETTING_READ_WINDOW";
        public const string SETTING_CHIP_TYPE = "SETTING_CHIP_TYPE";
        public const string SETTING_PLAY_SOUND = "SETTING_PLAY_SOUND";
        public const string SETTING_VOLUME = "SETTING_VOLUME";
        public const string SETTING_VOICE = "SETTING_VOICE";

        public const string TYPE_CHIP_DEC = "DEC";
        public const string TYPE_CHIP_HEX = "HEX";

        public const string VOICE_EMILY = "emily";
        public const string VOICE_MICHAEL = "michael";
        public const string VOICE_CUSTOM = "custom";

        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
