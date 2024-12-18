using Chronokeep.Interfaces;
using Chronokeep.Objects;
using Chronokeep.UI.UIObjects;
using System;
using System.IO.Ports;
using System.Threading;
using System.Windows;

namespace Chronokeep
{
    /// <summary>
    /// Interaction logic for ChipReaderWindow.xaml
    /// </summary>
    public partial class ChipReaderWindow : Window
    {
        public static byte deviceNo = 0;
        private static Thread readingThread;
        private static NewReader reader;
        private static int ReadNo = 1;
        RFIDSerial serial;
        private static ChipPersonWindow personWindow = null;
        private IDBInterface database;
        private IWindowCallback window = null;
        int eventId = -1;

        private ChipReaderWindow(IWindowCallback window, IDBInterface database)
        {
            InitializeComponent();
            InstantiateSerialPortList();
            reader = new NewReader(600, this);
            this.database = database;
            Event theEvent = database.GetCurrentEvent();
            if (theEvent == null)
            {
                throw new Exception("no event set");
            }
            eventId = theEvent.Identifier;
            EventNameHolder.Visibility = Visibility.Visible;
            eventName.Text = theEvent.Name;
        }

        public static ChipReaderWindow NewWindow(IWindowCallback window, IDBInterface database)
        {
            return new ChipReaderWindow(window, database);
        }

        internal void PersonWindowClosing()
        {
            personWindow = null;
            beautyBtn.Content = "Show Info Window";
        }

        public void InstantiateSerialPortList()
        {
            serialPortCB.Items.Clear();
            var Ports = SerialPort.GetPortNames();
            foreach (string port in Ports)
            {
                serialPortCB.Items.Add(port);
            }
            if (serialPortCB.Items.Count > 0)
            {
                serialPortCB.SelectedIndex = 0;
            }
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            InstantiateSerialPortList();
        }

        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (connectBtn.Content.Equals("Connect"))
            {
                if (serialPortCB.SelectedIndex >= 0)
                {
                    serial = new RFIDSerial(serialPortCB.Text, 9600);
                    reader.SetSerial(serial);
                }
                else
                {
                    DialogBox.Show("No serial port selected.");
                    return;
                }
                if (serial.Connect() != RFIDSerial.Error.NOERR)
                {
                    DialogBox.Show("Unable to connect to device.");
                    return;
                }
                connectBtn.Content = "Disconnect";
                beautyBtn.Visibility = Visibility.Visible;
                beautyBtn.Content = "Show Info Window";
                chipNumbers.Items.Add(new RFIDSerial.Info { DecNumber = 0 });
                readingThread = new Thread(new ThreadStart(reader.Run));
                readingThread.Start();
            }
            else
            {
                connectBtn.Content = "Connect";
                beautyBtn.Visibility = Visibility.Hidden;
                beautyBtn.Content = "Show Info Window";
                try
                {
                    KillReader();
                }
                catch
                {
                    DialogBox.Show("Something went wrong during disconnect.");
                }
                if (personWindow != null)
                {
                    personWindow.Close();
                }
            }
        }

        internal void KillReader()
        {
            serial?.Disconnect();
            chipNumbers.Items.Add(new RFIDSerial.Info { DecNumber = -1 });
            reader.Kill();
            if (readingThread != null)
            {
                readingThread.Join(TimeSpan.FromSeconds(1));
            }
            readingThread = null;
        }

        internal void AddRFIDItem(RFIDSerial.Info read)
        {
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                read.ReadNumber = ReadNo++;
                chipNumbers.Items.Add(read);
                if (personWindow != null)
                {
                    Participant person = new Participant();
                    if (database.GetAppSetting(Constants.Settings.DEFAULT_CHIP_TYPE).Value.Equals(Constants.Settings.CHIP_TYPE_DEC))
                    {
                        person.Chip = read.DecNumber.ToString();
                    }
                    else
                    {
                        person.Chip = read.HexNumber;
                    }
                    Participant thisPerson = database.GetParticipant(eventId, person);
                    personWindow.UpdateInfo(thisPerson);
                }
            }));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (personWindow != null)
                {
                    personWindow.Close();
                }
            }
            catch
            {
                Log.E("ChipReaderWindow", "Window not open.");
            }
            try
            {
                KillReader();
            }
            catch
            {
                Log.E("ChipReaderWindow", "Things are already closed.");
            }
            if (window != null) window.WindowFinalize(this);
        }

        private void beautyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (personWindow == null)
            {
                Event thisEvent = database.GetEvent(eventId);
                personWindow = new ChipPersonWindow(this, thisEvent.Date);
                personWindow.Show();
                beautyBtn.Content = "Close Info Window";
            }
            else
            {
                personWindow.Close();
                personWindow = null;
            }
        }
    }
}
