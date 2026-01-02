namespace Zentec.OrderService.Messaging
{
    public record PaymentSucceededEvent(
        string OrderId,
        string PaymentIntentId,
        string TransactionId,
        decimal Amount,
        string Currency,
        DateTime PaidAtUtc);

    public record PaymentFailedEvent(
        string OrderId,
        string PaymentIntentId,
        string Reason,
        DateTime FailedAtUtc);
}