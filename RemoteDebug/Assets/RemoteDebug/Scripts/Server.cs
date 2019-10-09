using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;
using System.Threading;

namespace RD
{
    public class RequestContext
    {
        public HttpListenerContext Context;
        public Match Match;
        public bool Pass;
        public string Path;
        public int CurrentRoute;

        public HttpListenerRequest Request => Context.Request;
        public HttpListenerResponse Response => Context.Response;

        public RequestContext(HttpListenerContext ctx)
        {
            Context = ctx;
            Match = null;
            Pass = false;
            Path = UnityWebRequest.UnEscapeURL(Context.Request.Url.AbsolutePath);
            if (Path == "/")
            {
                Path = "/index.html";
            }
            CurrentRoute = 0;
        }
    }

    // ==============================================================================================
    // ==============================================================================================

    public class Server : MonoBehaviour
    {
        public bool Initialized { get; private set; } = false;

        // ==============================================================================================

        public virtual void Initialize()
        {
            if(Initialized)
            {
                Debug.LogError("Remote debug server is already initialized.");
                return;
            }

            m_mainThread = Thread.CurrentThread;
            m_fileRoot = Path.Combine(Application.streamingAssetsPath, "RemoteDebug");

            // Start server
            Debug.Log("Starting CUDLR Server on port : " + m_port);
            m_listener = new HttpListener();
            m_listener.Prefixes.Add("http://*:" + m_port + "/");
            m_listener.Start();
            m_listener.BeginGetContext(ListenerCallback, null);

            StartCoroutine(HandleRequests());

            Initialized = true;
        }

        // ==============================================================================================

        private void RegisterRoutes()
        {
            if (m_registeredRoutes == null)
            {
                m_registeredRoutes = new List<RouteAttribute>();

                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        // FIXME add support for non-static methods (FindObjectByType?)
                        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                        {
                            RouteAttribute[] attrs = method.GetCustomAttributes(typeof(RouteAttribute), true) as RouteAttribute[];
                            if (attrs.Length == 0)
                            {
                                continue;
                            }

                            RouteAttribute.Callback cbm = Delegate.CreateDelegate(typeof(RouteAttribute.Callback), method, false) as RouteAttribute.Callback;
                            if (cbm == null)
                            {
                                Debug.LogError(string.Format("Method {0}.{1} takes the wrong arguments for a console route.", type, method.Name));
                                continue;
                            }

                            // try with a bare action
                            foreach (RouteAttribute route in attrs)
                            {
                                if (route.Route == null)
                                {
                                    Debug.LogError(string.Format("Method {0}.{1} needs a valid route regexp.", type, method.Name));
                                    continue;
                                }

                                route.Cbk = cbm;
                                m_registeredRoutes.Add(route);
                            }
                        }
                    }
                }

