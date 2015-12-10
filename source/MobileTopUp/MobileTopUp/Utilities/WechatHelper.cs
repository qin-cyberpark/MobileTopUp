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
            try {
                OAuthAccessTokenResult result = OAuthApi.GetAccessToken(_appId, _appSecret, code);
                if (string.IsNullOrEmpty(result.access_token)) {
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
            try {
                return OAuthApi.GetUserInfo(token, openID, Senparc.Weixin.Language.en);
            }
            catch(Exception ex)
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
        public static bool SendImage(string openid, byte[] fileByte)
        {
            string tempFile = _tempFolder + openid + ".png";
            System.IO.File.WriteAllBytes(tempFile, fileByte);
            UploadTemporaryMediaResult updateRst = MediaApi.UploadTemporaryMedia(_appId, Senparc.Weixin.MP.UploadMediaFileType.image, tempFile);
            if (updateRst.errcode != 0)
            {
                return false;
            }

            WxJsonResult sendRst = CustomApi.SendImage(_appId, openid, updateRst.media_id);
            return sendRst.errcode == 0;
        }
    }
}