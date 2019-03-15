namespace EventDirector.Interfaces
{
    public interface ISubPage : IMainPage
    {
        void Search(string value);
        void Show(PeopleType type);
        void SortBy(SortType type);
        void EditSelected();
    }

    public enum PeopleType { KNOWN, ALL, ONLYSTART, ONLYFINISH, DEFAULT }
    public enum SortType { SYSTIME, GUNTIME, BIB, DIVISION, AGEGROUP, GENDER, PLACE }
}
