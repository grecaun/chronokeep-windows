using System.Threading;

namespace Chronokeep.Interfaces
{
    public interface ISubPage : IMainPage
    {
        void CancelableUpdateView(CancellationToken token);
        void Show(PeopleType type);
        void SortBy(SortType type);
        void EditSelected();
    }

    public enum PeopleType { KNOWN, ALL, STARTS, FINISHES, DEFAULT, UNKNOWN, UNKNOWN_FINISHES, UNKNOWN_STARTS }
    public enum SortType { SYSTIME, GUNTIME, BIB, DISTANCE, AGEGROUP, GENDER, PLACE }
}
