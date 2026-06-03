
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using MailKit.Security;
using MailKit.Net.Smtp;

namespace SacoStayAPI.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var host = _configuration["Smtp:Host"];
            var portValue = _configuration["Smtp:Port"];
            var user = _configuration["Smtp:User"];
            var password = _configuration["Smtp:Pass"];
            var from = _configuration["Smtp:From"];

            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("Thiếu cấu hình SMTP host.");
            if (string.IsNullOrWhiteSpace(portValue) || !int.TryParse(portValue, out var port))
                throw new InvalidOperationException("Cấu hình SMTP port không hợp lệ.");
            if (string.IsNullOrWhiteSpace(user))
                throw new InvalidOperationException("Thiếu cấu hình SMTP user.");
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Thiếu cấu hình SMTP password.");
            if (string.IsNullOrWhiteSpace(from))
                throw new InvalidOperationException("Thiếu cấu hình SMTP from.");
            if (string.IsNullOrWhiteSpace(to))
                throw new InvalidOperationException("Địa chỉ email người nhận không hợp lệ.");

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(from));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            try
            {
                _logger.LogInformation("Sending email via SMTP. Host={Host}, Port={Port}, From={From}, To={To}, Subject={Subject}", host, port, from, to, subject);

                await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(user, password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {To} with subject {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email. Host={Host}, Port={Port}, From={From}, To={To}, Subject={Subject}", host, port, from, to, subject);
                try
                {
                    if (smtp.IsConnected)
                        await smtp.DisconnectAsync(true);
                }
                catch
                {
                    // Ignore disconnect errors to preserve original failure.
                }

                throw new InvalidOperationException($"Không gửi được email tới {to}. Vui lòng kiểm tra lại cấu hình SMTP.", ex);
            }
        }
    }
} 