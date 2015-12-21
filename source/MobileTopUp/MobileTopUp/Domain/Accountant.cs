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

            reqInput.AmountInput = amount.ToString("F2");
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
      
        #endregion
        public static string GeneratePayURL(Transaction trans, string urlFail, string urlSuccess, int attemptTime = 0)
        {
            string payUrl = null;
            string refStr = string.Format("TOPUP {0} {1}",trans.Brand,trans.ID);
            string tranIdStr = string.Format("{0}-{1}-{2}",trans.ID.ToString(),attemptTime,DateTime.Now.ToString("HHmmss"));
            string remark1 = FormatTopUpNumbers(trans);
            if (trans.PaymentType  == PaymentType.PxPay)
            {
                payUrl = GeneratePxPayRequestURL(trans.ChargeAmount, refStr, tranIdStr, remark1, urlFail, urlSuccess);
            }
            return payUrl;
        }

       
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
                if (!(isSuccess?"1":"0").Equals(output.Success))
                {
                    Store.SysError("ACCOUNT-PXPAY", string.Format("payment result not match except {0} - actual {1}", output.Success, isSuccess));
                }
                
                //set transaction
                int transactionId = int.Parse(output.TxnId.Split('-')[0]);
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

                    if (!isSuccess)
                    {
                        //pay fail
                        outTransactionId = transactionId;
                        trans.PayFail(output.ToString());
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
                        trans.Paid(output.ToString());
                        db.SaveChanges();
                        VoucherManager.UpdateStatistic();
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

        private static string FormatTopUpNumbers(Transaction trans)
        {
            string voucherStr = null;
            foreach (Voucher v in trans.Vouchers)
            {
                if (!string.IsNullOrEmpty(voucherStr))
                {
                    voucherStr += ",";
                }

                if (!string.IsNullOrEmpty(v.TopUpNumber))
                {
                    voucherStr += v.TopUpNumber;
                }
            }

            return voucherStr;
        }

    }
}