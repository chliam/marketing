using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageHandlers;
using Senparc.Weixin.MP.MvcExtension;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace marketing.Controllers
{
    public class WechatController : Controller
    {
        private static string token = WebConfigurationManager.AppSettings["WeixinToken"];
        private static string appId = WebConfigurationManager.AppSettings["WeixinAppId"];
        private static string appSecret = WebConfigurationManager.AppSettings["WeixinAppSecret"];

        // GET: Wechat
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [ActionName("Index")]
        public Task<ActionResult> Get(string signature, string timestamp, string nonce, string echostr)
        {
            LogHelper.LogInfo(string.Format("微信接口验证【signature:{0}，timestamp:{1}，nonce:{2}】", signature, timestamp, nonce));
            return Task.Factory.StartNew(() =>
            {
                if (CheckSignature.Check(signature, timestamp, nonce, token))
                {
                    return echostr; //返回随机字符串则表示验证通过
                }
                else
                {
                    return "failed:" + signature + "," + CheckSignature.GetSignature(timestamp, nonce, token) + "。" + "如果你在浏览器中看到这句话，说明此地址可以被作为微信公众账号后台的Url，请注意保持Token一致。";
                }
            }).ContinueWith<ActionResult>(task => Content(task.Result));
        }

        [HttpPost]
        [ActionName("Index")]
        public Task<ActionResult> Post(PostModel postModel)
        {
            return Task.Factory.StartNew<ActionResult>(() =>
            {
                if (!CheckSignature.Check(postModel.Signature, postModel.Timestamp, postModel.Nonce, token))
                {
                    return new WeixinResult("参数错误！");
                }

                postModel.Token = token;
                postModel.EncodingAESKey = appSecret; 
                postModel.AppId = appId; 

                var messageHandler = new CustomMessageHandler(Request.InputStream, postModel, 10);

                messageHandler.Execute(); //执行微信处理过程

                return new FixWeixinBugWeixinResult(messageHandler);

            }).ContinueWith<ActionResult>(task => task.Result);
        }
    }
}