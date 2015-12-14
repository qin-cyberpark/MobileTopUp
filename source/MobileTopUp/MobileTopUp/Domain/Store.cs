using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MobileTopUp.Models;
using MobileTopUp.Configuration;
using MobileTopUp.Utilities;
using Senparc.Weixin.MP.AdvancedAPIs.OAuth;

namespace MobileTopUp
{
    public partial class Store
    {
        /// <summary>
        /// store configuration instance
        /// </summary>
        public static StoreConfiguration Configuration
        {
            get
            {
                return StoreConfiguration.Instance;
            }
        }

        /// <summary>
        /// get account by code
        /// 1.use the code to get token
        /// 2.use token to get openid
        /// 3.search accoutn by openid
        /// 4.create a new accout if not exist
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static Account GetAccountByWechatCode(string code)
        {
            //get openid
            string userInfoAccessToken = null;

            string openid = null;
            if (Store.Configuration.FakeLogin && Store.Configuration.Administrators.Count > 0) {
                openid = Store.Configuration.Administrators[0].WechatId;
                Store.BizInfo("AUTH", null, string.Format("fake login set open id={0}", openid));
            }
            else
            {
                openid = WechatHelper.GetOpenID(code, out userInfoAccessToken);
            }

            if (string.IsNullOrEmpty(openid))
            {
                //failed to get open id
                Store.BizInfo("AUTH", null, string.Format("can not get open id by code={0}", code));
                return null;
            }

            //got open id
            Store.BizInfo("AUTH", null, string.Format("got open id {0}", openid));
            //get customer
            Account account = AccountManager.GetAccountById(AccountType.Wechat, openid);
            if (account == null)
            {
                //get name
                OAuthUserInfo userInfo = WechatHelper.GetUserInfo(userInfoAccessToken, openid);
                if(userInfo == null)
                {
                    Store.BizInfo("AUTH", null, string.Format("can not get user info by open id={0}", openid));
                    return null;
                }

                if (string.IsNullOrEmpty(userInfo.nickname))
                {
                    Store.BizInfo("AUTH", null, string.Format("can not get user name in userinfo open id={0}", openid));
                    return null;
                }

                string name = userInfo.nickname;
                Store.BizInfo("AUTH", null, string.Format("got user name={0}", name));

                //create account
                account = AccountManager.CreateAccount(AccountType.Wechat, openid, name);
                if (account.ID == 0)
                {
                    Store.BizInfo("AUTH",null, string.Format("can not create account open {0}:{1}", openid, name));
                    return null;
                }

                Store.BizInfo("AUTH", account.ID, string.Format("new account created {0}:{1}", openid, name));
            }

            return account;
        }
    }
}