/*----------------------------------------------------------------
    Copyright (C) 2017 Senparc

    文件名：CustomMessageHandler.cs
    文件功能描述：微信公众号自定义MessageHandler


    创建标识：Senparc - 20150312

    修改标识：Senparc - 20171027
    修改描述：v14.8.3 添加OnUnknownTypeRequest()方法Demo

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Senparc.Weixin.MP.Agent;
using Senparc.Weixin.Context;
using Senparc.Weixin.Exceptions;
using Senparc.Weixin.Helpers;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageHandlers;
using Senparc.Weixin.MP.Helpers;
using System.Xml.Linq;
using Senparc.Weixin.MP.AdvancedAPIs;
using System.Threading.Tasks;
using Senparc.Weixin.Entities.Request;
using System.Web.Configuration;
using System.Web;
using System.Configuration;




namespace marketing.Controllers
{
    /// <summary>
    /// 自定义MessageHandler
    /// 把MessageHandler作为基类，重写对应请求的处理方法
    /// </summary>
    public partial class CustomMessageHandler : MessageHandler<CustomMessageContext>
    {

        /// <summary>
        /// 模板消息集合（Key：checkCode，Value：OpenId）
        /// </summary>
        public static Dictionary<string, string> TemplateMessageCollection = new Dictionary<string, string>();

        public CustomMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount = 0)
            : base(inputStream, postModel, maxRecordCount)
        {

        }

        public CustomMessageHandler(RequestMessageBase requestMessage)
            : base(requestMessage)
        {
        }

        /// <summary>
        /// 通过二维码扫描关注扫描事件
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        public override IResponseMessageBase OnEvent_ScanRequest(RequestMessageEvent_Scan requestMessage)
        {
            try
            {
                LogHelper.LogInfo(string.Format("二维码扫描关注事件【FromUserName:{0}, Event:{1}, EventKey:{2}, Ticket:{3}】", requestMessage.FromUserName, requestMessage.Event, requestMessage.EventKey, requestMessage.Ticket));
                var tbevent = new data.tbevent();
                tbevent.ticket = requestMessage.Ticket;
                tbevent.@event = requestMessage.Event.ToString();
                tbevent.from_username = requestMessage.FromUserName;
                tbevent.to_username = requestMessage.ToUserName;
                tbevent.msg_type = requestMessage.MsgType.ToString();
                tbevent.create_time = DateTime.Now.Ticks;
                tbevent.event_key = requestMessage.EventKey;
                using (var db = data.Entities.NewInstance)
                {
                    db.tbevents.Add(tbevent);
                    db.SaveChanges();
                }
            }
            catch (Exception es)
            {
                LogHelper.LogError(es);
            }
            

            if (!string.IsNullOrEmpty(requestMessage.EventKey))
            {
                var sceneId = requestMessage.EventKey.Replace("qrscene_", "");
                
            }
            return base.OnEvent_ScanRequest(requestMessage);
        }

        /// <summary>
        /// 订阅（关注）事件
        /// </summary>
        /// <returns></returns>
        public override IResponseMessageBase OnEvent_SubscribeRequest(RequestMessageEvent_Subscribe requestMessage)
        {
            try
            {
                LogHelper.LogInfo(string.Format("订阅（关注）事件【FromUserName:{0}, Event:{1}, EventKey:{2}, Ticket:{3}】", requestMessage.FromUserName, requestMessage.Event, requestMessage.EventKey, requestMessage.Ticket));
                var tbevent = new data.tbevent();
                tbevent.ticket = requestMessage.Ticket;
                tbevent.@event = requestMessage.Event.ToString();
                tbevent.from_username = requestMessage.FromUserName;
                tbevent.to_username = requestMessage.ToUserName;
                tbevent.msg_type = requestMessage.MsgType.ToString();
                tbevent.create_time = DateTime.Now.Ticks;
                tbevent.event_key = requestMessage.EventKey;
                using (var db = data.Entities.NewInstance)
                {
                    db.tbevents.Add(tbevent);
                    db.SaveChanges();
                }
            }
            catch (Exception es)
            {
                LogHelper.LogError(es);
            }

            if (requestMessage.EventKey.StartsWith("qrscene_"))
            {
                var sceneId =requestMessage.EventKey.Replace("qrscene_", "");
            }

            return base.OnEvent_SubscribeRequest(requestMessage);
        }

        /// <summary>
        /// 退订
        /// 实际上用户无法收到非订阅账号的消息，所以这里可以随便写。
        /// unsubscribe事件的意义在于及时删除网站应用中已经记录的OpenID绑定，消除冗余数据。并且关注用户流失的情况。
        /// </summary>
        /// <returns></returns>
        public override IResponseMessageBase OnEvent_UnsubscribeRequest(RequestMessageEvent_Unsubscribe requestMessage)
        {
            try
            {
                LogHelper.LogInfo(string.Format("退订事件【FromUserName:{0}, Event:{1}】", requestMessage.FromUserName, requestMessage.Event));
                using (var db = data.Entities.NewInstance)
                {
                    var tbevent = db.tbevents.Where(p => p.from_username == requestMessage.FromUserName).OrderByDescending(p => p.create_time).FirstOrDefault();
                    if (tbevent != null)
                    {
                        tbevent.unsubscribe_time = DateTime.Now.Ticks;
                    }
                    db.SaveChanges();
                }
            }
            catch (Exception es)
            {
                LogHelper.LogError(es);
            }
            return base.OnEvent_UnsubscribeRequest(requestMessage);;
        }

        public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
        {
            LogHelper.LogInfo(string.Format("消息【FromUserName:{0},MsgType:{1}】", requestMessage.FromUserName, requestMessage.MsgType));
            var response = this.CreateResponseMessage<ResponseMessageText>();
            //response.Content = DateTime.Now.ToString();
            return response;
        }

    }
}
