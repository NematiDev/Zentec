namespace Zentec.OrderService.Messaging
{
    public record OrderPaidEvent(
        string OrderId,
        string UserId,
        string UserEmail,
        decimal TotalAmount,
        string? PaymentTransactionId,
        DateTime PaidAtUtc);

    public record OrderPaymentFailedEvent(
        string OrderId,
        string UserId,
        string UserEmail,
        decimal TotalAmount,
        string Reason,
        DateTime FailedAtUtc);
}
