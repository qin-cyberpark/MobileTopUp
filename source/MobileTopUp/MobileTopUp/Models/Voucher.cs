namespace MobileTopUp.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("t_voucher")]
    public partial class Voucher
    {
        [Key]
        [Column(Order = 0)]
        public string Brand { get; set; }

        //public BrandType Brand
        //{
        //    get
        //    {
        //        return (BrandType)BrandName;
        //    }
        //    set
        //    {
        //        BrandName = value.Value;
        //    }
        //}

        [Key]
        [Column(Order = 1)]
        [StringLength(30)]
        public string SerialNumber { get; set; }

        [Required]
        [StringLength(30)]
        public string TopUpNumber { get; set; }

        public decimal Denomination { get; set; }

        [Column(TypeName = "blob")]
        public byte[] Image { get; set; }

        public int CreatedBy { get; set; }
        public virtual Account Creator { get; set; }

        public DateTime CreatedDate { get; set; }

        [Column(TypeName = "bit")]
        public bool? IsSold { get; set; }

        public int? AccountID { get; set; }
        public virtual Account Consumer { get; set; }

        public int? TransactionID { get; set; }
        public virtual Transaction Transaction { get; set; }
    }
}
