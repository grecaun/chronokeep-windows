namespace EventDirector.Interfaces
{
    interface ISubPage : IMainPage
    {
        void Search(string value);
        void EditSelected();
    }
}
