using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EventDirector.Interfaces.Timing
{
    public interface ITimingSystemInterface
    {
        Dictionary<MessageType, List<string>> ParseMessages(string message);
        void SettingsWindow();
        void ClockWindow();
        void StartReading();
        void StopReading();
        void SetTime(DateTime date);
        void GetTime();
        void GetStatus();
        void StartSending();
        void StopSending();
        void Rewind(DateTime start, DateTime end);
        void Rewind(int from, int to);
        void Rewind();
        void SetMainSocket(Socket sock);
        void SetSettingsSocket(Socket sock);
    }

    public enum MessageType { CONNECTED, VOLTAGENORMAL, VOLTAGELOW, CHIPREAD, TIME, SETTINGVALUE, SETTINGCHANGE, STATUS, UNKNOWN, ERROR, NONE }
}
