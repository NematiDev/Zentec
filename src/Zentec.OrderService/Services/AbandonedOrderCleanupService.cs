using Microsoft.EntityFrameworkCore;
using Zentec.OrderService.Data;
using Zentec.OrderService.Models.Entities;

namespace Zentec.OrderService.Services
{
    /// <summary>
    /// Background service to release stock for abandoned orders
    /// </summary>
    public class AbandonedOrderCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<AbandonedOrderCleanupService> _logger;

        public AbandonedOrderCleanupService(
            IServiceProvider services,
            ILogger<AbandonedOrderCleanupService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AbandonedOrderCleanupService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAbandonedOrdersAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in abandoned order cleanup");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private async Task CleanupAbandonedOrdersAsync(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var productClient = scope.ServiceProvider.GetRequiredService<IProductClient>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // Find orders older than 30 minutes still pending
            var cutoff = DateTime.UtcNow.AddMinutes(-30);
            var abandonedOrders = await db.Orders
                .Include(o => o.Items)
                .Where(o => o.Status == OrderStatus.PendingPayment && o.CreatedAt < cutoff)
                .Take(50)
                .ToListAsync(ct);

            foreach (var order in abandonedOrders)
            {
                _logger.LogInformation("Cleaning up abandoned order {OrderId}", order.Id);

                // Release stock
                foreach (var item in order.Items)
                {
                    // Note: We need a service account token for this
                    // For now, this is a simplified version
                    try
                    {
                        // You'd need to implement GetServiceAccountToken()
                        // await productClient.ReleaseAsync(item.ProductId, item.Quantity, token, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to release stock for product {ProductId}", item.ProductId);
                    }
                }

                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
            }

            if (abandonedOrders.Any())
            {
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Cleaned up {Count} abandoned orders", abandonedOrders.Count);
            }
        }
    }
}