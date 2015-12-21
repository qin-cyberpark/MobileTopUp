using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Configuration;
using Senparc.Weixin.MP.TenPayLibV3;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.AdvancedAPIs.OAuth;
using Senparc.Weixin.MP.AdvancedAPIs.Media;
using Senparc.Weixin.Entities;
using MobileTopUp.Configuration;
using System.Threading;
using System.Web.Hosting;

namespace MobileTopUp.Utilities
{
    public class WechatHelper
    {
        private static readonly log4net.ILog _sysLogger = log4net.LogManager.GetLogger("SysLogger");

        private static readonly string _appId = StoreConfiguration.Instance.Wechat.Id;
        private static readonly string _appSecret = StoreConfiguration.Instance.Wechat.Key;
        private static readonly string _tempFolder = StoreConfiguration.Instance.TemporaryDirectory;

        public static bool CheckSignature(string signature, string timestamp, string nonce)
        {
            string token = "qLiFe2015";

            List<string> list = new List<string>();
            list.Add(token);
            list.Add(timestamp);
            list.Add(nonce);
            list.Sort();

            list.Sort();

            string res = string.Join("", list.ToArray());


            Byte[] data1ToHash = Encoding.ASCII.GetBytes(res);
            byte[] hashvalue1 = ((HashAlgorithm)CryptoConfig.CreateFromName("SHA1")).ComputeHash(data1ToHash);

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashvalue1)
            {
                sb.Append(b.ToString("x2"));
            }

            return signature == sb.ToString();
        }

        /// <summary>
        /// get open id
        /// </summary>
        /// <param name="code">code from wexin</param>
        /// <param name="userInfoAccessToken">token using to get userInfo</param>
        /// <returns></returns>
        public static string GetOpenID(string code, out string userInfoAccessToken)
        {
            _sysLogger.Info(string.Format("[WX]start to get open ID by code {0}", code));
            try
            {
                OAuthAccessTokenResult result = OAuthApi.GetAccessToken(_appId, _appSecret, code);
                if (string.IsNullOrEmpty(result.access_token))
                {
                    userInfoAccessToken = null;
                    return null;
                }
                userInfoAccessToken = result.access_token;
                _sysLogger.Info(string.Format("[WX]succeed to get open ID {0}, token {1}", result.openid, result.access_token));
                return result.openid;
            }
            catch (Exception ex)
            {
                _sysLogger.Error(string.Format("[WX]failed to get open ID by code {0}", code), ex);
                userInfoAccessToken = null;
                return null;
            }

        }

        /// <summary>
        /// get userInfo
        /// </summary>
        /// <param name="token"></param>
        /// <param name="openID"></param>
        /// <returns></returns>
        public static OAuthUserInfo GetUserInfo(string token, string openID)
        {
            _sysLogger.Info(string.Format("[WX]start to get user info by open id {0}", openID));
            try
            {
                return OAuthApi.GetUserInfo(token, openID, Senparc.Weixin.Language.en);
            }
            catch (Exception ex)
            {
                _sysLogger.Error(string.Format("[WX]failed to get user info by open id {0}", openID), ex);
                return null;
            }
        }

