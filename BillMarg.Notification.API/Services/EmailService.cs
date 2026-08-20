 using BillMarg.Notification.API.Interface;
    using BillMarg.Notification.API.Model;
    using Microsoft.Extensions.Options;
    using System.Net;
    using System.Net.Mail;

    namespace BillMarg.Notification.API.Services
    {
        public class EmailService : IEmailService
        {
            private readonly EmailSettings _settings;
            private readonly ILogger<EmailService> _logger;

            public EmailService(
                IOptions<EmailSettings> options,
                ILogger<EmailService> logger)
            {
                _settings = options.Value;
                _logger = logger;
            }

            // =====================================================
            // SEND NORMAL EMAIL
            // =====================================================

            public async Task<bool> SendEmailAsync(
                string recipientEmail,
                string subject,
                string htmlBody)
            {
                try
                {
                    using MailMessage mail = new MailMessage();

                    mail.From = new MailAddress(
                        _settings.FromEmail,
                        _settings.FromName);

                    mail.To.Add(recipientEmail);

                    mail.Subject = subject;

                    mail.Body = htmlBody;

                    mail.IsBodyHtml = true;

                    using SmtpClient smtp = new SmtpClient(
                        _settings.SmtpServer,
                        _settings.Port);

                    smtp.EnableSsl = true;

                    smtp.Credentials = new NetworkCredential(
                        _settings.FromEmail,
                        _settings.Password);

                    await smtp.SendMailAsync(mail);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Email sending failed to {Email}",
                        recipientEmail);

                    return false;
                }
            }


            // =====================================================
            // SEND EMAIL WITH ATTACHMENT
            // =====================================================

            public async Task<bool> SendEmailWithAttachmentAsync(
                string recipientEmail,
                string subject,
                string htmlBody,
                string attachmentPath)
            {
                try
                {
                    using MailMessage mail = new MailMessage();

                    mail.From = new MailAddress(
                        _settings.FromEmail,
                        _settings.FromName);

                    mail.To.Add(recipientEmail);

                    mail.Subject = subject;

                    mail.Body = htmlBody;

                    mail.IsBodyHtml = true;


                    // Attachment

                    if (!string.IsNullOrWhiteSpace(attachmentPath)
                        && File.Exists(attachmentPath))
                    {
                        mail.Attachments.Add(
                            new Attachment(attachmentPath));
                    }


                    using SmtpClient smtp = new SmtpClient(
                        _settings.SmtpServer,
                        _settings.Port);

                    smtp.EnableSsl = true;

                    smtp.Credentials = new NetworkCredential(
                        _settings.FromEmail,
                        _settings.Password);

                    await smtp.SendMailAsync(mail);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Email with attachment failed to {Email}",
                        recipientEmail);

                    return false;
                }
            }
        }
    }
