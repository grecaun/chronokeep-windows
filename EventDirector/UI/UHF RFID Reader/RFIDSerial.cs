using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventDirector
{
    class RFIDSerial
    {
        private String ComPort = "N/A";
        private int BaudRate = 0;
        private SerialPort Port;

        List<Info> ChipReads = new List<Info>();

        public RFIDSerial(string ComPort, int BaudRate)
        {
            this.ComPort = ComPort;
            this.BaudRate = BaudRate;
        }

        public Error DeviceInit(String ComPort, int BaudRate)
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
                Port = new SerialPort(ComPort, BaudRate)
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
                byte[] OutMsg = new byte[5] { 0xA0, 0x03, 0x50, 0x00, 0x0D };
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

        public void DeviceDeinit()
        {
        }

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
                    return new Info
                    {
                        ErrorCode = Error.NODATA
                    };
                }
                return new Info(InMsg);
            }
            catch
            {
                return new Info
                {
                    ErrorCode = Error.CONERROR
                };
            }
        }

        /*/
        public List<RFIDInfo> ReadMultiple()
        {
            ChipReads.Clear();
            byte[] OutMsg = { 0xA0, 0x03, 0x82, 0x00, 0xDB };
            try
            {
                Console.WriteLine("About to send data to reader.");
                Port.BaseStream.Write(OutMsg, 0, 5);
                int tries = 5;
                for (int i=0; i < tries; i++)
                {
                    int BytesToRead = Port.BytesToRead;
                    if (BytesToRead > 0)
                    {
                        int recvd = Port.BaseStream.Read(buffer, bufferSz, 256);
                        Console.WriteLine("Received data from reader. Length is " + recvd + ".");
                        bufferSz += recvd;
                        break;
                    }
                }
            } catch (IOException exc)
            {
                Console.WriteLine(exc.StackTrace);
            }
            while (bufferSz > 0)
            {
                if (buffer[0] == 0xE4 || buffer[0] == 0xE0)
                {
                    int len = buffer[1] + 2;
                    if (len <= bufferSz)
                    {
                        ProcessMessage(buffer, len);
                        for (int i = 0; i < bufferSz - len; i++)
                        {
                            buffer[i] = buffer[i + len];
                        }
                        bufferSz -= len;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    int count = 0;
                    while (bufferSz > count && buffer[count] != 0xE4 && buffer[count] != 0xE0)
                    {
                        count++;
                    }
                    if (bufferSz <= count)
                    {
                        bufferSz = 0;
                    }
                    else
                    {
                        for (int i=0; i < bufferSz - count; i++)
                        {
                            buffer[i] = buffer[i + count];
                        }
                        bufferSz -= count;
                    }
                }
            }
            return ChipReads;
        }

        public void ProcessMessage(byte[] data, int length)
        {
            if (data[0] != 0xE4 && data[0] != 0xE0)
            {
                Console.Write("Hmm, something went wrong and we're processing a message we shouldn't. ");
            }
            if (data[1] + 2 != length)
            {
                Console.Write("Hmm, the length doesn't match... ");
            }
            if (data[length-1] != CheckSum(data, length-1))
            {
                Console.WriteLine("Hmm, the checksum is wrong. Value is " + data[length-1] + " and it should be " + CheckSum(data, length-1));
            }
            switch (data[0])
            {
                case 0xE4:
                    Console.WriteLine("Data is " + BitConverter.ToString(data, 0, data[1]));
                    break;
                case 0xE0:
                    switch (data[2])
                    {
                        case 0x82:
                            Console.WriteLine(string.Format("Device Number {0} Antenna Number {1} Hex Chip {2:X2} {3:X2} {4:X2} {5:X2} {6:X2} {7:X2} {8:X2} {9:X2} {10:X2} {11:X2} {12:X2} {13:X2} ", data[3], data[4], data[5], data[6], data[7], data[8], data[9], data[10], data[11], data[12], data[13], data[14], data[15], data[16]));
                            ChipReads.Add(new RFIDInfo(data));
                            break;
                        case 0x80:
                            Console.WriteLine("Data is " + BitConverter.ToString(data, 0, data[1]));
                            break;
                        default:
                            Console.WriteLine("0xE0 with command of " + data[2]);
                            break;
                    }
                    break;
                default:
                    Console.WriteLine("Processing data with command code of " + data[0]);
                    break;
            }
        }
        //*/

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
                Data = new byte[1] { 0x00 };
            }

            public Info(byte[] inData)
            {
                ErrorCode = Error.NOERR;
                Data = new byte[inData[1] + 2];
                for (int i=0; i < this.Data.Length; i++)
                {
                    Data[i] = inData[i];
                }
                if (Data[Data.Length - 1] != CheckSum(Data, Data.Length-1))
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
