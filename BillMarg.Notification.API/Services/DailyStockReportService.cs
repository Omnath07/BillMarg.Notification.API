using BillMarg.Notification.API.Data;
using BillMarg.Notification.API.Interface;
using BillMarg.Notification.API.Model;
using BillSathi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BillMarg.Notification.API.Services
{
    public class DailyStockReportService : IDailyStockReportService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<DailyStockReportService> _logger;

        public DailyStockReportService(
            AppDbContext context,
            IEmailService emailService,
            IOptions<EmailSettings> emailOptions,
            ILogger<DailyStockReportService> logger)
        {
            _context = context;
            _emailService = emailService;
            _emailSettings = emailOptions.Value;
            _logger = logger;
        }

        // =========================================================
        // DAILY STOCK INVENTORY REPORT
        // SEND TO ALL ELIGIBLE USERS
        // =========================================================

        public async Task SendDailyStockInventoryReportsAsync()
        {
            try
            {
                var settingsList = await _context.UserNotificationSettings
                    .AsNoTracking()
                    .Where(x => x.EmailEnabled)
                    .ToListAsync();

                _logger.LogInformation(
                    "Daily stock inventory report started. Users found: {Count}",
                    settingsList.Count);

                foreach (var settings in settingsList)
                {
                    try
                    {
                        await SendDailyStockInventoryReportToUserAsync(
                            settings.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Daily stock inventory report failed for UserId {UserId}",
                            settings.UserId);
                    }
                }

                _logger.LogInformation(
                    "Daily stock inventory report completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Daily stock inventory report process failed.");
            }
        }

        // =========================================================
        // SEND INVENTORY REPORT TO ONE USER
        // =========================================================

        public async Task<bool> SendDailyStockInventoryReportToUserAsync(
            int userId)
        {
            try
            {
                // -------------------------------------------------
                // USER
                // -------------------------------------------------

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

                // -------------------------------------------------
                // EMAIL
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    _logger.LogWarning(
                        "User has no email address. UserId: {UserId}",
                        userId);

                    return false;
                }

                // -------------------------------------------------
                // PRODUCTS
                // -------------------------------------------------

                var products = await _context.Products
                    .AsNoTracking()
                    .Where(x =>
                        x.UserId == userId &&
                        x.IsActive)
                    .OrderBy(x => x.ProductName)
                    .ToListAsync();

                // -------------------------------------------------
                // NO PRODUCTS
                // -------------------------------------------------

                if (!products.Any())
                {
                    _logger.LogInformation(
                        "No active products found for UserId {UserId}",
                        userId);

                    return true;
                }

                // -------------------------------------------------
                // CALCULATIONS
                // -------------------------------------------------

                int totalProducts = products.Count;

                decimal totalQuantity =
                    products.Sum(x => x.CurrentStock);

                decimal totalPurchaseValue =
                    products.Sum(x =>
                        x.CurrentStock * x.PurchasePrice);

                decimal totalSellingValue =
                    products.Sum(x =>
                        x.CurrentStock * x.SellingPrice);

                decimal potentialProfit =
                    totalSellingValue - totalPurchaseValue;

                int lowStockCount =
                    products.Count(x =>
                        x.CurrentStock > 0 &&
                        x.CurrentStock <= x.MinimumStock);

                int outOfStockCount =
                    products.Count(x =>
                        x.CurrentStock <= 0);

                // -------------------------------------------------
                // SUBJECT
                // -------------------------------------------------

                string businessName =
                    string.IsNullOrWhiteSpace(user.BusinessName)
                        ? "BillMarg"
                        : user.BusinessName;

                string subject =
                    $"Daily Stock Inventory Report | {businessName} | {DateTime.Today:dd MMM yyyy}";

                // -------------------------------------------------
                // EMAIL BODY
                // -------------------------------------------------

                string htmlBody =
                    BuildDailyStockInventoryEmail(
                        user,
                        products,
                        totalProducts,
                        totalQuantity,
                        totalPurchaseValue,
                        totalSellingValue,
                        potentialProfit,
                        lowStockCount,
                        outOfStockCount);

                // -------------------------------------------------
                // SEND
                // -------------------------------------------------

                bool result =
                    await _emailService.SendEmailAsync(
                        user.Email,
                        subject,
                        htmlBody);

                if (result)
                {
                    _logger.LogInformation(
                        "Daily stock inventory report successfully sent to {Email}",
                        user.Email);
                }
                else
                {
                    _logger.LogWarning(
                        "Daily stock inventory report could not be sent to {Email}",
                        user.Email);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending daily stock inventory report to UserId {UserId}",
                    userId);

                return false;
            }
        }

        // =========================================================
        // LOW STOCK ALERT
        // SEND TO ALL ELIGIBLE USERS
        // =========================================================

        public async Task SendDailyLowStockAlertsAsync()
        {
            try
            {
                _logger.LogInformation(
                    "========== LOW STOCK ALERT STARTED ==========");

                var settingsList =
                    await _context.UserNotificationSettings
                        .AsNoTracking()
                        .Where(x =>
                            x.EmailEnabled &&
                            x.LowStockAlert)
                        .ToListAsync();

                _logger.LogInformation(
                    "Low Stock eligible users found: {Count}",
                    settingsList.Count);

                foreach (var settings in settingsList)
                {
                    try
                    {
                        _logger.LogInformation(
                            "Sending Low Stock Alert for UserId: {UserId}",
                            settings.UserId);

                        bool result =
                            await SendLowStockAlertToUserAsync(
                                settings.UserId);

                        _logger.LogInformation(
                            "Low Stock Alert result for UserId {UserId}: {Result}",
                            settings.UserId,
                            result);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Low Stock Alert failed for UserId {UserId}",
                            settings.UserId);
                    }
                }

                _logger.LogInformation(
                    "========== LOW STOCK ALERT COMPLETED ==========");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "LOW STOCK ALERT PROCESS FAILED");
            }
        }
        // =========================================================
        // SEND LOW STOCK ALERT TO ONE USER
        // =========================================================

        public async Task<bool> SendLowStockAlertToUserAsync(
            int userId)
        {
            try
            {
                // -------------------------------------------------
                // USER
                // -------------------------------------------------

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

                // -------------------------------------------------
                // EMAIL
                // -------------------------------------------------

                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    _logger.LogWarning(
                        "User has no email address. UserId: {UserId}",
                        userId);

                    return false;
                }

                // -------------------------------------------------
                // LOW STOCK PRODUCTS
                // -------------------------------------------------

                var lowStockProducts = await _context.Products
                    .AsNoTracking()
                    .Where(x =>
                        x.UserId == userId &&
                        x.IsActive &&
                        x.CurrentStock <= x.MinimumStock)
                    .OrderBy(x => x.CurrentStock)
                    .ThenBy(x => x.ProductName)
                    .ToListAsync();

                // -------------------------------------------------
                // NO LOW STOCK PRODUCTS
                // -------------------------------------------------

                if (!lowStockProducts.Any())
                {
                    _logger.LogInformation(
                        "No low stock products found for UserId {UserId}",
                        userId);

                    return true;
                }

                // -------------------------------------------------
                // CALCULATIONS
                // -------------------------------------------------

                int totalProducts =
                    lowStockProducts.Count;

                int outOfStockProducts =
                    lowStockProducts.Count(x =>
                        x.CurrentStock <= 0);

                int lowStockOnly =
                    lowStockProducts.Count(x =>
                        x.CurrentStock > 0 &&
                        x.CurrentStock <= x.MinimumStock);

                decimal totalStockValue =
                    lowStockProducts.Sum(x =>
                        x.CurrentStock * x.PurchasePrice);

                decimal requiredStockValue =
                    lowStockProducts.Sum(x =>
                        Math.Max(
                            0,
                            x.MinimumStock - x.CurrentStock)
                        * x.PurchasePrice);

                // -------------------------------------------------
                // SUBJECT
                // -------------------------------------------------

                string businessName =
                    string.IsNullOrWhiteSpace(user.BusinessName)
                        ? "BillMarg"
                        : user.BusinessName;

                string subject =
                    $"⚠ Low Stock Alert | {businessName} | {DateTime.Today:dd MMM yyyy}";

                // -------------------------------------------------
                // EMAIL BODY
                // -------------------------------------------------

                string htmlBody =
                    BuildLowStockAlertEmail(
                        user,
                        lowStockProducts,
                        totalProducts,
                        lowStockOnly,
                        outOfStockProducts,
                        totalStockValue,
                        requiredStockValue);

                // -------------------------------------------------
                // SEND
                // -------------------------------------------------

                bool result =
                    await _emailService.SendEmailAsync(
                        user.Email,
                        subject,
                        htmlBody);

                if (result)
                {
                    _logger.LogInformation(
                        "Low stock alert successfully sent to {Email}",
                        user.Email);
                }
                else
                {
                    _logger.LogWarning(
                        "Low stock alert could not be sent to {Email}",
                        user.Email);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending low stock alert to UserId {UserId}",
                    userId);

                return false;
            }
        }

        // =========================================================
        // BUILD DAILY STOCK INVENTORY EMAIL
        // =========================================================

        private string BuildDailyStockInventoryEmail(
            Users user,
            List<Product> products,
            int totalProducts,
            decimal totalQuantity,
            decimal totalPurchaseValue,
            decimal totalSellingValue,
            decimal potentialProfit,
            int lowStockCount,
            int outOfStockCount)
        {
            string businessName =
                string.IsNullOrWhiteSpace(user.BusinessName)
                    ? "Your Business"
                    : user.BusinessName;

            string fullName =
                string.IsNullOrWhiteSpace(user.FullName)
                    ? "Business Owner"
                    : user.FullName;

            string logoUrl =
                string.IsNullOrWhiteSpace(_emailSettings.LogoUrl)
                    ? "https://dev.billmarg.com/image/billmarg-logo.png"
                    : _emailSettings.LogoUrl;

            string websiteUrl =
                string.IsNullOrWhiteSpace(_emailSettings.WebsiteUrl)
                    ? "https://dev.billmarg.com"
                    : _emailSettings.WebsiteUrl;

            string rows = "";

            foreach (var product in products)
            {
                decimal stockValue =
                    product.CurrentStock *
                    product.PurchasePrice;

                decimal sellingValue =
                    product.CurrentStock *
                    product.SellingPrice;

                decimal profit =
                    sellingValue - stockValue;

                string status;
                string statusColor;

                if (product.CurrentStock <= 0)
                {
                    status = "OUT OF STOCK";
                    statusColor = "#dc3545";
                }
                else if (product.CurrentStock <= product.MinimumStock)
                {
                    status = "LOW STOCK";
                    statusColor = "#fd7e14";
                }
                else
                {
                    status = "IN STOCK";
                    statusColor = "#198754";
                }

                string productName =
                    System.Net.WebUtility.HtmlEncode(
                        product.ProductName ?? "-");

                string productCode =
                    System.Net.WebUtility.HtmlEncode(
                        product.ProductCode ?? "-");

                string unit =
                    System.Net.WebUtility.HtmlEncode(
                        product.Unit ?? "-");

                rows += $@"
<tr>

<td style='padding:12px;border-bottom:1px solid #eeeeee;'>
<strong>{productName}</strong>
</td>

<td style='padding:12px;border-bottom:1px solid #eeeeee;color:#666;'>
{productCode}
</td>

<td align='center'
    style='padding:12px;border-bottom:1px solid #eeeeee;'>
{unit}
</td>

<td align='right'
    style='padding:12px;border-bottom:1px solid #eeeeee;font-weight:bold;'>
{product.CurrentStock:N2}
</td>

<td align='right'
    style='padding:12px;border-bottom:1px solid #eeeeee;'>
{product.MinimumStock:N2}
</td>

<td align='right'
    style='padding:12px;border-bottom:1px solid #eeeeee;'>
₹ {product.PurchasePrice:N2}
</td>

<td align='right'
    style='padding:12px;border-bottom:1px solid #eeeeee;'>
₹ {stockValue:N2}
</td>

<td align='right'
    style='padding:12px;border-bottom:1px solid #eeeeee;'>
₹ {sellingValue:N2}
</td>

<td align='right'
    style='padding:12px;border-bottom:1px solid #eeeeee;color:#198754;'>
₹ {profit:N2}
</td>

<td align='center'
    style='padding:12px;border-bottom:1px solid #eeeeee;'>

<span style='background:{statusColor};
color:#ffffff;
padding:5px 9px;
border-radius:15px;
font-size:10px;
font-weight:bold;
white-space:nowrap;'>

{status}

</span>

</td>

</tr>";
            }

            return $@"
<!DOCTYPE html>

<html>

<head>

<meta charset='UTF-8'>

<meta name='viewport'
      content='width=device-width, initial-scale=1.0'>

<title>Daily Stock Inventory Report</title>

</head>

<body style='margin:0;
padding:0;
background:#f4f7fb;
font-family:Arial,Helvetica,sans-serif;'>

<table width='100%'
       cellpadding='0'
       cellspacing='0'
       style='background:#f4f7fb;padding:30px 10px;'>

<tr>

<td align='center'>

<table width='1100'
       cellpadding='0'
       cellspacing='0'
       style='max-width:1100px;
              width:100%;
              background:#ffffff;
              border-radius:16px;
              overflow:hidden;
              box-shadow:0 5px 25px rgba(0,0,0,.08);'>

<!-- HEADER -->

<tr>

<td style='background:#0d6efd;
padding:30px;
text-align:center;'>

<img src='{logoUrl}'
     alt='BillMarg'
     style='height:55px;
            max-width:180px;
            object-fit:contain;
            margin-bottom:12px;'>

<h1 style='margin:0;
color:#ffffff;
font-size:25px;'>

Daily Stock Inventory Report

</h1>

<p style='margin:8px 0 0;
color:#dbe9ff;
font-size:14px;'>

{DateTime.Today:dd MMMM yyyy}

</p>

</td>

</tr>

<!-- GREETING -->

<tr>

<td style='padding:30px 35px 15px;'>

<h2 style='margin:0 0 10px;
color:#222;
font-size:20px;'>

Hello {System.Net.WebUtility.HtmlEncode(fullName)},

</h2>

<p style='margin:0;
color:#666;
font-size:15px;
line-height:25px;'>

Here is your complete inventory position for today.

</p>

</td>

</tr>

<!-- BUSINESS -->

<tr>

<td style='padding:10px 35px 25px;'>

<div style='background:#f8f9fa;
border-radius:10px;
padding:15px;
text-align:center;'>

<strong style='font-size:17px;color:#333;'>

{System.Net.WebUtility.HtmlEncode(businessName)}

</strong>

<br>

<span style='font-size:12px;color:#777;'>

Daily Inventory Summary

</span>

</div>

</td>

</tr>

<!-- SUMMARY -->

<tr>

<td style='padding:0 30px 25px;'>

<table width='100%'
       cellpadding='5'
       cellspacing='0'>

<tr>

<td width='20%'>

<div style='background:#eef5ff;
padding:18px;
border-radius:12px;
text-align:center;'>

<div style='font-size:12px;color:#777;'>
Products
</div>

<div style='font-size:24px;
font-weight:bold;
color:#0d6efd;
margin-top:5px;'>

{totalProducts}

</div>

</div>

</td>

<td width='20%'>

<div style='background:#f8f9fa;
padding:18px;
border-radius:12px;
text-align:center;'>

<div style='font-size:12px;color:#777;'>
Total Quantity
</div>

<div style='font-size:21px;
font-weight:bold;
color:#333;
margin-top:5px;'>

{totalQuantity:N2}

</div>

</div>

</td>

<td width='20%'>

<div style='background:#eefaf3;
padding:18px;
border-radius:12px;
text-align:center;'>

<div style='font-size:12px;color:#777;'>
Purchase Value
</div>

<div style='font-size:19px;
font-weight:bold;
color:#198754;
margin-top:5px;'>

₹ {totalPurchaseValue:N2}

</div>

</div>

</td>

<td width='20%'>

<div style='background:#f5f0ff;
padding:18px;
border-radius:12px;
text-align:center;'>

<div style='font-size:12px;color:#777;'>
Selling Value
</div>

<div style='font-size:19px;
font-weight:bold;
color:#6f42c1;
margin-top:5px;'>

₹ {totalSellingValue:N2}

</div>

</div>

</td>

<td width='20%'>

<div style='background:#fff8e8;
padding:18px;
border-radius:12px;
text-align:center;'>

<div style='font-size:12px;color:#777;'>
Potential Profit
</div>

<div style='font-size:19px;
font-weight:bold;
color:#fd7e14;
margin-top:5px;'>

₹ {potentialProfit:N2}

</div>

</div>

</td>

</tr>

</table>

</td>

</tr>

<!-- STOCK STATUS -->

<tr>

<td style='padding:0 35px 25px;'>

<div style='background:#fff8e8;
border-left:5px solid #fd7e14;
padding:15px;
border-radius:8px;
font-size:14px;
color:#664d03;'>

<strong>Inventory Status:</strong>

{lowStockCount} product(s) are at or below minimum stock,
and {outOfStockCount} product(s) are out of stock.

</div>

</td>

</tr>

<!-- PRODUCT TABLE -->

<tr>

<td style='padding:0 25px 30px;'>

<h2 style='font-size:18px;
color:#333;
margin:0 0 15px;'>

Complete Inventory

</h2>

<table width='100%'
       cellpadding='0'
       cellspacing='0'
       style='border-collapse:collapse;
              border:1px solid #e5e7eb;
              font-size:11px;'>

<thead>

<tr style='background:#f8f9fa;'>

<th align='left' style='padding:12px;'>
Product
</th>

<th align='left' style='padding:12px;'>
Code
</th>

<th align='center' style='padding:12px;'>
Unit
</th>

<th align='right' style='padding:12px;'>
Current
</th>

<th align='right' style='padding:12px;'>
Minimum
</th>

<th align='right' style='padding:12px;'>
Purchase Price
</th>

<th align='right' style='padding:12px;'>
Stock Value
</th>

<th align='right' style='padding:12px;'>
Selling Value
</th>

<th align='right' style='padding:12px;'>
Potential Profit
</th>

<th align='center' style='padding:12px;'>
Status
</th>

</tr>

</thead>

<tbody>

{rows}

</tbody>

</table>

</td>

</tr>

<!-- BUTTON -->

<tr>

<td align='center'
style='padding:0 35px 35px;'>

<a href='{websiteUrl}'
style='display:inline-block;
background:#0d6efd;
color:#ffffff;
text-decoration:none;
padding:14px 30px;
border-radius:8px;
font-size:14px;
font-weight:bold;'>

Open BillMarg Inventory

</a>

</td>

</tr>

<!-- FOOTER -->

<tr>

<td style='background:#f8f9fa;
padding:25px;
text-align:center;'>

<div style='font-size:16px;
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

This is an automated BillMarg daily inventory report.

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

        // =========================================================
        // BUILD LOW STOCK ALERT EMAIL
        // =========================================================

        private string BuildLowStockAlertEmail(
            Users user,
            List<Product> products,
            int totalProducts,
            int lowStockOnly,
            int outOfStockProducts,
            decimal totalStockValue,
            decimal requiredStockValue)
        {
            string businessName =
                string.IsNullOrWhiteSpace(user.BusinessName)
                    ? "Your Business"
                    : user.BusinessName;

            string fullName =
                string.IsNullOrWhiteSpace(user.FullName)
                    ? "Business Owner"
                    : user.FullName;

            string logoUrl =
                string.IsNullOrWhiteSpace(_emailSettings.LogoUrl)
                    ? "https://dev.billmarg.com/image/billmarg-logo.png"
                    : _emailSettings.LogoUrl;

            string websiteUrl =
                string.IsNullOrWhiteSpace(_emailSettings.WebsiteUrl)
                    ? "https://dev.billmarg.com"
                    : _emailSettings.WebsiteUrl;

            string rows = "";

            foreach (var product in products)
            {
                string status;
                string statusColor;

                if (product.CurrentStock <= 0)
                {
                    status = "OUT OF STOCK";
                    statusColor = "#dc3545";
                }
                else
                {
                    status = "LOW STOCK";
                    statusColor = "#fd7e14";
                }

                decimal shortage =
                    Math.Max(
                        0,
                        product.MinimumStock -
                        product.CurrentStock);

                decimal stockValue =
                    product.CurrentStock *
                    product.PurchasePrice;

                decimal requiredValue =
                    shortage *
                    product.PurchasePrice;

                string productName =
                    System.Net.WebUtility.HtmlEncode(
                        product.ProductName ?? "-");

                string productCode =
                    System.Net.WebUtility.HtmlEncode(
                        product.ProductCode ?? "-");

                string unit =
                    System.Net.WebUtility.HtmlEncode(
                        product.Unit ?? "-");

                rows += $@"
<tr>

<td style='padding:12px;border-bottom:1px solid #eeeeee;'>
<strong>{productName}</strong>
</td>

<td style='padding:12px;border-bottom:1px solid #eeeeee;color:#666;'>
{productCode}
</td>

<td align='center'
style='padding:12px;border-bottom:1px solid #eeeeee;'>
{unit}
</td>

<td align='right'
style='padding:12px;border-bottom:1px solid #eeeeee;font-weight:bold;'>
{product.CurrentStock:N2}
</td>

<td align='right'
style='padding:12px;border-bottom:1px solid #eeeeee;'>
{product.MinimumStock:N2}
</td>

<td align='right'
style='padding:12px;border-bottom:1px solid #eeeeee;'>
{shortage:N2}
</td>

<td align='right'
style='padding:12px;border-bottom:1px solid #eeeeee;'>
₹ {product.PurchasePrice:N2}
</td>

<td align='right'
style='padding:12px;border-bottom:1px solid #eeeeee;'>
₹ {stockValue:N2}
</td>

<td align='right'
style='padding:12px;border-bottom:1px solid #eeeeee;'>
₹ {requiredValue:N2}
</td>

<td align='center'
style='padding:12px;border-bottom:1px solid #eeeeee;'>

<span style='background:{statusColor};
color:#ffffff;
padding:5px 9px;
border-radius:15px;
font-size:10px;
font-weight:bold;
white-space:nowrap;'>

{status}

</span>

</td>

</tr>";
            }

            return $@"
<!DOCTYPE html>

<html>

<head>

<meta charset='UTF-8'>

<meta name='viewport'
content='width=device-width, initial-scale=1.0'>

<title>Low Stock Alert</title>

</head>

<body style='margin:0;
padding:0;
background:#f4f7fb;
font-family:Arial,Helvetica,sans-serif;'>

<table width='100%'
cellpadding='0'
cellspacing='0'
style='background:#f4f7fb;padding:30px 10px;'>

<tr>

<td align='center'>

<table width='1100'
cellpadding='0'
cellspacing='0'
style='max-width:1100px;
width:100%;
background:#ffffff;
border-radius:16px;
overflow:hidden;'>

<!-- HEADER -->

<tr>

<td style='background:#dc3545;
padding:30px;
text-align:center;'>

<img src='{logoUrl}'
alt='BillMarg'
style='height:55px;
max-width:180px;
object-fit:contain;
margin-bottom:12px;'>

<h1 style='margin:0;
color:#ffffff;
font-size:25px;'>

⚠ Low Stock Alert

</h1>

<p style='margin:8px 0 0;
color:#ffe5e5;
font-size:14px;'>

{DateTime.Today:dd MMMM yyyy}

</p>

</td>

</tr>

<!-- GREETING -->

<tr>

<td style='padding:30px 35px 15px;'>

<h2 style='margin:0 0 10px;
color:#222;
font-size:20px;'>

Hello {System.Net.WebUtility.HtmlEncode(fullName)},

</h2>

<p style='margin:0;
color:#666;
font-size:15px;
line-height:25px;'>

The following products have reached or fallen below
their minimum stock level.

</p>

</td>

</tr>

<!-- BUSINESS -->

<tr>

<td style='padding:10px 35px 25px;'>

<div style='background:#f8f9fa;
border-radius:10px;
padding:15px;
text-align:center;'>

<strong style='font-size:17px;color:#333;'>

{System.Net.WebUtility.HtmlEncode(businessName)}

</strong>

<br>

<span style='font-size:12px;color:#777;'>

Inventory Alert Summary

</span>

</div>

</td>

</tr>

<!-- SUMMARY -->

<tr>

<td style='padding:0 30px 25px;'>

<table width='100%'
cellpadding='5'
cellspacing='0'>

<tr>

<td width='25%'>

<div style='background:#fff0f0;
padding:18px;
border-radius:12px;
text-align:center;'>

<div style='font-size:12px;color:#777;'>
Affected Products
</div>

<div style='font-size:24px;
font-weight:bold;
color:#dc3545;
margin-top:5px;'>

{totalProducts}

</div>

</div>

</td>

<td width='25%'>

<div style='background:#fff8e8;
padding:18px;
border-radius:12px;
text-align:center;'>

<div style='font-size:12px;color:#777;'>
Low Stock
</div>

<div style='font-size:24px;
font-weight:bold;
color:#fd7e14;
margin-top:5px;'>

{lowStockOnly}

</div>

</div>

</td>

<td width='25%'>

<div style='background:#fff0f0;
padding:18px;
border-radius:12px;
text-align:center;'>

<div style='font-size:12px;color:#777;'>
Out of Stock
</div>

<div style='font-size:24px;
font-weight:bold;
color:#dc3545;
margin-top:5px;'>

{outOfStockProducts}

</div>

</div>

</td>

<td width='25%'>

<div style='background:#eef5ff;
padding:18px;
border-radius:12px;
text-align:center;'>

<div style='font-size:12px;color:#777;'>
Required Stock Value
</div>

<div style='font-size:19px;
font-weight:bold;
color:#0d6efd;
margin-top:5px;'>

₹ {requiredStockValue:N2}

</div>

</div>

</td>

</tr>

</table>

</td>

</tr>

<!-- CURRENT VALUE -->

<tr>

<td style='padding:0 35px 25px;'>

<div style='background:#f8f9fa;
border-left:5px solid #0d6efd;
padding:15px;
border-radius:8px;
font-size:14px;
color:#555;'>

<strong>Current Stock Value:</strong>

₹ {totalStockValue:N2}

</div>

</td>

</tr>

<!-- PRODUCT TABLE -->

<tr>

<td style='padding:0 25px 30px;'>

<h2 style='font-size:18px;
color:#333;
margin:0 0 15px;'>

Products Requiring Attention

</h2>

<table width='100%'
cellpadding='0'
cellspacing='0'
style='border-collapse:collapse;
border:1px solid #e5e7eb;
font-size:11px;'>

<thead>

<tr style='background:#f8f9fa;'>

<th align='left' style='padding:12px;'>
Product
</th>

<th align='left' style='padding:12px;'>
Code
</th>

<th align='center' style='padding:12px;'>
Unit
</th>

<th align='right' style='padding:12px;'>
Current
</th>

<th align='right' style='padding:12px;'>
Minimum
</th>

<th align='right' style='padding:12px;'>
Shortage
</th>

<th align='right' style='padding:12px;'>
Purchase Price
</th>

<th align='right' style='padding:12px;'>
Current Value
</th>

<th align='right' style='padding:12px;'>
Required Value
</th>

<th align='center' style='padding:12px;'>
Status
</th>

</tr>

</thead>

<tbody>

{rows}

</tbody>

</table>

</td>

</tr>

<!-- BUTTON -->

<tr>

<td align='center'
style='padding:0 35px 35px;'>

<a href='{websiteUrl}'
style='display:inline-block;
background:#0d6efd;
color:#ffffff;
text-decoration:none;
padding:14px 30px;
border-radius:8px;
font-size:14px;
font-weight:bold;'>

Open BillMarg Inventory

</a>

</td>

</tr>

<!-- FOOTER -->

<tr>

<td style='background:#f8f9fa;
padding:25px;
text-align:center;'>

<div style='font-size:16px;
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

This is an automated BillMarg low stock alert.

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