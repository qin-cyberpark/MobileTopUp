using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MobileTopUp.Models;

namespace MobileTopUp
{
    public class AccountManager
    {
        private static Dictionary<string, string> _adminis = new Dictionary<string, string>();
        public static Account GetAccountById(AccountType accountType, string id)
        {
            Store.SysInfo("ACCOUNT",string.Format("start to get account info by id {0}@{1}", id, accountType));
            try {
                Account account;
                using (StoreEntities db = new StoreEntities())
                {
                    account = db.Accounts.FirstOrDefault(x => x.Type.Value.Equals(accountType.Value) && x.ReferenceID.Equals(id));
                }

                Store.SysInfo("ACCOUNT",string.Format("succeed to got account info id {0}", account == null?"NULL":account.ID.ToString()));
                return account;
            }catch(Exception ex)
            {
                Store.SysError("[ACCOUNT]","failed to get account info", ex);
                return null;
            }
        }


        public static Account CreateAccount(AccountType accountType, string oriId, string name)
        {
            Store.SysInfo("[ACCOUNT]", string.Format("start to create account {0}:{1}@{2}", oriId, name, accountType));
            try {
                Account account = new Account
                {
                    Type = accountType,
                    ReferenceID = oriId,
                    Name = name
                };
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

        public static bool IsAdministrator(Account account)
        {
            if(account == null)
            {
                return false;
            }

            if(_adminis.Count == 0)
            {
                object[] keys = Store.Configuration.Administrators.GetAllKeys();
                foreach(object k in keys)
                {
                    string id = (string)k;
                    _adminis.Add(id, Store.Configuration.Administrators[id]);
                }
            }

            return _adminis.ContainsKey(account.ReferenceID);
        }
    }
}