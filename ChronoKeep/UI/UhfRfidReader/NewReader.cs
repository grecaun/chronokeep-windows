using System;
using System.Threading;

namespace Chronokeep
{
    class NewReader(ChipReaderWindow chipReaderWindow)
    {
        private readonly int Delay = 500;
        private bool KeepAlive = false;
        private int counter = 1;
        private RFIDSerial serial = null;

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
