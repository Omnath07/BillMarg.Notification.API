using BillMarg.Notification.API.Data;
using BillMarg.Notification.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace BillMarg.Notification.API.Services
{
    public class SubscriptionRenewalEmailService : ISubscriptionRenewalEmailService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SubscriptionRenewalEmailService> _logger;

        public SubscriptionRenewalEmailService(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<SubscriptionRenewalEmailService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<SubscriptionRenewalEmailResult> SendSubscriptionRenewalEmailsAsync()
        {
            var result = new SubscriptionRenewalEmailResult();
            var today = DateTime.Today;

            try
            {
                var users = await _context.Users
     .AsNoTracking()
     .Where(x => x.IsActive &&
                 !string.IsNullOrEmpty(x.Email) &&
                 x.ParentUserId == null) // ✅ Only primary users
     .ToListAsync();


                result.UsersChecked = users.Count;

                foreach (var user in users)
                {
                    try
                    {
                        var payment = await _context.PaymentDetails
                            .AsNoTracking()
                            .Where(x => x.UserId == user.UserId)
                            .OrderByDescending(x => x.PaymentDate)
                            .FirstOrDefaultAsync();

                        if (payment == null || payment.Duration <= 0)
                        {
                            result.SkippedUsers++;
                            continue;
                        }

                        var paymentDate = payment.PaymentDate.Date;
                        var expiryDate = paymentDate.AddDays(payment.Duration);
                        var daysRemaining = (expiryDate - today).Days;

                        // ✅ Only send if between +5 and −15 days
                        if (daysRemaining <= 5 && daysRemaining >= -15)
                        {
                            var subject = BuildEmailSubject(payment, daysRemaining);
                            var html = BuildEmailTemplate(user, payment, expiryDate, daysRemaining);

                            await SendEmailAsync(user.Email.Trim(), subject, html);

                            result.EmailsSent++;
                            UpdateCounters(result, daysRemaining);
                            _logger.LogInformation($"Email sent to {user.Email}");
                        }
                        else
                        {
                            result.SkippedUsers++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedEmails++;
                        _logger.LogError(ex, $"EMAIL FAILED: UserId={user.UserId}, Email={user.Email ?? "(no email)"}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SUBSCRIPTION RENEWAL PROCESS FAILED");
            }

            _logger.LogInformation($"Process completed. UsersChecked={result.UsersChecked}, EmailsSent={result.EmailsSent}, Skipped={result.SkippedUsers}, Failed={result.FailedEmails}");
            return result;
        }

        // ============================================================
        // COUNTERS
        // ============================================================
        private void UpdateCounters(SubscriptionRenewalEmailResult result, int daysRemaining)
        {
            if (daysRemaining >= 1 && daysRemaining <= 5)
                result.ExpiringInFiveDays++;
            else if (daysRemaining == 0)
                result.ExpiringToday++;
            else if (daysRemaining < 0 && daysRemaining >= -15)
                result.ExpiredOneToFifteenDays++;
        }

        // ============================================================
        // EMAIL SUBJECT
        // ============================================================
        private string BuildEmailSubject(PaymentDetails payment, int daysRemaining)
        {
            var planName = string.IsNullOrWhiteSpace(payment.MyPlan)
                ? "BillMarg Subscription"
                : payment.MyPlan.Trim();

            if (daysRemaining > 0)
                return $"Your BillMarg Subscription Expires in {daysRemaining} Days – {planName}";
            if (daysRemaining == 0)
                return "Important: Your BillMarg Subscription Expires Today";
            if (daysRemaining < 0)
                return $"Your BillMarg Subscription Expired {Math.Abs(daysRemaining)} Days Ago – {planName}";

            return "BillMarg Subscription Renewal Reminder";
        }

        // ============================================================
        // EMAIL TEMPLATE
        // ============================================================
        private string BuildEmailTemplate(Users user, PaymentDetails payment, DateTime expiryDate, int daysRemaining)
        {
            var businessName = WebUtility.HtmlEncode(user.BusinessName ?? "Your Business");
            var fullName = WebUtility.HtmlEncode(user.FullName ?? "Customer");
            var planName = WebUtility.HtmlEncode(payment.MyPlan ?? "BillMarg Subscription");
            var renewalUrl = WebUtility.HtmlEncode(_configuration["EmailSettings:WebsiteUrl"] ?? "https://billmarg.com");

            var message = daysRemaining >= 0
                ? $"Your subscription for <strong>{businessName}</strong> will expire on <strong>{expiryDate:dd MMM yyyy}</strong>. Renew now to continue uninterrupted service."
                : $"Your subscription expired on <strong>{expiryDate:dd MMM yyyy}</strong>. Renew today to reactivate your account.";

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head><meta charset='UTF-8'><title>BillMarg Subscription Renewal</title></head>
<body style='font-family:Arial,Helvetica,sans-serif;background:#f3f4f6;padding:30px;'>
<div style='max-width:680px;margin:auto;background:#fff;border-radius:12px;padding:30px;'>
<h2 style='color:#111827;'>BillMarg Subscription Renewal</h2>
<p>Dear <strong>{fullName}</strong>,</p>
<p>{message}</p>
<table style='width:100%;border-collapse:collapse;margin-top:20px;'>
<tr><td style='padding:8px;border:1px solid #e5e7eb;'>Plan</td><td style='padding:8px;border:1px solid #e5e7eb;'>{planName}</td></tr>
<tr><td style='padding:8px;border:1px solid #e5e7eb;'>Payment Date</td><td style='padding:8px;border:1px solid #e5e7eb;'>{payment.PaymentDate:dd MMM yyyy}</td></tr>
<tr><td style='padding:8px;border:1px solid #e5e7eb;'>Expiry Date</td><td style='padding:8px;border:1px solid #e5e7eb;color:#dc2626;'>{expiryDate:dd MMM yyyy}</td></tr>
<tr><td style='padding:8px;border:1px solid #e5e7eb;'>Amount</td><td style='padding:8px;border:1px solid #e5e7eb;'>₹{payment.Amount:N2}</td></tr>
</table>
<div style='text-align:center;margin-top:30px;'>
<a href='{renewalUrl}' style='background:#2563eb;color:#fff;padding:12px 28px;border-radius:6px;text-decoration:none;font-weight:bold;'>Renew Subscription</a>
</div>
<p style='margin-top:30px;font-size:13px;color:#6b7280;'>This is an automated reminder from BillMarg. Please do not reply directly to this email.</p>
</div>
</body>
</html>";
        }

        // ============================================================
        // SMTP EMAIL
        // ============================================================
        private async Task SendEmailAsync(string recipient, string subject, string htmlBody)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var portValue = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var password = _configuration["EmailSettings:Password"];
            var fromName = _configuration["EmailSettings:FromName"] ?? "BillMarg";

            using var client = new SmtpClient(smtpServer, portValue)
            {
                Credentials = new NetworkCredential(fromEmail, password),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(recipient);

            await client.SendMailAsync(mail);
        }
    }
}
