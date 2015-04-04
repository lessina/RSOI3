using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using RestSharp;

namespace FrontendServer
{
    class Program
    {
        static string uri = ServerAddress.FrontendServerAddress;
        static string consoleHelp = @"Frontend.";

        static void Main(string[] args)
        {   try
            {   WebServer ws = new WebServer(dispatcher, uri);
                ws.Run();
                Console.WriteLine(uri + Environment.NewLine + consoleHelp);
                Console.ReadKey();
                ws.Stop();
            }
            catch (Exception) { }
        }

        static string dispatcher(HttpListenerRequest request, HttpListenerResponse response)
        {   switch (request.Url.AbsolutePath.ToLowerInvariant())
            {   //статические страницы
                case "/beginer":
                    return Pages.Main;
                case "/check":
                    return Pages.Register;
                case "/login":
                    return Pages.Login;
                case "/automanage":
                    return Pages.PhotosControl;
                case "/clientsmanage":
                    return Pages.ClientsControl;
                 
                #region обращения к auto server
                case "/automanage-submit":
                    {   if (!is_authorized(request, response))
                            return response.SetStatusWithMessage(HttpStatusCode.Unauthorized, "Unauthorized access");
                        var pars = request.GetPOSTParams();
                        if (!pars.Keys.Contains("button"))
                            return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "Request not recognized");

                        switch (pars["button"])
                        {     case "READ": //GET
                                {   var rc = new RestClient(ServerAddress.PhotosServerAddress).Execute(new RestRequest("photo/" + pars["id"], Method.GET));
                                    if (rc.StatusCode == HttpStatusCode.NotFound)
                                        return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "Not found");
                                    response.SetStatus((HttpStatusCode)rc.StatusCode, rc.StatusDescription);
                                    var obj = JsonConvert.DeserializeObject<List<Photo>>(rc.Content)[0];
                                    return
                                        Pages.Table.Replace("CONTENT", 
                                                            string.Format("<tr>"+
                                                            "<th class=\"tg-031e\">{0}</th>"+
                                                            "<th class=\"tg-031e\">{1}</th>"+
                                                            "<th class=\"tg-031e\">{2}</th></tr>",
                                                            obj.id, obj.owner, obj.mileage));
                                }
                            case "CREATE": //POST
                                {   if (pars["mileage"].Trim().Length == 0)
                                        return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "upload date and owner are required");
                                    var rq = new RestRequest("photo", Method.POST);
                                    rq.AddParameter("owner", pars["owner"], ParameterType.GetOrPost);
                                    rq.AddParameter("mileage", pars["mileage"], ParameterType.GetOrPost);
                                    var rc = new RestClient(ServerAddress.PhotosServerAddress).Execute(rq);
                                    return response.SetStatusWithMessage((HttpStatusCode)rc.StatusCode, rc.StatusDescription);
                                }
                            case "UPDATE": //PUT
                                {   if (pars["mileage"].Trim().Length == 0)
                                        return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "upload date or owner is required");
                                    var rq = new RestRequest("photo/" + pars["id"], Method.PUT);
                                    rq.AddParameter("owner", pars["owner"], ParameterType.GetOrPost);
                                    rq.AddParameter("mileage", pars["mileage"], ParameterType.GetOrPost);
                                    var rc = new RestClient(ServerAddress.PhotosServerAddress).Execute(rq);
                                    return response.SetStatusWithMessage((HttpStatusCode)rc.StatusCode, rc.StatusDescription);
                                }
                            case "DELETE":
                                {   var rc = new RestClient(ServerAddress.PhotosServerAddress).Execute(new RestRequest("photo/" + pars["id"], Method.DELETE));
                                    return response.SetStatusWithMessage((HttpStatusCode)rc.StatusCode, rc.StatusDescription);
                                }
                            default:
                                return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "Request not recognized");
                        }
                    }
                case "/auto": //часть результатов может возвращать без авторизции
                    {   if (is_authorized(request, response))
                        {   return Pages.Table.Replace("CONTENT",
                                JsonConvert.DeserializeObject<List<Photo>>(
                                    new RestClient(ServerAddress.PhotosServerAddress).Execute(new RestRequest("item" + request.Url.Query, Method.GET)).Content)
                                    .Select(p => string.Format("<th class=\"tg-031e\">{0}</th><th class=\"tg-031e\">{1}</th><th class=\"tg-031e\">{2}</th>", p.id, p.owner, p.mileage))
                                    .Aggregate("", (a, b) => a + string.Format("<tr>{0}</tr>", b)));
                        }
                        else
                        {   return Pages.Table.Replace("CONTENT",
                                JsonConvert.DeserializeObject<List<Photo>>(
                                    new RestClient(ServerAddress.PhotosServerAddress).Execute(new RestRequest("item")).Content)
                                    .Select(p => p.mileage)
                                    .Aggregate("", (a, b) => a + string.Format("<tr><th class=\"tg-031e\">{0}</th></tr>", b)));
                        }
                    }
                #endregion

                #region обращения к clients server
                case "/clientsmanage-submit":
                    {
                        if (!is_authorized(request, response))
                            return response.SetStatusWithMessage(HttpStatusCode.Unauthorized, "Unauthorized access");
                        var pars = request.GetPOSTParams();
                        if (!pars.Keys.Contains("button"))
                            return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "Request not recognized");

                        switch (pars["button"])
                        {   case "READ": //GET
                                {   var rc = new RestClient(ServerAddress.ClientsServerAddress).Execute(new RestRequest("client/" + pars["id"], Method.GET));
                                    if (rc.StatusCode == HttpStatusCode.NotFound)
                                        return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "Not found");
                                    response.SetStatus((HttpStatusCode)rc.StatusCode, rc.StatusDescription);
                                    var obj = JsonConvert.DeserializeObject<List<Client>>(rc.Content)[0];
                                    return
                                        Pages.Table.Replace("CONTENT",
                                                            string.Format("<tr>"+
                                                            "<th class=\"tg-031e\">{0}</th>" +
                                                            "<th class=\"tg-031e\">{1}</th>" +
                                                            "<th class=\"tg-031e\">{2}</th>" +
                                                            "<th class=\"tg-031e\">{3}</th></tr>",
                                                            obj.id, obj.name, obj.address, obj.tel));

                                }
                            case "CREATE": //POST
                                {   if (pars["name"].Trim().Length == 0)
                                        return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "name, tel & address are required");
                                    var rq = new RestRequest("client", Method.POST);
                                    rq.AddParameter("name", pars["name"], ParameterType.GetOrPost);
                                    rq.AddParameter("tel", pars["tel"], ParameterType.GetOrPost);
                                    rq.AddParameter("address", pars["address"], ParameterType.GetOrPost);
                                    var rc = new RestClient(ServerAddress.ClientsServerAddress).Execute(rq);
                                    return response.SetStatusWithMessage((HttpStatusCode)rc.StatusCode, rc.StatusDescription);
                                }
                            case "UPDATE": //PUT
                                {   if (pars["name"].Trim().Length == 0)
                                        return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "name, tel or address is required");
                                    var rq = new RestRequest("client/" + pars["id"], Method.PUT);
                                    rq.AddParameter("name", pars["name"], ParameterType.GetOrPost);
                                    rq.AddParameter("tel", pars["tel"], ParameterType.GetOrPost);
                                    rq.AddParameter("address", pars["address"], ParameterType.GetOrPost);
                                    var rc = new RestClient(ServerAddress.ClientsServerAddress).Execute(rq);
                                    return response.SetStatusWithMessage((HttpStatusCode)rc.StatusCode, rc.StatusDescription);
                                }
                            case "DELETE":
                                {   var rc = new RestClient(ServerAddress.ClientsServerAddress).Execute(new RestRequest("client/" + pars["id"], Method.DELETE));
                                    return response.SetStatusWithMessage((HttpStatusCode)rc.StatusCode, rc.StatusDescription);
                                }
                            default:
                                return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "Request not recognized");
                        }
                    }
                case "/clients": //часть результатов может возвращать без авторизции
                    {   if (is_authorized(request, response))
                        {   return Pages.Table.Replace("CONTENT",
                                JsonConvert.DeserializeObject<List<Client>>(
                                    new RestClient(ServerAddress.ClientsServerAddress).Execute(new RestRequest("item" + request.Url.Query, Method.GET)).Content)
                                    .Select(c => string.Format("<th class=\"tg-031e\">{0}</th>" +
                                                               "<th class=\"tg-031e\">{1}</th>" +
                                                               "<th class=\"tg-031e\">{2}</th>" +
                                                               "<th class=\"tg-031e\">{3}</th>",
                                                               c.id, c.name, c.tel, c.address))
                                    .Aggregate("", (a, b) => a + string.Format("<tr>{0}</tr>", b)))
                                    .Replace("%40", "@").Replace("%2540", "@");
                        }
                        else
                        {   return Pages.Table.Replace("CONTENT",
                                JsonConvert.DeserializeObject<List<Client>>(
                                    new RestClient(ServerAddress.ClientsServerAddress).Execute(new RestRequest("item")).Content)
                                    .Select(c => c.name)
                                    .Aggregate("", (a, b) => a + string.Format("<tr><th class=\"tg-031e\">{0}</th></tr>", b)));
                        }
                    }
                #endregion

                #region обращения к session server
                case "/check-submit":
                    {   var pars = request.GetPOSTParams();
                        var rq = new RestRequest("check", Method.POST);
                        rq.AddParameter("name", pars["name"]);
                        rq.AddParameter("password", pars["password"]);
                        var rc = new RestClient(ServerAddress.SessionServerAddress).Execute(rq);
                        //response.Redirect("/main");
                        return rc.Content;
                    }
                case "/login-submit":
                    {   var pars = request.GetPOSTParams();
                        var rq = new RestRequest("login", Method.POST);
                        rq.AddParameter("name", pars["name"]);
                        rq.AddParameter("password", pars["password"]);
                        var rc = new RestClient(ServerAddress.SessionServerAddress).Execute(rq);
                        foreach (var cookie in rc.Cookies)
                            response.AppendCookie(new Cookie()
                            {   Name = cookie.Name,
                                Value = cookie.Value,
                                Discard = cookie.Discard,
                                Expires = cookie.Expires
                            });
                        //response.Redirect("/main");
                        return rc.Content;
                    }
                case "/logout":
                    {   var rq = new RestRequest("logout", Method.POST);
                        var cname = request.Cookies["rsoi_name"];
                        var ctoken = request.Cookies["rsoi_token"];
                        if (cname != null && ctoken != null)
                        {   rq.AddCookie(cname.Name, cname.Value);
                            rq.AddCookie(ctoken.Name, ctoken.Value);
                        }
                        var rc = new RestClient(ServerAddress.SessionServerAddress).Execute(rq);
                        foreach (var cookie in rc.Cookies)
                            response.AppendCookie(new Cookie()
                            {   Name = cookie.Name,
                                Value = cookie.Value,
                                Discard = cookie.Discard,
                                Expires = cookie.Expires
                            });
                        //response.Redirect("/main");
                        return rc.Content;
                    }
                #endregion

                #region запрос с агрегированием к clients server и photos server
                case "/auto_of_users":
                    {   if (is_authorized(request, response))
                        {   var photos = JsonConvert.DeserializeObject<List<Photo>>(
                                new RestClient(ServerAddress.PhotosServerAddress).Execute(new RestRequest("item")).Content);

                            var rq = new RestRequest("item");
                            rq.AddQueryParameter("name",
                                                 photos.Select(p => p.owner)
                                                 .Distinct()
                                                 .Aggregate((a, b) => a + ',' + b));

                            var owners = JsonConvert.DeserializeObject<List<Client>>(
                                new RestClient(ServerAddress.ClientsServerAddress).Execute(rq).Content);
                            return
                                Pages.Table.Replace("CONTENT",
                                                    owners.Select(o => string.Format(
                                                        "<th class=\"tg-031e\">{0}</th>" +
                                                        "<th class=\"tg-031e\">{1}</th>" +
                                                        "<th class=\"tg-031e\">{2}</th>" +
                                                        "<th class=\"tg-031e\">{3}</th>" +
                                                        "<th class=\"tg-031e\">{4}</th>",
                                                        o.id,
                                                        o.name,
                                                        o.tel,
                                                        o.address,
                                                        photos.Where(p => p.owner == o.name)
                                                              .Select(p => p.mileage)
                                                              .Aggregate((a, b) => a + ", " + b)))
                                                    .Aggregate("", (a, b) => a + string.Format("<tr>{0}</tr>", b)))
                                                    .Replace("%40", "@").Replace("%2540", "@");
                        }
                        else
                            return response.SetStatusWithMessage(HttpStatusCode.Unauthorized, "Unauthorized access");
                    }
                #endregion

                default:
                    return response.SetStatusWithMessage(HttpStatusCode.NotFound, "Page not found");
            }
        }

        static bool is_authorized(HttpListenerRequest request, HttpListenerResponse response)
        {   RestClient rc = new RestClient(ServerAddress.SessionServerAddress);
            RestRequest rqToSessionServ = new RestRequest("authorize", Method.GET);
            var cname = request.Cookies["rsoi_name"];
            var ctoken = request.Cookies["rsoi_token"];
            if (cname != null && ctoken != null)
            {   rqToSessionServ.AddCookie(cname.Name, cname.Value);
                rqToSessionServ.AddCookie(ctoken.Name, ctoken.Value);
            }
            var rspFromSessionServ = rc.Execute(rqToSessionServ);
            if (JsonConvert.DeserializeAnonymousType(rspFromSessionServ.Content, new { status = "" }).status == "success")
                return true;
            else
            {   foreach (var cookie in rspFromSessionServ.Cookies)
                {   response.Cookies.Add(new Cookie()
                    {   Name = cookie.Name,
                        Value = cookie.Value,
                        Discard = cookie.Discard,
                        Expires = cookie.Expires
                    });
                }
                return false;
            }
        }
    }
}
