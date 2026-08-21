using BillMarg.Notification.API.Data;
using BillMarg.Notification.API.Interface;
using BillMarg.Notification.API.Models;
using BillSathi.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BillMarg.Notification.API.Services
{
    public class PurchaseStockAlertService : IPurchaseStockAlertService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<PurchaseStockAlertService> _logger;

        public PurchaseStockAlertService(
            AppDbContext context,
            IEmailService emailService,
            ILogger<PurchaseStockAlertService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task SendPurchaseStockAlertsAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // Get users who enabled Purchase Stock Alert
                var notificationSettings = await _context.UserNotificationSettings
                    .AsNoTracking()
                    .Where(x =>
                        x.PurchaseStockAlert &&
                        x.EmailEnabled)
                    .ToListAsync(cancellationToken);

                if (!notificationSettings.Any())
                {
                    _logger.LogInformation(
                        "No users have PurchaseStockAlert enabled.");

                    return;
                }

                foreach (var setting in notificationSettings)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Get user
                        var user = await _context.Users
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                x => x.UserId == setting.UserId,
                                cancellationToken);

                        if (user == null)
                        {
                            _logger.LogWarning(
                                "User not found for UserId {UserId}",
                                setting.UserId);

                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(user.Email))
                        {
                            _logger.LogWarning(
                                "Email not available for UserId {UserId}",
                                setting.UserId);

                            continue;
                        }

                        // Today's purchases where stock was added
                        var purchases = await _context.Purchases
                            .AsNoTracking()
                            .Where(x =>
                                x.UserId == setting.UserId &&
                                x.PurchaseDate >= today &&
                                x.PurchaseDate < tomorrow &&
                                x.StockAdded)
                            .OrderByDescending(x => x.PurchaseDate)
                            .ToListAsync(cancellationToken);

                        // Nothing purchased today
                        if (!purchases.Any())
                        {
                            continue;
                        }

                        var model = new PurchaseStockAlertModel
                        {
                            UserId = setting.UserId,

                            UserName = GetUserName(user),

                            Email = user.Email,

                            ReportDate = today,

                            TotalPurchases = purchases.Count,

                            TotalPurchaseAmount = purchases.Sum(x => x.GrandTotal),

                            Purchases = purchases.Select(x =>
                                new PurchaseStockAlertItem
                                {
                                    PurchaseNumber =
                                        x.PurchaseNumber,

                                    SupplierName =
                                        x.SupplierName ?? "-",

                                    SupplierInvoiceNumber =
                                        x.SupplierInvoiceNumber ?? "-",

                                    PurchaseDate =
                                        x.PurchaseDate,

                                    GrandTotal =
                                        x.GrandTotal,

                                    PurchaseStatus =
                                        x.PurchaseStatus,

                                    StockAdded =
                                        x.StockAdded
                                })
                                .ToList()
                        };

                        var subject =
                            $"BillMarg Purchase & Stock Alert - {today:dd MMM yyyy}";

                        var body =
                            BuildPurchaseStockEmail(model);

                        await _emailService.SendEmailAsync(
                            user.Email,
                            subject,
                            body);

                        _logger.LogInformation(
                            "Purchase stock alert sent to {Email} for UserId {UserId}",
                            user.Email,
                            setting.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error sending purchase stock alert for UserId {UserId}",
                            setting.UserId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "Purchase stock alert service was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error running purchase stock alert service.");
            }
        }

        private static string GetUserName(Users user)
        {
            if (!string.IsNullOrWhiteSpace(user.FullName))
                return user.FullName;

            return "Business Owner";
        }

        private static string BuildPurchaseStockEmail(
            PurchaseStockAlertModel model)
        {
            var rows = new StringBuilder();

            foreach (var purchase in model.Purchases)
            {
                rows.Append($@"
                    <tr>
                        <td>{System.Net.WebUtility.HtmlEncode(purchase.PurchaseNumber)}</td>

                        <td>
                            {System.Net.WebUtility.HtmlEncode(purchase.SupplierName)}
                        </td>

                        <td>
                            {System.Net.WebUtility.HtmlEncode(
                                purchase.SupplierInvoiceNumber)}
                        </td>

                        <td>
                            {purchase.PurchaseDate:dd MMM yyyy}
                        </td>

                        <td style='text-align:right;'>
                            ₹{purchase.GrandTotal:N2}
                        </td>

                        <td>
                            <span style='
                                background:#e8f5e9;
                                color:#2e7d32;
                                padding:5px 10px;
                                border-radius:15px;
                                font-size:12px;
                                font-weight:600;'>
                                Stock Added
                            </span>
                        </td>
                    </tr>");
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>

    <meta name='viewport'
          content='width=device-width, initial-scale=1.0'>
</head>

<body style='
    margin:0;
    padding:0;
    background:#f4f6f8;
    font-family:Arial,Helvetica,sans-serif;'>

<table width='100%'
       cellpadding='0'
       cellspacing='0'
       style='background:#f4f6f8;padding:30px 10px;'>

<tr>
<td align='center'>

<table width='700'
       cellpadding='0'
       cellspacing='0'
       style='
           max-width:700px;
           width:100%;
           background:#ffffff;
           border-radius:12px;
           overflow:hidden;
           box-shadow:0 3px 15px rgba(0,0,0,0.08);'>

<!-- Header -->

<tr>
<td style='
    background:#17365d;
    padding:28px 30px;
    color:#ffffff;'>

    <div style='
        font-size:26px;
        font-weight:bold;'>
        BillMarg
    </div>

    <div style='
        margin-top:6px;
        font-size:14px;
        opacity:.9;'>
        Purchase & Stock Alert
    </div>

</td>
</tr>

<!-- Content -->

<tr>
<td style='padding:30px;'>

    <h2 style='
        margin:0 0 10px;
        color:#222;'>
        Hello {System.Net.WebUtility.HtmlEncode(model.UserName)},
    </h2>

    <p style='
        color:#555;
        font-size:15px;
        line-height:1.6;'>
        Here is your purchase and stock update for
        <strong>{model.ReportDate:dd MMMM yyyy}</strong>.
    </p>

    <!-- Summary -->

    <table width='100%'
           cellpadding='0'
           cellspacing='0'
           style='margin:25px 0;'>

        <tr>

            <td width='50%'
                style='
                    background:#f1f7ff;
                    padding:20px;
                    border-radius:8px;'>

                <div style='
                    color:#666;
                    font-size:13px;'>
                    Total Purchases
                </div>

                <div style='
                    color:#17365d;
                    font-size:26px;
                    font-weight:bold;
                    margin-top:5px;'>
                    {model.TotalPurchases}
                </div>

            </td>

            <td width='20'></td>

            <td width='50%'
                style='
                    background:#f1fbf4;
                    padding:20px;
                    border-radius:8px;'>

                <div style='
                    color:#666;
                    font-size:13px;'>
                    Total Purchase Value
                </div>

                <div style='
                    color:#18864b;
                    font-size:22px;
                    font-weight:bold;
                    margin-top:5px;'>
                    ₹{model.TotalPurchaseAmount:N2}
                </div>

            </td>

        </tr>

    </table>

    <h3 style='
        color:#222;
        margin:25px 0 15px;'>
        Purchase Details
    </h3>

    <div style='overflow-x:auto;'>

    <table width='100%'
           cellpadding='10'
           cellspacing='0'
           style='
               border-collapse:collapse;
               font-size:13px;'>

        <thead>

            <tr style='
                background:#17365d;
                color:#ffffff;'>

                <th align='left'>Purchase</th>

                <th align='left'>Supplier</th>

                <th align='left'>Supplier Invoice</th>

                <th align='left'>Date</th>

                <th align='right'>Amount</th>

                <th align='left'>Stock</th>

            </tr>

        </thead>

        <tbody>

            {rows}

        </tbody>

    </table>

    </div>

    <div style='
        margin-top:25px;
        padding:15px;
        background:#fff8e1;
        border-left:4px solid #ffb300;
        color:#555;
        font-size:13px;
        line-height:1.6;'>

        <strong>Stock Update:</strong>
        The above purchases have been added to your inventory.

    </div>

    <p style='
        margin-top:25px;
        color:#666;
        font-size:13px;
        line-height:1.6;'>

        You can log in to BillMarg to view complete purchase,
        product and stock details.

    </p>

</td>
</tr>

<!-- Footer -->

<tr>
<td style='
    background:#f8f9fa;
    padding:20px 30px;
    text-align:center;
    color:#888;
    font-size:12px;'>

    <strong style='color:#17365d;'>
        BillMarg
    </strong>

    <br />

    Empowering Small Businesses

    <br /><br />

    This is an automated notification.
    Please do not reply to this email.

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