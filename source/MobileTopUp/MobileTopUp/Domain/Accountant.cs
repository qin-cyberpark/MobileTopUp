using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MobileTopUp.Models;
using MobileTopUp.Utilities;
using System.Data.Entity;
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
        /*
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
        }*/
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

        /*
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
        */
        /// <summary>
        /// verify px payment match transaction
        /// if pay success set transaction as paid
        /// </summary>
        /// <param name="resultId"></param>
        /// <param name="isSuccess"></param>
        /// <returns></returns>
        public static bool VerifyPxPayPayment(string resultId, bool isSuccess, out int outTransactionId)
        {
            try {
                //check response
                Store.SysInfo("ACCOUNT-PXPAY", "start to get pxpay payment result =" + resultId);
                ResponseOutput output = _pxPay.ProcessResponse(resultId);
                Store.SysInfo("ACCOUNT-PXPAY", "PXPAY PROCESS RESPONSE:" + output.ToString());
                if (output == null)
                {
                    Store.SysError("ACCOUNT-PXPAY", "can not get pxpay payment result - resposne is null");
                }
                Store.SysInfo("ACCOUNT-PXPAY", output.ToString());
                if (bool.Parse(output.Success) == isSuccess)
                {
                    Store.SysError("ACCOUNT-PXPAY", string.Format("payment result not match except {0} - actual {1}", output.Success, isSuccess));
                }
                
                //set transaction
                int transactionId = int.Parse(output.TxnId);
                decimal amount = decimal.Parse(output.AmountSettlement);
                using (StoreEntities db = new StoreEntities())
                {
                    Transaction trans = db.Transactions.Include(t => t.Vouchers).FirstOrDefault(t => t.ID == transactionId);
                    if (trans == null)
                    {
                        outTransactionId = 0;
                        Store.SysError("ACCOUNT-PXPAY", string.Format("payment {0} can not mactch transaction {1}", resultId, transactionId));
                        return false;
                    }

                    
                    trans.PaymentRef = (string.IsNullOrEmpty(trans.PaymentRef)? "" : trans.PaymentRef) + output.ToString();
                    if (!isSuccess)
                    {
                        //pay fail
                        outTransactionId = transactionId;
                        trans.PayFailedCount++;
                        Store.BizInfo("ACCOUNT-PAPAY", trans.AccountID, "pay failed count = " + trans.PayFailedCount);
                        db.SaveChanges();
                        return true;
                    }
                    else
                    {
                        //pay success
                        if (trans.ChargeAmount != amount)
                        {
                            outTransactionId = 0;
                            Store.BizInfo("ACCOUNT-PAPAY", trans.AccountID, string.Format("pxpay amount {0} <> transaction amount {1}", amount, trans.ChargeAmount));
                            return false;
                        }

                        outTransactionId = transactionId;
                        trans.Paid();
                        db.SaveChanges();
                        Store.BizInfo("ACCOUNT-PAPAY", trans.AccountID, string.Format("transaction set to paid, id={0}", trans.ID));
                        return true;
                    }
                }
            }
            catch(Exception ex)
            {
                outTransactionId = 0;
                Store.SysError("ACCOUNT", "can not get pxpay payment result", ex);
                return false;
            }
        }
    }
}