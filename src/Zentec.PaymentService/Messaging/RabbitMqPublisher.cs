using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Zentec.PaymentService.Messaging
{
    public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly RabbitMqOptions _options;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private IConnection? _connection;
        private RabbitMQ.Client.IModel? _channel;
        private readonly object _lock = new();

        public RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public void PublishPaymentSucceeded(PaymentSucceededEvent evt)
            => Publish("payment.succeeded", evt);

        public void PublishPaymentFailed(PaymentFailedEvent evt)
            => Publish("payment.failed", evt);

        private void EnsureConnection()
        {
            if (_connection?.IsOpen == true && _channel?.IsOpen == true)
                return;

            lock (_lock)
            {
                _connection?.Dispose();
                _channel?.Dispose();

                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true);
            }
        }

        private void Publish(string routingKey, object payload)
        {
            try
            {
                EnsureConnection();
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var body = Encoding.UTF8.GetBytes(json);
                var props = _channel!.CreateBasicProperties();
                props.Persistent = true;

                _channel.BasicPublish(_options.Exchange, routingKey, props, body);
                _logger.LogInformation("Published {RoutingKey}", routingKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish {RoutingKey}", routingKey);
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}