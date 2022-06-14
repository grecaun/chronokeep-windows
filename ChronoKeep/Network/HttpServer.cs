using Chronokeep.IO.HtmlTemplates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chronokeep.Network
{
    class HttpServer
    {
        private Thread _serverThread;
        private HttpListener _listener;
        private int _port;
        private IDBInterface database;
        private Event theEvent;
        private List<TimeResult> finishResults = new List<TimeResult>();
        private Dictionary<int, Participant> participantDictionary = new Dictionary<int, Participant>();

        // Time based results page specific items
        private Dictionary<string, int> maxLoops = new Dictionary<string, int>();
        private Dictionary<(int, int), TimeResult> LoopResults = new Dictionary<(int, int), TimeResult>();
        private Dictionary<int, int> RunnerLoopsCompleted = new Dictionary<int, int>();
        private Dictionary<string, double> DivisionDistancePerLoop = new Dictionary<string, double>();
        private Dictionary<string, string> DivisionDistanceType = new Dictionary<string, string>();

        private Mutex info_mutex = new Mutex();

        public int Port
        {
            get { return _port; }
            private set { }
        }

        public HttpServer(IDBInterface database, int port)
        {
            this.Initialize(database, port);
        }

        public HttpServer(IDBInterface database)
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            this.Initialize(database, port);
        }

        public void UpdateInformation()
        {
            Log.D("Network.HttpServer", "Updating information for HttpServer.");
            if (!info_mutex.WaitOne(3000))
            {
                Log.D("Network.HttpServer", "Unable to get mutex.");
                return;
            }
            theEvent = database.GetCurrentEvent();
            finishResults = database.GetFinishTimes(theEvent.Identifier);
            foreach (Participant person in database.GetParticipants(theEvent.Identifier))
            {
                participantDictionary[person.EventSpecific.Identifier] = person;
            }
            if (theEvent.EventType == Constants.Timing.EVENT_TYPE_TIME)
            {
                // Figure out all the information required for time based events.
                maxLoops.Clear();
                LoopResults.Clear();
                RunnerLoopsCompleted.Clear();
                foreach (TimeResult result in finishResults)
                {
                    if (!maxLoops.ContainsKey(result.DistanceName)) {
                        maxLoops[result.DistanceName] = result.Occurrence;
                    }
                    maxLoops[result.DistanceName] = result.Occurrence > maxLoops[result.DistanceName] ? result.Occurrence : maxLoops[result.DistanceName];
                    LoopResults[(result.EventSpecificId, result.Occurrence)] = result;
                    if (!RunnerLoopsCompleted.ContainsKey(result.EventSpecificId))
                    {
                        RunnerLoopsCompleted[result.EventSpecificId] = result.Occurrence;
                    }
                    RunnerLoopsCompleted[result.EventSpecificId] =
                        RunnerLoopsCompleted[result.EventSpecificId] > result.Occurrence ?
                            RunnerLoopsCompleted[result.EventSpecificId] :
                            result.Occurrence;
                }
                List<Distance> divs = database.GetDistances(theEvent.Identifier);
                foreach (Distance d in divs)
                {
                    DivisionDistancePerLoop[d.Name] = d.DistanceValue;
                    DivisionDistanceType[d.Name] = d.DistanceUnit == Constants.Distances.MILES ? "Miles" :
                        d.DistanceUnit == Constants.Distances.FEET ? "Feet" :
                        d.DistanceUnit == Constants.Distances.KILOMETERS ? "Kilometers" :
                        d.DistanceUnit == Constants.Distances.METERS ? "Meters" :
                        d.DistanceUnit == Constants.Distances.YARDS ? "Yards" :
                        "Unknown";
                }
            }
            info_mutex.ReleaseMutex();
        }

        public void Stop()
        {
            _serverThread.Abort();
            _listener.Stop();
        }

        private void Listen()
        {
            while (true)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    Process(context);
                }
                catch { }
            }
        }

        private void Process(HttpListenerContext context)
        {
            string filename = context.Request.Url.AbsolutePath;
            Log.D("Network.HttpServer", filename + " requested.");
            filename = filename.Substring(1);

            string message = "";
            bool answer = false;
            if (string.IsNullOrEmpty(filename) || filename.Equals("results.htm", StringComparison.OrdinalIgnoreCase) || filename.Equals("results.html", StringComparison.OrdinalIgnoreCase))
            {
                answer = true;
                // Serve up HtmlResultsTemplace
                if (!info_mutex.WaitOne(3000))
                {
                    Log.D("Network.HttpServer", "Unable to get mutex for outputting results page.");
                    message = "";
                }
                else
                {
                    if (theEvent.EventType == Constants.Timing.EVENT_TYPE_DISTANCE)
                    {
                        HtmlResultsTemplate results = new HtmlResultsTemplate(theEvent, finishResults, participantDictionary);
                        message = results.TransformText();
                        context.Response.ContentType = "text/html";
                        Log.D("Network.HttpServer", "Results html");
                    }
                    else
                    {
                        HtmlResultsTemplateTime results = new HtmlResultsTemplateTime(theEvent, finishResults, participantDictionary,
                            maxLoops, LoopResults, RunnerLoopsCompleted, DivisionDistancePerLoop, DivisionDistanceType);
                        message = results.TransformText();
                        context.Response.ContentType = "text/html";
                        Log.D("Network.HttpServer", "Results html");
                    }
                    info_mutex.ReleaseMutex();
                }
            }
            else if (filename.Equals("style.min.css", StringComparison.OrdinalIgnoreCase) || filename.Equals("style.css", StringComparison.OrdinalIgnoreCase))
            {
                answer = true;
                // Serve up style.css
                message = "";
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Chronokeep.IO.HtmlTemplates." + "style.min.css"))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        message = reader.ReadToEnd();
                    }
                }
                context.Response.ContentType = "text/css";
                Log.D("Network.HttpServer", "Style css");
            }
            else if (filename.Equals("bootstrap.min.css", StringComparison.OrdinalIgnoreCase) || filename.Equals("bootstrap.css", StringComparison.OrdinalIgnoreCase))
            {
                answer = true;
                // Serve up bootstrap.min.css
                message = "";
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Chronokeep.IO.HtmlTemplates." + "bootstrap.min.css"))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        message = reader.ReadToEnd();
                    }
                }
                context.Response.ContentType = "text/css";
                Log.D("Network.HttpServer", "Bootstrap css");
            }
            else if (filename.Equals("bootstrap.min.js", StringComparison.OrdinalIgnoreCase) || filename.Equals("bootstrap.js", StringComparison.OrdinalIgnoreCase))
            {
                answer = true;
                // Serve up bootstrap.min.js
                message = "";
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Chronokeep.IO.HtmlTemplates." + "bootstrap.min.js"))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        message = reader.ReadToEnd();
                    }
                }
                context.Response.ContentType = "text/javascript";
                Log.D("Network.HttpServer", "Bootstrap js");
            }
            else if (filename.Equals("jquery.min.js", StringComparison.OrdinalIgnoreCase) || filename.Equals("jquery.js", StringComparison.OrdinalIgnoreCase))
            {
                answer = true;
                // Serve up jquery-3.4.1.min.js
                message = "";
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Chronokeep.IO.HtmlTemplates." + "jquery-3.4.1.min.js"))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        message = reader.ReadToEnd();
                    }
                }
                context.Response.ContentType = "text/javascript";
                Log.D("Network.HttpServer", "jquery js");
            }
            if (answer)
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            context.Response.ContentLength64 = message.Length;
            context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
            context.Response.AddHeader("Last-Modified", DateTime.Now.ToString("r"));
            byte[] messageBytes = Encoding.Default.GetBytes(message);
            context.Response.OutputStream.Write(messageBytes, 0, messageBytes.Length);
            context.Response.OutputStream.Flush();
        }

        private void Initialize(IDBInterface database, int port)
        {
            this.database = database;
            this._port = port;
            UpdateInformation();

            // Test to ensure we can listen.
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
            _listener.Start();

            _serverThread = new Thread(this.Listen);
            _serverThread.Start();
        }
    }
}
