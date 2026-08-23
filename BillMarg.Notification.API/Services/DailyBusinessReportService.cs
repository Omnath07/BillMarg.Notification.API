using BillMarg.Notification.API.Data;
using BillMarg.Notification.API.Interface;
using BillMarg.Notification.API.Model;
using BillMarg.Notification.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BillMarg.Notification.API.Services
{
    public class DailyBusinessReportService : IDailyBusinessReportService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<DailyBusinessReportService> _logger;

        public DailyBusinessReportService(
            AppDbContext context,
            IEmailService emailService,
            IOptions<EmailSettings> emailOptions,
            ILogger<DailyBusinessReportService> logger)
        {
            _context = context;
            _emailService = emailService;
            _emailSettings = emailOptions.Value;
            _logger = logger;
        }

        // =========================================================
        // SEND REPORT TO ALL USERS WHO ENABLED DAILY REPORT
        // =========================================================

        public async Task SendDailyBusinessReportsAsync()
        {
            try
            {
                var settingsList = await _context.UserNotificationSettings
                    .AsNoTracking()
                    .Where(x =>
                        x.DailyBusinessReport == true &&
                        x.EmailEnabled == true)
                    .ToListAsync();

                _logger.LogInformation(
                    "Daily report process started. Users found: {Count}",
                    settingsList.Count);

                foreach (var settings in settingsList)
                {
                    try
                    {
                        await SendDailyBusinessReportToUserAsync(
                            settings.UserId);
                    }
                    catch (Exception ex)
                    {
                        // One user's failure should NOT stop other users
                        _logger.LogError(
                            ex,
                            "Daily report failed for UserId {UserId}",
                            settings.UserId);
                    }
                }

                _logger.LogInformation(
                    "Daily business report process completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Daily business report process failed.");
            }
        }


        // =========================================================
        // SEND REPORT TO ONE USER
        // =========================================================

        public async Task<bool> SendDailyBusinessReportToUserAsync(
            int userId)
        {
            // -----------------------------------------------------
            // USER
            // -----------------------------------------------------

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.IsActive);

            if (user == null)
            {
                _logger.LogWarning(
                    "User not found or inactive. UserId: {UserId}",
                    userId);

                return false;
            }


            // -----------------------------------------------------
            // TODAY
            // -----------------------------------------------------

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);


            // -----------------------------------------------------
            // INVOICES
            // -----------------------------------------------------

            var invoices = await _context.Invoices
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.InvoiceDate >= today &&
                    x.InvoiceDate < tomorrow)
                .ToListAsync();


            // -----------------------------------------------------
            // QUOTATIONS
            // -----------------------------------------------------

            var quotations = await _context.Quotations
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.QuotationDate >= today &&
                    x.QuotationDate < tomorrow)
                .ToListAsync();


            // -----------------------------------------------------
            // EXPENSES
            // -----------------------------------------------------

            var expenses = await _context.Expenses
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.ExpenseDate >= today &&
                    x.ExpenseDate < tomorrow)
                .ToListAsync();


            // -----------------------------------------------------
            // PURCHASES
            // -----------------------------------------------------

            var purchases = await _context.Purchases
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.PurchaseDate >= today &&
                    x.PurchaseDate < tomorrow)
                .ToListAsync();


            // -----------------------------------------------------
            // PAYMENTS
            // -----------------------------------------------------

            var payments = await _context.InvoicePayments
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.PaymentDate >= today &&
                    x.PaymentDate < tomorrow)
                .ToListAsync();


            // -----------------------------------------------------
            // QUICK BILLS
            // -----------------------------------------------------
            var quickBills = await _context.QuickBills
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.BillDate >= today && x.BillDate < tomorrow)
                .ToListAsync();

            // =====================================================
            // CALCULATIONS
            // =====================================================

            decimal invoiceAmount =
                invoices.Sum(x => x.Amount);

            decimal quotationAmount =
                quotations.Sum(x => x.Amount);

            decimal expenseAmount =
                expenses.Sum(x => x.Amount);

            decimal purchaseAmount =
                purchases.Sum(x => x.GrandTotal);

            decimal paymentReceived =
                payments.Sum(x => x.Amount);

            decimal quickBillAmount = quickBills.Sum(x => x.GrandTotal);

            // =====================================================
            // EMAIL SUBJECT
            // =====================================================

            string subject =
                $"Daily Business Summary | {user.BusinessName ?? "BillMarg"} | {today:dd MMM yyyy}";


            // =====================================================
            // EMAIL BODY
            // =====================================================

            string htmlBody =
                BuildDailyReportEmail(
                    user,
                    today,
                    invoices.Count,
                    invoiceAmount,
                    quotations.Count,
                    quotationAmount,
                    expenses.Count,
                    expenseAmount,
                    purchases.Count,
                    purchaseAmount,
                    payments.Count,
                    paymentReceived, quickBillAmount);


            // =====================================================
            // SEND EMAIL
            // =====================================================

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning(
                    "User has no email address. UserId: {UserId}",
                    userId);

                return false;
            }

            bool result =
                await _emailService.SendEmailAsync(
                    user.Email,
                    subject,
                    htmlBody);


            if (result)
            {
                _logger.LogInformation(
                    "Daily report successfully sent to {Email}",
                    user.Email);
            }
            else
            {
                _logger.LogWarning(
                    "Daily report could not be sent to {Email}",
                    user.Email);
            }

            return result;
        }


        // =========================================================
        // BUILD PROFESSIONAL EMAIL
        // =========================================================

        private string BuildDailyReportEmail(
            Users user,
            DateTime reportDate,
            int invoiceCount,
            decimal invoiceAmount,
            int quotationCount,
            decimal quotationAmount,
            int expenseCount,
            decimal expenseAmount,
            int purchaseCount,
            decimal purchaseAmount,
            int paymentCount,
            decimal paymentReceived, decimal quickBillAmount)
        {
            string businessName =
                string.IsNullOrWhiteSpace(user.BusinessName)
                    ? "Your Business"
                    : user.BusinessName;


            string logoUrl =
                string.IsNullOrWhiteSpace(_emailSettings.LogoUrl)
                    ? "https://dev.billmarg.com/image/billmarg-logo.png"
                    : _emailSettings.LogoUrl;


            string websiteUrl =
                string.IsNullOrWhiteSpace(_emailSettings.WebsiteUrl)
                    ? "https://dev.billmarg.com"
                    : _emailSettings.WebsiteUrl;


            return $@"
<!DOCTYPE html>

<html>

<head>

<meta charset='UTF-8' />

<meta name='viewport'
      content='width=device-width, initial-scale=1.0' />

<title>Daily Business Summary</title>

</head>


<body style='margin:0;
             padding:0;
             background:#f4f7fb;
             font-family:Arial,Helvetica,sans-serif;'>


<table width='100%'
       cellpadding='0'
       cellspacing='0'
       style='background:#f4f7fb;padding:35px 10px;'>


<tr>

<td align='center'>


<table width='650'
       cellpadding='0'
       cellspacing='0'
       style='max-width:650px;
              width:100%;
              background:#ffffff;
              border-radius:16px;
              overflow:hidden;
              box-shadow:0 5px 25px rgba(0,0,0,.08);'>


<!-- ================================================= -->
<!-- HEADER -->
<!-- ================================================= -->

<tr>

<td style='background:#0d6efd;
           padding:28px;
           text-align:center;'>


<img src='{logoUrl}'
     alt='BillMarg'
     style='height:55px;
            max-width:180px;
            object-fit:contain;
            margin-bottom:12px;' />


<h1 style='margin:0;
           color:#ffffff;
           font-size:23px;'>
    Daily Business Summary
</h1>


<p style='margin:8px 0 0;
          color:#dbe9ff;
          font-size:14px;'>
    {reportDate:dd MMMM yyyy}
</p>


</td>

</tr>


<!-- ================================================= -->
<!-- GREETING -->
<!-- ================================================= -->

<tr>

<td style='padding:30px 35px 15px;'>


<h2 style='margin:0 0 10px;
           color:#222;
           font-size:20px;'>
    Hello {user.FullName},
</h2>


<p style='margin:0;
          color:#666;
          font-size:15px;
          line-height:25px;'>

Here is your daily business summary for
<strong>{reportDate:dd MMMM yyyy}</strong>.

</p>


</td>

</tr>


<!-- ================================================= -->
<!-- BUSINESS -->
<!-- ================================================= -->

<tr>

<td style='padding:10px 35px 20px;'>


<div style='background:#f8f9fa;
            border-radius:10px;
            padding:15px;
            text-align:center;'>

<strong style='font-size:17px;color:#333;'>
    {businessName}
</strong>

<br />

<span style='font-size:12px;color:#777;'>
    Business Activity Summary
</span>

</div>


</td>

</tr>


<!-- ================================================= -->
<!-- SUMMARY -->
<!-- ================================================= -->

<tr>

<td style='padding:0 35px 20px;'>


<table width='100%'
       cellpadding='0'
       cellspacing='0'>


<tr>


<!-- INVOICE -->

<td width='50%'
    style='padding:8px;'>

<div style='border:1px solid #e5e7eb;
            border-radius:12px;
            padding:18px;
            background:#ffffff;'>

<div style='font-size:13px;
            color:#777;'>
    Invoices
</div>

<div style='font-size:22px;
            font-weight:bold;
            color:#0d6efd;
            margin-top:5px;'>
    {invoiceCount}
</div>

<div style='font-size:14px;
            color:#333;
            margin-top:5px;'>
    ₹ {invoiceAmount:N2}
</div>

</div>

</td>


<!-- QUOTATION -->

<td width='50%'
    style='padding:8px;'>

<div style='border:1px solid #e5e7eb;
            border-radius:12px;
            padding:18px;
            background:#ffffff;'>

<div style='font-size:13px;
            color:#777;'>
    Quotations
</div>

<div style='font-size:22px;
            font-weight:bold;
            color:#6f42c1;
            margin-top:5px;'>
    {quotationCount}
</div>

<div style='font-size:14px;
            color:#333;
            margin-top:5px;'>
    ₹ {quotationAmount:N2}
</div>

</div>

</td>


</tr>


<tr>


<!-- EXPENSE -->

<td width='50%'
    style='padding:8px;'>

<div style='border:1px solid #e5e7eb;
            border-radius:12px;
            padding:18px;
            background:#ffffff;'>

<div style='font-size:13px;
            color:#777;'>
    Expenses
</div>

<div style='font-size:22px;
            font-weight:bold;
            color:#dc3545;
            margin-top:5px;'>
    {expenseCount}
</div>

<div style='font-size:14px;
            color:#333;
            margin-top:5px;'>
    ₹ {expenseAmount:N2}
</div>

</div>

</td>


<!-- PURCHASE -->

<td width='50%'
    style='padding:8px;'>

<div style='border:1px solid #e5e7eb;
            border-radius:12px;
            padding:18px;
            background:#ffffff;'>

<div style='font-size:13px;
            color:#777;'>
    Purchases
</div>

<div style='font-size:22px;
            font-weight:bold;
            color:#198754;
            margin-top:5px;'>
    {purchaseCount}
</div>

<div style='font-size:14px;
            color:#333;
            margin-top:5px;'>
    ₹ {purchaseAmount:N2}
</div>

</div>

</td>


</tr>


</table>


</td>

</tr>


<!-- ================================================= -->
<!-- PAYMENT -->
<!-- ================================================= -->

<tr>

<td style='padding:5px 35px 25px;'>


<div style='background:#eaf7ef;
            border-radius:12px;
            padding:20px;
            text-align:center;'>


<div style='font-size:13px;
            color:#666;'>
    Payment Received Today
</div>


<div style='font-size:28px;
            font-weight:bold;
            color:#198754;
            margin-top:6px;'>
    ₹ {paymentReceived:N2}
</div>


<div style='font-size:13px;
            color:#777;
            margin-top:5px;'>
    {paymentCount} payment(s) received
</div>


</div>


</td>

</tr>


 <!-- ================================================= -->
    <!-- QUICKBILL -->
    <!-- ================================================= -->

    <tr>
    <td style='padding:5px 35px 25px;'>

    <div style='background:#fff3cd;
                border-radius:12px;
                padding:20px;
                text-align:center;'>

        <div style='font-size:13px;
                    color:#856404;'>
            Quick Bills Generated Today
        </div>

        <div style='font-size:28px;
                    font-weight:bold;
                    color:#fd7e14;
                    margin-top:6px;'>
            ₹ {quickBillAmount:N2}
        </div>

        <div style='font-size:13px;
                    color:#777;
                    margin-top:5px;'>
            Total value of all QuickBills created
        </div>

    </div>

    </td>
    </tr>


<!-- ================================================= -->
<!-- QUICK SUMMARY -->
<!-- ================================================= -->

<tr>

<td style='padding:0 35px 30px;'>


<table width='100%'
       cellpadding='10'
       cellspacing='0'
       style='border-collapse:collapse;
              border:1px solid #e5e7eb;
              border-radius:10px;'>


<tr style='background:#f8f9fa;'>

<td style='font-weight:bold;color:#555;'>
    Business Activity
</td>

<td align='right'
    style='font-weight:bold;color:#555;'>
    Today
</td>

</tr>


<tr>

<td style='border-top:1px solid #eee;'>
    Invoice Amount
</td>

<td align='right'
    style='border-top:1px solid #eee;'>
    ₹ {invoiceAmount:N2}
</td>

</tr>


<tr>

<td style='border-top:1px solid #eee;'>
    Payment Received
</td>

<td align='right'
    style='border-top:1px solid #eee;color:#198754;'>
    ₹ {paymentReceived:N2}
</td>

</tr>


<tr>

<td style='border-top:1px solid #eee;'>
    Expenses
</td>

<td align='right'
    style='border-top:1px solid #eee;color:#dc3545;'>
    ₹ {expenseAmount:N2}
</td>

</tr>


<tr>

<td style='border-top:1px solid #eee;'>
    Purchases
</td>

<td align='right'
    style='border-top:1px solid #eee;'>
    ₹ {purchaseAmount:N2}
</td>

</tr>


<!-- ✅ NEW QUICKBILL ROW -->
<tr>
    <td style='border-top:1px solid #eee;'>Quick Bills</td>
    <td align='right' style='border-top:1px solid #eee;color:#fd7e14;'>₹ {quickBillAmount:N2}</td>
</tr>

</table>


</td>

</tr>


<!-- ================================================= -->
<!-- BUTTON -->
<!-- ================================================= -->

<tr>

<td align='center'
    style='padding:0 35px 35px;'>


<a href='{websiteUrl}'
   style='display:inline-block;
          background:#0d6efd;
          color:#ffffff;
          text-decoration:none;
          padding:13px 28px;
          border-radius:8px;
          font-size:14px;
          font-weight:bold;'>

Open BillMarg Dashboard

</a>


</td>

</tr>


<!-- ================================================= -->
<!-- FOOTER -->
<!-- ================================================= -->

<tr>

<td style='background:#f8f9fa;
           padding:25px;
           text-align:center;'>


<div style='font-size:15px;
            font-weight:bold;
            color:#333;'>
    BillMarg
</div>


<div style='font-size:13px;
            color:#777;
            margin-top:7px;'>
    Empowering Small Businesses
</div>


<div style='font-size:12px;
            color:#999;
            margin-top:12px;'>
    This is an automated daily business summary.
</div>


<div style='font-size:12px;
            color:#999;
            margin-top:5px;'>
    You are receiving this email because
    Daily Business Report is enabled in your BillMarg account.
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