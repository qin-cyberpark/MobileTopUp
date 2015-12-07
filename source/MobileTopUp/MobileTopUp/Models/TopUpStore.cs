using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MobileTopUp.Models;
using MobileTopUp.Utilities;

namespace MobileTopUp.Models
{
    public enum CurrencyTypes { NZD, CNY }
    public enum PaymentTypes { Unknown, WechatPay, PaymentExpressCC, PaymentExpressA2A }
    public partial class TopUpStore
    {
        public enum CustomerSource { Wechat }
        public decimal Discount = 0.9M;

        public decimal GetExchangeRate(CurrencyTypes type)
        {
            switch (type)
            {
                case CurrencyTypes.CNY: return 4.3M;
                case CurrencyTypes.NZD: return 1.0M;
                default: return 0.0M;
            }
        }

        public Customer GetCustomerByWechatCode(string code)
        {
            //get openid
            string userInfoAccessToken = null;
            //string openid = WechatHelper.GetOpenID(code, out userInfoAccessToken);
            string openid = "opDxls3kxQNdVPqkKW4c8DAfDGX8";

            //get customer
            //"opDxls3kxQNdVPqkKW4c8DAfDGX8"
            string cusSrcStr = TopUpStoreHelper.CustomerSourceToString(TopUpStore.CustomerSource.Wechat);
            Customer customer = Customers.FirstOrDefault(x => x.Type.Equals(cusSrcStr) && x.ReferenceID.Equals(openid));
            if (customer == null)
            {
                customer = new Customer();
            }
            if (string.IsNullOrEmpty(customer.Name))
            {
                //first time
                string name = WechatHelper.GetUserInfo(userInfoAccessToken, openid).nickname;
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }

                //save customer
                customer.Type = cusSrcStr;
                customer.ReferenceID = openid;
                customer.Name = name;
                Customers.Add(customer);
                SaveChanges();
            }

            return customer;
        }

        public bool IsValidBrand(string brand)
        {
            if (string.IsNullOrEmpty(brand))
            {
                return false;
            }
            switch (brand)
            {
                case "SPARK":
                case "VODAFONE":
                case "SKINNY":
                case "2DEGREE": return true;
                default: return false;
            }
        }

        public string GetVerifiedBrandOrDefault(string brand)
        {
            brand = brand.ToUpper();
            if (!IsValidBrand(brand))
            {
                return "SPARK";
            }

            return brand;
        }

        public bool IsValidAmount(int? amount)
        {
            if(amount == null)
            {
                return false;
            }
            switch (amount)
            {
                case 20: case 40: case 60: case 80: case 100: return true;
                default: return false;
            }
        }

        public bool IsValidPayType(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return false;
            }

            switch(type)
            {
                case "WECHAT":
                case "PECC":
                case "PEA2A": return true;
                default: return false;
            }
        }

        public int GetVerifiedAmountOrDefault(int? amount)
        {
            return IsValidAmount(amount) ? (int)amount : 20;
        }

    }
}