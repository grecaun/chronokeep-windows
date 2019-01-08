using EventDirector.Interfaces;
using EventDirector.Interfaces.Timing;
using EventDirector.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventDirector.Timing
{
    class TimingController
    {
        private static Mutex mut = new Mutex();
        bool Running = false;
        List<Socket> TimingSystemSockets = new List<Socket>(), readList = new List<Socket>();
        Dictionary<Socket, TimingSystem> TimingSystemDict = new Dictionary<Socket, TimingSystem>();

        IDBInterface database;
        INewMainWindow mainWindow;

        public TimingController(IDBInterface database, INewMainWindow mainWindow)
        {
            this.database = database;
            this.mainWindow = mainWindow;
        }

        public bool IsRunning()
        {
            bool output = false;
            if (mut.WaitOne(6000))
            {
                output = this.Running;
                mut.ReleaseMutex();
            }
            return output;
        }

        public List<TimingSystem> GetConnectedSystems()
        {
            List<TimingSystem> output = new List<TimingSystem>();
            output.AddRange(TimingSystemDict.Values);
            return output;
        }

        public void ConnectTimingSystem(TimingSystem system)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Log.D("Attempting to connect to " + system.IPAddress);
            TimingSystemDict[sock] = system;
            try
            {
                sock.Connect(system.IPAddress, system.Port);
            }
            catch
            {
                Log.D("Unable to connect to " + system.IPAddress);
                system.Status = SYSTEM_STATUS.DISCONNECTED;
                return;
            }
            system.Socket = sock;
            if (sock.Connected)
            {
                Log.D("Connected to " + system.IPAddress);
                TimingSystemSockets.Add(sock);
                TimingSystemDict[sock].SetTime();
            }
            else
            {
                Log.D("Unable to connect to " + system.IPAddress);
                TimingSystemDict.Remove(sock);
                system.Status = SYSTEM_STATUS.DISCONNECTED;
            }
        }

        public void DisconnectTimingSystem(TimingSystem system)
        {
            system.Socket.Disconnect(false);
            TimingSystemSockets.Remove(system.Socket);
            TimingSystemDict.Remove(system.Socket);
            system.Status = SYSTEM_STATUS.DISCONNECTED;
        }

        public void Run()
        {
            Log.D("Timing Controller is now running.");
            if (mut.WaitOne(6000))
            {
                this.Running = true;
                mut.ReleaseMutex();
            }
            else
            {
                return;
            }
            while (TimingSystemSockets.Count > 0)
            {
                readList.Clear();
                readList.AddRange(TimingSystemSockets);
                Socket.Select(readList, null, null, 3000000);
                foreach (Socket sock in readList)
                {
                    byte[] recvd = new byte[2056];
                    try
                    {
                        int num_recvd = sock.Receive(recvd);
                        if (num_recvd == 0)
                        {
                            Log.D("No longer connected to Timing System");
                            TimingSystem disconnected = TimingSystemDict[sock];
                            TimingSystemSockets.Remove(sock);
                            TimingSystemDict.Remove(sock);
                            mainWindow.TimingSystemDisconnected(disconnected);
                        }
                        else
                        {
                            String msg = Encoding.UTF8.GetString(recvd, 0, num_recvd);
                            Log.D("Timing System - Message is :" + msg);
                            HashSet<MessageType> messageTypes = TimingSystemDict[sock].SystemInterface.ParseMessages(msg);
                            foreach (MessageType type in messageTypes)
                            {
                                switch (type)
                                {
                                    case MessageType.CONNECTED:
                                        Log.D("Timing system successfully connected.");
                                        TimingSystemDict[sock].Status = SYSTEM_STATUS.CONNECTED;
                                        break;
                                    case MessageType.SETTINGCHANGE:
                                        Log.D("Setting value changed.");
                                        break;
                                    case MessageType.SETTINGVALUE:
                                        Log.D("Setting value given.");
                                        break;
                                    case MessageType.VOLTAGENORMAL:
                                        Log.D("System voltage normal.");
                                        break;
                                    case MessageType.VOLTAGELOW:
                                        Log.D("System voltage low.");
                                        break;
                                    case MessageType.TIME:
                                        Log.D("Time value received.");
                                        break;
                                    case MessageType.STATUS:
                                        Log.D("Status message received.");
                                        break;
                                    case MessageType.ERROR:
                                        Log.D("Error from timing system.");
                                        break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        Log.D("Socket errored on us.");
                        TimingSystem disconnected = TimingSystemDict[sock];
                        TimingSystemSockets.Remove(sock);
                        TimingSystemDict.Remove(sock);
                        mainWindow.TimingSystemDisconnected(disconnected);
                    }
                }
                // Check Sockets we've started to connect to and verify that they've successfully connected.
                List<Socket> toRemove = new List<Socket>();
                foreach (Socket sock in TimingSystemSockets)
                {
                    TimingSystem sys = TimingSystemDict[sock];
                    if (sys != null)
                    {
                        if (sys.Status != SYSTEM_STATUS.CONNECTED && sys.TimedOut()) // Not connected & Timed out.
                        {
                            sys.Status = SYSTEM_STATUS.DISCONNECTED;
                            mainWindow.UpdateTimingWindow();
                            TimingSystemDict.Remove(sock);
                            toRemove.Add(sock);
                        }
                    }
                    else // Socket not found in dictionary.
                    {
                        toRemove.Add(sock);
                    }
                }
                TimingSystemSockets.RemoveAll(i => toRemove.Contains(i));
            }
            if (mut.WaitOne(6000))
            {
                this.Running = false;
                mut.ReleaseMutex();
            }
        }
    }
}
