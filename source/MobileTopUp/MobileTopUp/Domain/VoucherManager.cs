using System;
using System.Collections.Generic;
using System.Linq;
using MobileTopUp.Models;
using MobileTopUp.Utilities;

namespace MobileTopUp
{
    public class VoucherManager
    {
        private Account _manager;
        private static VoucherStatistic _voucherStatistic = null;
        private static object _lock = new object();

        public VoucherManager(Account account)
        {
            _manager = account;
        }
        /// <summary>
        /// add voucher
        /// </summary>
        /// <param name="v"></param>
        public bool Add(Voucher voucher)
        {
            try
            {
                using (StoreEntities db = new StoreEntities())
                {
                    db.Accounts.Attach(_manager);
                    voucher.Creator = _manager;
                    voucher.CreatedDate = DateTime.Now;
                    db.Vouchers.Add(voucher);
                    int impactCount = db.SaveChanges();
                    if (impactCount > 0)
                    {
                        UpdateStatistic();
                        Store.BizInfo("VOUCHER", _manager.ID, string.Format("succeed to add voucher brand={0},sn={1}", voucher.Brand, voucher.SerialNumber));
                        return true;
                    }
                    else
                    {
                        Store.SysError("VOUCHER", string.Format("faild to add voucher brand={0},sn={1}", voucher.Brand, voucher.SerialNumber));
                        return true;
                    }

                }
                
            }
            catch (Exception ex)
            {
                Store.SysError("VOUCHER", string.Format("failed to add voucher brand={0}, sn={1}", voucher.Brand, voucher.SerialNumber), ex);
            }

            return false;
        }

