using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MobileTopUp;
using MobileTopUp.Models;

namespace MobileTopUp.Tests.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            
            using (var db = new StoreEntities()) { 

                //Account a = new Account
                //{
                //    Type = AccountType.Wechat,
                //    ReferenceID = "ref_id",
                //    Name = "test"
                //};
                //db.Accounts.Add(a);
                //db.SaveChanges();

                Account a = db.Accounts.Find(48);
                System.Console.WriteLine(a.Type == AccountType.Unknown);
                System.Console.WriteLine(a.Type == AccountType.Wechat);

                AccountType typeNull = new AccountType();
                AccountType typeNull1 = new AccountType();
                System.Console.WriteLine(typeNull == AccountType.Unknown);
                System.Console.WriteLine(typeNull == AccountType.Wechat);
                System.Console.WriteLine(typeNull == typeNull1);

                typeNull.Value = "WECHAT";
                typeNull1 = AccountType.Wechat;
                System.Console.WriteLine(typeNull == typeNull1);
                System.Console.WriteLine(typeNull == AccountType.Unknown);
                System.Console.WriteLine(typeNull == AccountType.Wechat);
                System.Console.WriteLine(AccountType.Wechat == AccountType.Wechat);

            }
            System.Console.WriteLine("================="); //true
            PaymentType p1 = new PaymentType();
            PaymentType p2 = new PaymentType();
            System.Console.WriteLine(p1 == PaymentType.Unknown); //true
            System.Console.WriteLine(p1 == CustomEnum.Unknown); //true
            System.Console.WriteLine(p1 == p2);//true
            System.Console.WriteLine(p1 == AccountType.Unknown);//true

            p1.Value = "PXPAY";
            p2 = PaymentType.PxPay;
            System.Console.WriteLine(p1 == PaymentType.WechatPay);//false
            System.Console.WriteLine(p1 == PaymentType.PxPay);//true
            System.Console.WriteLine(p2 == p1);//true
            System.Console.WriteLine(PaymentType.PxPay == p2);//true


            System.Console.ReadLine();
        }
    }
}
