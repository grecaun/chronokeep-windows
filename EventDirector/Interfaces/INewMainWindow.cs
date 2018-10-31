using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector.Interfaces
{
    public interface INewMainWindow : IMainWindow
    {
        Task<bool> StartNetworkServices();
        Task<bool> StopNetworkServices();
    }
}
