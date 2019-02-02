namespace EventDirector.Interfaces
{
    public interface IMainPage
    {
        void UpdateView();
        void Closing();
        void UpdateDatabase();
        void Keyboard_Ctrl_A();
        void Keyboard_Ctrl_S();
        void Keyboard_Ctrl_Z();
    }
}
