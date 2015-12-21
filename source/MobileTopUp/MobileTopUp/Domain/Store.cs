using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using MobileTopUp.Models;
using MobileTopUp.Configuration;
using MobileTopUp.Utilities;
using Senparc.Weixin.MP.AdvancedAPIs.OAuth;
using System.Text;

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
            if (Store.Configuration.FakeLogin && Store.Configuration.Administrators.Count > 0)
            {
                openid = Store.Configuration.Administrators[0].WechatId;
                Store.BizInfo("AUTH", null, string.Format("fake login set open id={0}", openid));
            }
            else if (!string.IsNullOrEmpty(code))
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
                if (userInfo == null)
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
                    Store.BizInfo("AUTH", null, string.Format("can not create account open {0}:{1}", openid, name));
                    return null;
                }

                Store.BizInfo("AUTH", account.ID, string.Format("new account created {0}:{1}", openid, name));
            }

            return account;
        }

        public static Transaction GetTransactionByConsumer(Account consumer, int transactionId)
        {
            Store.BizInfo("TRANS", null, string.Format("start to get transaction {0} of id={1}", transactionId, consumer.ID));
            Transaction trans = null;
            using (StoreEntities db = new StoreEntities())
            {
                try
                {
                    trans = db.Transactions.Include(t => t.Consumer).Include(t => t.Vouchers)
                                .FirstOrDefault(t => t.ID == transactionId && t.AccountID == consumer.ID);

                    Store.BizInfo("TRANS", null, string.Format("success to get transaction {0} of id={1}", transactionId, consumer.ID));
                }
                catch (Exception ex)
                {
                    Store.SysError("TRANS", string.Format("fail to get transaction {0} of id={1}", transactionId, consumer.ID), ex);
                }
            }

            return trans;
        }

        public static IList<Transaction> GetTransactionByConsumer(Account consumer)
        {
            Store.BizInfo("TRANS", null, string.Format("start to get transactions of id={0}", consumer.ID));
            List<Transaction> transList = new List<Transaction>();
            using (StoreEntities db = new StoreEntities())
            {
                try
                {
                    var transactions = db.Transactions.Include(t => t.Vouchers).Where(t => t.AccountID == consumer.ID).OrderByDescending(t => t.OrderDate);
                    foreach (Transaction t in transactions)
                    {
                        transList.Add(t);
                    }
                    Store.BizInfo("TRANS", null, string.Format("success to get transactions of id={0}", consumer.ID));
                }
                catch (Exception ex)
                {
                    Store.SysError("TRANS", string.Format("fail to get transactions of id={0}", consumer.ID), ex);
                }
            }

            return transList;
        }

        public static void NotifyAdministrators(string message)
        {

            string[] adminWechatIds = Store.Configuration.Administrators.GetAllWechatIds();
            Store.BizInfo("NOTIFY", null, string.Format("start to notify admin:{0}",message));
            WechatHelper.SendMessageAsync(adminWechatIds, message);
        }

        struct LowLevelNotification
        {
            public string Brand;
            public int Stock;
            public bool DoesNotify;
            public bool IsOptional;
        }
        public static void NotifyVoucherChanges(VoucherStatistic previous, VoucherStatistic current)
        {
            //low stock
            if (!Store.Configuration.DoesNotifyLowStock)
            {
                return;
            }
            Store.BizInfo("NOTIFY", null, "start to create notification");
            LowLevelNotification[] notifications = new LowLevelNotification[4];
            notifications[0] = GenerateNotificationByBrand(BrandType.Spark, previous?.SparkStatistic, current?.SparkStatistic);
            notifications[1] = GenerateNotificationByBrand(BrandType.Vodafone, previous?.VodafoneStatistic, current?.VodafoneStatistic);
            notifications[2] = GenerateNotificationByBrand(BrandType.TwoDegrees, previous?.TwoDegreesStatistic, current?.TwoDegreesStatistic);
            notifications[3] = GenerateNotificationByBrand(BrandType.Skinny, previous?.SkinnyStatistic, current?.SkinnyStatistic);
            bool hasMadantory = false;
            foreach(LowLevelNotification n in notifications)
            {
                hasMadantory = n.DoesNotify && !n.IsOptional;
                if (hasMadantory)
                {
                    break;
                }
            }

            if (!hasMadantory)
            {
                Store.BizInfo("NOTIFY", null, "no madantory notification");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("[LOW STOCK LEVEL]");
            foreach (LowLevelNotification n in notifications)
            {
                if (n.DoesNotify)
                {
                    sb.Append("\n").Append(n.Brand).Append(":").Append(n.Stock);
                }
            }

            NotifyAdministrators(sb.ToString());
        }

        private static LowLevelNotification GenerateNotificationByBrand(BrandType brand, BrandStatistic previous, BrandStatistic current)
        {
            LowLevelNotification n = new LowLevelNotification()
            {
                Brand = brand.Value,
                Stock = current.AvailableCount,
                DoesNotify = current == null ? false : current.AvailableCount <= Store.Configuration.LowStockLevel,
                IsOptional = previous == null ? false : current.AvailableCount >= previous.AvailableCount
  
            };

            return n;
        }
    }
}