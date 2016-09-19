using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MobileTopUp.Models;
using System.Linq;
namespace MobileTopUp.Tests
{
    [TestClass]
    public class UnitTestModel
    {
        private const string ACCOUNT_SOURCE_REF_ID = "UNIT_TEST_ID";
        private const string ACCOUNT_NAME = "UNIT_TEST";

        private const string TRANS_PAY_REF = "UNIT_PAY_REF";

        [TestMethod]
        public void TestAccountModel()
        {
            using (StoreEntities store = new StoreEntities())
            {
                //create
                Account user = new Account
                {
                    Name = ACCOUNT_NAME,
                    ReferenceID = ACCOUNT_SOURCE_REF_ID,
                    Type = AccountType.Wechat
                };
                store.Accounts.Add(user);
                store.SaveChanges();
                int id = user.ID;
                Assert.AreNotEqual(0, id);

                //verify create
                user = null;
                user = store.Accounts.FirstOrDefault(x => x.ReferenceID.Equals(ACCOUNT_SOURCE_REF_ID));
                Assert.IsNotNull(user);
                Assert.AreEqual(id, user.ID);
                Assert.AreEqual(AccountType.Wechat, user.Type);
                Assert.AreEqual(ACCOUNT_SOURCE_REF_ID, user.ReferenceID);
                Assert.AreEqual(ACCOUNT_NAME, user.Name);

                //update
                user.Type = AccountType.Wechat;
                user.ReferenceID = "NEW_ID";
                user.Name = "NEW_NAME";
                store.Entry(user).State = System.Data.Entity.EntityState.Modified;
                store.SaveChanges();
                //verify update
                user = null;
                user = store.Accounts.Find(id);
                Assert.IsNotNull(user);
                Assert.AreEqual(AccountType.Wechat, user.Type);
                Assert.AreEqual("NEW_ID", user.ReferenceID);
                Assert.AreEqual("NEW_NAME", user.Name);

                //delete
                store.Accounts.Remove(user);
                store.SaveChanges();

                //verify delete
                user = null;
                user = store.Accounts.Find(id);
                Assert.IsNull(user);
            }
        }

        [TestMethod]
        public void TestVoucherModel()
        {
            using (StoreEntities store = new StoreEntities())
            {
                #region create
                //create account
                Account user = new Account
                {
                    Name = ACCOUNT_NAME,
                    ReferenceID = ACCOUNT_SOURCE_REF_ID,
                    Type = AccountType.Wechat
                };
                store.Accounts.Add(user);
                store.SaveChanges();
                Assert.AreNotEqual(0, user.ID);

                //new voucher
                string vNo = DateTime.Now.ToString("yyyy-MMdd-HHmm-ssffffff");
                DateTime createdTime = DateTime.Now;
                Voucher v = new Voucher
                {
                    Brand = BrandType.Spark,
                    Denomination = VoucherType.Twenty,
                    TopUpNumber = vNo,
                    SerialNumber = vNo.Substring(1),
                    Creator = user,
                    //Image = System.IO.File.ReadAllBytes(@"../voucher_sample.jpg"),
                    CreatedDate = createdTime
                };
                store.Vouchers.Add(v);
                store.SaveChanges();
                int id = v.ID;
                Assert.AreNotEqual(0, id);

                //verify create
                v = null;
                v = store.Vouchers.FirstOrDefault(x => x.SerialNumber.Equals(vNo));
                Assert.IsNotNull(v);
                Assert.AreEqual(id, v.ID);
                Assert.AreEqual(BrandType.Spark, v.Brand);
                Assert.AreEqual(VOUCHER_DENOMINATION, v.Denomination);
                Assert.AreEqual(vNo, v.Number);
                Assert.AreEqual(vNo.Substring(1), v.SerialNumber);
                Assert.AreEqual(user, v.Creator);
                //Assert.IsNotNull(v.Image);
                Assert.AreEqual(createdTime, v.CreatedDate);
                #endregion

                #region update
                //update
                Account newUser = new Account
                {
                    Name = "NEW_ACCOUNT",
                    ReferenceID = ACCOUNT_SOURCE_REF_ID,
                    Type = AccountType.Wechat
                };
                store.Accounts.Add(newUser);
                v.Creator = newUser;
                v.Brand = BrandType.Vodafone;
                v.Denomination = 100;
                vNo = DateTime.Now.ToString("yyyy-MMdd-HHmm-ssffffff");
                v.Number = vNo;
                v.SerialNumber = vNo.Substring(1);
                createdTime = DateTime.Now;
                v.CreatedDate = createdTime;
                store.SaveChanges();

                //verify update
                v = null;
                v = store.Vouchers.Find(id);
                Assert.IsNotNull(v);
                Assert.AreEqual(id, v.ID);
                Assert.AreEqual(BrandType.Vodafone, v.Brand);
                Assert.AreEqual(100, v.Denomination);
                Assert.AreEqual(vNo, v.Number);
                Assert.AreEqual(vNo.Substring(1), v.SerialNumber);
                Assert.AreEqual(newUser.ID, v.Creator.ID);
                //Assert.IsNotNull(v.Image);
                Assert.AreEqual(createdTime, v.CreatedDate);
                #endregion

                #region
                //delete
                store.Vouchers.Remove(v);
                store.Accounts.Remove(user);
                store.Accounts.Remove(newUser);
                store.SaveChanges();

                //verify delete
                v = null;
                v = store.Vouchers.Find(id);
                Assert.IsNull(v);
                #endregion
            }
        }

