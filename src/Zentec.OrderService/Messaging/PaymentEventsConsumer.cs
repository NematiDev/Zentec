using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zentec.OrderService.Data;
using Zentec.OrderService.Models.Entities;

namespace Zentec.OrderService.Messaging
{
    public class PaymentEventsConsumer : BackgroundService
    {
        private readonly RabbitMqOptions _options;
        private readonly IServiceProvider _services;
        private readonly ILogger<PaymentEventsConsumer> _logger;
        private IConnection? _connection;
        private IModel? _channel;

        public PaymentEventsConsumer(
            IOptions<RabbitMqOptions> options,
            IServiceProvider services,
            ILogger<PaymentEventsConsumer> logger)
        {
            _options = options.Value;
            _services = services;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password,
                    DispatchConsumersAsync = true
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare payment exchange
                _channel.ExchangeDeclare("zentec.payment", ExchangeType.Topic, durable: true);

                // Declare order service queue
                var queueName = "zentec.order.payment";
                _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);

                // Bind to payment events
                _channel.QueueBind(queueName, "zentec.payment", "payment.succeeded");
                _channel.QueueBind(queueName, "zentec.payment", "payment.failed");

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += OnMessageAsync;

                _channel.BasicConsume(queueName, autoAck: false, consumer: consumer);

                _logger.LogInformation("OrderService consuming payment events from queue {Queue}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start payment events consumer");
            }

            return Task.CompletedTask;
        }

        private async Task OnMessageAsync(object sender, BasicDeliverEventArgs ea)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

            try
            {
                var routingKey = ea.RoutingKey;
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                if (routingKey == "payment.succeeded")
                {
                    var evt = JsonSerializer.Deserialize<PaymentSucceededEvent>(json,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                    if (evt != null)
                        await HandlePaymentSucceededAsync(db, publisher, evt);
                }
                else if (routingKey == "payment.failed")
                {
                    var evt = JsonSerializer.Deserialize<PaymentFailedEvent>(json,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                    if (evt != null)
                        await HandlePaymentFailedAsync(db, publisher, evt);
                }

                _channel?.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment event {RoutingKey}", ea.RoutingKey);
                _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        }

        private async Task HandlePaymentSucceededAsync(
            OrderDbContext db,
            IRabbitMqPublisher publisher,
            PaymentSucceededEvent evt)
        {
            if (!Guid.TryParse(evt.OrderId, out var orderId))
            {
                _logger.LogWarning("Invalid OrderId: {OrderId}", evt.OrderId);
                return;
            }

            var order = await db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order not found: {OrderId}", evt.OrderId);
                return;
            }

            if (order.Status == OrderStatus.Paid)
            {
                _logger.LogInformation("Order {OrderId} already marked as paid", evt.OrderId);
                return;
            }

            order.Status = OrderStatus.Paid;
            order.PaymentTransactionId = evt.TransactionId;
            order.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            // Publish OrderPaidEvent for notification service
            publisher.PublishOrderPaid(new OrderPaidEvent(
                OrderId: order.Id.ToString(),
                UserId: order.UserId,
                UserEmail: order.UserEmail,
                TotalAmount: order.TotalAmount,
                PaymentTransactionId: evt.TransactionId,
                PaidAtUtc: DateTime.UtcNow
            ));

            _logger.LogInformation("Order {OrderId} marked as PAID", evt.OrderId);
        }

        private async Task HandlePaymentFailedAsync(
            OrderDbContext db,
            IRabbitMqPublisher publisher,
            PaymentFailedEvent evt)
        {
            if (!Guid.TryParse(evt.OrderId, out var orderId))
            {
                _logger.LogWarning("Invalid OrderId: {OrderId}", evt.OrderId);
                return;
            }

            var order = await db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order not found: {OrderId}", evt.OrderId);
                return;
            }

            order.Status = OrderStatus.PaymentFailed;
            order.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            // Publish OrderPaymentFailedEvent
            publisher.PublishOrderPaymentFailed(new OrderPaymentFailedEvent(
                OrderId: order.Id.ToString(),
                UserId: order.UserId,
                UserEmail: order.UserEmail,
                TotalAmount: order.TotalAmount,
                Reason: evt.Reason,
                FailedAtUtc: DateTime.UtcNow
            ));

            _logger.LogWarning("Order {OrderId} marked as PAYMENT FAILED: {Reason}",
                evt.OrderId, evt.Reason);
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}