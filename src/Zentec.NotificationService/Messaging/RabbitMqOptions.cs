namespace Zentec.NotificationService.Messaging
{
    public class RabbitMqOptions
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Exchange { get; set; } = "zentec.order";
        public string Queue { get; set; } = "zentec.notification";
    }
}
