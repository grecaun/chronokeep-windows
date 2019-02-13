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
        List<Socket> TimingSystemSockets = new List<Socket>(), readList = new List<Socket>();
        Dictionary<Socket, TimingSystem> TimingSystemDict = new Dictionary<Socket, TimingSystem>();

        private static Mutex mut = new Mutex();
        private static bool Running = false;

        IDBInterface database;
        IMainWindow mainWindow;

        public TimingController(IMainWindow mainWindow, IDBInterface database)
        {
            this.database = database;
            this.mainWindow = mainWindow;
        }

        public static bool IsRunning()
        {
            bool output = false;
            if (mut.WaitOne(6000))
            {
                output = Running;
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
            system.CreateTimingSystemInterface(database, sock);
            TimingSystemDict[sock] = system;
            if (sock.Connected)
            {
                Log.D("Connected to " + system.IPAddress);
                TimingSystemSockets.Add(sock);
                TimingSystemDict[sock].SetLastCommunicationTime();
            }
            else
            {
                Log.D("Unable to connect to " + system.IPAddress);
                TimingSystemDict.Remove(sock);
                system.Status = SYSTEM_STATUS.DISCONNECTED;
            }
        }

        public void Shutdown()
        {
            foreach (Socket sock in TimingSystemSockets)
            {
                sock.Close();
                TimingSystemDict.Remove(sock);
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
            if (mut.WaitOne(3000))
            {
                if (Running == true)
                {
                    Log.D("Timing Controller Thread already running.");
                    mut.ReleaseMutex();
                    return;
                }
                Running = true;
                mut.ReleaseMutex();
            }
            else
            {
                Log.D("Unable to aquire mutex.");
                return;
            }
            bool UpdateTiming = false;
            while (TimingSystemSockets.Count > 0)
            {
                readList.Clear();
                readList.AddRange(TimingSystemSockets);
                Socket.Select(readList, null, null, 3000000);
                foreach (Socket sock in readList)
                {
                    byte[] recvd = new byte[4112];
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
                            Log.D("Timing System - Message is :" + msg.Trim());
                            Dictionary<MessageType, List<string>> messageTypes = TimingSystemDict[sock].SystemInterface.ParseMessages(msg);
                            foreach (MessageType type in messageTypes.Keys)
                            {
                                switch (type)
                                {
                                    case MessageType.CONNECTED:
                                        Log.D("Timing system successfully connected.");
                                        TimingSystemDict[sock].Status = SYSTEM_STATUS.CONNECTED;
                                        UpdateTiming = true;
                                        break;
                                    case MessageType.CHIPREAD:
                                        Log.D("Chipreads found");
                                        UpdateTiming = true;
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
                                        TimingSystemDict[sock].SystemTime = messageTypes[MessageType.TIME].First<string>();
                                        UpdateTiming = true;
                                        break;
                                    case MessageType.STATUS:
                                        Log.D("Status message received.");
                                        TimingSystemDict[sock].SystemStatus = messageTypes[MessageType.STATUS].Last<string>();
                                        UpdateTiming = true;
                                        break;
                                    case MessageType.ERROR:
                                        Log.D("Error from timing system.");
                                        break;
                                }
                            }
                        }
                        if (UpdateTiming && !sock.Poll(100, SelectMode.SelectRead))
                        {
                            mainWindow.UpdateTimingFromController();
                        }
                    }
                    catch
                    {
                        if (TimingSystemDict.ContainsKey(sock))
                        {
                            Log.D("Socket errored on us.");
                            TimingSystemSockets.Remove(sock);
                            TimingSystem disconnected = TimingSystemDict[sock];
                            TimingSystemDict.Remove(sock);
                            mainWindow.TimingSystemDisconnected(disconnected);
                        } else
                        {
                            Log.D("Successful disconnect.");
                        }
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
                            mainWindow.UpdateTimingFromController();
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
                Running = false;
                mut.ReleaseMutex();
            }
        }
    }
}
