using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MobileTopUp.Models;
using System.IO;

namespace MobileTopUp.Tests
{
    [TestClass]
    public class UnitTestVoucher
    {
        private static Account _account;

        [ClassInitialize()]
        public static void Initialize(TestContext context)
        {
            //_account = AccountManager.GetAccountById(Store.AccountSources.Wechat, Store.Configuration.Administrators[0].WechatId);
        }

        [TestMethod]
        public void TestCreateVoucher()
        {
            Account a = AccountManager.GetAccountById(Store.AccountSources.Wechat, Store.Configuration.Administrators[0].WechatId);
            VoucherManager mnger = new VoucherManager(a);
            Voucher v = new Voucher();
            v.Brand = "SPARK";
            v.Denomination = 20;
            v.Image = File.ReadAllBytes(@"../voucher_2degree.jpg");
            mnger.Add(v);

            //verify
            Assert.AreNotEqual(0, v.ID);
            Voucher newV = VoucherManager.FindById(v.ID);
            Assert.AreEqual(newV.Brand, v.Brand);
            Assert.AreEqual(newV.Denomination, v.Denomination);
        }

        [TestMethod]
        public void TestCreateVouchers()
        {
            string folder = @"D:\works\Greenspot\MobileTopUp\design\voucher\";
            Random random = new Random();
            Voucher v;
            string brand = null;
            string fileName = null;
            using (StoreEntities db = new StoreEntities())
            {
                for (int i = 0; i < 200; i++)
                {
                    switch (random.Next(0, 4))
                    {
                        case 0:
                            brand = "SPARK";
                            fileName = "sample1.png";
                            break;
                        case 1:
                            brand = "VODAFONE";
                            fileName = "sample2.png";
                            break;
                        case 2:
                            brand = "2DEGREE";
                            fileName = "sample3.png";
                            break;
                        case 3:
                            brand = "SKINNY";
                            fileName = "sample4.png";
                            break;
                    }
                    v = new Voucher();
                    v.Brand = brand;
                    v.Denomination = 20;
                    v.Image = File.ReadAllBytes(folder + fileName);
                    v.CreatedBy = "TEST";
                    v.CreatedDate = DateTime.Now;
                    db.Vouchers.Add(v);
                }
                db.SaveChanges();
            }
        }
    }
}
