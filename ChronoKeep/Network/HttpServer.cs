using ChronoKeep.IO.HtmlTemplates;
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

namespace ChronoKeep.Network
{
    class HttpServer
    {
        private Thread _serverThread;
        private HttpListener _listener;
        private int _port;
        private IDBInterface _database;

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
            Log.D(filename + " requested.");
            filename = filename.Substring(1);

            string message = "";
            if (string.IsNullOrEmpty(filename) || filename.Equals("results.htm", StringComparison.OrdinalIgnoreCase) || filename.Equals("results.html", StringComparison.OrdinalIgnoreCase))
            {
                // Serve up HtmlResultsTemplace
                HtmlResultsTemplate results = new HtmlResultsTemplate(_database);
                message = results.TransformText();
                context.Response.ContentType = "text/html";
                Log.D("Results html");
            }
            else if (filename.Equals("style.min.css", StringComparison.OrdinalIgnoreCase) || filename.Equals("style.css", StringComparison.OrdinalIgnoreCase))
            {
                // Serve up style.css
                message = "";
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ChronoKeep.IO.HtmlTemplates." + "style.min.css"))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        message = reader.ReadToEnd();
                    }
                }
                context.Response.ContentType = "text/css";
                Log.D("Style css");
            }
            else if (filename.Equals("bootstrap.min.css", StringComparison.OrdinalIgnoreCase) || filename.Equals("bootstrap.css", StringComparison.OrdinalIgnoreCase))
            {
                // Serve up bootstrap.min.css
                message = "";
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ChronoKeep.IO.HtmlTemplates." + "bootstrap.min.css"))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        message = reader.ReadToEnd();
                    }
                }
                context.Response.ContentType = "text/css";
                Log.D("Bootstrap css");
            }
            else if (filename.Equals("bootstrap.min.js", StringComparison.OrdinalIgnoreCase) || filename.Equals("bootstrap.js", StringComparison.OrdinalIgnoreCase))
            {
                // Serve up bootstrap.min.js
                message = "";
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ChronoKeep.IO.HtmlTemplates." + "bootstrap.min.js"))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        message = reader.ReadToEnd();
                    }
                }
                context.Response.ContentType = "text/javascript";
                Log.D("Bootstrap js");
            }
            else if (filename.Equals("jquery.min.js", StringComparison.OrdinalIgnoreCase) || filename.Equals("jquery.js", StringComparison.OrdinalIgnoreCase))
            {
                // Serve up jquery-3.4.1.min.js
                message = "";
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ChronoKeep.IO.HtmlTemplates." + "jquery-3.4.1.min.js"))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        message = reader.ReadToEnd();
                    }
                }
                context.Response.ContentType = "text/javascript";
                Log.D("jquery js");
            }
            context.Response.ContentLength64 = message.Length;
            context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
            context.Response.AddHeader("Last-Modified", DateTime.Now.ToString("r"));

            byte[] messageBytes = Encoding.Default.GetBytes(message);
            context.Response.OutputStream.Write(messageBytes, 0, messageBytes.Length);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.OutputStream.Flush();
        }

        private void Initialize(IDBInterface database, int port)
        {
            this._database = database;
            this._port = port;

            // Test to ensure we can listen.
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
            _listener.Start();

            _serverThread = new Thread(this.Listen);
            _serverThread.Start();
        }
    }
}
