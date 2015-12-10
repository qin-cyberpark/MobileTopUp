using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MobileTopUp.Configuration;
using MobileTopUp.Utilities;

namespace MobileTopUp.Tests
{
    [TestClass]
    public class UnitTestConfig
    {
        [TestMethod]
        public void TestConfigReading()
        {
            StoreConfiguration cfg = StoreConfiguration.Instance;
            //payment
            Assert.AreEqual(false, cfg.Payment.IsFullCharge);
            Assert.AreEqual(0.9M, cfg.Payment.Discount);
            Assert.AreEqual(4.3, cfg.Payment.ExchangeRateCNY);

            //wechat
            Assert.AreEqual("wx8f0f03f3f09da028", cfg.Wechat.Id);
            Assert.AreEqual("d4624c36b6795d1d99dcf0547af5443d", cfg.Wechat.Key);

            //PxPay
            Assert.AreEqual("Cyberpark", cfg.PxPay.Id);
            Assert.AreEqual("1cf77dcb55854b0dd9e12782c844c05f11a4e49a61571037b91ee9720d514de7", cfg.PxPay.Key);

            //directories
            Assert.AreEqual(@"E:\Temp\voucher\tmp\", cfg.TemporaryDirectory);
            Assert.AreEqual(@"E:\Temp\voucher\tessdata\", cfg.TesseractDataDirectory);
            Assert.AreEqual(@"E:\Temp\voucher\voucerImage\", cfg.VoucherImageDirectory);

            //administrator
            Assert.AreEqual(1, cfg.Administrators.Count);
            Assert.AreEqual("opDxls3kxQNdVPqkKW4c8DAfDGX8", cfg.Administrators["Qin"]);
        }

        [TestMethod]
        public void TestLog()
        {
 
        }
    }
}
