using System;
using System.Threading;

namespace Chronokeep
{
    class NewReader
    {
        int Delay = 500;
        bool KeepAlive = false;
        int counter = 1;
        RFIDSerial serial = null;

        private int v;
        private ChipReaderWindow chipReaderWindow;

        public NewReader(int v, ChipReaderWindow chipReaderWindow)
        {
            this.v = v;
            this.chipReaderWindow = chipReaderWindow;
        }

        public void SetSerial(RFIDSerial serial)
        {
            this.serial = serial;
        }

        public void Run()
        {
            KeepAlive = serial != null;
            while (KeepAlive)
            {
                System.Console.WriteLine("Active - Loop Number " + counter++);
                Console.Write("Hello? Is anyone there?");
                RFIDSerial.Info read = serial.ReadData();
                if (read.ErrorCode == RFIDSerial.Error.NOERR)
                {
                    Console.WriteLine(" Ahhh! It's a monster!");
                    chipReaderWindow.AddRFIDItem(read);
                }
                else
                {
                    Console.WriteLine(" Hmm. Must've been my imagination.");
                }
                Thread.Sleep(Delay);
            }
            System.Console.WriteLine("InActive - Finished after " + counter + " loops.");
        }

        public void Kill()
        {
            System.Console.WriteLine("Kill command received.");
            KeepAlive = false;
        }

        public void Stop()
        {
            Kill();
        }
    }
}
