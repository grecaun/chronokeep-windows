namespace Chronokeep.Objects
{
    public class DistanceStat
    {
        public string DistanceName { get; set; }
        public int DistanceID { get; set; }
        public int Total { get => DNF + DNS + Finished + Active; }
        public int DNF { get; set; }
        public int DNS { get; set; }
        public int Finished { get; set; }
        public int Active { get; set; }
    }
}
