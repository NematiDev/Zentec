using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Zentec.NotificationService.Services
{
    /// <summary>
    /// Email service using Gmail SMTP with MailKit/MimeKit
    /// </summary>
    public class GmailEmailService : IEmailService
    {
        private readonly ILogger<GmailEmailService> _logger;
        private readonly IConfiguration _config;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _fromPassword;
        private readonly string _fromName;

        public GmailEmailService(ILogger<GmailEmailService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            _smtpServer = _config["Email:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            _fromEmail = _config["Email:FromEmail"] ?? throw new Exception("Email:FromEmail not configured");
            _fromPassword = _config["Email:FromPassword"] ?? throw new Exception("Email:FromPassword not configured");
            _fromName = _config["Email:FromName"] ?? "Zentec E-Commerce";
        }

        public async Task<bool> SendOrderConfirmationEmailAsync(string toEmail, string orderId, decimal amount)
        {
            try
            {
                var subject = $"Order Confirmation - {orderId}";
                var htmlBody = BuildOrderConfirmationHtml(orderId, amount);
                var textBody = $"Order Confirmation\n\nThank you for your order!\n\nOrder ID: {orderId}\nAmount: ${amount:F2}\nDate: {DateTime.UtcNow:MMMM dd, yyyy HH:mm} UTC";

                return await SendEmailAsync(toEmail, subject, htmlBody, textBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order confirmation email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendPaymentFailedEmailAsync(string toEmail, string orderId, string reason)
        {
            try
            {
                var subject = $"Payment Failed - Order {orderId}";
                var htmlBody = BuildPaymentFailedHtml(orderId, reason);
                var textBody = $"Payment Failed\n\nWe were unable to process payment for your order.\n\nOrder ID: {orderId}\nReason: {reason}\nDate: {DateTime.UtcNow:MMMM dd, yyyy HH:mm} UTC";

                return await SendEmailAsync(toEmail, subject, htmlBody, textBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send payment failed email to {Email}", toEmail);
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string textBody)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_fromName, _fromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                // Create multipart/alternative message (HTML + plain text fallback)
                var builder = new BodyBuilder
                {
                    HtmlBody = htmlBody,
                    TextBody = textBody
                };

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();

                // Connect to Gmail SMTP
                await client.ConnectAsync(_smtpServer, _smtpPort, SecureSocketOptions.StartTls);

                // Authenticate
                await client.AuthenticateAsync(_fromEmail, _fromPassword);

                // Send email
                await client.SendAsync(message);

                // Disconnect
                await client.DisconnectAsync(true);

                _logger.LogInformation("✅ Email sent successfully to {Email} - Subject: {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending email to {Email}", toEmail);
                return false;
            }
        }
        private string BuildOrderConfirmationHtml(string orderId, decimal amount)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; margin-top: 0; }}
        .order-details {{ background-color: white; padding: 15px; margin: 15px 0; border-left: 4px solid #4CAF50; border-radius: 4px; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; padding: 15px; }}
        .amount {{ font-size: 24px; color: #4CAF50; font-weight: bold; }}
        h1 {{ margin: 0; font-size: 28px; }}
        h3 {{ color: #333; margin-top: 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Order Confirmed!</h1>
        </div>
        <div class='content'>
            <p>Dear Customer,</p>
            <p>Thank you for your order! Your payment has been successfully processed.</p>
            
            <div class='order-details'>
                <h3>Order Details</h3>
                <p><strong>Order ID:</strong> {orderId}</p>
                <p><strong>Amount Paid:</strong> <span class='amount'>${amount:F2}</span></p>
                <p><strong>Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy HH:mm} UTC</p>
            </div>

            <p>Your order is now being processed and will be shipped soon. You will receive another email with tracking information once your order ships.</p>
            
            <p>If you have any questions, please don't hesitate to contact our support team.</p>
        </div>
        <div class='footer'>
            <p>This is an automated email from Zentec E-Commerce</p>
            <p>&copy; {DateTime.UtcNow.Year} Zentec. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildPaymentFailedHtml(string orderId, string reason)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f44336; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; margin-top: 0; }}
        .order-details {{ background-color: white; padding: 15px; margin: 15px 0; border-left: 4px solid #f44336; border-radius: 4px; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; padding: 15px; }}
        h1 {{ margin: 0; font-size: 28px; }}
        h3 {{ color: #333; margin-top: 0; }}
        ul {{ padding-left: 20px; }}
        li {{ margin-bottom: 8px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>❌ Payment Failed</h1>
        </div>
        <div class='content'>
            <p>Dear Customer,</p>
            <p>We were unable to process the payment for your order.</p>
            
            <div class='order-details'>
                <h3>Order Information</h3>
                <p><strong>Order ID:</strong> {orderId}</p>
                <p><strong>Reason:</strong> {reason}</p>
                <p><strong>Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy HH:mm} UTC</p>
            </div>

            <p><strong>What happens next?</strong></p>
            <ul>
                <li>Your order has been cancelled</li>
                <li>No charges have been made to your account</li>
                <li>All reserved items have been returned to stock</li>
            </ul>

            <p>If you would like to try again, please visit our website and place a new order.</p>
            
            <p>If you believe this was a mistake or need assistance, please contact our support team.</p>
        </div>
        <div class='footer'>
            <p>This is an automated email from Zentec E-Commerce</p>
            <p>&copy; {DateTime.UtcNow.Year} Zentec. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
