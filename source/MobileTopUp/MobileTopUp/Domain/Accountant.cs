﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MobileTopUp.Models;
using MobileTopUp.Utilities;
namespace MobileTopUp
{
    public class Accountant
    {
        //PxPay
        private static PxPay _pxPay = new PxPay(Store.Configuration.PxPay.Id, Store.Configuration.PxPay.Key);
        private static string GeneratePxPayRequestURL(decimal amount, string reference, string transactionID, string urlFail, string urlSuccess)
        {
            RequestInput reqInput = new RequestInput();

            reqInput.AmountInput = amount.ToString("#.##");
            reqInput.CurrencyInput = Store.CURRENCY_CODE_NZD;
            reqInput.MerchantReference = reference;
            reqInput.TxnId = transactionID;
            reqInput.TxnType = "Purchase";
            reqInput.UrlFail = urlFail;
            reqInput.UrlSuccess = urlSuccess;

            RequestOutput output = _pxPay.GenerateRequest(reqInput);
            if (output.valid == "1" && output.Url != null)
            {
                // Redirect user to payment page
                return output.Url;
            }
            else
            {
                return null;
            }
        }

        public static string GeneratePayURL(Transaction trans, string urlFail, string urlSuccess)
        {
            string payUrl = null;
            decimal ttlCharge = trans.SellingPrice;
            if (!Store.Configuration.Payment.IsFullCharge)
            {
                ttlCharge = 0.01M;
            }
            switch (Store.PaymentCodeToType(trans.PaymentType))
            {
                case Store.PaymentTypes.PxPayCreditCard:
                case Store.PaymentTypes.PxPayAccount2Account:
                    payUrl = GeneratePxPayRequestURL(ttlCharge, "TOPUP " + trans.Brand, trans.ID.ToString(), urlFail, urlSuccess);
                    break;
                default: break;
            }

            return payUrl;
        }
    }
}