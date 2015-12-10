using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MobileTopUp.Models;
using MobileTopUp.Configuration;
using MobileTopUp.Utilities;

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

#if DEBUG
            string openid = "opDxls3kxQNdVPqkKW4c8DAfDGX8";
#else
            string openid = WechatHelper.GetOpenID(code, out userInfoAccessToken);
#endif
            if (string.IsNullOrEmpty(openid))
            {
                //failed to get open id
                Store.BizInfo("AUTH", string.Format("can not get open id by code {0}", code));
                return null;
            }

            //got open id
            Store.BizInfo("AUTH", string.Format("got open id {0}", openid));

            //get customer
            Account account = AccountManager.GetAccountById(AccountSources.Wechat, openid);
            if (account == null)
            {
                //new account
                Store.BizInfo("AUTH", string.Format("new account, open id {0}", openid));

                //get name
                string name = WechatHelper.GetUserInfo(userInfoAccessToken, openid).nickname;
                if (string.IsNullOrEmpty(name))
                {
                    Store.BizInfo("AUTH", string.Format("can not get user name by open id {0}", openid));
                    return null;
                }

                Store.BizInfo("AUTH", string.Format("got user name {0}", name));

                //create account
                account = AccountManager.CreateAccount(AccountSources.Wechat, openid, name);
                if (account.ID == 0)
                {
                    Store.BizInfo("AUTH", string.Format("can not create account open {0}:{1}", openid, name));
                    return null;
                }

                Store.BizInfo("AUTH", string.Format("account created {0}:{1}:{2}", openid, name));
            }

            return account;
        }
    }
}