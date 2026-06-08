using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SacoStayAPI.Services
{
    public class EmailService
    {
        private static readonly HttpClient HttpClient = new();
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var apiKey = _configuration["Resend:ApiKey"];
            var from = _configuration["Resend:From"];

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Thiếu cấu hình Resend API key.");
            if (string.IsNullOrWhiteSpace(from))
                throw new InvalidOperationException("Thiếu cấu hình Resend From address.");
            if (string.IsNullOrWhiteSpace(to))
                throw new InvalidOperationException("Địa chỉ email người nhận không hợp lệ.");

            var payload = new
            {
                from,
                to = new[] { to },
                subject,
                html = body
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                _logger.LogInformation("Sending email via Resend. From={From}, To={To}, Subject={Subject}", from, to, subject);

                var response = await HttpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Resend send failed. StatusCode={StatusCode}, Response={Response}", response.StatusCode, content);
                    throw new InvalidOperationException($"Không gửi được email tới {to}. Vui lòng kiểm tra cấu hình Resend.");
                }

                _logger.LogInformation("Email sent successfully to {To} with subject {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via Resend. From={From}, To={To}, Subject={Subject}", from, to, subject);
                throw new InvalidOperationException($"Không gửi được email tới {to}. Vui lòng kiểm tra lại cấu hình Resend.", ex);
            }
        }
    }
}