using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Zentec.OrderService.Messaging
{
    public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly RabbitMqOptions _options;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private IConnection? _connection;
        private IModel? _channel;
        private readonly object _lock = new();

        public RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public void PublishOrderPaid(OrderPaidEvent evt) => Publish("order.paid", evt);

        public void PublishOrderPaymentFailed(OrderPaymentFailedEvent evt) => Publish("order.payment_failed", evt);

        private void EnsureConnection()
        {
            if (_connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen)
                return;

            lock (_lock)
            {
                if (_connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen)
                    return;

                _connection?.Dispose();
                _channel?.Dispose();

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

                // durable topic exchange
                _channel.ExchangeDeclare(exchange: _options.Exchange, type: ExchangeType.Topic, durable: true, autoDelete: false);
            }
        }

        private void Publish(string routingKey, object payload)
        {
            try
            {
                EnsureConnection();

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                var body = Encoding.UTF8.GetBytes(json);

                var props = _channel!.CreateBasicProperties();
                props.ContentType = "application/json";
                props.DeliveryMode = 2; // persistent

                _channel.BasicPublish(
                    exchange: _options.Exchange,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: props,
                    body: body);

                _logger.LogInformation("Published RabbitMQ event {RoutingKey} for payload type {Type}", routingKey, payload.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish RabbitMQ event {RoutingKey}", routingKey);
                // For this assignment, we log and continue; in production you might use outbox pattern.
            }
        }

        public void Dispose()
        {
            try
            {
                _channel?.Dispose();
                _connection?.Dispose();
            }
            catch
            {
                // ignore
            }
        }
    }
}
