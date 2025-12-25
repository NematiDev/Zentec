using Microsoft.EntityFrameworkCore;
using Zentec.OrderService.Models.Entities;

namespace Zentec.OrderService.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("orders");
                entity.HasKey(o => o.Id);

                entity.Property(o => o.UserId).IsRequired().HasMaxLength(64);
                entity.Property(o => o.UserEmail).IsRequired().HasMaxLength(256);
                entity.Property(o => o.Status).IsRequired();
                entity.Property(o => o.TotalAmount).HasColumnType("numeric(18,2)");

                entity.Property(o => o.CreatedAt).IsRequired();
                entity.Property(o => o.UpdatedAt).IsRequired();

                entity.HasMany(o => o.Items)
                      .WithOne(i => i.Order)
                      .HasForeignKey(i => i.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(o => new { o.UserId, o.CreatedAt });
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("order_items");
                entity.HasKey(i => i.Id);

                entity.Property(i => i.ProductId).IsRequired().HasMaxLength(64);
                entity.Property(i => i.ProductName).IsRequired().HasMaxLength(256);
                entity.Property(i => i.UnitPrice).HasColumnType("numeric(18,2)");
                entity.Property(i => i.LineTotal).HasColumnType("numeric(18,2)");
            });
        }
    }
}