                RegisterFileHandlers();
            }
        }

        private static void FindFileType(RequestContext context, bool download, out string path, out string type)
        {
            path = Path.Combine(m_fileRoot, context.Match.Groups[1].Value);

            string ext = Path.GetExtension(path).ToLower().TrimStart(new char[] { '.' });
            if (download || !fileTypes.TryGetValue(ext, out type))
            {
                type = "application/octet-stream";
            }
        }

        private static void RequestFileHandler(RequestContext context, bool download)
        {
            FindFileType(context, download, out string path, out string type);

            UnityWebRequest request = new UnityWebRequest(path, context.Request.HttpMethod);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SendWebRequest();
            while(!request.isDone)
            {
                Thread.Sleep(0);
            }

            if (string.IsNullOrEmpty(request.error))
            {
                context.Response.ContentType = type;
                if (download)
                {
                    context.Response.AddHeader("Content-disposition", string.Format("attachment; filename={0}", Path.GetFileName(path)));
                }

                context.Response.WriteBytes(request.downloadHandler.data);
                return;
            }

            if (request.error.StartsWith("Couldn't open file"))
            {
                context.Pass = true;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.StatusDescription = string.Format("Fatal error:\n{0}", request.error);
            }
        }

        private static void FileHandler(RequestContext context, bool download)
        {
            FindFileType(context, download, out string path, out string type);

            if (File.Exists(path))
            {
                context.Response.WriteFile(path, type, download);
            }
            else
            {
                context.Pass = true;
            }
        }

        private static void RegisterFileHandlers()
        {
            string pattern = string.Format("({0})", string.Join("|", fileTypes.Select(x => x.Key).ToArray()));
            RouteAttribute downloadRoute = new RouteAttribute(string.Format(@"^/download/(.*\.{0})$", pattern));
            RouteAttribute fileRoute = new RouteAttribute(string.Format(@"^/(.*\.{0})$", pattern));

            bool needsRequest = m_fileRoot.Contains("://");
            downloadRoute.RunOnMainThread = needsRequest;
            fileRoute.RunOnMainThread = needsRequest;

            Action<RequestContext, bool> cbk = FileHandler;
            if (needsRequest)
            {
                cbk = RequestFileHandler;
            }
            
            downloadRoute.Cbk = delegate (RequestContext context) { cbk(context, true); };
            fileRoute.Cbk = delegate (RequestContext context) { cbk(context, false); };

            m_registeredRoutes.Add(downloadRoute);
            m_registeredRoutes.Add(fileRoute);
        }

        private void ListenerCallback(IAsyncResult result)
        {
            RequestContext context = new RequestContext(m_listener.EndGetContext(result));
            HandleRequest(context);

            if (m_listener.IsListening)
            {
                m_listener.BeginGetContext(ListenerCallback, null);
            }
        }

        private void HandleRequest(RequestContext context)
        {
            RegisterRoutes();

            try
            {
                bool handled = false;

                for (; context.CurrentRoute < m_registeredRoutes.Count; ++context.CurrentRoute)
                {
                    RouteAttribute route = m_registeredRoutes[context.CurrentRoute];
                    Match match = route.Route.Match(context.Path);
                    if (!match.Success)
                    {
                        continue;
                    }

                    if (!route.Methods.IsMatch(context.Request.HttpMethod))
                    {
                        continue;
                    }

                    // Upgrade to main thread if necessary
                    if (route.RunOnMainThread && Thread.CurrentThread != m_mainThread)
                    {
                        lock (m_mainRequests)
                        {
                            m_mainRequests.Enqueue(context);
                        }
                        return;
                    }

                    context.Match = match;
                    route.Cbk(context);
                    handled = !context.Pass;
                    if (handled)
                    {
                        break;
                    }
                }

                if (!handled)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.StatusDescription = "Not Found";
                }
            }
            catch (Exception exception)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.StatusDescription = string.Format("Fatal error:\n{0}", exception);

                Debug.LogException(exception);
            }

            context.Response.OutputStream.Close();
        }

        private IEnumerator HandleRequests()
        {
            while (true)
            {
                while (m_mainRequests.Count == 0)
                {
                    yield return new WaitForEndOfFrame();
                }

                RequestContext context = null;
                lock (m_mainRequests)
                {
                    context = m_mainRequests.Dequeue();
                }

                HandleRequest(context);
            }
        }

        // ==============================================================================================

        private void Awake()
        {
            if (m_initOnAwake)
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (Initialized)
            {
                Console.Update();
            }
        }

        private void OnDestroy()
        {
            Initialized = false;
            m_listener.Close();
            m_listener = null;
        }

        private void OnEnable()
        {
            if (m_registerLogCallback)
            {
                // Capture Console Logs
#if UNITY_5_3_OR_NEWER
                Application.logMessageReceived += Console.LogCallback;
#else
        Application.RegisterLogCallback(Console.LogCallback);
#endif
            }
        }

        private void OnDisable()
        {
            if (m_registerLogCallback)
            {
#if UNITY_5_3_OR_NEWER
                Application.logMessageReceived -= Console.LogCallback;
#else
        Application.RegisterLogCallback(null);
#endif
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                m_listener.Stop();
            }
            else
            {
                m_listener.Start();
                m_listener.BeginGetContext(ListenerCallback, null);
            }
        }

        // ==============================================================================================

        [SerializeField] private bool m_initOnAwake = false;
        [SerializeField] private int m_port = 55055;
        [SerializeField] private bool m_registerLogCallback = false;

        private static Thread m_mainThread = null;
        private static string m_fileRoot = "";
        private static HttpListener m_listener = null;
        private static List<RouteAttribute> m_registeredRoutes = null;
        private static Queue<RequestContext> m_mainRequests = new Queue<RequestContext>();

        // List of supported files
        // FIXME add an api to register new types
        private static Dictionary<string, string> fileTypes = new Dictionary<string, string>
        {
            {"js",   "application/javascript"},
            {"json", "application/json"},
            {"jpg",  "image/jpeg" },
            {"jpeg", "image/jpeg"},
            {"gif",  "image/gif"},
            {"png",  "image/png"},
            {"css",  "text/css"},
            {"htm",  "text/html"},
            {"html", "text/html"},
            {"ico",  "image/x-icon"},
        };
    }
}