        [TestMethod]
        public void TestTransactionModel()
        {
            using (StoreEntities store = new StoreEntities())
            {
                #region create
                //create creator
                Account creator = new Account
                {
                    Name = ACCOUNT_NAME,
                    ReferenceID = ACCOUNT_SOURCE_REF_ID,
                    Type = AccountType.Wechat
                };

                //create voucher
                string vNo = DateTime.Now.ToString("yyyy-MMdd-HHmm-ssffffff");
                DateTime createdTime = DateTime.Now;
                Voucher v = new Voucher
                {
                    Brand = BrandType.Spark,
                    Denomination = VOUCHER_DENOMINATION,
                    Number = vNo,
                    Creator = creator,
                    Image = System.IO.File.ReadAllBytes(@"../voucher_sample.jpg"),
                    CreatedDate = createdTime
                };

                //create consumer
                Account consumer = new Account
                {
                    Name = "CONSUMER",
                    ReferenceID = ACCOUNT_SOURCE_REF_ID,
                    Type = AccountType.Wechat
                };

                //create Transaction
                DateTime orderTime = DateTime.Now;
                decimal ttlDenomination = v.Denomination * EXCHANGE_RATE_NZD;
                decimal selling = ttlDenomination * DISCOUNT;

                Transaction tran = new Transaction
                {
                    Consumer = consumer,
                    PaymentType = PaymentType.Skip,
                    PaymentRef = TRANS_PAY_REF,
                    Currency = CurrencyType.NZD,
                    ExchangeRate = EXCHANGE_RATE_NZD,
                    Brand = BrandType.Spark,
                    Quantity = 1,
                    TotalDenomination = ttlDenomination,
                    SellingPrice = selling,
                    OrderDate = orderTime
                };
                v.Consumer = consumer;
                tran.Vouchers.Add(v);
                store.Transactions.Add(tran);
                store.SaveChanges();
                int id = tran.ID;
                Assert.AreNotEqual(0, id);

                //verify create
                tran = null;
                tran = store.Transactions.FirstOrDefault(x => x.ID == id);
                Assert.IsNotNull(tran);
                Assert.AreEqual(consumer, tran.Consumer);
                Assert.AreEqual(PaymentType.Skip, tran.PaymentType);
                Assert.AreEqual(TRANS_PAY_REF, tran.PaymentRef);
                Assert.AreEqual(CurrencyType.NZD, tran.Currency);
                Assert.AreEqual(EXCHANGE_RATE_NZD, tran.ExchangeRate);
                Assert.AreEqual(BrandType.Spark, tran.Brand);
                Assert.AreEqual(1, tran.Quantity);
                Assert.AreEqual(ttlDenomination, tran.TotalDenomination);
                Assert.AreEqual(selling, tran.SellingPrice);
                Assert.AreEqual(orderTime, tran.OrderDate);
                Assert.AreEqual(1, tran.Vouchers.Count);
                Assert.IsNotNull(tran.Vouchers.FirstOrDefault(x => x.Equals(v)));
                #endregion

                #region update
                //new voucher
                vNo = DateTime.Now.ToString("yyyy-MMdd-HHmm-ssffffff");
                createdTime = DateTime.Now;
                Voucher newV1 = new Voucher
                {
                    Brand = BrandType.Skinny,
                    Denomination = VOUCHER_DENOMINATION,
                    Number = vNo + "-1",
                    Creator = creator,
                    Image = System.IO.File.ReadAllBytes(@"../voucher_sample.jpg"),
                    CreatedDate = createdTime
                };

                //new voucher
                createdTime = DateTime.Now;
                Voucher newV2 = new Voucher
                {
                    Brand = BrandType.Skinny,
                    Denomination = VOUCHER_DENOMINATION,
                    Number = vNo + "-2",
                    Creator = creator,
                    Image = System.IO.File.ReadAllBytes(@"../voucher_sample.jpg"),
                    CreatedDate = createdTime
                };

                //new consumer
                Account newConsumer = new Account
                {
                    Name = "NEW CONSUMER",
                    ReferenceID = ACCOUNT_SOURCE_REF_ID,
                    Type = AccountType.Wechat
                };

                //update
                orderTime = DateTime.Now;
                DateTime paidTime = orderTime.AddMinutes(1);
                ttlDenomination = newV1.Denomination + newV2.Denomination * EXCHANGE_RATE_CNY;
                selling = ttlDenomination * DISCOUNT;

                tran.Consumer = newConsumer;
                tran.PaymentType = PaymentType.PxPay;
                tran.PaymentRef = "NEW_PAY_REF";
                tran.Currency = CurrencyType.CNY;
                tran.ExchangeRate = EXCHANGE_RATE_CNY;
                tran.Brand = BrandType.Skinny;
                tran.Quantity = 2;
                tran.TotalDenomination = ttlDenomination;
                tran.SellingPrice = selling;
                tran.OrderDate = orderTime;
                tran.PaidDate = paidTime;

                newV1.Consumer = newConsumer;
                newV1.IsSold = true;
                newV2.Consumer = newConsumer;
                newV2.IsSold = true;
                tran.Vouchers.Remove(v);
                tran.Vouchers.Add(newV1);
                tran.Vouchers.Add(newV2);
                store.Entry(tran).State = System.Data.Entity.EntityState.Modified;
                store.SaveChanges();

                //verify upate
                tran = null;
                tran = store.Transactions.Find(id);
                Assert.IsNotNull(tran);
                Assert.AreEqual(newConsumer, tran.Consumer);
                Assert.AreEqual(PaymentType.PxPay, tran.PaymentType);
                Assert.AreEqual("NEW_PAY_REF", tran.PaymentRef);
                Assert.AreEqual(CurrencyType.CNY, tran.Currency);
                Assert.AreEqual(EXCHANGE_RATE_CNY, tran.ExchangeRate);
                Assert.AreEqual(BrandType.Skinny, tran.Brand);
                Assert.AreEqual(2, tran.Quantity);
                Assert.AreEqual(ttlDenomination, tran.TotalDenomination);
                Assert.AreEqual(selling, tran.SellingPrice);
                Assert.AreEqual(orderTime, tran.OrderDate);
                Assert.AreEqual(paidTime, tran.PaidDate);
                Assert.AreEqual(2, tran.Vouchers.Count);
                Assert.IsNotNull(tran.Vouchers.FirstOrDefault(x => x.Equals(newV1)));
                Assert.IsNotNull(tran.Vouchers.FirstOrDefault(x => x.Equals(newV2)));
                #endregion

                #region delete
                
                using (var dbTran = store.Database.BeginTransaction())
                {
                    store.Vouchers.Remove(v);
                    store.Vouchers.Remove(newV1);
                    store.Vouchers.Remove(newV2);
                    store.Transactions.Remove(tran);
                    store.Accounts.Remove(creator);
                    store.Accounts.Remove(consumer);
                    store.Accounts.Remove(newConsumer);
                    store.SaveChanges();
                    dbTran.Commit();
                }

                //verify
                tran = null;
                tran = store.Transactions.Find(id);
                Assert.IsNull(tran);
                
                #endregion
            }
        }
    }
}
