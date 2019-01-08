using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace EventDirector
{
    public interface IMainWindow
    {
        void WindowClosed(Window window);
        void UpdateEvent(int identifier, string nameString, long dateVal, int nextYear, int shirtOptionalVal, int shirtPrice);
        void AddEvent(string nameString, long dateVal, int shirtOptionalVal, int shirtPrice);
    }
}
