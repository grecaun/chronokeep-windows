using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector.Interfaces
{
    interface IMainPage
    {
        void UpdateView();
        void UpdateDatabase();
        void Keyboard_Ctrl_A();
        void Keyboard_Ctrl_S();
        void Keyboard_Ctrl_Z();
    }
}
