using MailKit.Net.Smtp;
using MimeKit;

namespace EmployeeMonitoringAPI.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> TrySendEmailAsync(string toEmail, string subject, string body)
        {
            try { await SendEmailAsync(toEmail, subject, body); return true; }
            catch { return false; }
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string body)
        {
            var from = _configuration["EmailSettings:Email"];
            var host = _configuration["EmailSettings:Host"];
            var password = _configuration["EmailSettings:Password"];
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(password)) throw new InvalidOperationException("Email provider is not configured.");
            var email =
                new MimeMessage();

            email.From.Add(
                MailboxAddress.Parse(
                    from
                )
            );

            email.To.Add(
                MailboxAddress.Parse(toEmail)
            );

            email.Subject = subject;

            email.Body =
                new TextPart("html")
                {
                    Text = body
                };

            using var smtp =
                new SmtpClient();

            await smtp.ConnectAsync(
                host,
                int.Parse(
                    _configuration["EmailSettings:Port"]!
                ),
                MailKit.Security.SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(
                from,
                password
            );

            await smtp.SendAsync(email);

            await smtp.DisconnectAsync(true);
        }
    }
}