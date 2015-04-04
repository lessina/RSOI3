using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Newtonsoft.Json;
using System.Net;
using System.IO;

namespace PhotosServer
{
    class Program
    {
        static string photosFile = "../../../resources/db/auto";
        static string uri = ServerAddress.PhotosServerAddress;
        static List<Photo> photos;
        static string consoleHelp =
@"Photos backend. Endpoints:
/item GET [owner] [uploaded]  - возвращает список владельцев авто с пробегом
/photo/id GET, DELETE
/photo/id PUT [owner] [uploaded]
/photo    POST {owner} {uploaded}
Все параметры являются подстроками для поиска.";
        static int idToIssue;

        static int GetNewID()
        {   return idToIssue++;
        }

        static void Main(string[] args)
        {   try
            {   photos = ExtensionMethods.FromXML<List<Photo>>(photosFile);
            }
            catch (Exception)
            {   photos = new List<Photo>();
            }
            idToIssue = photos.Select(p => p.id).Max() + 1;
            try
            {   WebServer ws = new WebServer(dispatcher, uri);
                ws.Run();
                Console.WriteLine(uri + Environment.NewLine + consoleHelp);
                Console.ReadKey();
                ws.Stop();
            }
            catch (Exception) { }
            finally
            {   photos.ToXML(photosFile);
            }
        }

        static string dispatcher(HttpListenerRequest request, HttpListenerResponse response)
        {   switch (request.Url.AbsolutePath)
            {
                case "/item":
                    {   var pars = request.QueryString;
                        return JsonConvert.SerializeObject(photos.Where(p =>
                               p.owner.ContainsAny(pars.GetVals("owner")) && p.mileage.ContainsAny(pars.GetVals("mileage"))));
                    }
                default: // auto/
                    {   if (request.Url.Segments.Length > 1)
                        {   int id;
                            if (!int.TryParse(request.Url.Segments.Length > 2 ? request.Url.Segments[2] : null, out id))
                                id = -1;
                            var record = photos.FirstOrDefault(w => w.id == id);
                            if (request.Url.Segments[1].Replace("/", "") == "photo")
                                switch (request.HttpMethod)
                                {   case "GET":
                                        {   if (record != null)
                                                return JsonConvert.SerializeObject(new Photo[] { record });
                                            else
                                                return response.SetStatusWithMessage(HttpStatusCode.NotFound, "Element not found");
                                        }
                                    case "POST":
                                        {   if (id != -1)
                                                return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "Endpoint for adding should be /photo");
                                            var pars = request.GetPOSTParams();
                                            if (pars.Keys.Contains("owner") && pars.Keys.Contains("mileage"))
                                            {   photos.Add(new Photo(GetNewID(), pars["mileage"], pars["owner"]));
                                                return response.SetStatusWithMessage(HttpStatusCode.Created, "Successfully created");
                                            }
                                            else
                                                return response.SetStatusWithMessage(HttpStatusCode.BadRequest, "Request should contain POST owner, mileage date parameters");
                                        }
                                    case "PUT":
                                        {   var pars = request.GetPOSTParams();
                                            if (record != null)
                                            {   if (pars.ContainsKey("owner"))
                                                    record.owner = pars["owner"];
                                            if (pars.ContainsKey("mileage"))
                                                record.mileage = pars["mileage"];
                                                return JsonConvert.SerializeObject(new { status = "successful" });
                                            }
                                            else
                                                return response.SetStatusWithMessage(HttpStatusCode.NotFound, "Element not found");
                                        }
                                    case "DELETE":
                                        {   photos.Remove(record);
                                            if (record != null)
                                                return JsonConvert.SerializeObject(new { status = "successful" });
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
}
