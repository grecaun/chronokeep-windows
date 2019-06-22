using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChronoKeep.Interfaces
{
    public interface IWindowCallback
    {
        void WindowFinalize(Window w);
    }
}
