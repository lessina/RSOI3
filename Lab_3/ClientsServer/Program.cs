using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Newtonsoft.Json;
using System.Net;
using System.IO;

namespace EmployeesServer
{
    class Program
    {
        static string clientsFile = "../../../resources/db/clients";
        static string uri = ServerAddress.ClientsServerAddress;
        static List<Client> clients;
        static string consoleHelp =
@"Employees backend. Endpoints:
/item GET [name] [address] [email] - возвращает список владельцев
/client/id GET, DELETE
/client/id PUT [name] [address] [email]
/client    POST {name} {address} {email}
Все параметры являются подстроками для поиска.";
        static int idToIssue;
        static int GetNewID()
        {   return idToIssue++;
        }

        static void Main(string[] args)
        {   try
            {   clients = ExtensionMethods.FromXML<List<Client>>(clientsFile);
            }
            catch (Exception)
            {   clients = new List<Client>();
            }
            idToIssue = clients.Select(c => c.id).Max() + 1;

            try
            {   WebServer ws = new WebServer(dispatcher, uri);
                ws.Run();
                Console.WriteLine(uri + Environment.NewLine + consoleHelp);
                Console.ReadKey();
                ws.Stop();
            }
            catch (Exception) { }
            finally
            {   clients.ToXML(clientsFile);
            }
        }

        static string dispatcher(HttpListenerRequest request, HttpListenerResponse response)
        {   switch (request.Url.AbsolutePath)
            {   case "/item": //возвращает список владельцев
                    {   var pars = request.QueryString;
                        return JsonConvert.SerializeObject(clients.Where(c =>
                               c.name.ContainsAny(pars.GetVals("name"))
                               && c.address.ContainsAny(pars.GetVals("address"))
                               && c.tel.ContainsAny(pars.GetVals("tel"))));
                    }
                default: // /client
                    if (request.Url.Segments.Length > 1)
                    {   int id;
                        if (!int.TryParse(request.Url.Segments.Length > 2 ? request.Url.Segments[2] : null, out id))
                            id = -1;
                        var record = clients.FirstOrDefault(c => c.id == id);
                        if (request.Url.Segments[1].Replace("/", "") == "client")
                            switch (request.HttpMethod)
                            {   case "GET":
                                    {   if (record != null)
                                            return JsonConvert.SerializeObject(new Client[] { record });
                                        else
                                            return response.SetStatusWithMessage(HttpStatusCode.NotFound, "Element not found");
                                    }
                                case "POST":
                                    {   if (id != -1)
                                            return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "Endpoint for adding should be /client");
                                        var pars = request.GetPOSTParams();
                                        if (pars.Keys.Contains("name") && pars.Keys.Contains("tel") && pars.Keys.Contains("address"))
                                        {
                                            clients.Add(new Client(GetNewID(), pars["name"], pars["address"], pars["tel"]));
                                            return response.SetStatusWithMessage(HttpStatusCode.Created, "Successfully created");
                                        }
                                        else
                                            return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "Request should contain POST name, tel, address parameters");
                                    }
                                case "PUT":
                                    {   var pars = request.GetPOSTParams();
                                        if (record != null)
                                        {   if (pars.ContainsKey("name"))
                                                record.name = pars["name"];
                                            if (pars.ContainsKey("tel"))
                                                record.tel = pars["tel"];
                                            if (pars.ContainsKey("name"))
                                                record.address = pars["address"];
                                            return JsonConvert.SerializeObject(new { status = "successful" });
                                        }
                                        else
                                            return response.SetStatusWithMessage(HttpStatusCode.NotFound, "Element not found");
                                    }
                                case "DELETE":
                                    {   clients.Remove(record);
                                        if (record != null)
                                            return JsonConvert.SerializeObject(new { status = "success" });
                                        else
                                            return response.SetStatusWithMessage(HttpStatusCode.NotFound, "Element not found");
                                    }
                            }
                    }
                    return response.SetStatusWithMessage(HttpStatusCode.NotFound, "Endpoint not found");
            }
        }
    }
}
