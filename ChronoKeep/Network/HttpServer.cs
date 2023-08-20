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

        private Mutex info_mutex = new Mutex();

        private bool keepAlive = true;

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
            finishResults.Clear();
            Dictionary<int, TimeResult> lastResult = new Dictionary<int, TimeResult>();
            foreach (TimeResult r in database.GetTimingResults(theEvent.Identifier))
            {
                if (!lastResult.ContainsKey(r.Bib))
                {
                    lastResult[r.Bib] = r;
                }
                else if (lastResult[r.Bib].SystemTime.CompareTo(r.SystemTime) < 0)
                {
                    lastResult[r.Bib] = r;
                }
            }
            finishResults.AddRange(lastResult.Values);
            finishResults.RemoveAll(r => r.Bib < 0 || string.IsNullOrEmpty(r.First) || string.IsNullOrEmpty(r.Last));
            info_mutex.ReleaseMutex();
        }

        public void Stop()
        {
            keepAlive = false;
            _listener.Stop();
        }

        private void Listen()
        {
            while (keepAlive)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    Process(context);
                }
                catch (Exception ex)
                {
                    Log.E("Network.HttpServer", "Exception caught trying to serve something.\n" + ex.Message);
                }
            }
        }

        private void Process(HttpListenerContext context)
        {
            string filename = context.Request.Url.AbsolutePath;
            Log.D("Network.HttpServer", "'" + filename + "' requested.");
            filename = filename.Substring(1);

            byte[] message = Encoding.Default.GetBytes("");
            bool answer = false;
            if (string.IsNullOrEmpty(filename) || filename.Equals("results.htm", StringComparison.OrdinalIgnoreCase) || filename.Equals("results.html", StringComparison.OrdinalIgnoreCase))
            {
                answer = true;
                // Serve up HtmlResultsTemplace
                if (!info_mutex.WaitOne(3000))
                {
                    Log.D("Network.HttpServer", "Unable to get mutex for outputting results page.");
                    message = Encoding.Default.GetBytes("");
                }
                else
                {
                    HtmlResultsTemplate results = new HtmlResultsTemplate(
                        theEvent,
                        finishResults
                        );
                    message = Encoding.Default.GetBytes(results.TransformText());
                    context.Response.ContentType = "text/html";
                    Log.D("Network.HttpServer", "Results html");
                    info_mutex.ReleaseMutex();
                }
            }
            else if (filename.StartsWith("css/", StringComparison.OrdinalIgnoreCase) || filename.StartsWith("js/", StringComparison.OrdinalIgnoreCase))
            {
                Log.D("Network.HttpServer", "Fetching " + filename);
                answer = true;
                // Serve up the file requested.
                string newName = filename.Replace('/', '.');
                Log.D("Network.HttpServer", "Newname is " + newName);
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Chronokeep.IO.HtmlTemplates." + newName))
                {
                    message = new byte[stream.Length];
                    stream.Read(message, 0, message.Length);
                }
                if (filename.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.ContentType = "text/css";
                }
                else if (filename.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.ContentType= "text/javascript";
                }
                else if (filename.EndsWith(".html", StringComparison.OrdinalIgnoreCase) || filename.EndsWith(".html"))
                {
                    context.Response.ContentType = "text/html";
                }
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
            try
            {
                context.Response.OutputStream.Write(message, 0, message.Length);
                context.Response.OutputStream.Flush();
            }
            catch (Exception ex)
            {
                Log.E("Network.HttpServer", "Error attempting to write response.\n" + ex.Message);
            }
        }

        private void Initialize(IDBInterface database, int port)
        {
            this.database = database;
            this._port = port;
            keepAlive = true;
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
