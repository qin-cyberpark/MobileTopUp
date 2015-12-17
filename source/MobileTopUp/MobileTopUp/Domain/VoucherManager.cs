using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Tesseract;
using System.Text.RegularExpressions;
using MobileTopUp.Models;
using MobileTopUp.Utilities;
using System.IO;
using System.Drawing;

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
                    Bitmap image = OcrHelper.CreateVoucherImage(voucher.Brand, voucher.Number, voucher.SerialNumber);
                    ImageConverter converter = new ImageConverter();
                    voucher.Image = (byte[])converter.ConvertTo(image, typeof(byte[]));
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

        public static Voucher Find(int id)
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
                    Store.BizInfo("VOUCHER", null, string.Format("found voucher id={0}", id));
                }
                return v;
            }
            catch (Exception ex)
            {
                Store.SysError("VOUCHER", string.Format("failed to find voucher id {0}", id), ex);
                return null;
            }
        }

        public static Voucher Find(BrandType brand, string number)
        {
            Voucher voucher = null;
            try
            {
                using (StoreEntities db = new StoreEntities())
                {
                    voucher = db.Vouchers.FirstOrDefault(v => v.Brand.Value.Equals(brand.Value) && v.Number.Equals(number));
                }
                if (voucher == null)
                {
                    Store.BizInfo("VOUCHER", null, string.Format("can not find voucher brand={0}, number={1}", brand, number));
                }
                else
                {
                    Store.BizInfo("VOUCHER", null, string.Format("found voucher brand={0}, number={1}", brand, number));
                }
                return voucher;
            }
            catch (Exception ex)
            {
                Store.SysError("VOUCHER", string.Format("failed to find voucher brand={0}, number={1}", brand, number), ex);
                return null;
            }

        }

        public static Voucher Find(Voucher v)
        {
            if (v.ID > 0)
            {
                return Find(v.ID);
            }
            else
            {
                return Find(v.Brand, v.Number);
            }
        }

        public static bool IsExist(Voucher v)
        {
            return Find(v) != null;
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
        public static bool CheckStock(string brand, int quantity)
        {
            int stock = VoucherManager.GetStock(brand);
            if (stock < quantity)
            {
                Store.SysInfo("PAY", string.Format("not enough {0} voucher stock {1}/{2}", brand, stock, quantity));
                return false;
            }
            else
            {
                return true;
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

        public static bool SendVoucher(Transaction trans)
        {
            try
            {
                //send voucher
                Store.BizInfo("VOUCHER", trans.Consumer.ID, string.Format("start to send voucher transId={0}", trans.ID));
                byte[][] imageBytes = new byte[trans.Vouchers.Count][];
                int idx = 0;
                foreach (Voucher v in trans.Vouchers)
                {
                    imageBytes[idx++] = v.Image;
                }
               WechatHelper.SendImagesAsync(trans.Consumer.ReferenceID, imageBytes);
                
                WechatHelper.SendMessageAsync(trans.Consumer.ReferenceID, string.Format("Your {0} voucher number:{1}", trans.Brand, trans.VoucherNumberString));
                using (StoreEntities db = new StoreEntities())
                {
                    db.Transactions.Attach(trans);
                    trans.VoucherSendDate = DateTime.Now;
                    db.SaveChanges();
                    Store.BizInfo("VOUCHER", trans.Consumer.ID, string.Format("voucher sent date updated transId={0}", trans.ID));
                }
                return true;
            }
            catch
            {
                Store.BizInfo("VOUCHER", trans.Consumer.ID, string.Format("fail to send voucher transId={0}", trans.ID));
                return false;
            }
        }

        public static List<string> RecognizeVoucherNumber(string imgBase64)
        {
            List<string> result = new List<string>();
            byte[] imageBytes = Convert.FromBase64String(imgBase64);

            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                Image image = Image.FromStream(ms, true);
                Bitmap bitmap = new Bitmap(image);
                //bitmap = new Bitmap(@"C:\GreenSpot\voucher\image.jpg");
                string text = OcrHelper.Recognize(bitmap);
                if (!string.IsNullOrEmpty(text))
                {
                    Regex regx = new Regex(@"[\d- ]+");
                    MatchCollection finds = regx.Matches(text);
                    foreach (Match m in finds)
                    {
                        if (m.Success && m.Value.Trim().Length > 0)
                        {
                            result.Add(m.Value);
                        }
                    }
                }
            }

            return result;
        }
    }
}