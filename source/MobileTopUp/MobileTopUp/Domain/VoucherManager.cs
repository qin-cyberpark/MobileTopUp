﻿using System;
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
                    db.Accounts.Attach(_manager);
                    voucher.Creator = _manager;
                    voucher.CreatedDate = DateTime.Now;
                    db.Vouchers.Add(voucher);
                    db.SaveChanges();
                }
                Store.BizInfo("VOUCHER", _manager.ID, string.Format("succeed to add voucher {0}", voucher.Number));
            }
            catch (Exception ex)
            {
                Store.SysError("VOUCHER", string.Format("failed to add voucher id={0}, voucher={1}", _manager.ID, voucher.Number), ex);
            }
        }
        public void Add(IList<Voucher> vouchers)
        {
            try
            {
                using (StoreEntities db = new StoreEntities())
                {
                    db.Accounts.Attach(_manager);
                    foreach (Voucher voucher in vouchers)
                    {
                        voucher.Creator = _manager;
                        voucher.CreatedDate = DateTime.Now;
                        db.Vouchers.Add(voucher);
                    }
                    db.SaveChanges();
                }
                Store.BizInfo("VOUCHER", _manager.ID, string.Format("succeed to add batch of voucher"));
            }
            catch (Exception ex)
            {
                Store.SysError("VOUCHER", string.Format("failed to add batch of voucher, id={0}", _manager.ID), ex);
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
                    Store.BizInfo("VOUCHER", null, string.Format("can not find voucher id={0}", id));
                }
                else
                {
                    Store.BizInfo("VOUCHER",null, string.Format("found voucher id={0}", id));
                }
                return v;
            }
            catch (Exception ex)
            {
                Store.SysError("VOUCHER", string.Format("failed to find voucher id {0}", id), ex);
                return null;
            }
        }

        public static int GetStock(string brand)
        {
            using (StoreEntities db = new StoreEntities())
            {
                try
                {
                    int stock = db.Vouchers.Count(x => x.Brand.Value.Equals(brand) && x.TransactionID == null);
                    Store.BizInfo("VOUCHER", null, string.Format("{0} Voucher stock={1}", brand, stock));
                    return stock;
                }
                catch (Exception ex)
                {
                    Store.SysError("VOUCHER", string.Format("failed to get stock of {0} voucher", brand), ex);
                    return -1;
                }
            }
        }

        public static void Sold(Transaction trans)
        {
            //update transaction
            using (StoreEntities db = new StoreEntities())
            { 
                db.Accounts.Attach(trans.Consumer);
                db.Transactions.Attach(trans);
                trans.Sold();
                db.SaveChanges();
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
                        db.Accounts.Attach(trans.Consumer);
                        //got voucher
                        IEnumerable<Voucher> vouchers = db.Vouchers.Where(x => x.Brand.Value == trans.Brand.Value && x.TransactionID == null).Take(trans.Quantity);
                        Store.BizInfo("VOUCHER", trans.Consumer.ID, string.Format("got {0} of {1} voucher", vouchers.Count(), trans.Quantity));

                        if (vouchers.Count() != trans.Quantity)
                        {
                            Store.BizInfo("VOUCHER", trans.Consumer.ID, string.Format("not enough voucher {0}/{1} for {2}", vouchers.Count(), trans.Quantity, trans.Brand));
                            return false;
                        }

                        //save transaction
                        trans.OrderDate = DateTime.Now;
                        db.Transactions.Add(trans);
                        db.SaveChanges();

                        //hold voucher
                        foreach (Voucher v in vouchers)
                        {
                            trans.Vouchers.Add(v);
                        }
                        db.SaveChanges();
                        dbTrans.Commit();
                        Store.BizInfo("VOUCHER", trans.Consumer.ID, string.Format("hold {0} of {1} voucher for transaction={2}", trans.Quantity, trans.Brand, trans.ID));
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