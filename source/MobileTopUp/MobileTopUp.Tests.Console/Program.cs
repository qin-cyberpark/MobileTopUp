using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MobileTopUp;
using MobileTopUp.Utilities;
using System.Drawing;
using System.Drawing.Imaging;
using MobileTopUp.Models;

namespace MobileTopUp.Tests.Console
{
 
    class Program
    {
        static void Main(string[] args)
        {
            using (StoreEntities db = new StoreEntities())
            {
                var result = db.Vouchers.Select(v => new { brand = v.Brand, status = v.IsSold ?? false ? 2 : v.TransactionID > 0 ? 1 : 0 })
                                                       .GroupBy(v => new { v.brand, v.status })
                                                       .Select(v => new { v.Key.brand, v.Key.status, count = v.Count() })
                                                       .OrderBy(v => v.brand);
                foreach (var r in result)
                {
                    System.Console.WriteLine(string.Format("{0},{1},{2}", (BrandType)r.brand, (VoucherStatus)r.status, r.count));
                }
            }
            System.Console.ReadLine();
        }
    }
}
