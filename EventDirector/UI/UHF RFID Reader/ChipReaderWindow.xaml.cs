using EventDirector.Interfaces;
using EventDirector.UI.EventWindows;
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
        private IMainWindow mainWindow = null;
        private IWindowCallback window = null;
        int eventId = -1;
        
        public ChipReaderWindow(IDBInterface database, IMainWindow mWindow)
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

        private ChipReaderWindow(IWindowCallback window, IDBInterface database)
        {
            InitializeComponent();
            InstantiateSerialPortList();
            reader = new NewReader(600, this);
            this.database = database;
            this.mainWindow = null;
            eventId = Convert.ToInt32(database.GetAppSetting(Constants.Settings.CURRENT_EVENT).value);
            EventPickerHolder.Visibility = Visibility.Hidden;
            EventNameHolder.Visibility = Visibility.Visible;
            eventName.Content = database.GetEvent(eventId).Name;
        }

        public static ChipReaderWindow NewWindow(IWindowCallback window, IDBInterface database)
        {
            if (StaticEvent.changeMainEventWindow != null || StaticEvent.chipReaderWindow != null)
            {
                return null;
            }
            ChipReaderWindow output = new ChipReaderWindow(window, database);
            StaticEvent.chipReaderWindow = output;
            return output;
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
                    MessageBox.Show("No serial port selected.");
                    return;
                }
                if (serial.Connect() != RFIDSerial.Error.NOERR)
                {
                    MessageBox.Show("Unable to connect to device.");
                    return;
                }
                connectBtn.Content = "Disconnect";
                if (eventId == -1)
                {
                    eventId = Convert.ToInt32(((ComboBoxItem)eventCB.SelectedItem).Uid);
                }
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
            if (mainWindow != null) mainWindow.WindowClosed(this);
            if (window != null) window.WindowFinalize(this);
            StaticEvent.chipReaderWindow = null;
        }
    }
}
