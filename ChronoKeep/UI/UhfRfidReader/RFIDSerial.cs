using System;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace Chronokeep
{
    class RFIDSerial(string ComPort, int BaudRate)
    {
        private string ComPort = ComPort;
        private int BaudRate = BaudRate;
        private SerialPort Port;

        public Error DeviceInit(string ComPort, int BaudRate)
        {
            this.ComPort = ComPort;
            this.BaudRate = BaudRate;
            return DeviceInit();
        }

        public Error DeviceInit()
        {
            if (ComPort == "N/A" || BaudRate == 0)
            {
                return Error.BADSETTINGS;
            }
            try
            {
                Port = new(ComPort, BaudRate)
                {
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };
            }
            catch (IOException Exc)
            {
                Console.WriteLine(Exc.StackTrace);
                return Error.UNABLETOCONNECT;
            }
            return Error.NOERR;
        }

        public Error DeviceConnect()
        {
            try
            {
                Port.Open();
                byte[] OutMsg = [0xA0, 0x03, 0x50, 0x00, 0x0D];
                byte[] InMsg = new byte[56];
                OutMsg[4] = CheckSum(OutMsg, 4);
                Port.BaseStream.Write(OutMsg, 0, 5);
                Thread.Sleep(20); // Need to give it time or we won't get the whole message.
                int recvd = Port.Read(InMsg, 0, 56);
                while (recvd < 6)
                {
                    Thread.Sleep(20);
                    recvd = Port.Read(InMsg, recvd, 56 - recvd);
                }
                if (InMsg[0] != 0xE4 || InMsg[InMsg[1] + 1] != CheckSum(InMsg, InMsg[1] + 1) || InMsg[1] < 0x04 || InMsg[4] != 0x00)
                {
                    return Error.UNABLETOCONNECT;
                }
            }
            catch
            {
                return Error.UNABLETOCONNECT;
            }
            return Error.NOERR;
        }

        public void DeviceDisconnect()
        {
            Port.Close();
        }

        public static void DeviceDeinit() { }

        public Error Connect()
        {
            Error err = DeviceInit();
            if (err != Error.NOERR)
            {
                return err;
            }
            return DeviceConnect();
        }

        public void Disconnect()
        {
            DeviceDisconnect();
            DeviceDeinit();
        }

        private static byte CheckSum (byte[] buffer, int buffLen)
        {
            byte sum = 0;
            for (int i=0; i < buffLen; i++)
            {
                sum += buffer[i];
            }
            int bit = ~sum;
            sum = (byte)bit;
            sum += 1;
            return sum;
        }

        public Info ReadData()
        {
            byte[] OutMsg = { 0xA0, 0x03, 0x82, 0x00, 0xDB };
            byte[] InMsg = new byte[256];
            try
            {
                Port.BaseStream.Write(OutMsg, 0, 5);
                Thread.Sleep(50); // Need to give it time or we won't get the whole message.
                int recvd = Port.Read(InMsg, 0, 256);
                int pos = 0;
                while (pos < recvd && InMsg[pos] != 0xE4 && InMsg[pos] != 0xE0)
                {
                    pos++;
                }
                if (pos > 0 && pos < 256)
                {
                    for (int i=0; i<256-pos; i++)
                    {
                        InMsg[i] = InMsg[i + pos];
                    }
                }
                else if (pos > 255)
                {
                    return new()
                    {
                        ErrorCode = Error.NODATA
                    };
                }
                return new(InMsg);
            }
            catch
            {
                return new()
                {
                    ErrorCode = Error.CONERROR
                };
            }
        }

        public class Info
        {
            public long DecNumber { get; set; }
            public int DeviceNumber { get; set; }
            public int AntennaNumber { get; set; }
            public string HexNumber { get; set; }
            public byte[] Data { get; set; }
            public string DataRep { get => BitConverter.ToString(Data); }
            public int ReadNumber { get; set; }
            public Error ErrorCode { get; set; }

            public Info(int DecChip, string HexChip, int DeviceNo, int AntennaNo, byte[] Data)
            {
                this.DecNumber = DecChip;
                this.HexNumber = HexChip;
                this.DeviceNumber = DeviceNo;
                this.AntennaNumber = AntennaNo;
                this.Data = Data;
                this.ErrorCode = Error.NOERR;
            }

            public Info()
            {
                Data = [0x00];
            }

            public Info(byte[] inData)
            {
                ErrorCode = Error.NOERR;
                Data = new byte[inData[1] + 2];
                for (int i=0; i < this.Data.Length; i++)
                {
                    Data[i] = inData[i];
                }
                if (Data[^1] != CheckSum(Data, Data.Length - 1))
                {
                    ErrorCode = Error.BADDATA;
                }
                if (Data.Length == 18)
                {
                    HexNumber = BitConverter.ToString(Data, 5, 12);
                    byte[] epc = new byte[8];
                    for (int i=0; i<8; i++)
                    {
                        epc[i] = inData[16 - i];
                    }
                    DecNumber = BitConverter.ToInt64(epc, 0);
                    DeviceNumber = inData[3];
                    AntennaNumber = inData[4];
                }
                else if (Data.Length == 6)
                {
                    ErrorCode = Error.NODATA;
                }
                else
                {
                    ErrorCode = Error.BADDATA;
                }
            }
        }

        public enum Error
        {
            UNABLETOCONNECT, NOERR, UNKNOWNERR, BADSETTINGS, NODATA, BADDATA, CONERROR
        };
    }
}
