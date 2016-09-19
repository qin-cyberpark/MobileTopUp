namespace MobileTopUp.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    [Table("t_mobileVerifyCode")]
    public partial class VerifyCode
    {
        [Key]
        [Required]
        [StringLength(20)]
        [Column("MobileNumber")]
        public string Number { get; set; }

        [Column("VerifyCode")]
        public int Code { get; set; }
        
        public DateTime SendTime { get; set; }
    }
}