using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep
{
    public interface IDataImporter
    {
        ImportData Data { get; }
        void FetchHeaders();
        void FetchData();
        void Finish();
    }
}
