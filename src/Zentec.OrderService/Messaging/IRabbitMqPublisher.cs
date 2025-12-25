namespace Zentec.OrderService.Messaging
{
    public interface IRabbitMqPublisher
    {
        void PublishOrderPaid(OrderPaidEvent evt);
        void PublishOrderPaymentFailed(OrderPaymentFailedEvent evt);
    }
}
