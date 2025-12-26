using Microsoft.EntityFrameworkCore;
using Zentec.OrderService.Models.Entities;

namespace Zentec.OrderService.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options)
            : base(options)
        {
        }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // Order
            // =========================
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("orders");
                entity.HasKey(o => o.Id);

                entity.Property(o => o.UserId)
                      .IsRequired()
                      .HasMaxLength(64);

                entity.Property(o => o.UserEmail)
                      .IsRequired()
                      .HasMaxLength(256);

                entity.Property(o => o.Status)
                      .IsRequired();

                entity.Property(o => o.TotalAmount)
                      .HasColumnType("numeric(18,2)");

                entity.Property(o => o.CreatedAt)
                      .IsRequired();

                entity.Property(o => o.UpdatedAt)
                      .IsRequired();

                entity.HasMany(o => o.Items)
                      .WithOne(i => i.Order)
                      .HasForeignKey(i => i.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(o => new { o.UserId, o.CreatedAt });
            });

            // =========================
            // OrderItem
            // =========================
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("order_items");
                entity.HasKey(i => i.Id);

                entity.Property(i => i.ProductId)
                      .IsRequired()
                      .HasMaxLength(64);

                entity.Property(i => i.ProductName)
                      .IsRequired()
                      .HasMaxLength(256);

                entity.Property(i => i.UnitPrice)
                      .HasColumnType("numeric(18,2)");

                entity.Property(i => i.LineTotal)
                      .HasColumnType("numeric(18,2)");

                entity.Property(i => i.Quantity)
                      .IsRequired();
            });

            // =========================
            // Cart
            // =========================
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.ToTable("carts");
                entity.HasKey(c => c.Id);

                entity.Property(c => c.UserId)
                      .IsRequired()
                      .HasMaxLength(64);

                entity.Property(c => c.Status)
                      .IsRequired();

                entity.Property(c => c.CreatedAt)
                      .IsRequired();

                entity.Property(c => c.UpdatedAt)
                      .IsRequired();

                entity.HasMany(c => c.Items)
                      .WithOne(i => i.Cart)
                      .HasForeignKey(i => i.CartId)
                      .OnDelete(DeleteBehavior.Cascade);

                // ? PostgreSQL filtered index
                // Only one ACTIVE cart per user
                entity.HasIndex(c => new { c.UserId, c.Status })
                      .HasFilter("\"Status\" = 0");
            });

            // =========================
            // CartItem
            // =========================
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.ToTable("cart_items");
                entity.HasKey(i => i.Id);

                entity.Property(i => i.ProductId)
                      .IsRequired()
                      .HasMaxLength(64);

                entity.Property(i => i.ProductName)
                      .IsRequired()
                      .HasMaxLength(256);

                entity.Property(i => i.UnitPrice)
                      .HasColumnType("numeric(18,2)");

                entity.Property(i => i.LineTotal)
                      .HasColumnType("numeric(18,2)");

                entity.Property(i => i.Quantity)
                      .IsRequired();

                entity.Property(i => i.AddedAt)
                      .IsRequired();

                // Prevent duplicate products in same cart
                entity.HasIndex(i => new { i.CartId, i.ProductId })
                      .IsUnique();
            });
        }
    }
}
