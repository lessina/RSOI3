using System;
using System.Net;
using System.Threading;
using System.Linq;
using System.Text;
using Common;

namespace Common
{
    public class WebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, HttpListenerResponse, string> _responderMethod;      //делегат возвращающий нужный ответ

        public WebServer(Func<HttpListenerRequest, HttpListenerResponse, string> method, params string[] prefixes)
            : this(prefixes, method)
        {
        }

        public WebServer(string[] prefixes, Func<HttpListenerRequest, HttpListenerResponse, string> method)
        {   if (!HttpListener.IsSupported)
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");

            // URI prefixes are required, for example "http://localhost:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes list is empty");

            // A responder method is required
            if (method == null)
                throw new ArgumentException("no responder method");

            foreach (string s in prefixes)
                _listener.Prefixes.Add(s);
            _responderMethod = method;
            _listener.Start();
        }

        public void Run()
        {   ThreadPool.QueueUserWorkItem((o) => //поток прослушивания HTTP
            {   Console.WriteLine("Webserver running...");
                try
                {   while (_listener.IsListening)
                    {   ThreadPool.QueueUserWorkItem((c) => //поток обработки конкретного запроса
                        {   var ctx = c as HttpListenerContext;
                            try
                            {   Log(ctx.Request);
                                //ctx.Response.ContentType = "text/html";
                                string rstr = _responderMethod(ctx.Request, ctx.Response);
                                Log(ctx.Response, rstr);
                                if (rstr != null)
                                {   byte[] buf = Encoding.UTF8.GetBytes(rstr);
                                    ctx.Response.ContentLength64 = buf.Length;
                                    ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                                }
                            }
                            catch (Exception) { } // suppress any exceptions
                            finally
                            {   // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }

        public void Stop()
        {   _listener.Stop();
            _listener.Close();
        }

        private void Log(HttpListenerRequest req)
        {   Console.WriteLine("=============================");
            Console.WriteLine("REQUEST: ");
            Console.WriteLine(req.Url);
            Console.WriteLine("get params:");
            foreach (var par_name in req.QueryString.AllKeys)
                Console.WriteLine("  {0}={1}", par_name, req.QueryString[par_name]);
        }

        private void Log(HttpListenerResponse res, string content)
        {   Console.WriteLine("RESPONSE:");
            Console.WriteLine("  " + res.StatusCode.ToString());
            Console.WriteLine("  " + res.StatusDescription.ToString());
            Console.WriteLine("  " + content);
            Console.WriteLine("=============================");
        }
    }
}