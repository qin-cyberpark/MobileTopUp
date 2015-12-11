using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tesseract;
using System.Text.RegularExpressions;
using MobileTopUp.Models;
namespace MobileTopUp
{
    public class VoucherManager
    {
        private Account _manager;
        public VoucherManager(Account account)
        {
            _manager = account;
        }
        /// <summary>
        /// add voucher
        /// </summary>
        /// <param name="v"></param>
        public void Add(Voucher voucher)
        {
            try
            {
                using (StoreEntities db = new StoreEntities())
                {
                    voucher.CreatedBy = _manager.Name;
                    voucher.CreatedDate = DateTime.Now;
                    db.Vouchers.Add(voucher);
                    db.SaveChanges();
                }
                Store.BizInfo("VOUCHER", string.Format("{0} succeed to add voucher {1}", _manager.ID, voucher.Number));
            }
            catch (Exception ex)
            {
                Store.SysError("VOUCHER", string.Format("{0} failed to add voucher {1}", _manager.ID, voucher.Number), ex);
            }
        }
        public void Add(IList<Voucher> vouchers)
        {
            try
            {
                using (StoreEntities db = new StoreEntities())
                {
                    foreach (Voucher voucher in vouchers)
                    {
                        voucher.CreatedBy = _manager.Name;
                        voucher.CreatedDate = DateTime.Now;
                        db.Vouchers.Add(voucher);
                    }
                    db.SaveChanges();
                }
                Store.BizInfo("VOUCHER", string.Format("{0} succeed to add batch of voucher", _manager.ID));
            }
            catch (Exception ex)
            {
                Store.SysError("VOUCHER", string.Format("{0} failed to add batch of voucher", _manager.ID), ex);
            }
        }

        public static Voucher FindById(int id)
        {
            Voucher v = null;
            try
            {
                using (StoreEntities db = new StoreEntities())
                {
                    v = db.Vouchers.Find(id);
                }
                if (v == null)
                {
                    Store.BizInfo("VOUCHER", string.Format("not find voucher id {0}", id));
                }
                else
                {
                    Store.BizInfo("VOUCHER", string.Format("find voucher id {0}", id));
                }
                return v;
            }
            catch (Exception ex)
            {
                Store.SysError("VOUCHER", string.Format("failed to find voucher id {0}", id), ex);
                return null;
            }
        }
        public static IList<Voucher> FindByTranscationId(int transactionId)
        {
            List<Voucher> result = new List<Voucher>();
            //get holded voucher
            using (StoreEntities db = new StoreEntities())
            {
                IEnumerable<Voucher> vouchers = db.Vouchers.Where(x => x.TransactionID == transactionId);
                //flag vouchour to sold and send image to customer
                foreach (Voucher v in vouchers)
                {
                    result.Add(v);
                }
            }
            return result;
        }
        public static IList<Voucher> Sold(int transactionId)
        {
            List<Voucher> result = new List<Voucher>();
            //get holded voucher
            using (StoreEntities db = new StoreEntities())
            {
                IEnumerable<Voucher> vouchers = db.Vouchers.Where(x => x.TransactionID == transactionId);
                //flag vouchour to sold and send image to customer
                foreach (Voucher v in vouchers)
                {
                    result.Add(v);
                    v.IsSold = true;
                }
                db.SaveChanges();
            }
            return result;
        }

        public static int GetStock(string brandCode)
        {
            using (StoreEntities db = new StoreEntities())
            {
                try
                {
                    int stock = db.Vouchers.Count(x => x.Brand.Equals(brandCode) && x.TransactionID == null);
                    Store.BizInfo("VOUCHER", string.Format("{0} Voucher stock=", stock));
                    return stock;
                }
                catch (Exception ex)
                {
                    Store.SysError("VOUCHER", string.Format("failed to get stock {0} voucher", brandCode), ex);
                    return -1;
                }
            }
        }

        public static bool Hold(Transaction trans)
        {
            //hold vouchour
            using (StoreEntities db = new StoreEntities())
            {
                using (var dbTrans = db.Database.BeginTransaction())
                {
                    try
                    {
                        //got voucher
                        IEnumerable<Voucher> vouchers = db.Vouchers.Where(x => x.Brand.Equals(trans.Brand) && x.TransactionID == null).Take(trans.Quantity);
                        Store.BizInfo("VOUCHER", string.Format("got {0} of {1} voucher", vouchers.Count(), trans.Quantity));

                        if (vouchers.Count() != trans.Quantity)
                        {
                            Store.BizInfo("VOUCHER", string.Format("not enough voucher {0}/{1}", vouchers.Count(), trans.Quantity));
                            return false;
                        }

                        //save transaction
                        trans.OrderDate = DateTime.Now;
                        db.Transactions.Add(trans);
                        db.SaveChanges();

                        //hold voucher
                        foreach (Voucher v in vouchers)
                        {
                            v.AccountID = trans.AccountID;
                            v.TransactionID = trans.ID;
                        }
                        db.SaveChanges();
                        dbTrans.Commit();
                        Store.BizInfo("VOUCHER", string.Format("hold {0} of {1} voucher for {2}, transaction={3}", trans.Brand, trans.Quantity, trans.AccountID, trans.ID));
                        return true;
                    }
                    catch (Exception ex)
                    {
                        dbTrans.Rollback();
                        Store.SysError("VOUCHER", string.Format("failed to hold {0} of {1} voucher for {2}", trans.Brand, trans.Quantity, trans.AccountID), ex);
                        return false;
                    }
                }
            }
        }

        public static string ScanVoucherNumber(string file)
        {
            string text = null;
            using (TesseractEngine engine = new TesseractEngine(Store.Configuration.TemporaryDirectory, "eng", EngineMode.Default))
            {
                using (Pix img = Pix.LoadFromFile(file))
                {
                    using (Page page = engine.Process(img, Rect.FromCoords(0, 0, img.Width, img.Height)))
                    {
                        text = page.GetText();
                    }
                }
            }
            string matchedStr = "";
            if (!string.IsNullOrEmpty(text))
            {
                Regex regx = new Regex(@"[\d\s-]+");
                MatchCollection finds = regx.Matches(text);
                foreach (Match m in finds)
                {
                    if (m.Success && m.Value.Trim().Length > 5)
                    {
                        matchedStr += m.Value.Trim() + "\r\n";
                    }
                }
            }
            text = matchedStr + "==========================================\r\n" + text;
            return text;
        }
    }
}