        public bool Add(IList<Voucher> vouchers)
        {
            try
            {
                using (StoreEntities db = new StoreEntities())
                {
                    db.Accounts.Attach(_manager);
                    foreach (Voucher v in vouchers)
                    {
                        v.Creator = _manager;
                        v.CreatedDate = DateTime.Now;
                        db.Vouchers.Add(v);
                    }
                    int impactCount = db.SaveChanges();
                    if (impactCount > 0)
                    {
                        UpdateStatistic();
                        Store.BizInfo("VOUCHER", _manager.ID, "succeed to add vouchers");
                        return true;
                    }
                    else
                    {
                        Store.SysError("VOUCHER", "failed to add vouchers");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Store.SysError("VOUCHER", "failed to add vouchers", ex);
                return false;
            }
        }

        public bool Update(Voucher ori, Voucher now)
        {
            try
            {
                using (StoreEntities db = new StoreEntities())
                {
                    db.Accounts.Attach(_manager);
                    string sql = "UPDATE T_VOUCHER SET Brand='{0}', SerialNumber='{1}', TopUpNumber='{2}', CreatedBy={3}";
                    sql += " WHERE Brand='{4}' AND SerialNumber='{5}' AND TopUpNumber='{6}'";
                    sql = string.Format(sql, now.Brand, now.SerialNumber, now.TopUpNumber, _manager.ID,
                                ori.Brand, ori.SerialNumber, ori.TopUpNumber);
                    int impactCount = db.Database.ExecuteSqlCommand(sql);
                    db.SaveChanges();
                    if (impactCount > 0)
                    {
                        UpdateStatistic();
                        Store.BizInfo("VOUCHER", _manager.ID, string.Format("succeed to update voucher {0}-{1}-{2} to {3}-{4}-{5}",
                            ori.Brand, ori.SerialNumber, ori.TopUpNumber, now.Brand, now.SerialNumber, now.TopUpNumber));
                        return true;
                    }
                    else
                    {
                        Store.SysError("VOUCHER", string.Format("fail to update voucher {0}-{1}-{2} to {3}-{4}-{5}",
                    ori.Brand, ori.SerialNumber, ori.TopUpNumber, now.Brand, now.SerialNumber, now.TopUpNumber));
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Store.SysError("VOUCHER", string.Format("fail to update voucher {0}-{1}-{2} to {3}-{4}-{5}",
                    ori.Brand, ori.SerialNumber, ori.TopUpNumber, now.Brand, now.SerialNumber, now.TopUpNumber), ex);
                return false;
            }
        }

        public bool Delete(Voucher v)
        {
            if (!AccountManager.IsAdministrator(_manager))
            {
                Store.SysError("VOUCHER", "unauthorized deletion");
                return false;
            }

            try
            {
                using (StoreEntities db = new StoreEntities())
                {
                    db.Vouchers.Attach(v);
                    db.Vouchers.Remove(v);
                    int impactCount = db.SaveChanges();
                    if (impactCount > 0)
                    {
                        UpdateStatistic();
                        Store.BizInfo("VOUCHER", _manager.ID, string.Format("succeed to delete voucher {0} SN-{1} {2}",v.Brand, v.SerialNumber, v.TopUpNumber));
                        return true;
                    }
                    else
                    {
                        Store.SysError("VOUCHER", string.Format("succeed to delete voucher {0} SN-{1} {2}", v.Brand, v.SerialNumber, v.TopUpNumber));
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Store.SysError("VOUCHER",string.Format("succeed to delete voucher {0} SN-{1} {2}", v.Brand, v.SerialNumber, v.TopUpNumber), ex);
                return false;
            }
        }

        public static Voucher FindBySerialOrTopupNumber(Voucher v)
        {
            return FindBySerialOrTopupNumber(v.Brand, v.SerialNumber, v.TopUpNumber);
        }
        public static Voucher FindBySerialOrTopupNumber(BrandType brand, string serialNumber, string topupNumber)
        {
            Voucher voucher = null;
            try
            {
                using (StoreEntities db = new StoreEntities())
                {
                    voucher = db.Vouchers.FirstOrDefault(v => v.Brand.Equals(brand.Value) && (v.SerialNumber.Equals(serialNumber) || v.TopUpNumber.Equals(topupNumber)));
                }
                if (voucher == null)
                {
                    Store.BizInfo("VOUCHER", null, string.Format("not found voucher brand={0}, SN={1} or TUN={2}", brand, serialNumber, topupNumber));
                }
                else
                {
                    Store.BizInfo("VOUCHER", null, string.Format("found voucher brand={0}, SN={1} or TUN={2}", brand, serialNumber, topupNumber));
                }
            }
            catch (Exception ex)
            {
                Store.SysError("VOUCHER", string.Format("failed to search voucher brand={0},  SN={1} or TUN={2}", brand, serialNumber, topupNumber), ex);
            }
            return voucher;
        }

        public static Voucher FindBySerialNumber(Voucher v)
        {
            return FindBySerialNumber(v.Brand, v.SerialNumber);
        }
        public static Voucher FindBySerialNumber(BrandType brand, string serialNumber)
        {
            Voucher voucher = null;
            try
            {
                using (StoreEntities db = new StoreEntities())
                {
                    voucher = db.Vouchers.FirstOrDefault(v => v.Brand.Equals(brand.Value) && v.SerialNumber.Equals(serialNumber));
                }
                if (voucher == null)
                {
                    Store.BizInfo("VOUCHER", null, string.Format("not found voucher brand={0}, SN={1}", brand, serialNumber));
                }
                else
                {
                    Store.BizInfo("VOUCHER", null, string.Format("found voucher brand={0}, SN={1}", brand, serialNumber));
                }
            }
            catch (Exception ex)
            {
                Store.SysError("VOUCHER", string.Format("failed to search voucher brand={0},  SN={1}", brand, serialNumber), ex);
            }
            return voucher;
        }

        public static Voucher FindByTopUpNumber(Voucher v)
        {
            return FindByTopUpNumber(v.Brand, v.TopUpNumber);
        }
        public static Voucher FindByTopUpNumber(BrandType brand, string topupNumber)
        {
            Voucher voucher = null;
            try
            {
                using (StoreEntities db = new StoreEntities())
                {
                    voucher = db.Vouchers.FirstOrDefault(v => v.Brand.Equals(brand.Value) && v.TopUpNumber.Equals(topupNumber));
                }
                if (voucher == null)
                {
                    Store.BizInfo("VOUCHER", null, string.Format("not found voucher brand={0}, TUN={1}", brand, topupNumber));
                }
                else
                {
                    Store.BizInfo("VOUCHER", null, string.Format("found voucher brand={0}, TUN={1}", brand, topupNumber));
                }
            }
            catch (Exception ex)
            {
                Store.SysError("VOUCHER", string.Format("failed to search voucher brand={0},  TUN={1}", brand, topupNumber), ex);
            }
            return voucher;
        }


        //public static bool IsExist(Voucher v)
        //{
        //    return FindBySerialOrTopupNumber(v) != null;
        //}

        public static int GetStock(BrandType brand)
        {
            using (StoreEntities db = new StoreEntities())
            {
                try
                {
                    int stock = db.Vouchers.Count(v => v.Brand.Equals(brand.Value) && v.TransactionID == null);
                    Store.BizInfo("VOUCHER", null, string.Format("{0} Voucher stock={1}", brand.Value, stock));
                    return stock;
                }
                catch (Exception ex)
                {
                    Store.SysError("VOUCHER", string.Format("failed to get stock of {0} voucher", brand.Value), ex);
                    return -1;
                }
            }
        }

        public static VoucherStatistic GetStatistic(bool realtime = false)
        {
            if (realtime || _voucherStatistic == null)
            {
                UpdateStatistic();
            }
            return _voucherStatistic;
        }

        public static void UpdateStatistic()
        {
            lock (_lock)
            {
                using (StoreEntities db = new StoreEntities())
                {
                    try
                    {
                        Store.SysInfo("VOUCHER", "start to update statistic");
                        VoucherStatistic pre = _voucherStatistic;
                        _voucherStatistic = new VoucherStatistic();

                        //breif
                        //sold = 2, hold = 1, available = 0
                        var result = db.Vouchers.Select(v => new { brand = v.Brand, status = v.IsSold ?? false ? 2 : v.TransactionID > 0 ? 1 : 0 })
                                            .GroupBy(v => new { v.brand, v.status })
                                            .Select(v => new { v.Key.brand, v.Key.status, count = v.Count() })
                                            .OrderBy(v => v.brand);
                        foreach (var r in result)
                        {
                            _voucherStatistic.Set((BrandType)r.brand, (VoucherStatus)r.status, r.count);
                        }

                        //available voucher
                        var vouchers = db.Vouchers.Where(v => v.TransactionID == null).OrderBy(v=>v.Brand).OrderByDescending(v=>v.CreatedDate);
                        foreach (Voucher v in vouchers)
                        {
                            _voucherStatistic.Add(v);
                        }

                        Store.NotifyVoucherChanges(pre, _voucherStatistic);

                        Store.SysInfo("VOUCHER", "success to update statistic");
                    }
                    catch (Exception ex)
                    {
                        Store.SysError("VOUCHER", "failed to update statistic", ex);
                    }
                }
            }
        }

        public static bool Hold(Transaction trans)
        {
            //hold vouchour
            using (StoreEntities db = new StoreEntities())
            {
                using (var dbTrans = db.Database.BeginTransaction(System.Data.IsolationLevel.RepeatableRead))
                {
                    try
                    {
                        db.Accounts.Attach(trans.Consumer);
                        //got voucher
                        IEnumerable<Voucher> vouchers = db.Vouchers.Where(x => x.Brand.Equals(trans.Brand) && x.TransactionID == null).Take(trans.Quantity);
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
                            v.Consumer = trans.Consumer;
                            trans.Vouchers.Add(v);
                        }
                        db.SaveChanges();
                        dbTrans.Commit();
                        UpdateStatistic();
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
                WechatHelper.SendMessageAsync(trans.Consumer.ReferenceID, FormatMessage(trans));
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

        public static void ReleaseUnpaidVouchers()
        {
            using (StoreEntities db = new StoreEntities())
            {
                try
                {
                    DateTime clrTime = DateTime.Now.AddMinutes(-Store.Configuration.UnpaidVouchKeepMinutes);
                    Store.SysInfo("VOUCHER", "start to release unpaid voucher");
                    var transactions = db.Transactions.Where(t => !t.HasCancelled && !t.HasPaid && t.OrderDate <= clrTime).ToList();
                    transactions.ForEach(t =>
                    {
                        t.Cancel();
                        Store.BizInfo("VOUCHER", null, string.Format("transaction {0} cancelled, order time={1:yyyy-MM-dd HH:mm:ss}", t.ID, t.OrderDate));
                    });
                    db.SaveChanges();
                    Store.SysInfo("VOUCHER", "success to release unpaid voucher");
                    UpdateStatistic();
                }
                catch (Exception ex)
                {
                    Store.SysError("VOUCHER", "faild to release unpaid voucher", ex);
                }
            }
        }

        private static string FormatMessage(Transaction trans)
        {
            string voucherStr = null;
            foreach (Voucher v in trans.Vouchers)
            {
                if (!string.IsNullOrEmpty(voucherStr))
                {
                    voucherStr += "\n";
                }

                if (!string.IsNullOrEmpty(v.TopUpNumber))
                {
                    voucherStr += v.TopUpNumber;
                }
            }

            return string.Format("Your {0} Top-Up Number:\n {1}", trans.Brand.Value, voucherStr);
        }
    }
}