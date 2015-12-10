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
            try {
                using (StoreEntities db = new StoreEntities())
                {
                    voucher.CreatedBy = _manager.Name;
                    voucher.CreatedDate = DateTime.Now;
                    db.Vouchers.Add(voucher);
                    db.SaveChanges();
                }
                Store.BizInfo("VOUCHER", string.Format("{0} succeed to add voucher {1}", _manager.ID, voucher.Number));
            }
            catch
            {
                Store.SysError("VOUCHER",string.Format("{0} failed to add voucher {1}", _manager.ID, voucher.Number));
            }
        }
        public void Add(IList<Voucher> vouchers)
        {
            try
            {
                using (StoreEntities db = new StoreEntities())
                {
                    foreach(Voucher voucher in vouchers) { 
                        voucher.CreatedBy = _manager.Name;
                        voucher.CreatedDate = DateTime.Now;
                        db.Vouchers.Add(voucher);
                    }
                    db.SaveChanges();
                }
                Store.BizInfo("VOUCHER", string.Format("{0} succeed to add batch of voucher", _manager.ID));
            }
            catch
            {
                Store.SysError("VOUCHER", string.Format("{0} failed to add batch of voucher", _manager.ID));
            }
        }

        public static Voucher FindById(int id)
        {
            Voucher v = null;
            try {
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
            catch
            {
                Store.BizInfo("VOUCHER", string.Format("failed to find voucher id {0}", id));
                return null;
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
                foreach(Match m in finds)
                {
                    if(m.Success && m.Value.Trim().Length > 5) { 
                        matchedStr += m.Value.Trim() + "\r\n";
                    }
                }
            }
            text = matchedStr + "==========================================\r\n" + text;
            return text;
        }
    }
}