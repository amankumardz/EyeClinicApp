using EyeClinicApp.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace EyeClinicApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailService(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_smtpSettings.Email),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail.Trim());

            using var smtp = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_smtpSettings.Email, _smtpSettings.Password)
            };

            await smtp.SendMailAsync(message);
        }
    }
}
