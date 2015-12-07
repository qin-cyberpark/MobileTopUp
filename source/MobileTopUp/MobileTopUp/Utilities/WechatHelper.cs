using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Configuration;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.AdvancedAPIs.OAuth;

namespace MobileTopUp.Utilities
{
    public class WechatHelper
    {
        private static string _appId = WebConfigurationManager.AppSettings["WeixinAppId"];


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


        public void GetUserInfo()
        {
            UserApi.Info(_appId, "", Senparc.Weixin.Language.en);
        }

        public static string GetOpenID(string code, out string userInfoAccessToken)
        {

            OAuthAccessTokenResult result = OAuthApi.GetAccessToken(WebConfigurationManager.AppSettings["WeixinAppId"], WebConfigurationManager.AppSettings["WeixinAppSecret"], code);
            if (string.IsNullOrEmpty(result.access_token)) {
                userInfoAccessToken = null;
                return null;
            }

            userInfoAccessToken = result.access_token;
            return result.openid;
        }

        public static OAuthUserInfo GetUserInfo(string token, string openID)
        {
            return OAuthApi.GetUserInfo(token, openID, Senparc.Weixin.Language.en);
        }
    }
}