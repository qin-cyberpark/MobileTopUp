namespace MobileTopUp.Models
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class StoreEntities : DbContext
    {
        public StoreEntities()
            : base("name=StoreEntities")
        {
            //this.Configuration.LazyLoadingEnabled = true;
        }

        public virtual DbSet<Account> Accounts { get; set; }
        public virtual DbSet<Transaction> Transactions { get; set; }
        public virtual DbSet<Voucher> Vouchers { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //== mapping ==
            //account
            modelBuilder.Entity<Account>().Property(a => a.Type.Value).HasColumnName("Type")
               .HasColumnType("varchar")
               .HasMaxLength(20);

            ////voucher
            //modelBuilder.Entity<Voucher>().Property(v => v.Brand.Value).HasColumnName("Brand")
            //   .HasColumnType("varchar")
            //   .HasMaxLength(20);

            //transaction
            modelBuilder.Entity<Transaction>().Property(t => t.Brand.Value).HasColumnName("Brand")
               .HasColumnType("varchar")
               .HasMaxLength(20);

            modelBuilder.Entity<Transaction>().Property(t => t.PaymentType.Value).HasColumnName("PaymentType")
               .HasColumnType("varchar")
               .HasMaxLength(20);

            modelBuilder.Entity<Transaction>().Property(t => t.Currency.Value).HasColumnName("Currency")
              .HasColumnType("char")
              .HasMaxLength(3);


            //== relationship ==
            //account to purchased voucher
            modelBuilder.Entity<Account>()
                .HasMany(e => e.PurchasedVouchers)
                .WithOptional(e => e.Consumer)
                .HasForeignKey(e => e.AccountID);

            //account to created voucher
            modelBuilder.Entity<Account>()
                .HasMany(e => e.CreatedVouchers)
                .WithRequired(e => e.Creator)
                .HasForeignKey(e => e.CreatedBy)
                .WillCascadeOnDelete(false);

            //account to transaction
            modelBuilder.Entity<Account>()
                .HasMany(e => e.Transactions)
                .WithRequired(e => e.Consumer)
                .HasForeignKey(e => e.AccountID)
                .WillCascadeOnDelete(false);

            //transaction to voucher
            modelBuilder.Entity<Transaction>()
                .HasMany(e => e.Vouchers)
                .WithOptional(e => e.Transaction)
                .HasForeignKey(e => e.TransactionID);
        }
    }
}
