using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MobileTopUp.Models;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace MobileTopUp.Tests
{
    [TestClass]
    public class UnitTestBiz
    {
        [TestInitialize]
        public void ClearTestData()
        {
            using (var db = new StoreEntities())
            {
                db.Database.ExecuteSqlCommand("DELETE FROM t_voucher WHERE Denomination = 19");                
                db.Database.ExecuteSqlCommand("DELETE FROM t_transaction WHERE PaymentRef='UNIT_TEST'");
                db.Database.ExecuteSqlCommand("DELETE FROM t_account WHERE ReferenceID='UNIT_TEST_ID'");
            }
        }

        private static string GenerateRandomNumber()
        {
            Random random = new Random();
            var number = "";
            for (int j = 0; j < 2; j++)
            {
                number += string.Format("{0:000000}", random.Next(0, 999999));
            }

            return number;
        }

        [TestMethod]
        public void TestPurchaseOneVoucher()
        {
            //create account
            Account account = AccountManager.CreateAccount(AccountType.Wechat, "UNIT_TEST_ID", "UNIT_TEST");
            
            
            //create voucher
            VoucherManager mnger = new VoucherManager(account);
            Voucher v1 = new Voucher();
            v1.Brand = BrandType.Spark;
            v1.Denomination = VoucherType.Twenty;
            v1.SerialNumber = GenerateRandomNumber();
            v1.TopUpNumber = GenerateRandomNumber();
            Voucher newV = VoucherManager.FindBySerialOrTopupNumber(v1.Brand, v1.SerialNumber, v1.TopUpNumber);
            Assert.AreEqual(newV.Brand, v1.Brand);
            Assert.AreEqual(newV.SerialNumber, v1.SerialNumber);
            Assert.AreEqual(newV.TopUpNumber, v1.TopUpNumber);

            //order
            Transaction t1 = new Transaction();
            t1.Consumer = account;
            t1.PaymentType = PaymentType.PxPay;
            t1.Currency = CurrencyType.NZD;
            t1.ExchangeRate = 1.0M;
            t1.Brand = BrandType.Spark;
            t1.Quantity = 1;
            t1.TotalDenomination = 19;
            t1.SellingPrice = 18.5M;
            VoucherManager.Hold(t1);
            Assert.AreNotEqual(0, t1.ID);

            //another order but not enought voucher
            Transaction t2 = new Transaction();
            t2.Consumer = account;
            t2.PaymentType = PaymentType.WechatPay;
            t2.Currency = CurrencyType.NZD;
            t2.ExchangeRate = 1.0M;
            t2.Brand = BrandType.Spark;
            t2.Quantity = 1;
            t2.TotalDenomination = 20;
            t2.SellingPrice = 18.5M;
            VoucherManager.Hold(t1);
            Assert.AreEqual(0, t2.ID);

            //pay-sold
            t1.PaymentRef = "UNIT_TEST";
            //VoucherManager.Sold(t1);

            //get account
            Account aResult = AccountManager.GetAccountById(AccountType.Wechat, "UNIT_TEST_ID");

            //verify
            using (var db = new StoreEntities())
            {
                //transaction
                db.Accounts.Attach(aResult);
                var count = db.Entry(aResult).Collection(a => a.Transactions).Query().Count();
                Assert.AreEqual(1, aResult.Transactions.Count());

                Transaction tResult = aResult.Transactions.First();
                Assert.IsNotNull(tResult.PaidDate);

                //voucher
                Assert.AreEqual(1, tResult.Vouchers.Count());

                Voucher vResult = tResult.Vouchers.First();
                Assert.AreEqual(aResult.ID, vResult.AccountID);
                Assert.AreEqual(true, vResult.IsSold);
            }
        }

        [TestMethod]
        public void TestPurchaseMultiVoucher()
        {
            int numVoucher = 5;
            //create account
            Account account = AccountManager.CreateAccount(AccountType.Wechat, "UNIT_TEST_ID", "UNIT_TEST");

            //create voucher
            VoucherManager mnger = new VoucherManager(account);
            for (int i = 0; i < numVoucher; i++)
            {
                Voucher v = new Voucher();
                v.Brand = BrandType.Vodafone;
                v.Denomination = VoucherType.Twenty;
                v.SerialNumber = GenerateRandomNumber();
                v.TopUpNumber = GenerateRandomNumber();
                mnger.Add(v);
            }

            //order
            Transaction t1 = new Transaction();
            t1.Consumer = account;
            t1.PaymentType = PaymentType.PxPay;
            t1.Currency = CurrencyType.NZD;
            t1.ExchangeRate = 1.0M;
            t1.Brand = BrandType.Vodafone;
            t1.Quantity = 5;
            t1.TotalDenomination = 20 * t1.Quantity;
            t1.SellingPrice = t1.TotalDenomination * 0.925M;
            VoucherManager.Hold(t1);
            Assert.AreNotEqual(0, t1.ID);

            //another order but not enought voucher
            Transaction t2 = new Transaction();
            t2.Consumer = account;
            t2.PaymentType = PaymentType.Skip;
            t2.Currency = CurrencyType.NZD;
            t2.ExchangeRate = 1.0M;
            t2.Brand = BrandType.Vodafone;
            t2.Quantity = 1;
            t2.TotalDenomination = 20;
            t2.SellingPrice = 18.5M;
            VoucherManager.Hold(t2);
            Assert.AreEqual(0, t2.ID);

            //pay-sold
            t1.PaymentRef = "UNIT_TEST";
            //VoucherManager.Sold(t1);


            //verify
            //get account
            Account aResult = AccountManager.GetAccountById(AccountType.Wechat, "UNIT_TEST_ID");
            using (var db = new StoreEntities())
            {
                //transaction
                IEnumerable<Transaction> tResults = db.Transactions.Where(x => x.AccountID == aResult.ID);
                Assert.AreEqual(1, tResults.Count());

                Transaction tResult = tResults.First();
                Assert.IsNotNull(tResult.PaidDate);

                //voucher
                IEnumerable<Voucher> vResults = db.Vouchers.Where(x => x.TransactionID == tResult.ID);
                Assert.AreEqual(numVoucher, vResults.Count());
                foreach (Voucher vResult in vResults)
                {
                    Assert.AreEqual(aResult.ID, vResult.AccountID);
                    Assert.AreEqual(true, vResult.IsSold);
                }
            }
        }
    }
}
