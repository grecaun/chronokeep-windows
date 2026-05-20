namespace Chronokeep.UI.Timing.Notifications
{
    internal class WaveSMS
    {
        public int Wave { get; set; }
        public string WaveName { get => string.Format("Wave {0}", Wave); }
        public bool SMSEnabled { get; set; }
    }
}
