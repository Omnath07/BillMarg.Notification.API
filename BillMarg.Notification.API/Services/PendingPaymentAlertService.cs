using BillMarg.Notification.API.Data;
using BillMarg.Notification.API.Interface;
using BillMarg.Notification.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BillMarg.Notification.API.Services
{
    public class PendingPaymentAlertService : IPendingPaymentAlertService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public PendingPaymentAlertService(
            AppDbContext context,
            IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task SendPendingPaymentAlertsAsync(
            CancellationToken cancellationToken = default)
        {
            var users = await _context.UserNotificationSettings
                .AsNoTracking()
                .Where(x =>
                    x.PendingPaymentAlert &&
                    x.EmailEnabled)
                .Select(x => x.UserId)
                .ToListAsync(cancellationToken);

            foreach (var userId in users)
            {
                try
                {
                    await SendPendingPaymentAlertToUserAsync(
                        userId,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Pending payment alert failed for UserId {userId}: {ex.Message}");
                }
            }
        }

        public async Task<bool> SendPendingPaymentAlertToUserAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.UserId == userId,
                    cancellationToken);

            if (user == null ||
                string.IsNullOrWhiteSpace(user.Email))
            {
                return false;
            }

            // Pending invoices
            var invoices = await _context.Invoices
                .AsNoTracking()
                .Include(x => x.Client)
                .Where(x =>
                    x.UserId == userId &&
                    x.Status != "Paid")
                .OrderBy(x => x.DueDate)
                .ToListAsync(cancellationToken);




            if (!invoices.Any())
            {
                // No pending payment = no email
                return false;
            }

            var invoiceIds = invoices
                .Select(x => x.InvoiceId)
                .ToList();

            // Get payments
            var payments = await _context.InvoicePayments
                .AsNoTracking()
                .Where(x =>
                    invoiceIds.Contains(x.InvoiceId))
                .ToListAsync(cancellationToken);

            decimal totalPending = 0;

            var rows = "";

            foreach (var invoice in invoices)
            {
                string customerName = invoice.Client?.Name ?? "Unknown Customer";

                decimal paidAmount = payments
                    .Where(x => x.InvoiceId == invoice.InvoiceId)
                    .Sum(x => x.Amount);

                decimal pendingAmount =
                    invoice.Amount - paidAmount;

                if (pendingAmount <= 0)
                    continue;

                totalPending += pendingAmount;

                int overdueDays = 0;

                if (invoice.DueDate.Date < DateTime.Today)
                {
                    overdueDays =
                        (DateTime.Today - invoice.DueDate.Date).Days;
                }

                string status =
                    overdueDays > 0
                        ? $"Overdue by {overdueDays} days"
                        : "Payment Pending";

                rows += $@"
                <tr>
                <td style='padding:12px;border-bottom:1px solid #eee;'>
                        {customerName}
                    </td>
                    <td style='padding:12px;border-bottom:1px solid #eee;'>
                        {invoice.InvoiceNumber}
                    </td>

                    <td style='padding:12px;border-bottom:1px solid #eee;'>
                        {invoice.InvoiceDate:dd MMM yyyy}
                    </td>

                    <td style='padding:12px;border-bottom:1px solid #eee;'>
                        {invoice.DueDate:dd MMM yyyy}
                    </td>

                    <td style='padding:12px;border-bottom:1px solid #eee;text-align:right;'>
                        ₹{invoice.Amount:N2}
                    </td>

                    <td style='padding:12px;border-bottom:1px solid #eee;text-align:right;'>
                        ₹{paidAmount:N2}
                    </td>

                    <td style='padding:12px;border-bottom:1px solid #eee;text-align:right;font-weight:bold;'>
                        ₹{pendingAmount:N2}
                    </td>

                    <td style='padding:12px;border-bottom:1px solid #eee;'>
                        {status}
                    </td>
                </tr>";
            }

            if (totalPending <= 0)
                return false;

            string html = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
</head>

<body style='margin:0;background:#f4f6f9;font-family:Arial,sans-serif;'>

<table width='100%' cellpadding='0' cellspacing='0'
       style='background:#f4f6f9;padding:30px 0;'>

<tr>
<td align='center'>

<table width='700' cellpadding='0' cellspacing='0'
       style='background:#ffffff;border-radius:12px;overflow:hidden;
              box-shadow:0 4px 15px rgba(0,0,0,0.08);'>

<!-- HEADER -->

<tr>
<td style='background:#1f2937;padding:25px 30px;color:white;'>

<div style='font-size:26px;font-weight:bold;'>
BillMarg
</div>

<div style='font-size:14px;color:#d1d5db;margin-top:5px;'>
Empowering Your Business
</div>

</td>
</tr>

<!-- TITLE -->

<tr>
<td style='padding:30px;'>

<h2 style='margin:0;color:#111827;'>
Pending Payment Reminder
</h2>

<p style='color:#6b7280;margin-top:10px;'>
Hello {user.FullName},
</p>

<p style='color:#4b5563;line-height:1.6;'>
This is your daily payment reminder from
<strong>BillMarg</strong>.
The following invoices have outstanding payments.
</p>

</td>
</tr>

<!-- SUMMARY -->

<tr>
<td style='padding:0 30px 25px 30px;'>

<table width='100%' cellpadding='0' cellspacing='0'>

<tr>

<td width='50%'
style='background:#fff7ed;padding:20px;border-radius:8px;'>

<div style='font-size:13px;color:#9a3412;'>
TOTAL PENDING
</div>

<div style='font-size:28px;font-weight:bold;color:#c2410c;margin-top:5px;'>
₹{totalPending:N2}
</div>

</td>

<td width='20'></td>

<td width='50%'
style='background:#eff6ff;padding:20px;border-radius:8px;'>

<div style='font-size:13px;color:#1d4ed8;'>
PENDING INVOICES
</div>

<div style='font-size:28px;font-weight:bold;color:#1e40af;margin-top:5px;'>
{invoices.Count}
</div>

</td>

</tr>

</table>

</td>
</tr>

<!-- TABLE -->

<tr>
<td style='padding:0 30px 30px 30px;'>

<table width='100%' cellpadding='0' cellspacing='0'
style='border-collapse:collapse;font-size:13px;'>

<tr style='background:#f3f4f6;'>
<th style='padding:12px;text-align:left;'>Customer</th>
<th style='padding:12px;text-align:left;'>Invoice</th>
<th style='padding:12px;text-align:left;'>Invoice Date</th>
<th style='padding:12px;text-align:left;'>Due Date</th>
<th style='padding:12px;text-align:right;'>Amount</th>
<th style='padding:12px;text-align:right;'>Paid</th>
<th style='padding:12px;text-align:right;'>Pending</th>
<th style='padding:12px;text-align:left;'>Status</th>

</tr>

{rows}

</table>

</td>
</tr>

<!-- FOOTER -->

<tr>
<td style='background:#f9fafb;padding:25px 30px;color:#6b7280;font-size:12px;'>

<p style='margin:0;'>
This is an automated notification from BillMarg.
</p>

<p style='margin:8px 0 0 0;'>
Please review your pending invoices and follow up with your customers.
</p>

<p style='margin:15px 0 0 0;font-weight:bold;color:#374151;'>
© {DateTime.Now.Year} BillMarg
</p>

</td>
</tr>

</table>

</td>
</tr>

</table>

</body>
</html>";

            await _emailService.SendEmailAsync(
                user.Email,
                "BillMarg - Daily Pending Payment Reminder",
                html);

            return true;
        }
    }
}