﻿namespace EventDirector.Interfaces
{
    public interface ISubPage : IMainPage
    {
        void Search(string value);
        void Show(PeopleType type);
        void SortBy(SortType type);
        void EditSelected();
    }

    public enum PeopleType { KNOWN, ALL, ONLYSTART, ONLYFINISH }
    public enum SortType { SYSTIME, GUNTIME, BIB, DIVISION }
}