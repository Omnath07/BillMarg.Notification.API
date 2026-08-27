using BillMarg.Notification.API.Data;
using BillMarg.Notification.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace BillMarg.Notification.API.Services
{
    public class OverdueInvoiceEmailService : IOverdueInvoiceEmailService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OverdueInvoiceEmailService> _logger;

        public OverdueInvoiceEmailService(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<OverdueInvoiceEmailService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<OverdueInvoiceEmailResult>
            SendOverdueInvoiceEmailsAsync()
        {
            var result = new OverdueInvoiceEmailResult();

            var today = DateTime.Today;

            // Get overdue invoices
            var invoices = await _context.Invoices
                .Include(x => x.Client)
                .Include(x => x.Users)
                .Where(x =>
                    x.DueDate.Date < today &&
                    x.Status != "Paid" &&
                    x.Status != "Cancel" &&
                    x.ClientId != null &&
                    x.Client != null &&
                    x.Client.Email != null &&
                    x.Client.Email != "")
                .ToListAsync();

            if (!invoices.Any())
            {
                return result;
            }

            // Get all payments for these invoices in ONE query
            var invoiceIds = invoices
                .Select(x => x.InvoiceId)
                .ToList();

            var payments = await _context.InvoicePayments
                .Where(x => invoiceIds.Contains(x.InvoiceId))
                .GroupBy(x => x.InvoiceId)
                .Select(x => new
                {
                    InvoiceId = x.Key,
                    PaidAmount = x.Sum(p => p.Amount)
                })
                .ToDictionaryAsync(
                    x => x.InvoiceId,
                    x => x.PaidAmount);

            // Calculate pending amount
            foreach (var invoice in invoices)
            {
                invoice.PaidAmount =
                    payments.TryGetValue(
                        invoice.InvoiceId,
                        out var paid)
                        ? paid
                        : 0;
            }

            // Remove fully paid invoices
            invoices = invoices
                .Where(x => x.PendingAmount > 0)
                .ToList();

            if (!invoices.Any())
            {
                return result;
            }

            // GROUP BY CLIENT
            var clientGroups = invoices
                .GroupBy(x => new
                {
                    x.ClientId,
                    ClientName = x.Client!.Name,
                    ClientEmail = x.Client.Email
                })
                .ToList();

            result.ClientsProcessed = clientGroups.Count;

            foreach (var clientGroup in clientGroups)
            {
                try
                {
                    var clientInvoices = clientGroup
                        .OrderBy(x => x.DueDate)
                        .ToList();

                    var client = clientGroup.Key;

                    var totalInvoiceAmount =
                        clientInvoices.Sum(x => x.Amount);

                    var totalPaidAmount =
                        clientInvoices.Sum(x => x.PaidAmount);

                    var totalPendingAmount =
                        clientInvoices.Sum(x => x.PendingAmount);

                    var businessName =
                        clientInvoices
                            .FirstOrDefault()?
                            .Users?
                            .BusinessName
                        ?? "BillMarg Business";

                    var htmlBody =
                        BuildEmailTemplate(
                            businessName,
                            client.ClientName,
                            clientInvoices,
                            totalInvoiceAmount,
                            totalPaidAmount,
                            totalPendingAmount);

                    var subject =
                        $"Payment Reminder - {clientInvoices.Count} Overdue Invoice(s)";

                    await SendEmailAsync(
                        client.ClientEmail!,
                        subject,
                        htmlBody);

                    result.EmailsSent++;
                    result.InvoicesIncluded +=
                        clientInvoices.Count;

                    _logger.LogInformation(
                        "Overdue payment email sent to {Email}. " +
                        "Invoices: {InvoiceCount}, Outstanding: {Amount}",
                        client.ClientEmail,
                        clientInvoices.Count,
                        totalPendingAmount);
                }
                catch (Exception ex)
                {
                    result.FailedEmails++;

                    _logger.LogError(
                        ex,
                        "Failed to send overdue invoice email to {Email}",
                        clientGroup.Key.ClientEmail);
                }
            }

            return result;
        }

        private async Task SendEmailAsync(
     string recipient,
     string subject,
     string htmlBody)
        {
            var smtpServer =
                _configuration["EmailSettings:SmtpServer"];

            var portValue =
                _configuration["EmailSettings:Port"];

            var fromEmail =
                _configuration["EmailSettings:FromEmail"];

            var password =
                _configuration["EmailSettings:Password"];

            var fromName =
                _configuration["EmailSettings:FromName"]
                ?? "BillMarg";

            // ============================================================
            // VALIDATION
            // ============================================================

            if (string.IsNullOrWhiteSpace(smtpServer))
            {
                throw new InvalidOperationException(
                    "EmailSettings:SmtpServer is missing.");
            }

            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new InvalidOperationException(
                    "EmailSettings:FromEmail is missing.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "EmailSettings:Password is missing.");
            }

            if (!int.TryParse(
                    portValue,
                    out var smtpPort))
            {
                smtpPort = 587;
            }

            // ============================================================
            // CREATE EMAIL
            // ============================================================

            using var message = new MailMessage();

            message.From = new MailAddress(
                fromEmail,
                fromName);

            message.To.Add(
                new MailAddress(recipient));

            message.Subject =
                subject;

            message.SubjectEncoding =
                System.Text.Encoding.UTF8;

            message.Body =
                htmlBody;

            message.BodyEncoding =
                System.Text.Encoding.UTF8;

            message.IsBodyHtml =
                true;

            // ============================================================
            // SMTP
            // ============================================================

            using var smtp =
                new SmtpClient(
                    smtpServer,
                    smtpPort);

            smtp.Credentials =
                new System.Net.NetworkCredential(
                    fromEmail,
                    password);

            smtp.EnableSsl = true;

            // ============================================================
            // SEND
            // ============================================================

            await smtp.SendMailAsync(message);
        }
        private string BuildEmailTemplate(
        string businessName,
        string clientName,
        List<Invoices> invoices,
        decimal totalInvoiceAmount,
        decimal totalPaidAmount,
        decimal totalPendingAmount)
        {
            var rows = new StringBuilder();

            foreach (var invoice in invoices)
            {
                var overdueDays =
                    Math.Max(
                        0,
                        (DateTime.Today - invoice.DueDate.Date).Days);

                var overdueText =
                    overdueDays == 1
                        ? "1 day"
                        : $"{overdueDays} days";

                rows.Append($@"
<tr>
    <td style='padding:14px 12px;
               border-bottom:1px solid #e5e7eb;
               font-size:13px;
               color:#111827;
               font-weight:600;'>
        {WebUtility.HtmlEncode(invoice.InvoiceNumber)}
    </td>

    <td style='padding:14px 12px;
               border-bottom:1px solid #e5e7eb;
               font-size:13px;
               color:#4b5563;'>
        {invoice.InvoiceDate:dd MMM yyyy}
    </td>

    <td style='padding:14px 12px;
               border-bottom:1px solid #e5e7eb;
               font-size:13px;
               color:#dc2626;
               font-weight:600;'>
        {invoice.DueDate:dd MMM yyyy}
    </td>

    <td style='padding:14px 12px;
               border-bottom:1px solid #e5e7eb;
               font-size:13px;
               color:#dc2626;'>
        {overdueText}
    </td>

    <td style='padding:14px 12px;
               border-bottom:1px solid #e5e7eb;
               font-size:13px;
               color:#374151;
               text-align:right;'>
        ₹{invoice.Amount:N2}
    </td>

    <td style='padding:14px 12px;
               border-bottom:1px solid #e5e7eb;
               font-size:13px;
               color:#059669;
               text-align:right;'>
        ₹{invoice.PaidAmount:N2}
    </td>

    <td style='padding:14px 12px;
               border-bottom:1px solid #e5e7eb;
               font-size:13px;
               color:#dc2626;
               font-weight:700;
               text-align:right;'>
        ₹{invoice.PendingAmount:N2}
    </td>
</tr>");
            }

            var invoiceText =
                invoices.Count == 1
                    ? "invoice is"
                    : "invoices are";

            var invoiceCountText =
                invoices.Count == 1
                    ? "1 overdue invoice"
                    : $"{invoices.Count} overdue invoices";

            return $@"
<!DOCTYPE html>

<html lang='en'>

<head>

<meta charset='UTF-8'>

<meta name='viewport'
      content='width=device-width, initial-scale=1.0'>

<meta name='color-scheme'
      content='light'>

<meta name='supported-color-schemes'
      content='light'>

<title>Payment Reminder - BillMarg</title>

</head>

<body style='margin:0;
             padding:0;
             background-color:#f4f6f8;
             font-family:Arial,Helvetica,sans-serif;
             color:#1f2937;'>

<table width='100%'
       cellpadding='0'
       cellspacing='0'
       border='0'
       style='background-color:#f4f6f8;
              margin:0;
              padding:30px 0;'>

<tr>

<td align='center'>

<table width='700'
       cellpadding='0'
       cellspacing='0'
       border='0'
       style='width:700px;
              max-width:700px;
              background:#ffffff;
              border-radius:10px;
              overflow:hidden;'>

<!-- ================================================= -->
<!-- HEADER -->
<!-- ================================================= -->

<tr>

<td style='background:#111827;
           padding:28px 35px;'>

<table width='100%'
       cellpadding='0'
       cellspacing='0'
       border='0'>

<tr>

<td>

<div style='font-size:26px;
            line-height:32px;
            font-weight:700;
            color:#ffffff;'>
    {WebUtility.HtmlEncode(businessName)}
</div>

<div style='font-size:13px;
            line-height:20px;
            color:#d1d5db;
            margin-top:5px;'>
    Powered by BillMarg
</div>

</td>

<td align='right'
    valign='middle'>

<div style='display:inline-block;
            background:#fee2e2;
            color:#b91c1c;
            padding:7px 12px;
            border-radius:20px;
            font-size:12px;
            font-weight:700;'>
    PAYMENT OVERDUE
</div>

</td>

</tr>

</table>

</td>

</tr>

<!-- ================================================= -->
<!-- CONTENT -->
<!-- ================================================= -->

<tr>

<td style='padding:35px;'>

<p style='margin:0 0 18px 0;
          font-size:16px;
          line-height:25px;
          color:#111827;'>

Dear
<strong>
{WebUtility.HtmlEncode(clientName)}
</strong>,

</p>

<p style='margin:0 0 18px 0;
          font-size:14px;
          line-height:24px;
          color:#4b5563;'>

We hope you are doing well.

This is a friendly reminder that the following
{invoiceText} currently overdue and have an
outstanding balance.

</p>

<!-- ================================================= -->
<!-- ALERT -->
<!-- ================================================= -->

<table width='100%'
       cellpadding='0'
       cellspacing='0'
       border='0'
       style='margin:25px 0;'>

<tr>

<td style='background:#fff7ed;
           border-left:4px solid #f97316;
           padding:18px 20px;'>

<div style='font-size:15px;
            font-weight:700;
            color:#9a3412;
            margin-bottom:5px;'>

Payment Required

</div>

<div style='font-size:13px;
            line-height:21px;
            color:#7c2d12;'>

You currently have
<strong>
{invoiceCountText}
</strong>
with an outstanding balance of
<strong>
₹{totalPendingAmount:N2}
</strong>.

</div>

</td>

</tr>

</table>

<!-- ================================================= -->
<!-- SUMMARY -->
<!-- ================================================= -->

<table width='100%'
       cellpadding='0'
       cellspacing='0'
       border='0'
       style='margin-bottom:30px;'>

<tr>

<td width='33%'
    style='padding:18px 10px;
           background:#f8fafc;
           border:1px solid #e5e7eb;
           text-align:center;'>

<div style='font-size:11px;
            color:#6b7280;
            text-transform:uppercase;
            letter-spacing:.5px;'>
    Total Invoiced
</div>

<div style='font-size:20px;
            font-weight:700;
            color:#111827;
            margin-top:7px;'>
    ₹{totalInvoiceAmount:N2}
</div>

</td>

<td width='33%'
    style='padding:18px 10px;
           background:#f8fafc;
           border-top:1px solid #e5e7eb;
           border-bottom:1px solid #e5e7eb;
           text-align:center;'>

<div style='font-size:11px;
            color:#6b7280;
            text-transform:uppercase;
            letter-spacing:.5px;'>
    Total Paid
</div>

<div style='font-size:20px;
            font-weight:700;
            color:#059669;
            margin-top:7px;'>
    ₹{totalPaidAmount:N2}
</div>

</td>

<td width='34%'
    style='padding:18px 10px;
           background:#fff7f7;
           border:1px solid #fecaca;
           text-align:center;'>

<div style='font-size:11px;
            color:#991b1b;
            text-transform:uppercase;
            letter-spacing:.5px;'>
    Outstanding
</div>

<div style='font-size:20px;
            font-weight:700;
            color:#dc2626;
            margin-top:7px;'>
    ₹{totalPendingAmount:N2}
</div>

</td>

</tr>

</table>

<!-- ================================================= -->
<!-- INVOICE TABLE -->
<!-- ================================================= -->

<div style='font-size:17px;
            font-weight:700;
            color:#111827;
            margin-bottom:12px;'>

Overdue Invoice Details

</div>

<table width='100%'
       cellpadding='0'
       cellspacing='0'
       border='0'
       style='border:1px solid #e5e7eb;
              border-collapse:collapse;'>

<thead>

<tr style='background:#f8fafc;'>

<th align='left'
    style='padding:12px;
           border-bottom:2px solid #e5e7eb;
           font-size:11px;
           color:#6b7280;'>
    INVOICE
</th>

<th align='left'
    style='padding:12px;
           border-bottom:2px solid #e5e7eb;
           font-size:11px;
           color:#6b7280;'>
    INVOICE DATE
</th>

<th align='left'
    style='padding:12px;
           border-bottom:2px solid #e5e7eb;
           font-size:11px;
           color:#6b7280;'>
    DUE DATE
</th>

<th align='left'
    style='padding:12px;
           border-bottom:2px solid #e5e7eb;
           font-size:11px;
           color:#6b7280;'>
    OVERDUE
</th>

<th align='right'
    style='padding:12px;
           border-bottom:2px solid #e5e7eb;
           font-size:11px;
           color:#6b7280;'>
    AMOUNT
</th>

<th align='right'
    style='padding:12px;
           border-bottom:2px solid #e5e7eb;
           font-size:11px;
           color:#6b7280;'>
    PAID
</th>

<th align='right'
    style='padding:12px;
           border-bottom:2px solid #e5e7eb;
           font-size:11px;
           color:#6b7280;'>
    BALANCE
</th>

</tr>

</thead>

<tbody>

{rows}

<tr style='background:#f8fafc;'>

<td colspan='4'
    style='padding:15px 12px;
           font-size:13px;
           font-weight:700;
           color:#111827;'>
    TOTAL OUTSTANDING
</td>

<td align='right'
    style='padding:15px 12px;
           font-size:13px;
           font-weight:700;
           color:#111827;'>
    ₹{totalInvoiceAmount:N2}
</td>

<td align='right'
    style='padding:15px 12px;
           font-size:13px;
           font-weight:700;
           color:#059669;'>
    ₹{totalPaidAmount:N2}
</td>

<td align='right'
    style='padding:15px 12px;
           font-size:14px;
           font-weight:700;
           color:#dc2626;'>
    ₹{totalPendingAmount:N2}
</td>

</tr>

</tbody>

</table>

<!-- ================================================= -->
<!-- CTA -->
<!-- ================================================= -->

<table width='100%'
       cellpadding='0'
       cellspacing='0'
       border='0'
       style='margin-top:30px;'>

<tr>

<td style='background:#f8fafc;
           border:1px solid #e5e7eb;
           padding:20px;'>

<div style='font-size:14px;
            font-weight:700;
            color:#111827;
            margin-bottom:7px;'>

Kindly arrange the outstanding payment

</div>

<div style='font-size:13px;
            line-height:21px;
            color:#6b7280;'>

We request you to settle the outstanding balance
at your earliest convenience.

</div>

</td>

</tr>

</table>

<!-- ================================================= -->
<!-- MESSAGE -->
<!-- ================================================= -->

<p style='margin:28px 0 15px 0;
          font-size:14px;
          line-height:23px;
          color:#4b5563;'>

If you have already made any of these payments,
please disregard this reminder and share the
payment details with us so that we can update
our records.

</p>

<p style='margin:0 0 25px 0;
          font-size:14px;
          line-height:23px;
          color:#4b5563;'>

If you have any questions regarding the invoices
or outstanding balance, please contact us.

</p>

<p style='margin:0;
          font-size:14px;
          line-height:23px;
          color:#374151;'>

Thank you for your continued business and support.

<br><br>

Regards,<br>

<strong>
{WebUtility.HtmlEncode(businessName)}
</strong>

</p>

</td>

</tr>

<!-- ================================================= -->
<!-- FOOTER -->
<!-- ================================================= -->

<tr>

<td style='background:#f8fafc;
           border-top:1px solid #e5e7eb;
           padding:22px 35px;
           text-align:center;'>

<div style='font-size:12px;
            color:#6b7280;
            line-height:20px;'>

This is an automated payment reminder
powered by <strong>BillMarg</strong>.

<br>

Please do not reply directly to this automated email.

</div>

<div style='margin-top:10px;
            font-size:11px;
            color:#9ca3af;'>

© {DateTime.Today.Year} BillMarg. All rights reserved.

</div>

</td>

</tr>

</table>

</td>

</tr>

</table>

</body>

</html>";
        }
    }
}