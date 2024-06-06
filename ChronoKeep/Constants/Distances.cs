namespace Chronokeep.Constants
{
    public class Distances
    {
        public const int UNKNOWN = 0;
        public const int MILES = 1;
        public const int YARDS = 2;
        public const int FEET = 3;
        public const int KILOMETERS = 101;
        public const int METERS = 102;

        public static string DistanceString(int dist)
        {
            switch (dist)
            {
                case Distances.MILES:
                    return "Miles";
                case Distances.YARDS:
                    return "Yards";
                case Distances.FEET:
                    return "Feet";
                case Distances.METERS:
                    return "Meters";
                case Distances.KILOMETERS:
                    return "Kilometers";
            }
            return "";
        }
    }
}
