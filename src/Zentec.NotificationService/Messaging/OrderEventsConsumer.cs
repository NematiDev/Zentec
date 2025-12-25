using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Zentec.NotificationService.Messaging
{
    /// <summary>
    /// Consumes order events from RabbitMQ and simulates sending notifications.
    /// </summary>
    public class OrderEventsConsumer : BackgroundService
    {
        private readonly RabbitMqOptions _options;
        private readonly ILogger<OrderEventsConsumer> _logger;
        private IConnection? _connection;
        private IModel? _channel;

        public OrderEventsConsumer(IOptions<RabbitMqOptions> options, ILogger<OrderEventsConsumer> logger)
        {
            _options = options.Value;
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

                _channel.ExchangeDeclare(exchange: _options.Exchange, type: ExchangeType.Topic, durable: true, autoDelete: false);
                _channel.QueueDeclare(queue: _options.Queue, durable: true, exclusive: false, autoDelete: false);

                // Bind to routing keys emitted by OrderService
                _channel.QueueBind(queue: _options.Queue, exchange: _options.Exchange, routingKey: "order.paid");
                _channel.QueueBind(queue: _options.Queue, exchange: _options.Exchange, routingKey: "order.payment_failed");

                _channel.BasicQos(0, 20, false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += OnMessageAsync;

                _channel.BasicConsume(queue: _options.Queue, autoAck: false, consumer: consumer);

                _logger.LogInformation("NotificationService consuming from exchange {Exchange} queue {Queue}", _options.Exchange, _options.Queue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start RabbitMQ consumer");
                // If RabbitMQ isn't available at startup, keep service alive and let orchestrator restart.
            }

            return Task.CompletedTask;
        }

        private async Task OnMessageAsync(object sender, BasicDeliverEventArgs ea)
        {
            try
            {
                var routingKey = ea.RoutingKey;
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                if (routingKey == "order.paid")
                {
                    var evt = JsonSerializer.Deserialize<OrderPaidEvent>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    _logger.LogInformation("[NOTIFY] Order PAID. OrderId={OrderId} Email={Email} Amount={Amount}", evt?.OrderId, evt?.UserEmail, evt?.TotalAmount);
                }
                else if (routingKey == "order.payment_failed")
                {
                    var evt = JsonSerializer.Deserialize<OrderPaymentFailedEvent>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    _logger.LogWarning("[NOTIFY] Order PAYMENT FAILED. OrderId={OrderId} Email={Email} Reason={Reason}", evt?.OrderId, evt?.UserEmail, evt?.Reason);
                }
                else
                {
                    _logger.LogInformation("[NOTIFY] Unknown routing key {RoutingKey}: {Body}", routingKey, json);
                }

                // Simulate sending email/SMS
                await Task.Delay(30);

                _channel?.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling message {RoutingKey}", ea.RoutingKey);
                // Requeue once
                _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        }

        public override void Dispose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
                _channel?.Dispose();
                _connection?.Dispose();
            }
            catch
            {
            }

            base.Dispose();
        }
    }
}
