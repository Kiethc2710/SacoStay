
using MimeKit;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using MailKit.Security;
using MailKit.Net.Smtp;

namespace SacoStayAPI.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_configuration["Smtp:From"]));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;
                email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                    _configuration["Smtp:Host"],
                    int.Parse(_configuration["Smtp:Port"]),
                    SecureSocketOptions.StartTls
                );
                await smtp.AuthenticateAsync(_configuration["Smtp:User"], _configuration["Smtp:Pass"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw new Exception("Không gửi được email. Email không tồn tại hoặc không hợp lệ.", ex);
            }
        }
    }
} 