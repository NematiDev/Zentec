namespace Zentec.PaymentService.Messaging
{
    public interface IRabbitMqPublisher
    {
        void PublishPaymentSucceeded(PaymentSucceededEvent evt);
        void PublishPaymentFailed(PaymentFailedEvent evt);
    }
}