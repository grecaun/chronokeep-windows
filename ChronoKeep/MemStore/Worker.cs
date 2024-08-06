using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.MemStore
{
    public interface Worker
    {
        void DoWork(IDBInterface database);
        int GetQueuePosition();
        void SetQueuePosition(int pos);
    }
}
