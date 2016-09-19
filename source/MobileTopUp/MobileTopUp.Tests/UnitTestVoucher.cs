using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MobileTopUp.Models;
using System.Linq;
using System.IO;

namespace MobileTopUp.Tests
{
    [TestClass]
    public class UnitTestVoucher
    {
        private Account _account;
        private static Random _random;

        [TestInitialize()]
        public void Initialize()
        {
            _account = AccountManager.GetAccountById(AccountType.Wechat, Store.Configuration.Administrators[0].WechatId);
            if(_account == null)
            {
                Assert.Fail("fail to get accout info");
            }
            _random = new Random();
        }

        private static string GenerateRandomNumber()
        {
            var number = "";
            for (int j = 0; j < 2; j++)
            {
                number += string.Format("{0:000000}", _random.Next(0, 999999));
            }

            return number;
        }

        [TestMethod]
        public void TestCreateVoucher()
        {
            string sn = GenerateRandomNumber();
            string topupNum = GenerateRandomNumber();
            VoucherManager mnger = new VoucherManager(_account);
            Voucher v = new Voucher();
            v.Brand = BrandType.Spark;

            v.SerialNumber = sn;
            v.TopUpNumber = topupNum;
            v.Denomination = 20;

            if (!mnger.Add(v)) {
                Assert.Fail("fail to add voucher");
            }

            //verify
            Voucher newV = VoucherManager.FindBySerialOrTopupNumber(v);
            Assert.AreEqual(newV.Brand, v.Brand);
            Assert.AreEqual(sn, v.SerialNumber);
            Assert.AreEqual(topupNum, v.TopUpNumber);
            Assert.AreEqual(newV.Denomination, v.Denomination);
        }

        [TestMethod]
        public void TestCreateVouchers()
        {
            Random random = new Random();

            Voucher v;
            BrandType brand = null;
            using (StoreEntities db = new StoreEntities())
            {
                int countBefore = db.Vouchers.Count();
                for (int i = 0; i < 20; i++)
                {
                    //number
                    switch (random.Next(0, 4))
                    {
                        case 0:
                            brand = BrandType.Spark;
                            break;
                        case 1:
                            brand = BrandType.Vodafone;
                            break;
                        case 2:
                            brand = BrandType.TwoDegrees;
                            break;
                        case 3:
                            brand = BrandType.Skinny;
                            break;
                    }
                    v = new Voucher
                    {
                        TopUpNumber = GenerateRandomNumber(),
                        SerialNumber = GenerateRandomNumber(),
                        Brand = brand,
                        Denomination = 20,
                        Creator = _account,
                        CreatedDate = DateTime.Now
                    };

                    db.Vouchers.Add(v);
                }
                db.SaveChanges();
                int countAfter = db.Vouchers.Count();
                Assert.AreEqual(countBefore + 20, countAfter);
            }
        }

        [TestMethod]
        public void TestGetStock()
        {
            var a = VoucherManager.GetStatistic();
        }
    }
}
