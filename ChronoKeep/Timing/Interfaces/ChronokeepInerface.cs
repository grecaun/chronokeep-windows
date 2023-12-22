using Chronokeep.Interfaces;
using Chronokeep.Interfaces.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Timing.Interfaces
{
    internal class ChronokeepInerface : ITimingSystemInterface
    {
        IDBInterface database;
        readonly int locationId;
        Event theEvent;
        StringBuilder buffer = new StringBuilder();
        Socket sock;
        IMainWindow window = null;

        public ChronokeepInerface(IDBInterface database, int locationId, IMainWindow window)
        {
            this.database = database;
            this.theEvent = database.GetCurrentEvent();
            this.locationId = locationId;
            this.window = window;
        }

        public List<Socket> Connect(string IpAddress, int Port)
        {
            throw new NotImplementedException();
        }

        public void GetStatus()
        {
            throw new NotImplementedException();
        }

        public void GetTime()
        {
            throw new NotImplementedException();
        }

        public Dictionary<MessageType, List<string>> ParseMessages(string message, Socket sock)
        {
            throw new NotImplementedException();
        }

        public void Rewind(DateTime start, DateTime end, int reader = 1)
        {
            throw new NotImplementedException();
        }

        public void Rewind(int from, int to, int reader = 1)
        {
            throw new NotImplementedException();
        }

        public void Rewind(int reader = 1)
        {
            throw new NotImplementedException();
        }

        public void SetMainSocket(Socket sock)
        {
            throw new NotImplementedException();
        }

        public void SetSettingsSocket(Socket sock)
        {
            throw new NotImplementedException();
        }

        public void SetTime(DateTime date)
        {
            throw new NotImplementedException();
        }

        public void StartReading()
        {
            throw new NotImplementedException();
        }

        public void StartSending()
        {
            throw new NotImplementedException();
        }

        public void StopReading()
        {
            throw new NotImplementedException();
        }

        public void StopSending()
        {
            throw new NotImplementedException();
        }
    }
}
