namespace MobileTopUp.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("t_transaction")]
    public partial class Transaction
    {
        public Transaction()
        {
            Vouchers = new HashSet<Voucher>();
        }

        public int ID { get; set; }

        public int AccountID { get; set; }
        public virtual Account Consumer { get; set; }
     
        [Required]
        public PaymentType PaymentType { get; set; }

        [Column(TypeName = "text")]
        [MaxLength]
        public string PaymentRef { get; set; }

        [Required]
        public CurrencyType Currency { get; set; }

        public decimal ExchangeRate { get; set; }

        [Required]
        public BrandType Brand { get; set; }

        public int Quantity { get; set; }

        public decimal TotalDenomination { get; set; }

        public decimal SellingPrice { get; set; }
        public decimal ChargeAmount { get; set; }

        public DateTime OrderDate { get; set; }

        public DateTime? PaidDate { get; set; }
        public DateTime? VoucherSendDate { get; set; }

        public int PayFailedCount { get; set; }

        public bool HasPaid { get; set; }
        public bool HasCancelled { get; set; }


        public virtual ICollection<Voucher> Vouchers { get; set; }

        public void PayFail(string refStr) {
            PayFailedCount++;
            PaymentRef = string.IsNullOrEmpty(PaymentRef)? "" : PaymentRef + refStr;
        }

        public void Paid(string refStr)
        {
            PaidDate = DateTime.Now;
            HasPaid = true;
            foreach(Voucher v in Vouchers)
            {
                v.IsSold = true;
            }
            PaymentRef = string.IsNullOrEmpty(PaymentRef) ? "" : PaymentRef + refStr;
        }

        public void Cancel()
        {
            this.HasCancelled = true;
            foreach(Voucher v in Vouchers)
            {
                v.AccountID = null;
            }
            Vouchers.Clear();
        }
    }
}
