using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Chronokeep.Interfaces.Timing
{
    public interface ITimingSystemInterface
    {
        Dictionary<MessageType, List<string>> ParseMessages(string message, Socket sock);
        List<Socket> Connect(string IpAddress, int Port);
        void Disconnect();
        void StartReading();
        void StopReading();
        void SetTime(DateTime date);
        void GetTime();
        void GetStatus();
        void StartSending();
        void StopSending();
        void Rewind(DateTime start, DateTime end, int reader = 1);
        void Rewind(int from, int to, int reader = 1);
        void Rewind(int reader = 1);
        void SetMainSocket(Socket sock);
        void SetSettingsSocket(Socket sock);
    }

    public enum MessageType {
        CONNECTED,
        VOLTAGENORMAL,
        VOLTAGELOW,
        CHIPREAD,
        TIME,
        SETTINGVALUE,
        SETTINGCHANGE,
        STATUS,
        UNKNOWN,
        ERROR,
        NONE,
        SUCCESS,
        DISCONNECT
    }
}
