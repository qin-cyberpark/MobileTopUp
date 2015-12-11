using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MobileTopUp
{
    public partial class Store
    {
        //voucher
        public const int VOUCHER_DEFAULT_DENOMINATION = 20;
        //brand
        public enum Brands {Spark, Vodafone, TwoDegree, Skinny }
        public const string BRAND_SPARK = "SPARK";
        public const string BRAND_VODAFONE = "VODAFONE";
        public const string BRAND_TWO_DEGREE = "2DEGREE";
        public const string BRAND_SKINNY = "SKINNY";
        public static bool VerfiyBrand(string brand)
        {
            if (string.IsNullOrEmpty(brand))
            {
                return false;
            }
            switch (brand.ToUpper())
            {
                case BRAND_SPARK:
                case BRAND_VODAFONE:
                case BRAND_TWO_DEGREE:
                case BRAND_SKINNY: return true;
                default: return false;
            }
        }
        public static string VerifiedBrandOrDefault(string brand)
        {
            if (!VerfiyBrand(brand))
            {
                return BRAND_SPARK;
            }

            return brand.ToUpper();
        }
        public static string BrandTypeToCode(Brands type)
        {
            switch (type)
            {
                case Brands.Spark: return BRAND_SPARK;
                case Brands.Vodafone: return BRAND_VODAFONE;
                case Brands.TwoDegree:return BRAND_TWO_DEGREE;
                case Brands.Skinny:return BRAND_SKINNY;
                default: throw new UnknownDataException("BrandTypeToCode", type.ToString());
            }
        }
        public static Brands BrandCodeToType(string code)
        {
            switch (code.ToUpper())
            {
                case BRAND_SPARK:return Brands.Spark;
                case BRAND_VODAFONE:return Brands.Vodafone;
                case BRAND_TWO_DEGREE:return Brands.TwoDegree;
                case BRAND_SKINNY:return Brands.Skinny;
                default: throw new UnknownDataException("BrandCodeToType", code);
            }
        }


        //account source
        public enum AccountSources {Wechat }
        public const string ACCOUNT_SOURCE_WECHAT = "WECHAT";
        public static string AccountSourceTypeToCode(AccountSources source)
        {
            switch (source)
            {
                case AccountSources.Wechat: return ACCOUNT_SOURCE_WECHAT;
                default: throw new UnknownDataException("AccountSourceTypeToCode", source.ToString());
            }
        }

        //currency
        public enum Currencies { NZD, CNY }
        public const string CURRENCY_CODE_NZD = "NZD";
        public const string CURRENCY_CODE_CNY = "CNY";
        public static string CurrencyTypeToCode(Currencies type)
        {
            switch (type)
            {
                case Currencies.NZD: return CURRENCY_CODE_NZD;
                case Currencies.CNY: return CURRENCY_CODE_CNY;
                default: throw new UnknownDataException("CurrencyTypeToCode", type.ToString());
            }
        }

        //payment
        public enum PaymentTypes { Unknown, WechatPay, PxPayCreditCard, PxPayAccount2Account}
        public const string PAYMENT_CODE_WECHAT = "WECHAT";
        public const string PAYMENT_CODE_PX_CC = "PXCC";
        public const string PAYMENT_CODE_PX_A2A = "PXA2A";
        public const string PAYMENT_NAME_WECHAT = "WECHAT PAY";
        public const string PAYMENT_NAME_PX_CC = "PAYMENT EXPRESS";
        public const string PAYMENT_NAME_PX_A2A = "ACCOUNT 2 ACCOUNT";
        public static PaymentTypes PaymentCodeToType(string code)
        {
            switch (code.ToUpper())
            {
                case PAYMENT_CODE_WECHAT: return PaymentTypes.WechatPay;
                case PAYMENT_CODE_PX_CC: return PaymentTypes.PxPayCreditCard;
                case PAYMENT_CODE_PX_A2A: return PaymentTypes.PxPayAccount2Account;
                default: throw new UnknownDataException("PaymentCodeToType", code);
            }
        }
        public static string PaymentTypeToName(PaymentTypes type)
        {
            switch (type)
            {
                case PaymentTypes.WechatPay: return PAYMENT_NAME_WECHAT;
                case PaymentTypes.PxPayCreditCard: return PAYMENT_NAME_PX_CC;
                case PaymentTypes.PxPayAccount2Account: return PAYMENT_NAME_PX_A2A;
                default: throw new UnknownDataException("PaymentTypeToName", type.ToString());
            }
        }
        public static string PaymentCodeToName(string code)
        {
            switch (code.ToUpper())
            {
                case PAYMENT_CODE_WECHAT: return PAYMENT_NAME_WECHAT;
                case PAYMENT_CODE_PX_CC: return PAYMENT_NAME_PX_CC;
                case PAYMENT_CODE_PX_A2A: return PAYMENT_NAME_PX_A2A;
                default: throw new UnknownDataException("PaymentCodeToName", code);
            }
        }
        public static bool VerifyPayType(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return false;
            }

            switch (type.ToUpper())
            {
                case PAYMENT_CODE_WECHAT:
                case PAYMENT_CODE_PX_CC:
                case PAYMENT_CODE_PX_A2A: return true;
                default: return false;
            }
        }
    }
}