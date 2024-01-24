namespace Chronokeep.Database
{
    class InvalidDatabaseVersion : System.Exception
    {
        public int FoundVersion { get; set; } = -1;
        public int MaxVersion { get; set; } = -1;
        public InvalidDatabaseVersion() : base() { }
        public InvalidDatabaseVersion(int foundVersion, int maxVersion) : base()
        {
            FoundVersion = foundVersion;
            MaxVersion = maxVersion;
        }
    }
}
