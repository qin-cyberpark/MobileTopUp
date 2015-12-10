using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MobileTopUp.Models;

namespace MobileTopUp
{
    public class AccountManager
    {
        public static Account GetAccountById(Store.AccountSources source, string id)
        {
            Store.SysInfo("ACCOUNT",string.Format("start to get account info by id {0}@{1}", id, source));
            try {
                string accountSrc = Store.AccountSourceTypeToCode(source);
                Account account;
                using (StoreEntities db = new StoreEntities())
                {
                    account = db.Accounts.First(x => x.Type.Equals(accountSrc) && x.ReferenceID.Equals(id));
                }

                Store.SysInfo("ACCOUNT",string.Format("succeed to got account info id {0}", account == null?"NULL":account.ID.ToString()));
                return account;
            }catch(Exception ex)
            {
                Store.SysError("[ACCOUNT]","failed to get account info", ex);
                return null;
            }
        }

        public static Account CreateAccount(Store.AccountSources source, string oriId, string name)
        {
            Store.SysInfo("[ACCOUNT]", string.Format("start to create account {0}:{1}@{2}", oriId, name, source));
            try {
                Account account = new Account();
                account.Type = Store.AccountSourceTypeToCode(source);
                account.ReferenceID = oriId;
                account.Name = name;
                using (StoreEntities db = new StoreEntities())
                {
                    account = db.Accounts.Add(account);
                    db.SaveChanges();
                }
                Store.SysInfo("[ACCOUNT]", string.Format("succeed to create account id {0}", account == null ? "NULL" : account.ID.ToString()));
                return account;
            }catch(Exception ex)
            {
                Store.SysError("[ACCOUNT]","failed to create account", ex);
                return null;
            }
        }
    }
}