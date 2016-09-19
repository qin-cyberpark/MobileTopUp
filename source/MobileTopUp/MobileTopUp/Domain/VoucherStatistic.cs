using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MobileTopUp.Models;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace MobileTopUp
{
    [DataContract]
    public class BrandStatistic
    {
        [DataMember(Name = "AVAILABLE", IsRequired = true)]
        public int AvailableCount { get; set; }

        [DataMember(Name = "UNPAID", IsRequired = true)]
        public int UnpaidCount { get; set; }

        [DataMember(Name = "PAID", IsRequired = true)]
        public int PaidCount { get; set; }

        [DataMember(Name = "VOUCHERS", IsRequired = true)]
        private List<VoucherData> _availableVouchers = new List<VoucherData>();
        
        public IList<VoucherData> AvailableVouchers
        {
            get { return _availableVouchers; }
        }
    }

    [DataContract]
    public class VoucherData
    {
        [DataMember(Name = "SN", IsRequired = true)]
        public string SerialNumber { get; set; }

        [DataMember(Name = "NO", IsRequired = true)]
        public string TopUpNumber { get; set; }
    }

    [DataContract]
    public class VoucherStatistic
    {
        private DateTime _updatedTime;

        [DataMember]
        private string UpdatedTime
        {
            get
            {
                return _updatedTime.ToString("yyyy-MM-dd HH:mm");
            }
            set
            {
                _updatedTime = DateTime.Parse(value);
            }
        }
        [DataMember(Name = "SPARK", IsRequired = true)]
        public BrandStatistic SparkStatistic { get; set; }

        [DataMember(Name = "VODAFONE", IsRequired = true)]
        public BrandStatistic VodafoneStatistic { get; set; }

        [DataMember(Name = "2DEGREES", IsRequired = true)]
        public BrandStatistic TwoDegreesStatistic { get; set; }

        [DataMember(Name = "SKINNY", IsRequired = true)]
        public BrandStatistic SkinnyStatistic { get; set; }

        public VoucherStatistic()
        {
            SparkStatistic = new BrandStatistic();
            VodafoneStatistic = new BrandStatistic();
            TwoDegreesStatistic = new BrandStatistic();
            SkinnyStatistic = new BrandStatistic();
            _updatedTime = DateTime.Now;
        }

        public void Set(BrandType brand, VoucherStatus status, int count)
        {
            BrandStatistic target = GetTargetStatistic(brand);
            switch (status)
            {
                case VoucherStatus.Available:
                    target.AvailableCount = count; break;
                case VoucherStatus.Unpaid:
                    target.UnpaidCount = count; break;
                case VoucherStatus.Paid:
                    target.PaidCount = count; break;
                default: return;
            }

            _updatedTime = DateTime.Now;
        }

        public void Add(Voucher voucher)
        {
            BrandStatistic target = GetTargetStatistic(voucher.Brand);
            target.AvailableVouchers.Add(new VoucherData { SerialNumber = voucher.SerialNumber, TopUpNumber = voucher.TopUpNumber });
        }

        private BrandStatistic GetTargetStatistic(BrandType brand)
        {
            if (brand == BrandType.Spark)
            {
                return SparkStatistic;
            }
            else if (brand == BrandType.Vodafone)
            {
                return VodafoneStatistic;
            }
            else if (brand == BrandType.TwoDegrees)
            {
                return TwoDegreesStatistic;
            }
            else if (brand == BrandType.Skinny)
            {
                return SkinnyStatistic;
            }
            else
            {
                return null;
            }
        }

        public string ToJson()
        {
            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(VoucherStatistic));
            MemoryStream ms = new MemoryStream();
            js.WriteObject(ms, this);
            return Encoding.Default.GetString(ms.ToArray());
        }
    }
}