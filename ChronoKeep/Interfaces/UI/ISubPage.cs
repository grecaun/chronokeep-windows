using System.Threading;

namespace ChronoKeep.Interfaces
{
    public interface ISubPage : IMainPage
    {
        void Search(string value, CancellationToken token);
        void Show(PeopleType type);
        void SortBy(SortType type);
        void EditSelected();
    }

    public enum PeopleType { KNOWN, ALL, ONLYSTART, ONLYFINISH, DEFAULT }
    public enum SortType { SYSTIME, GUNTIME, BIB, DISTANCE, AGEGROUP, GENDER, PLACE }
}