        /// <summary>
        /// send image to openid
        /// </summary>
        /// <param name="openid"></param>
        /// <param name="fileByte"></param>
        /// <returns></returns>
        public static bool SendImage(string openid, byte[] imageBytes, int maxAttamptTime = 3)
        {
            _sysLogger.Info(string.Format("[WX]start to sent image to {0}", openid));
            string tempFile = _tempFolder + openid + ".jpg";
            System.IO.File.WriteAllBytes(tempFile, imageBytes);
            UploadTemporaryMediaResult updateRst = null;
            WxJsonResult sendRst = null;
            for (int i = 0; i < maxAttamptTime; i++)
            {
                _sysLogger.Info(string.Format("[WX]start to upload temporary image try {0}", i));
                updateRst = MediaApi.UploadTemporaryMedia(_appId, Senparc.Weixin.MP.UploadMediaFileType.image, tempFile);
                if (updateRst != null && updateRst.errcode == 0)
                {
                    break;
                }
            }
            if (updateRst == null || updateRst.errcode != 0)
            {
                _sysLogger.Info("[WX]faild to upload temporary image");
                return false;
            }

            for (int i = 0; i < maxAttamptTime; i++)
            {
                _sysLogger.Info(string.Format("[WX]start to send image to {0} try {1}", openid, i));
                sendRst = CustomApi.SendImage(_appId, openid, updateRst.media_id);
                if (sendRst != null && sendRst.errcode == 0)
                {
                    break;
                }
            }
            if (sendRst == null || sendRst.errcode != 0)
            {
                _sysLogger.Info("[WX]faild to send image");
                return false;
            }

            return true;
        }
        public static void SendImages(string openid, byte[][] imageBytes)
        {
            if (imageBytes == null)
            {
                return;
            }

            _sysLogger.Info(string.Format("[WX]start to sent images{0} to {1}", imageBytes.Length, openid));
            foreach (byte[] bytes in imageBytes)
            {
                SendImage(openid, bytes);
            }
            _sysLogger.Info(string.Format("[WX]finish to sent images{0} to {1}", imageBytes.Length, openid));
        }

        public static void SendImageAsync(string openid, byte[] imageBytes)
        {
            if (imageBytes == null)
            {
                return;
            }
            _sysLogger.Info(string.Format("[WX]start thread to sent image to {0}", openid));
            ThreadStart starter = () => SendImage(openid, imageBytes);
            Thread thread = new Thread(starter);
            thread.Start();
        }

        public static void SendImagesAsync(string openid, byte[][] imageBytes)
        {
            if (imageBytes == null)
            {
                return;
            }
            _sysLogger.Info(string.Format("[WX]start thread to sent images{0} to {1}", imageBytes.Length, openid));
            ThreadStart starter = () => SendImages(openid, imageBytes);
            Thread thread = new Thread(starter);
            thread.Start();
        }

        /// <summary>
        /// send image to openid
        /// </summary>
        /// <param name="openid"></param>
        /// <param name="fileByte"></param>
        /// <returns></returns>
        public static bool SendMessage(string openid, string message, int maxAttamptTime = 3)
        {
            try
            {
                WxJsonResult sendRst = null;
                for (int i = 0; i < maxAttamptTime; i++)
                {
                    _sysLogger.Info(string.Format("[WX]send message to {0} try {1}", openid, i));
                    sendRst = CustomApi.SendText(_appId, openid, message);
                    if (sendRst != null && sendRst.errcode == 0)
                    {
                        break;
                    }
                }
                if (sendRst == null || sendRst.errcode != 0)
                {
                    _sysLogger.Info("[WX]faild to send message");
                    return false;
                }
                return true;
            }catch{
                return false;
            }
        }

        public static void SendMessage(string[] openids, string message)
        {
            if (openids == null || openids.Length == 0 || string.IsNullOrEmpty(message))
            {
                return;
            }

            foreach (string openid in openids)
            {
                SendMessage(openid, message);
            }
        }
        public static void SendMessage(string openid, string[] messages)
        {
            if (messages == null || messages.Length == 0)
            {
                return;
            }

            foreach(string msg in messages)
            {
                SendMessage(openid, msg);
            } 
        }

        public static void SendMessageAsync(string openid, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            _sysLogger.Info(string.Format("[WX]start thread to sent message to {0}", openid));
            HostingEnvironment.QueueBackgroundWorkItem(ct => SendMessage(openid, message));
        }

        public static void SendMessageAsync(string openid, string[] messages)
        {
            if (messages == null || messages.Length == 0)
            {
                return;
            }
            _sysLogger.Info(string.Format("[WX]start thread to sent messages to {0}", openid));
            HostingEnvironment.QueueBackgroundWorkItem(ct => SendMessage(openid, messages));
        }

        public static void SendMessageAsync(string[] openids, string message)
        {
            if (openids == null || openids.Length == 0 || string.IsNullOrEmpty(message))
            {
                return;
            }
            _sysLogger.Info(string.Format("[WX]start thread to sent message to openids"));
            HostingEnvironment.QueueBackgroundWorkItem(ct => SendMessage(openids, message));
        }
    }
}