using Microsoft.EntityFrameworkCore;
using Zentec.PaymentService.Models.Entities;

namespace Zentec.PaymentService.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
        {
        }

        public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PaymentTransaction>(entity =>
            {
                entity.ToTable("payment_transactions");
                entity.HasKey(p => p.Id);

                entity.Property(p => p.OrderId).IsRequired().HasMaxLength(64);
                entity.Property(p => p.UserId).IsRequired().HasMaxLength(64);
                entity.Property(p => p.Currency).IsRequired().HasMaxLength(3);
                entity.Property(p => p.Status).IsRequired();
                entity.Property(p => p.StripePaymentIntentId).HasMaxLength(255);
                entity.Property(p => p.StripeChargeId).HasMaxLength(255);
                entity.Property(p => p.PaymentMethod).HasMaxLength(50);
                entity.Property(p => p.CardLast4).HasMaxLength(4);
                entity.Property(p => p.CardBrand).HasMaxLength(20);
                entity.Property(p => p.CreatedAt).IsRequired();
                entity.Property(p => p.UpdatedAt).IsRequired();

                // Indexes
                entity.HasIndex(p => p.OrderId);
                entity.HasIndex(p => p.UserId);
                entity.HasIndex(p => p.StripePaymentIntentId);
                entity.HasIndex(p => new { p.UserId, p.CreatedAt });
            });
        }
    }
}