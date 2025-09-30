using Chronokeep.Interfaces;
using Chronokeep.Interfaces.Timing;
using Chronokeep.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Chronokeep.Timing
{
    class TimingController(IMainWindow mainWindow, IDBInterface database)
    {
        readonly List<Socket> TimingSystemSockets = [], readList = [];
        readonly Dictionary<Socket, TimingSystem> TimingSystemDict = [];

        private static readonly Lock timingLock = new();
        private static readonly Lock readsLock = new();
        private static bool Running = false;
        private static bool NewReads = false;

        public static bool IsRunning()
        {
            bool output = false;
            Log.D("Timing.TimingController", "Lock Wait 01");
            if (timingLock.TryEnter(6000))
            {
                try
                {
                    output = Running;
                }
                finally
                {
                    timingLock.Exit();
                }
            }
            return output;
        }

        public static bool NewReadsExist()
        {
            bool output = false;
            Log.D("Timing.TimingController", "Lock Wait 02");
            if (readsLock.TryEnter(3000))
            {
                try
                {
                    output = NewReads;
                    NewReads = false;
                }
                finally
                {
                    readsLock.Exit();
                }
            }
            return output;
        }

        public List<TimingSystem> GetConnectedSystems()
        {
            List<TimingSystem> output = [.. TimingSystemDict.Values];
            return output;
        }

        public void ConnectTimingSystem(TimingSystem system)
        {
            Log.D("Timing.TimingController", "-- UPDATE -- Creating interface for communication with timing system.");
            system.CreateTimingSystemInterface(database, mainWindow);
            List<Socket> sockets = system.Connect();
            if (sockets == null || sockets.Count < 1)
            {
                Log.D("Timing.TimingController", "No sockets returned.");
                system.Status = SYSTEM_STATUS.DISCONNECTED;
            }
            else
            {
                int i = 1;
                foreach (Socket sock in sockets)
                {
                    Log.D("Timing.TimingController", "Socket " + i++);
                    TimingSystemDict[sock] = system;
                    if (sock.Connected)
                    {
                        Log.D("Timing.TimingController", "Connected to " + system.IPAddress);
                        TimingSystemSockets.Add(sock);
                        TimingSystemDict[sock].SetLastCommunicationTime();
                        TimingSystemDict[sock].Status = SYSTEM_STATUS.CONNECTED;
                    }
                    else
                    {
                        TimingSystemDict.Remove(sock);
                        if (!TimingSystemDict.Values.Contains(system))
                        {
                            system.Status = SYSTEM_STATUS.DISCONNECTED;
                        }
                    }
                }
            }
        }

        public void Shutdown()
        {
            foreach (Socket sock in TimingSystemSockets)
            {
                sock.Close();
            }
        }

        public void DisconnectTimingSystem(TimingSystem system)
        {
            system.Disconnect();
            foreach (Socket sock in system.Sockets)
            {
                TimingSystemSockets.Remove(sock);
                TimingSystemDict.Remove(sock);
            }
            system.Status = SYSTEM_STATUS.DISCONNECTED;
        }

        public void Run()
        {
            Log.D("Timing.TimingController", "Timing Controller is now running.");
            if (timingLock.TryEnter(3000))
            {
                try
                {
                    if (Running == true)
                    {
                        Log.D("Timing.TimingController", "Timing Controller Thread already running.");
                        return;
                    }
                    Running = true;
                }
                finally
                {
                    timingLock.Exit();
                }
            }
            else
            {
                Log.D("Timing.TimingController", "Unable to acquire lock.");
                return;
            }
            bool UpdateTiming = false;
            bool ChipRead = false;
            while (TimingSystemSockets.Count > 0)
            {
                Log.D("Timing.TimingController", "Loop start.");
                readList.Clear();
                readList.AddRange(TimingSystemSockets);
                try
                {
                    Socket.Select(readList, null, null, 3000000);
                }
                catch { }
                foreach (Socket sock in readList)
                {
                    Log.D("Timing.TimingController", "Reading from socket.");
                    ChipRead = false;
                    UpdateTiming = false;
                    byte[] recvd = new byte[4112];
                    try
                    {
                        int num_recvd = sock.Receive(recvd);
                        if (num_recvd == 0)
                        {
                            Log.D("Timing.TimingController", "No longer connected to Timing System");
                            TimingSystem disconnected = TimingSystemDict[sock];
                            TimingSystemSockets.Remove(sock);
                            TimingSystemDict.Remove(sock);
                            mainWindow.TimingSystemDisconnected(disconnected);
                        }
                        else
                        {
                            string msg = Encoding.UTF8.GetString(recvd, 0, num_recvd);
                            Log.D("Timing.TimingController", "Timing System - Message is :" + msg.Trim());
                            Dictionary<MessageType, List<string>> messageTypes = TimingSystemDict[sock].SystemInterface.ParseMessages(msg, sock);
                            foreach (MessageType type in messageTypes.Keys)
                            {
                                switch (type)
                                {
                                    case MessageType.CONNECTED:
                                        Log.D("Timing.TimingController", "Timing system successfully connected.");
                                        TimingSystemDict[sock].Status = SYSTEM_STATUS.CONNECTED;
                                        UpdateTiming = true;
                                        break;
                                    case MessageType.CHIPREAD:
                                        Log.D("Timing.TimingController", "Chipreads found");
                                        ChipRead = true;
                                        break;
                                    case MessageType.SETTINGCHANGE:
                                        Log.D("Timing.TimingController", "Setting value changed.");
                                        break;
                                    case MessageType.SETTINGVALUE:
                                        Log.D("Timing.TimingController", "Setting value given.");
                                        break;
                                    case MessageType.VOLTAGENORMAL:
                                        Log.D("Timing.TimingController", "System voltage normal.");
                                        break;
                                    case MessageType.VOLTAGELOW:
                                        Log.D("Timing.TimingController", "System voltage low.");
                                        break;
                                    case MessageType.TIME:
                                        Log.D("Timing.TimingController", "Time value received.");
                                        TimingSystemDict[sock].SystemTime = messageTypes[MessageType.TIME].First<string>();
                                        UpdateTiming = true;
                                        break;
                                    case MessageType.STATUS:
                                        Log.D("Timing.TimingController", "Status message received.");
                                        TimingSystemDict[sock].SystemStatus = messageTypes[MessageType.STATUS].Last<string>();
                                        UpdateTiming = true;
                                        break;
                                    case MessageType.ERROR:
                                        Log.D("Timing.TimingController", "Error from timing system.");
                                        break;
                                }
                            }
                        }
                        if (UpdateTiming && !sock.Poll(100, SelectMode.SelectRead))
                        {
                            UpdateTiming = false;
                            mainWindow.UpdateTimingFromController();
                        }
                        if (ChipRead)
                        {
                            Log.D("Timing.TimingController", "Lock Wait 05");
                            if (readsLock.TryEnter(3000))
                            {
                                try
                                {
                                    NewReads = true;
                                    mainWindow.NotifyTimingWorker();
                                }
                                finally
                                {
                                    readsLock.Exit();
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.E("Timing.TimingController", "Error trying to parse messages. " + e.Message);
                        if (TimingSystemDict.TryGetValue(sock, out TimingSystem system))
                        {
                            Log.D("Timing.TimingController", "Socket errored on us.");
                            try
                            {
                                system.SystemInterface.CloseSettings();
                            }
                            catch (Exception ex)
                            {
                                Log.E("Timing.TimingController", "Error attempting to close settings. " + ex.Message);
                            }
                            TimingSystemSockets.Remove(sock);
                            TimingSystemDict.Remove(sock);
                            mainWindow.TimingSystemDisconnected(system);
                        } else
                        {
                            Log.D("Timing.TimingController", "Successful disconnect.");
                        }
                    }
                }
                // Check Sockets we've started to connect to and verify that they've successfully connected.
                List<Socket> toRemove = [];
                try
                {
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
                }
                catch (Exception e)
                {
                    Log.E("Timing.TimingController", "Something went wrong trying to remove a socket. " + e.Message);
                }
                TimingSystemSockets.RemoveAll(i => toRemove.Contains(i));
                Log.D("Timing.TimingController", "Loop end.");
            }
            Log.D("Timing.TimingController", "Lock Wait 06");
            if (timingLock.TryEnter(6000))
            {
                try
                {
                    Running = false;
                }
                finally
                {
                    timingLock.Exit();
                }
            }
        }
    }
}
