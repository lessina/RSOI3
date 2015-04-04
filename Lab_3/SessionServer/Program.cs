using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Timers;

namespace SessionServer
{
    class Program
    {
        static string usersfile = "../../../resources/db/users";
        static string uri = ServerAddress.SessionServerAddress;
        static List<User> users;
        static string consoleHelp =
@"Session backend. Endpoints:
/register POST {name} {password}
/login POST {name} {password}
/authorize GET cookies:rsoi_name, rsoi_token
/logout POST cookies:rsoi_name, rsoi_token
all params are substrings to be searched";

        const double CookieLifeMS = 10 * 60 * 1000;
        static Dictionary<string, string> activeCookies = new Dictionary<string, string>();
        static Dictionary<string, Timer> cookieTimers = new Dictionary<string, Timer>();
        static Random r = new Random();

        static string GenerateRandomString(int length = 64)
        {   StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
                sb.Append((char)('a' + r.Next('z' - 'a')));
            return sb.ToString();
        }

        static string IssueCookie(string username, double timeToLive = CookieLifeMS)
        {   
            
            string cookie = GenerateRandomString();
            activeCookies.Add(username, cookie);
            
            Timer t = new Timer();
            t.AutoReset = false;
            t.Elapsed += (sender, e) =>
                {   activeCookies.Remove(username);
                    cookieTimers.Remove(username);
                    t.Dispose();
                };
            t.Interval = timeToLive;
            cookieTimers.Add(username, t);
            t.Start();

            return cookie;
        }

        static void Main(string[] args)
        {   try
            {   users = ExtensionMethods.FromXML<List<User>>(usersfile);
            }
            catch (Exception)
            {   users = new List<User>();
            }

            try
            {   WebServer ws = new WebServer(dispatcher, uri);
                ws.Run();
                Console.WriteLine(uri + Environment.NewLine + consoleHelp);
                Console.ReadKey();
                ws.Stop();
            }
            catch (Exception) { }
            finally
            {   users.ToXML(usersfile);
            }
        }

        static string dispatcher(HttpListenerRequest request, HttpListenerResponse response)
        {   switch (request.Url.AbsolutePath)
            {   case "/check":
                    {   var pars = request.GetPOSTParams();
                        if (pars.Keys.Contains("name") && pars.Keys.Contains("password"))
                        {   if (!users.Exists(u => u.name == pars["name"]))
                            {   users.Add(new User(pars["name"], pars["password"]));
                                return JsonConvert.SerializeObject(new { status = "success" });
                            }
                            else
                                return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "User with such name already exists");
                        }
                        else
                            return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "POST request should contain both 'name' and 'password' parameters");
                    }

                case "/login":
                    {   var pars = request.GetPOSTParams();
                        if (pars.Keys.Contains("name") && pars.Keys.Contains("password"))
                        {   if (users.Exists(u => u.name == pars["name"] && u.password == pars["password"]))
                            {   response.AppendCookie(new Cookie("rsoi_name", pars["name"])                );
                                response.AppendCookie(new Cookie("rsoi_token", IssueCookie(pars["name"]))  );
                                return JsonConvert.SerializeObject(new { status = "success" });
                            }
                            else
                                return JsonConvert.SerializeObject(new { status = "error", description = "login failed" });
                        }
                        else
                            return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "POST request should contain both 'name' and 'password' parameters");
                    }

                case "/authorize":
                    {   var uname = request.Cookies["rsoi_name"];
                        var utoken = request.Cookies["rsoi_token"];
                        if (uname != null && utoken != null &&
                            activeCookies.ContainsKey(uname.Value) &&
                            activeCookies[uname.Value] == utoken.Value)
                        {   cookieTimers[uname.Value].Interval = CookieLifeMS;
                            return JsonConvert.SerializeObject(new { status = "success" });
                        }
                        if (uname != null)
                        {   uname.Discard = true;
                            uname.Expires = DateTime.Now.AddYears(-1);
                            response.Cookies.Add(uname);
                        }
                        if (utoken != null)
                        {   utoken.Discard = true;
                            utoken.Expires = DateTime.Now.AddYears(-1);
                            response.Cookies.Add(utoken);
                        }
                        return JsonConvert.SerializeObject(new { status = "error", description = "auth failed" });
                    }

                case "/logout":
                    {   string reply;
                        var uname = request.Cookies["rsoi_name"];
                        var utoken = request.Cookies["rsoi_token"];
                        if (uname != null && utoken != null &&
                            activeCookies[uname.Value] == utoken.Value)
                        {   cookieTimers[uname.Value].Interval = 1;
                            reply = JsonConvert.SerializeObject(new { status = "success" });
                        }
                        else
                            reply = JsonConvert.SerializeObject(new { status = "error", description = "auth failed" });
                        if (uname != null)
                        {   uname.Discard = true;
                            uname.Expires = DateTime.Now.AddYears(-1);
                            response.Cookies.Add(uname);
                        }
                        if (utoken != null)
                        {   utoken.Discard = true;
                            utoken.Expires = DateTime.Now.AddYears(-1);
                            response.Cookies.Add(utoken);
                        }
                        return reply;
                    }
                default:
                    return response.SetStatusWithMessage(HttpStatusCode.NotFound, "endpoint not found");
            }
        }
    }
}
