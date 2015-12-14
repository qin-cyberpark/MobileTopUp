using System;
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
        #region PxPay
        private static PxPay _pxPay = new PxPay(Store.Configuration.PxPay.Id, Store.Configuration.PxPay.Key);
        private static string GeneratePxPayRequestURL(decimal amount, string reference, string transactionID, string txnData, string urlFail, string urlSuccess)
        {
            RequestInput reqInput = new RequestInput();

            reqInput.AmountInput = amount.ToString("#.##");
            reqInput.CurrencyInput = CurrencyType.NZD.Value;
            reqInput.MerchantReference = reference;
            reqInput.TxnId = transactionID;
            reqInput.TxnData1 = txnData;
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
        private static string GeneratePxPayRequestURL(decimal amount, string reference, string transactionID, string urlFail, string urlSuccess)
        {
            return GeneratePxPayRequestURL(amount, reference, transactionID, null, urlFail, urlSuccess);
        }
        private static decimal GetPxPaymentAmount(string resultId, out ResponseOutput output)
        {
            output = _pxPay.ProcessResponse(resultId);
            if (output == null)
            {
                return 0.0M;
            }

            if (!output.Success.Equals("1"))
            {
                return 0.0M;
            }

            if (string.IsNullOrEmpty(output.AmountSettlement))
            {
                return 0.0M;
            }

            try
            {
                decimal amount = decimal.Parse(output.AmountSettlement);
                return amount;
            }catch{
                return 0.0M;
            }
        }
        #endregion
        public static string GeneratePayURL(Transaction trans, string urlFail, string urlSuccess, int attemptTime = 0)
        {
            string payUrl = null;
            string refStr = string.Format("TOPUP {0} {1}",trans.Brand,trans.ID);
            string tranIdStr = string.Format("{0}-{1}",trans.ID.ToString(), attemptTime);
            string remark1 = trans.VoucherNumberString;
            if (trans.PaymentType  == PaymentType.PxPay)
            {
                payUrl = GeneratePxPayRequestURL(trans.ChargeAmount, refStr, tranIdStr, remark1, urlFail, urlSuccess);
            }
            return payUrl;
        }


        public static bool VerifyPayment(decimal amount, PaymentType type, string refId, out string authCode, out string response)
        {
            decimal chargedAmount = 0.0M;
            authCode = null;
            response = null;

            if (type == PaymentType.PxPay)
            {
                ResponseOutput output = null;
                chargedAmount = GetPxPaymentAmount(refId, out output);
                authCode = output.AuthCode;
                response = output.ToString();
            }

            return chargedAmount == amount;
        }
     
    }
}