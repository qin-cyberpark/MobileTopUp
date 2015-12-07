using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MobileTopUp.Models;
using MobileTopUp.Utilities;
using System.IO;

namespace MobileTopUp.Tests
{
    [TestClass]
    public class UnitTestVoucher
    {
        private TopUpStore _store = new TopUpStore();

        [TestMethod]
        public void TestCreateVoucher()
        {
            Voucher v = new Voucher();
            v.Brand = "SPARK";
            v.Denomination = 20;
            v.Image = File.ReadAllBytes(@"D:\works\Greenspot\MobileTopUp\design\voucher\sample1.png");
            v.CreatedBy = "TEST";
            v.CreatedDate = DateTime.Now;
            _store.Vouchers.Add(v);
            _store.SaveChanges();
        }

        [TestMethod]
        public void TestCreateVouchers()
        {
            string folder = @"D:\works\Greenspot\MobileTopUp\design\voucher\";
            Random random = new Random();
            Voucher v;
            string brand = null;
            string fileName = null;
            for (int i = 0; i < 20; i++)
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
                v.Brand = "SKINNY";
                v.Denomination = 20;
                v.Image = File.ReadAllBytes(folder + fileName);
                v.CreatedBy = "TEST";
                v.CreatedDate = DateTime.Now;
                _store.Vouchers.Add(v);
            }
            _store.SaveChanges();
        }
    }
}
