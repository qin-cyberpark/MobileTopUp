namespace MobileTopUp.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("t_account")]
    public partial class Account
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Account()
        {
            Transactions = new HashSet<Transaction>();
            PurchasedVouchers = new HashSet<Voucher>();
            CreatedVouchers = new HashSet<Voucher>();
        }

        public int ID { get; set; }

        [Required]
        public AccountType Type { get; set; }

        [Required]
        [StringLength(30)]
        public string ReferenceID { get; set; }

        [StringLength(50)]
        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Transaction> Transactions { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Voucher> PurchasedVouchers { get; set; }

        public virtual ICollection<Voucher> CreatedVouchers { get; set; }
    }
}
