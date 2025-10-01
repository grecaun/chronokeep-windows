using Chronokeep.Database;
using Chronokeep.Helpers;
using Chronokeep.IO.HtmlTemplates;
using Chronokeep.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Chronokeep.Network
{
    class HttpServer
    {
        private Thread _serverThread;
        private HttpListener _listener;
        private int _port;
        private IDBInterface database;
        private Event theEvent;
        private readonly List<TimeResult> finishResults = [];
        private readonly Dictionary<string, TimeResult> finishDictionary = [];
        private readonly Dictionary<string, List<TimeResult>> participantResults = [];

        private byte[] resultsCache = null;
        private readonly Dictionary<string, byte[]> participantCache = [];
        private readonly Dictionary<string, byte[]> emailCache = [];

        private readonly Dictionary<string, Participant> participantDictionary = [];
        private readonly HashSet<string> distanceNames = [];
        private readonly Dictionary<int, APIObject> apiDictionary = [];

        private readonly Lock infoLock = new();

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
            TcpListener l = new(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            this.Initialize(database, port);
        }

        public void UpdateInformation()
        {
            Log.D("Network.HttpServer", "Updating information for HttpServer.");
            if (!infoLock.TryEnter(3000))
            {
                Log.D("Network.HttpServer", "Unable to get lock.");
                return;
            }
            try
            {
                theEvent = database.GetCurrentEvent();
                finishResults.Clear();
                finishDictionary.Clear();
                participantResults.Clear();
                foreach (TimeResult r in database.GetTimingResults(theEvent.Identifier))
                {
                    if (!finishDictionary.TryGetValue(r.Bib, out TimeResult finRes))
                    {
                        finishDictionary[r.Bib] = r;
                    }
                    else if (finRes.SystemTime.CompareTo(r.SystemTime) < 0)
                    {
                        finishDictionary[r.Bib] = r;
                    }
                    if (!participantResults.TryGetValue(r.Bib, out List<TimeResult> pResList))
                    {
                        pResList = [];
                        participantResults[r.Bib] = pResList;
                    }

                    pResList.Add(r);
                }
                distanceNames.Clear();
                foreach (Distance d in database.GetDistances(theEvent.Identifier))
                {
                    if (d.LinkedDistance == Constants.Timing.DISTANCE_NO_LINKED_ID)
                    {
                        distanceNames.Add(d.Name);
                    }
                }
                finishResults.AddRange(finishDictionary.Values);
                finishResults.RemoveAll(r => string.IsNullOrEmpty(r.Bib));
                // clear response caches whenever we update information
                resultsCache = null;
                participantCache.Clear();
                participantDictionary.Clear();
                foreach (Participant p in database.GetParticipants(theEvent.Identifier))
                {
                    participantDictionary[p.Identifier.ToString()] = p;
                }
                apiDictionary.Clear();
                foreach (APIObject api in database.GetAllAPI())
                {
                    apiDictionary[api.Identifier] = api;
                }
            }
            finally
            {
                infoLock.Exit();
            }
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

            string partBib = "";
            if (filename.StartsWith("part/", StringComparison.OrdinalIgnoreCase))
            {
                filename = filename.Substring(5);
                partBib = filename;
            }
            string emailBib = "";
            if (filename.StartsWith("email/", StringComparison.OrdinalIgnoreCase))
            {
                filename = filename.Substring(6);
                emailBib = filename;
            }

            byte[] message = Encoding.Default.GetBytes("");
            bool answer = false;
            if (string.IsNullOrEmpty(filename) || filename.Equals("results.htm", StringComparison.OrdinalIgnoreCase) || filename.Equals("results.html", StringComparison.OrdinalIgnoreCase))
            {
                answer = true;
                // Serve up HtmlResultsTemplate
                if (!infoLock.TryEnter(3000))
                {
                    Log.D("Network.HttpServer", "Unable to get lock for outputting results page.");
                    message = Encoding.Default.GetBytes("");
                }
                else
                {
                    try
                    {
                        if (resultsCache == null)
                        {
                            HtmlResultsTemplate results = new(
                                theEvent,
                                finishResults,
                                true
                                );
                            resultsCache = Encoding.Default.GetBytes(results.TransformText());
                        }
                        message = resultsCache;
                        context.Response.ContentType = "text/html";
                        Log.D("Network.HttpServer", "Results html");
                    }
                    finally
                    {
                        infoLock.Exit();
                    }
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
                    stream.ReadExactly(message);
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
            else if (partBib.Length > 0)
            {
                answer = true;
                // Serve up HtmlParticipantTemplate
                if (!infoLock.TryEnter(3000))
                {
                    Log.D("Network.HttpServer", string.Format("Unable to get lock for outputting participant page for bib {0}.", partBib));
                    message = Encoding.Default.GetBytes("");
                }
                else
                {
                    try
                    {
                        if (!participantResults.TryGetValue(partBib, out List<TimeResult> resList))
                        {
                            resList = [];
                            participantResults[partBib] = resList;
                        }
                        if (!participantCache.TryGetValue(partBib, out byte[] partCache))
                        {
                            HtmlParticipantTemplate results = new(theEvent, resList);
                            partCache = Encoding.Default.GetBytes(results.TransformText());
                            participantCache[partBib] = partCache;
                        }
                        message = partCache;
                        context.Response.ContentType = "text/html";
                        Log.D("Network.HttpServer", "Participant html");
                    }
                    finally
                    {
                        infoLock.Exit();
                    }
                }
            }
            else if (emailBib.Length > 0)
            {
                answer = true;
                // Serve up the HtmlCertificateEmailTemplate
                if (!infoLock.TryEnter(3000))
                {
                    Log.D("Network.HttpServer", string.Format("Unable to get lock for outputting email page for bib {0}.", partBib));
                    message = Encoding.Default.GetBytes("");
                }
                else
                {
                    try
                    {
                        message = Encoding.Default.GetBytes("");
                        if (finishDictionary.TryGetValue(emailBib, out TimeResult finishResult) && participantDictionary.TryGetValue(finishResult.ParticipantId, out Participant finPart))
                        {
                            if (!emailCache.TryGetValue(emailBib, out byte[] cachedEmail))
                            {
                                HtmlCertificateEmailTemplate email = new(
                                    theEvent,
                                    finishResult,
                                    finPart.Email,
                                    distanceNames.Count == 1,
                                    apiDictionary.TryGetValue(theEvent.API_ID, out APIObject api) ? api : null
                                    );
                                cachedEmail = Encoding.Default.GetBytes(email.TransformText());
                                emailCache[emailBib] = cachedEmail;
                            }
                            Log.D("Network.HttpServer", "Email html");
                            message = cachedEmail;
                            context.Response.ContentType = "text/html";
                        }
                        Log.D("Network.HttpServer", "Email html");
                    }
                    finally
                    {
                        infoLock.Exit();
                    }
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
            _listener = new();
            _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
            _listener.Start();

            _serverThread = new(this.Listen);
            _serverThread.Start();
        }
    }
}
