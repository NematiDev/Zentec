namespace Zentec.NotificationService.Services
{
    /// <summary>
    /// Email service interface
    /// </summary>
    public interface IEmailService
    {
        Task<bool> SendOrderConfirmationEmailAsync(string toEmail, string orderId, decimal amount);
        Task<bool> SendPaymentFailedEmailAsync(string toEmail, string orderId, string reason);
    }
}
