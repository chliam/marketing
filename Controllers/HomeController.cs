using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace marketing.Controllers
{
    public class HomeController : Controller
    {
        private static string token = WebConfigurationManager.AppSettings["WeixinToken"];
        private static string appId = WebConfigurationManager.AppSettings["WeixinAppId"];
        private static string appSecret = WebConfigurationManager.AppSettings["WeixinAppSecret"];
        private static string lastAccessToken = string.Empty;
        private static DateTime lastGetAccessTokenTime = DateTime.MinValue;

        public ActionResult Index()
        {
            ViewBag.Title = "推广记录";
            return View();
        }
       
        public JsonResult Get(int? page, int? limit, string sortBy, string direction, string scene)
        {
            int skip = ((page ?? 1) - 1) * (limit ?? 10);
            int take = limit ?? 10;
            if (string.IsNullOrEmpty(scene))
            {
                scene = "";
            }
            if (string.IsNullOrEmpty(direction))
            {
                direction = "desc";
            }
            if (string.IsNullOrEmpty(sortBy))
            {
                sortBy = "create_time";
            }
            using (var db = data.Entities.NewInstance)
            {
                var total = db.tbtickets.Count(p => p.scene_str.Contains(scene) && p.status == 0);
                var query = from t in db.tbtickets
                            join e in db.tbevents on t.ticket equals e.ticket into e1
                            from e2 in e1.DefaultIfEmpty()
                            where t.scene_str.Contains(scene) && t.status == 0
                            group e2 by new
                            {
                                uid = t.uid,
                                scene_str = t.scene_str,
                                note = t.note,
                                create_time = t.create_time,
                                url = t.url,
                                expire_seconds = t.expire_seconds,
                                ticket = t.ticket
                            }
                            into grouped
                            select new
                            {
                                uid = grouped.Key.uid,
                                scene_str = grouped.Key.scene_str,
                                note = grouped.Key.note,
                                create_time = grouped.Key.create_time,
                                url = grouped.Key.url,
                                expire_seconds = grouped.Key.expire_seconds,
                                ticket = grouped.Key.ticket,
                                count = grouped.Count(t => t.ticket != null && t.unsubscribe_time == null)
                            };

                if (direction.ToLower() == "asc")
                {
                    if (sortBy == "expire_time")
                    {
                        query = query.OrderBy(p => p.create_time + p.expire_seconds * 10000000);
                    }
                    else if (sortBy == "count")
                    {
                        query = query.OrderBy(p => p.count);
                    }
                    else
                    {
                        query = query.OrderBy(p => p.create_time);
                    }
                }
                else
                {
                    if (sortBy == "expire_time")
                    {
                        query = query.OrderByDescending(p => p.create_time + p.expire_seconds * 10000000);
                    }
                    else if (sortBy == "count")
                    {
                        query = query.OrderByDescending(p => p.count);
                    }
                    else
                    {
                        query = query.OrderByDescending(p => p.create_time);
                    }
                }
                var records = query.Skip(skip).Take(take)
                    .ToList()
                    .Select(p => new
                    {
                        uid = p.uid,
                        scene_str = p.scene_str,
                        note = p.note,
                        create_time = new DateTime(p.create_time).ToString("yyyy-MM-dd HH:mm"),
                        url = p.url,
                        expire_time = new DateTime(p.create_time + p.expire_seconds * 10000000).ToString("yyyy-MM-dd HH:mm"),
                        expire_seconds = p.expire_seconds / (24*60*60),
                        ticket = p.ticket,
                        count = p.count
                    })
                    .ToList();

                return Json(new { records, total }, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpPost]
        public JsonResult Save(data.tbticket ticke)
        {
            data.tbticket entity;
            using (var db = data.Entities.NewInstance)
            {
                if (ticke.uid > 0)
                {
                    entity = db.tbtickets.First(p => p.uid == ticke.uid);
                    entity.scene_str = ticke.scene_str;
                    entity.expire_seconds = ticke.expire_seconds;
                    entity.note = ticke.note;
                }
                else
                {
                    var requstUrl = string.Format("https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token={0}", GetAccessToken());
                    var jsonParam = JsonConvert.SerializeObject(new { expire_seconds = ticke.expire_seconds, action_name = "QR_STR_SCENE", action_info = new { scene = new { scene_str = ticke.scene_str } } });
                    var jsonResult = PostJson(requstUrl, jsonParam);
                    var json = JsonConvert.DeserializeObject<dynamic>(jsonResult);
                    db.tbtickets.Add(new data.tbticket
                    {
                        action_name = "QR_STR_SCENE",
                        expire_seconds = ticke.expire_seconds,
                        note = ticke.note,
                        create_time = DateTime.Now.Ticks,
                        scene_str = ticke.scene_str,
                        ticket = json.ticket,
                        url = json.url,
                        status = 0
                    });
                }
                db.SaveChanges();
            }
            return Json(new { result = true });
        }

        [HttpPost]
        public JsonResult Delete(int uid)
        {           
            using (var db = data.Entities.NewInstance)
            {
                data.tbticket entity = db.tbtickets.First(p => p.uid == uid);
                if (entity != null)
                {
                    entity.status = 1;
                }
                db.SaveChanges();
            }
            return Json(new { result = true });
        }
     
        public static string GetAccessToken()
        {
            if (!string.IsNullOrEmpty(lastAccessToken) && DateTime.Now.AddMinutes(-90) > lastGetAccessTokenTime)
            {
                return lastAccessToken;
            }
            else
            {
                string url = string.Format("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}", appId, appSecret);
                var jsonResult = PostJson(url, string.Empty, "GET");
                var json = JsonConvert.DeserializeObject<dynamic>(jsonResult);
                lastAccessToken = json.access_token;
                lastGetAccessTokenTime = DateTime.Now;
                return lastAccessToken;
            }
        }

        public static string PostJson(string url, string jsonParam, string method="POST")
        {
            var encode = "utf-8";
            var request = WebRequest.Create(url);
            request.Method = method;
            request.ContentType = "application/json;charset=" + encode.ToUpper();
            if (method.ToUpper() == "POST")
            {
                string paraUrlCoded = jsonParam;
                byte[] payload;
                payload = Encoding.GetEncoding(encode.ToUpper()).GetBytes(paraUrlCoded);
                request.ContentLength = payload.Length;
                Stream writer = request.GetRequestStream();
                writer.Write(payload, 0, payload.Length);
                writer.Close();
            }           
            var response = request.GetResponse();
            var s = response.GetResponseStream();
            string StrDate = "";
            string strValue = "";
            StreamReader Reader = new StreamReader(s, Encoding.GetEncoding(encode.ToUpper()));
            while ((StrDate = Reader.ReadLine()) != null)
            {
                strValue += StrDate + "\r\n";
            }
            return strValue;
        }
    }
 
}
