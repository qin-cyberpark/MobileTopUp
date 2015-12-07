using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MobileTopUp.Models;

namespace MobileTopUp.Utilities
{
    public class TopUpStoreHelper
    {
        public static string CustomerSourceToString(TopUpStore.CustomerSource source)
        {
            switch (source)
            {
                case TopUpStore.CustomerSource.Wechat: return "WECHAT";
                default: return "UNKNOWN";
            }
        }

        public static string CurrencyTypeToString(CurrencyTypes type)
        {
            switch (type)
            {
                case CurrencyTypes.NZD: return "NZD";
                case CurrencyTypes.CNY: return "CNY";
                default:return "---";
            }
        }
        public static PaymentTypes PaymentCodeToType(string code)
        {
            switch (code.ToUpper())
            {
                case "WECHAT": return PaymentTypes.WechatPay;
                case "PECC": return PaymentTypes.PaymentExpressCC;
                case "PEA2A": return PaymentTypes.PaymentExpressA2A;
                default: return  PaymentTypes.Unknown;
            }
        }

        public static string PaymentTypeToCode(PaymentTypes type)
        {
            switch (type)
            {
                case PaymentTypes.WechatPay: return "WECHAT PAY";
                case PaymentTypes.PaymentExpressCC: return "PAYMENT EXPRESS - CREDIT CARD";
                case PaymentTypes.PaymentExpressA2A: return "PAYMENT EXPRESS - ACCOUNT 2 ACCOUNT";
                default: return "UNKNOWN";
            }
        }
        public static string PaymentCodeToName(string code)
        {
            switch (code.ToUpper())
            {
                case "WECHAT": return "WECHAT PAY";
                case "PECC": return "PAYMENT EXPRESS - CREDIT CARD";
                case "PEA2A": return "PAYMENT EXPRESS - ACCOUNT 2 ACCOUNT";
                default: return "---";
            }
        }
        public static int  CalcNeededVoucherNumber(decimal totalDenomination)
        {
            return (int)Math.Ceiling(totalDenomination / 20);
        }
    }
}