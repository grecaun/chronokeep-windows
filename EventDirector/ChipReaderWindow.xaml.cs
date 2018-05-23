using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EventDirector
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
        private static ChipPersonWindow personWindow;
        private IDBInterface database;
        private MainWindow mainWindow;
        int eventId = -1;

        public ChipReaderWindow(IDBInterface database, MainWindow mWindow)
        {
            InitializeComponent();
            InstantiateSerialPortList();
            reader = new NewReader(600, this);
            this.database = database;
            this.mainWindow = mWindow;
            List<Event> events = database.GetEvents();
            eventCB.Items.Clear();
            foreach (Event e in events)
            {
                eventCB.Items.Add(new ComboBoxItem()
                {
                    Content = e.Name,
                    Uid = e.Identifier.ToString()
                });
            }
            eventCB.SelectedIndex = 0;
        }

        internal void PersonWindowClosing()
        {
            personWindow = null;
            try
            {
                KillReader();
            }
            catch
            {
                Log.E("Already dead.");
            }
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

        private void refreshBtn_Click(object sender, RoutedEventArgs e)
        {
            InstantiateSerialPortList();
        }

        private void connectBtn_Click(object sender, RoutedEventArgs e)
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
                    MessageBox.Show("No serial port selected.");
                    return;
                }
                if (serial.Connect() != RFIDSerial.Error.NOERR)
                {
                    MessageBox.Show("Unable to connect to device.");
                    return;
                }
                connectBtn.Content = "Disconnect";
                eventId = Convert.ToInt32(((ComboBoxItem)eventCB.SelectedItem).Uid);
                Event thisEvent = database.GetEvent(eventId);
                chipNumbers.Items.Add(new RFIDSerial.Info { DecNumber = 0 });
                readingThread = new Thread(new ThreadStart(reader.Run));
                readingThread.Start();
                personWindow = new ChipPersonWindow(this, thisEvent.Date);
                personWindow.Show();
            }
            else
            {
                connectBtn.Content = "Connect";
                personWindow.Close();
            }
        }

        internal void KillReader()
        {
            serial.Disconnect();
            chipNumbers.Items.Add(new RFIDSerial.Info { DecNumber = -1 });
            reader.Kill();
            if (readingThread != null)
            {
                readingThread.Join();
            }
            readingThread = null;
        }

        internal void AddRFIDItem(RFIDSerial.Info read)
        {
            Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate ()
            {
                read.ReadNumber = ReadNo++;
                chipNumbers.Items.Add(read);
                Participant person = new Participant();
                person.EventSpecific.Chip = (int)read.DecNumber;
                Participant thisPerson = database.GetParticipant(eventId, person);
                personWindow.UpdateInfo(thisPerson);
            }));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                personWindow.Close();
            }
            catch
            {
                Log.E("Window not open.");
            }
            try
            {
                KillReader();
            }
            catch
            {
                Log.E("Things are already closed.");
            }
            mainWindow.WindowClosed(this);
        }
    }
}
