using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace EventDirector.Interfaces
{
    public interface IWindowCallback
    {
        void WindowFinalize(Window w);
        void Update();
    }
}
